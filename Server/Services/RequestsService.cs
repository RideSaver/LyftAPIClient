using InternalAPI;
using Grpc.Core;
using LyftApiClient.Server.Models;
using LyftApiClient.Server.Extensions;
using LyftClient.Interface;
using LyftAPI.Client.Model;
using LyftClient.Extensions;
using Microsoft.Extensions.Caching.Distributed;

using UserAPI = LyftAPI.Client.Api.UserApi;
using APIConfig = LyftAPI.Client.Client.Configuration;
using CreateRideRequest = LyftAPI.Client.Model.CreateRideRequest;
using Microsoft.Extensions.Logging;


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
           
            _logger.LogInformation($"[LyftClient::RequestsService::PostRideRequest] Sending (CreateRideRequest) to the MockAPI... \n{rideRequest}");

            var rideResponseInstance = await _apiClient.RidesPostAsync(createRideRequest: rideRequest);

            _logger.LogInformation($"[LyftClient::RequestsService::PostRideRequest] Received (Ride) from the MockAPI... \n{rideResponseInstance}");

            if (rideResponseInstance is null) { _logger.LogError("[LyftClient::RequestsService::PostRideRequest] Ride Instance is null!"); }

            _apiClient.Configuration = new APIConfig { AccessToken = await _accessToken.GetAccessTokenAsync(SessionToken!, serviceID) };
 
            var rideDetailsResponseInstance = await _apiClient.RidesIdGetAsync(rideResponseInstance!.RideId.ToString());

            _logger.LogInformation($"[LyftClient::RequestsService::PostRideRequest] Received (RideDetails) from the MockAPI... \n{rideDetailsResponseInstance}");

            if (rideDetailsResponseInstance is null) { _logger.LogError("[LyftClient::RequestsService::PostRideRequest] Ride Details Instance is null!"); }

            var requestCache = new EstimateCache
            {
                GetEstimatesRequest = cacheEstimate.GetEstimatesRequest,
                Cost = cacheEstimate.Cost,
                ProductId = Guid.Parse(serviceID),
                RequestId = Guid.Parse(rideDetailsResponseInstance!.RideId),
            };
            
            await _cache.SetAsync(rideDetailsResponseInstance.RideId, requestCache);

            var rideModel = new RideModel
            {
                RideId = rideResponseInstance!.RideId.ToString(),
                RiderOnBoard = false,
                EstimatedTimeOfArrival = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(rideDetailsResponseInstance.Pickup.Time.DateTime),
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
                    DriverPronounciation = rideDetailsResponseInstance.Driver.FirstName,
                },
                DriverLocation = new LocationModel
                {
                    Latitude = rideDetailsResponseInstance.Location.Lat,
                    Longitude = rideDetailsResponseInstance.Location.Lng,
                },
            };

            _logger.LogInformation($"[LyftClient::RequestsService::PostRideRequest] Returning (RideModel) to caller... \n{rideModel}");
            return rideModel;
        }

        public override async Task<RideModel> GetRideRequest(GetRideRequestModel request, ServerCallContext context)
        {
            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"];

            _logger.LogInformation($"[LyftClient::RequestsService::GetRideRequest] HTTP Context session token: {SessionToken}");

            var CacheEstimate = await _cache.GetAsync<EstimateCache>(request.RideId);

            _apiClient.Configuration = new APIConfig { AccessToken = await _accessToken.GetAccessTokenAsync(SessionToken!, CacheEstimate.ProductId.ToString()), };

            var ride = await _apiClient.RidesIdGetAsync(request.RideId);

            return new RideModel
            {
                RideId = request.RideId,
                EstimatedTimeOfArrival = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(ride.Pickup.Time.DateTime),
                RiderOnBoard = ride.Status == RideStatusEnum.PickedUp,
                Price = new CurrencyModel
                {
                    Price = (double)ride.Price.Amount / 100,
                    Currency = ride.Price.Currency,
                },
                Driver = new DriverModel
                {
                    DisplayName = ride.Driver.FirstName,
                    LicensePlate = ride.Vehicle.LicensePlate,
                    CarPicture = ride.Vehicle.ImageUrl,
                    CarDescription = ride.Vehicle.Model,
                    DriverPronounciation = ride.Driver.FirstName
                },
                RideStage = StagefromStatus(ride.Status),
                DriverLocation = new LocationModel
                {
                    Latitude = ride.Location.Lat,
                    Longitude = ride.Location.Lng,
                },
            };
        }

        public override async Task<CurrencyModel> DeleteRideRequest(DeleteRideRequestModel request, ServerCallContext context)
        {
            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"];

            _logger.LogInformation($"[LyftClient::RequestsService::GetRideRequest] HTTP Context User: {SessionToken}");

            var CacheEstimate = await _cache.GetAsync<EstimateCache>(request.RideId);

            _apiClient.Configuration = new APIConfig { AccessToken = await _accessToken.GetAccessTokenAsync(SessionToken!, CacheEstimate.ProductId.ToString()) };

            await _apiClient.RidesIdCancelPostAsync(CacheEstimate.CancelationToken.ToString());
            return CacheEstimate.CancelationCost;
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
