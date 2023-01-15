using Grpc.Core;
using InternalAPI;
using LyftClient.Helper;
using LyftClient.Interface;
using LyftAPI.Client.Model;
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
        private readonly IAccessTokenService _tokenService;
        private readonly IDistributedCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly UserAPI _apiClient;

        public RequestsService(ILogger<RequestsService> logger, IDistributedCache cache, IAccessTokenService accessToken, IHttpContextAccessor httpContextAccessor)
        {
            _apiClient = new UserAPI();
            _tokenService = accessToken;
            _logger = logger;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;  
        }
        public override async Task<RideModel> PostRideRequest(PostRideRequestModel request, ServerCallContext context)
        {
            // Extract the JWT token from the request-headers for the current-user.
            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"];
            if(SessionToken is null) { throw new ArgumentNullException(nameof(SessionToken)); }

            // Retrieve the EstimateID used as key for the EstimateInstance cache.
            var internalServiceID = request.EstimateId.ToString();
            if (internalServiceID is null) { throw new ArgumentNullException(nameof(internalServiceID)); }

            // Retrieve the Estimate instance stored in the cache.
            var cacheEstimate = await _cache.GetAsync<EstimateCache>(internalServiceID);
            if (cacheEstimate is null) { throw new ArgumentNullException(nameof(cacheEstimate)); }
            var serviceID = cacheEstimate!.ProductId.ToString();

            // Retrieve the user access token from IdentityService for the current user.
            _apiClient.Configuration = new APIConfig { AccessToken = await _tokenService.GetAccessTokenAsync(SessionToken!, serviceID) };
            if (_apiClient.Configuration.AccessToken is null) { throw new ArgumentNullException(nameof(_apiClient.Configuration.AccessToken)); }

            // Create the RideRequest instance that will be sent to the MockAPI.
            var rideRequest = new CreateRideRequest(costToken: Guid.NewGuid().ToString())
            {
                RideType = Utility.RideTypeFromServiceID(serviceID),
                Origin = Utility.ConvertLocationModelToLocation(cacheEstimate!.GetEstimatesRequest!.StartPoint),
                Destination = Utility.ConvertLocationModelToLocation(cacheEstimate!.GetEstimatesRequest!.EndPoint),
                Passenger = new PassengerDetail(firstName: "Exempt", imageUrl: "Exempt", rating: "Exempt")
            };

            // Make the request to the MockAPI tp post the ride request.
            var rideResponseInstance = await _apiClient.RidesPostAsync(createRideRequest: rideRequest);
            if (rideResponseInstance is null) { throw new ArgumentNullException(nameof(rideResponseInstance)); }
            var rideID = rideResponseInstance!.RideId.ToString();

            // Retrieve the user access token from IdentityService for the current user.
            _apiClient.Configuration = new APIConfig { AccessToken = await _tokenService.GetAccessTokenAsync(SessionToken!, serviceID)  };
            if (_apiClient.Configuration.AccessToken is null) { throw new ArgumentNullException(nameof(_apiClient.Configuration.AccessToken)); }

            // Make the request to the MockAPI to get the RideDetail.
            var rideDetailsResponseInstance = await _apiClient.RidesIdGetAsync(rideID);
            if (rideDetailsResponseInstance is null) { throw new ArgumentNullException(nameof(rideDetailsResponseInstance)); }

            // Update the CacheEstimate requestID & CanceallationCost and save it into the cache.
            cacheEstimate.RequestId = Guid.Parse(rideID);
            cacheEstimate.CancelationCost = new CurrencyModel
            {
                Currency = rideDetailsResponseInstance.CancellationPrice.Currency,
                Price = rideDetailsResponseInstance.CancellationPrice.Amount
            };
            await _cache.SetAsync(internalServiceID, cacheEstimate);

            // Create a new instance of RideModel to be sent back to RequestsAPI.
            return new RideModel()
            {
                RideId = internalServiceID,
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
                    CarPicture = rideDetailsResponseInstance.Vehicle.ImageUrl,
                    CarDescription = $"{rideDetailsResponseInstance.Vehicle.Make} {rideDetailsResponseInstance.Vehicle.Model}",
                    DriverPronounciation = rideDetailsResponseInstance.Driver.FirstName,
                },
                DriverLocation = new LocationModel
                {
                    Latitude = rideDetailsResponseInstance.Location.Lat,
                    Longitude = rideDetailsResponseInstance.Location.Lng,
                    Height = 300f,
                    Planet = "Earth"
                },
            };
        }

        public override async Task<RideModel> GetRideRequest(GetRideRequestModel request, ServerCallContext context)
        {
            // Extract JWT token from the requst headers.
            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"];
            if (SessionToken is null) { throw new ArgumentNullException(nameof(SessionToken)); }

            // RideID used as the cache-instance key.
            var internalServiceID = request.RideId.ToString();
            if (internalServiceID is null) { throw new ArgumentNullException(nameof(internalServiceID)); }

            // Get the cache instance for the ride-request.
            var cacheEstimate = await _cache.GetAsync<EstimateCache>(internalServiceID);
            if (cacheEstimate is null) { throw new ArgumentNullException(nameof(cacheEstimate)); }
            var serviceID = cacheEstimate!.ProductId.ToString();
            var requestID = cacheEstimate.RequestId.ToString();

            // Retrieve the user-access-token from IdentityService for the current user.
            _apiClient.Configuration = new APIConfig { AccessToken = await _tokenService.GetAccessTokenAsync(SessionToken!, serviceID) };
            if (_apiClient.Configuration.AccessToken is null) { throw new ArgumentNullException(nameof(_apiClient.Configuration.AccessToken)); }

            //  Make the request to the MockAPI.
            var rideDetailsResponseInstance = await _apiClient.RidesIdGetAsync(requestID);
            if (rideDetailsResponseInstance is null) { throw new ArgumentNullException(nameof(rideDetailsResponseInstance)); }

            // Create a new instance of RideModel to send back to RequestsAPI.
            return new RideModel
            {
                RideId = internalServiceID,
                RiderOnBoard = false,
                EstimatedTimeOfArrival = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime((rideDetailsResponseInstance!.Pickup.Time.DateTime).ToUniversalTime()),
                RideStage = Utility.StagefromStatus(rideDetailsResponseInstance.Status),
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
                    CarDescription = $"{rideDetailsResponseInstance.Vehicle.Make} {rideDetailsResponseInstance.Vehicle.Model}",
                    DriverPronounciation = rideDetailsResponseInstance.Driver.FirstName
                },
                DriverLocation = new LocationModel
                {
                    Latitude = rideDetailsResponseInstance.Location.Lat,
                    Longitude = rideDetailsResponseInstance.Location.Lng,
                    Height = 300f,
                    Planet = "Earth"
                },
            };
        }

        public override async Task<CurrencyModel> DeleteRideRequest(DeleteRideRequestModel request, ServerCallContext context)
        {
            // Extract the JWT token from the request headers.
            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"];
            if (SessionToken is null) { throw new ArgumentNullException(nameof(SessionToken)); }

            // RideID used as the cache-instance key
            var internalServiceID = request.RideId.ToString();
            if (internalServiceID is null) { throw new ArgumentNullException(nameof(internalServiceID)); }

            // Get the cache-instance for the ride-request
            var cacheEstimate = await _cache.GetAsync<EstimateCache>(internalServiceID);
            if (cacheEstimate is null) { throw new ArgumentNullException(nameof(cacheEstimate)); }
            var serviceID = cacheEstimate!.ProductId.ToString();
            var requestID = cacheEstimate.RequestId.ToString();

            // Retrieve the user-access-token from IdentityService for the current user.
            _apiClient.Configuration = new APIConfig { AccessToken = await _tokenService.GetAccessTokenAsync(SessionToken!, serviceID) };
            if (_apiClient.Configuration.AccessToken is null) { throw new ArgumentNullException(nameof(_apiClient.Configuration.AccessToken)); }

            // CancellationToken instance for the CancelRide request.
            var cancellationToken = new CancellationRequest { CancelConfirmationToken = Guid.NewGuid().ToString() };

            // Make the Delete request to the MockAPI
            await _apiClient.RidesIdCancelPostAsync(requestID, cancellationToken);

            // Return the cancellation-fee price breakdown saved in the cache.
            if (cacheEstimate.CancelationCost is null) { throw new ArgumentNullException(nameof(cacheEstimate.CancelationCost)); }
            return cacheEstimate.CancelationCost!;
        }
    }
}
