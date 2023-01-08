using Grpc.Core;
using InternalAPI;
using Microsoft.Extensions.Caching.Distributed;
using LyftApiClient.Server.Models;
using LyftApiClient.Server.Extensions;
using LyftClient.Interface;

using UserAPI = LyftAPI.Client.Api.UserApi;


namespace LyftClient.Services
{
    public class RequestsService : Requests.RequestsBase // TBA
    {
        private readonly ILogger<RequestsService> _logger;
        private readonly IAccessTokenService _accessToken;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IDistributedCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly HttpClient _httpClient;
        private readonly UserAPI _apiClient;

        public RequestsService(ILogger<RequestsService> logger, IDistributedCache cache, IHttpClientFactory clientFactory, IAccessTokenService accessToken, IHttpContextAccessor httpContextAccessor)
        {
            _clientFactory = clientFactory;
            _httpClient = _clientFactory.CreateClient();
            _accessToken = accessToken;
            _logger = logger;
            _cache = cache;
            _apiClient = new UserAPI();
            _httpContextAccessor = httpContextAccessor;
        }
        public override async Task<RideModel> PostRideRequest(PostRideRequestModel request, ServerCallContext context)
        {
            _logger.LogInformation("[LyftClient::RequestsService::PostRideRequest] Method invoked at {DT}", DateTime.UtcNow.ToLongTimeString());

            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"];

            _logger.LogInformation($"[LyftClient::RequestsService::PostRideRequest] HTTP Context session token: {SessionToken}");

            var CacheEstimate = await _cache.GetAsync<EstimateCache>(request.EstimateId);

            LyftAPI.Client.Model.CreateRideRequest _request = new LyftAPI.Client.Model.CreateRideRequest();

            _apiClient.Configuration = new LyftAPI.Client.Client.Configuration
            {
                AccessToken = await _accessToken.GetAccessTokenAsync(SessionToken!, CacheEstimate.ProductId.ToString())
            };

            var ride = await _apiClient.RidesPostAsync(_request);

            _apiClient.Configuration = new LyftAPI.Client.Client.Configuration
            {
                AccessToken = await _accessToken.GetAccessTokenAsync(SessionToken!, CacheEstimate.ProductId.ToString())
            };

            var RideDetails = await _apiClient.RidesIdGetAsync(ride.RideId);

            CacheEstimate.CancelationCost = new CurrencyModel()
            {
                Price = RideDetails.CancellationPrice.Amount,
                Currency = RideDetails.CancellationPrice.Currency
            };

            CacheEstimate.CancelationToken = new Guid(RideDetails.CancellationPrice.Token);

            var rideModel = new RideModel()
            {
                RideId = request.EstimateId,
                EstimatedTimeOfArrival = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(RideDetails.Pickup.Time.DateTime),
                RiderOnBoard = false,
                Price = new CurrencyModel
                {
                    Price = (double)RideDetails.Price.Amount / 100,
                    Currency = RideDetails.Price.Currency,
                },
                Driver = new DriverModel
                {
                    DisplayName = RideDetails.Driver.FirstName,
                    LicensePlate = RideDetails.Vehicle.LicensePlate,
                    CarPicture = RideDetails.Vehicle.ImageUrl,
                    CarDescription = RideDetails.Vehicle.Model,
                    DriverPronounciation = RideDetails.Driver.FirstName,
                },
                RideStage = StagefromStatus(RideDetails.Status),
                DriverLocation = new LocationModel
                {
                    Latitude = RideDetails.Location.Lat,
                    Longitude = RideDetails.Location.Lng,
                },
            };
            return rideModel;
        }

        public override async Task<RideModel> GetRideRequest(GetRideRequestModel request, ServerCallContext context)
        {
            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"];

            _logger.LogInformation($"[LyftClient::RequestsService::GetRideRequest] HTTP Context session token: {SessionToken}");

            var CacheEstimate = await _cache.GetAsync<EstimateCache>(request.RideId);

            _apiClient.Configuration = new LyftAPI.Client.Client.Configuration
            {
                AccessToken = await _accessToken.GetAccessTokenAsync(SessionToken!, CacheEstimate.ProductId.ToString()),
            };

            var ride = await _apiClient.RidesIdGetAsync(request.RideId);

            return new RideModel
            {
                RideId = request.RideId,
                EstimatedTimeOfArrival = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(ride.Pickup.Time.DateTime),
                RiderOnBoard = ride.Status == LyftAPI.Client.Model.RideStatusEnum.PickedUp,
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

            _apiClient.Configuration = new LyftAPI.Client.Client.Configuration
            {
                AccessToken = await _accessToken.GetAccessTokenAsync(SessionToken!, CacheEstimate.ProductId.ToString())
            };

            await _apiClient.RidesIdCancelPostAsync(CacheEstimate.CancelationToken.ToString());
            return CacheEstimate.CancelationCost;
        }

        private static Stage StagefromStatus(LyftAPI.Client.Model.RideStatusEnum? status)
        {
            switch (status)
            {
                case LyftAPI.Client.Model.RideStatusEnum.Pending: return Stage.Pending;
                case LyftAPI.Client.Model.RideStatusEnum.Arrived: return Stage.Accepted;
                case LyftAPI.Client.Model.RideStatusEnum.PickedUp: return Stage.Accepted;
                case LyftAPI.Client.Model.RideStatusEnum.Accepted: return Stage.Accepted;
                case LyftAPI.Client.Model.RideStatusEnum.Canceled: return Stage.Cancelled;
                case LyftAPI.Client.Model.RideStatusEnum.DroppedOff: return Stage.Completed;
                default: return Stage.Unknown;
            }
        }
    }
}
