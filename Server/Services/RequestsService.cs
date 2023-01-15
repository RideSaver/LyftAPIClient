using Grpc.Core;
using InternalAPI;
using LyftAPI.Client.Model;
using LyftClient.Interface;
using LyftClient.Extensions;
using LyftApiClient.Server.Models;
using LyftApiClient.Server.Extensions;
using Microsoft.Extensions.Caching.Distributed;

using UserAPI = LyftAPI.Client.Api.UserApi;
using APIConfig = LyftAPI.Client.Client.Configuration;
using CreateRideRequest = LyftAPI.Client.Model.CreateRideRequest;

namespace LyftClient.Services
{
    public class RequestsService : Requests.RequestsBase
    {
        private readonly ILogger<RequestsService> _logger;
        private readonly IAccessTokenService _accessToken;
        private readonly IDistributedCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly UserAPI _apiClient;

        public RequestsService(ILogger<RequestsService> logger, IDistributedCache cache, IAccessTokenService accessToken, IHttpContextAccessor httpContextAccessor)
        {
            _accessToken = accessToken;
            _logger = logger;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;

            _apiClient = new UserAPI();
        }
        public override async Task<RideModel> PostRideRequest(PostRideRequestModel request, ServerCallContext context)
        {
            _logger.LogInformation("[LyftClient::RequestsService::PostRideRequest] Method invoked at {DT}", DateTime.UtcNow.ToLongTimeString());

            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"];
            var estimateId = request.EstimateId.ToString();
            var cacheEstimate = await _cache.GetAsync<EstimateCache>(estimateId);
            var serviceID = cacheEstimate!.ProductId.ToString();

            if (cacheEstimate is null) { throw new ArgumentNullException("[LyftClient::RequestsService::PostRideRequest] CacheEstimate instance is null!"); }

            _apiClient.Configuration = new APIConfig { AccessToken = await _accessToken.GetAccessTokenAsync(SessionToken!, serviceID) };

            var rideRequest = new CreateRideRequest(costToken: "UserCostTokenPerRide")
            {
                RideType = RideTypeFromServiceID(serviceID),
                Origin = ConvertLocationModelToLocation(cacheEstimate!.GetEstimatesRequest!.StartPoint),
                Destination = ConvertLocationModelToLocation(cacheEstimate!.GetEstimatesRequest!.EndPoint),
                Passenger = new PassengerDetail(firstName: "PlaceHolder", imageUrl: "Exempt", rating: "Exempt")
            };
           
            var rideResponseInstance = await _apiClient.RidesPostAsync(createRideRequest: rideRequest);

            if (rideResponseInstance is null) { throw new ArgumentNullException("[LyftClient::RequestsService::PostRideRequest] Ride Instance is null!"); }

            _apiClient.Configuration = new APIConfig { AccessToken = await _accessToken.GetAccessTokenAsync(SessionToken!, serviceID)  };

            var rideDetailsResponseInstance = await _apiClient.RidesIdGetAsync(rideResponseInstance!.RideId.ToString());

            if (rideDetailsResponseInstance is null) { throw new ArgumentNullException("[LyftClient::RequestsService::PostRideRequest] Ride Details Instance is null!"); }

            var requestCache = new EstimateCache
            {
                GetEstimatesRequest = cacheEstimate.GetEstimatesRequest,
                Cost = cacheEstimate.Cost,
                ProductId = Guid.Parse(serviceID),
                RequestId = Guid.Parse(rideResponseInstance!.RideId.ToString()),
                CancelationCost = new CurrencyModel
                {
                    Price = rideDetailsResponseInstance.CancellationPrice.Amount,
                    Currency = rideDetailsResponseInstance.CancellationPrice.Currency
                },
            };

            await _cache.SetAsync(rideResponseInstance.RideId.ToString(), requestCache);

            var rideModel = new RideModel
            {
                RideId = rideResponseInstance!.RideId.ToString(),
                RiderOnBoard = false,
                RideStage = Stage.Pending,
                EstimatedTimeOfArrival = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime((rideDetailsResponseInstance!.Pickup.Time.DateTime).ToUniversalTime()),
                Price = new CurrencyModel
                {
                    Price = (double)rideDetailsResponseInstance.Price.Amount / 100,
                    Currency = rideDetailsResponseInstance.Price.Currency,
                },
                Driver = new DriverModel
                {
                    DisplayName = rideDetailsResponseInstance.Driver.FirstName,
                    LicensePlate = rideDetailsResponseInstance.Vehicle.LicensePlate,
                    CarPicture = "Exempt",
                    CarDescription = rideDetailsResponseInstance.Vehicle.Model,
                    DriverPronounciation = rideDetailsResponseInstance.Driver.FirstName,
                },
                DriverLocation = new LocationModel
                {
                    Latitude = rideDetailsResponseInstance.Location.Lat,
                    Longitude = rideDetailsResponseInstance.Location.Lng,
                },
            };

            return rideModel;
        }

        public override async Task<RideModel> GetRideRequest(GetRideRequestModel request, ServerCallContext context)
        {
            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"];
            var cacheEstimate = await _cache.GetAsync<EstimateCache>(request.RideId.ToString());
            var serviceID = cacheEstimate!.ProductId.ToString();

            _apiClient.Configuration = new APIConfig { AccessToken = await _accessToken.GetAccessTokenAsync(SessionToken!, serviceID) };
            var rideDetailsResponseInstance = await _apiClient.RidesIdGetAsync(request.RideId.ToString());

            _logger.LogInformation($"[LyftClient::RequestsService::GetRideRequest] Received (RideDetails) from the MockAPI... \n{rideDetailsResponseInstance}");

            if (rideDetailsResponseInstance is null) { throw new ArgumentNullException("[LyftClient::RequestsService::GetRideRequest] Ride Details instance is null!"); }

            return new RideModel
            {
                RideId = request.RideId.ToString(),
                RiderOnBoard = false,
                EstimatedTimeOfArrival = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime((rideDetailsResponseInstance!.Pickup.Time.DateTime).ToUniversalTime()),
                RideStage = StagefromStatus(rideDetailsResponseInstance.Status),
                Price = new CurrencyModel
                {
                    Price = (double)rideDetailsResponseInstance.Price.Amount / 100,
                    Currency = rideDetailsResponseInstance.Price.Currency,
                },
                Driver = new DriverModel
                {
                    DisplayName = rideDetailsResponseInstance.Driver.FirstName,
                    LicensePlate = rideDetailsResponseInstance.Vehicle.LicensePlate,
                    CarPicture = rideDetailsResponseInstance.Vehicle.ImageUrl,
                    CarDescription = rideDetailsResponseInstance.Vehicle.Model,
                    DriverPronounciation = rideDetailsResponseInstance.Driver.FirstName
                },
                DriverLocation = new LocationModel
                {
                    Latitude = rideDetailsResponseInstance.Location.Lat,
                    Longitude = rideDetailsResponseInstance.Location.Lng
                },
            };
        }

        public override async Task<CurrencyModel> DeleteRideRequest(DeleteRideRequestModel request, ServerCallContext context)
        {
            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"];
            var cacheEstimate = await _cache.GetAsync<EstimateCache>(request.RideId.ToString());
            var serviceID = cacheEstimate!.ProductId.ToString();

            if (cacheEstimate is null) { throw new ArgumentNullException("[LyftClient::RequestsService::DeleteRideRequest] CacheEstimate instance is null!"); }

            _apiClient.Configuration = new APIConfig { AccessToken = await _accessToken.GetAccessTokenAsync(SessionToken!, serviceID) };
            await _apiClient.RidesIdCancelPostAsync(cacheEstimate.CancelationToken.ToString());
            return cacheEstimate.CancelationCost!;
        }

        private static Stage StagefromStatus(RideStatusEnum? status)
        {
            switch (status)
            {
                case RideStatusEnum.Pending: return Stage.Pending;
                case RideStatusEnum.Arrived: return Stage.Accepted;
                case RideStatusEnum.PickedUp: return Stage.Accepted;
                case RideStatusEnum.Accepted: return Stage.Accepted;
                case RideStatusEnum.Canceled: return Stage.Cancelled;
                case RideStatusEnum.DroppedOff: return Stage.Completed;
                default: return Stage.Unknown;
            }
        }

        private static RideTypeEnum RideTypeFromServiceID(string serviceID)
        {
            ServiceLinker.ServiceIDs.TryGetValue(serviceID.ToUpper(), out string? serviceName);
            switch (serviceName)
            {
                case "lyft": return RideTypeEnum.Lyft;
                case "lyft_shared": return RideTypeEnum.LyftLine;
                case "lyft_lux": return RideTypeEnum.LyftPlus;
                case "lyft_suv": return RideTypeEnum.LyftSuv;
                default: return RideTypeEnum.Lyft;
            }
        }

        public Location ConvertLocationModelToLocation(LocationModel locationModel) // Converts LocationModel to Location
        {
            var location = new Location()
            {
                Lat = locationModel.Latitude,
                Lng = locationModel.Longitude,
                Address = "N/A"
            };

            return location;
        }
    }
}
