using Grpc.Core;
using InternalAPI;
using LyftAPI.Client.Model;
using LyftClient.HTTPClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.ComponentModel;
using Microsoft.Bot.Schema;
using LyftApiClient.Server.Models;
using LyftApiClient.Server.Extensions;

namespace LyftClient.Services
{
    public class RequestsService : Requests.RequestsBase // TBA
    {
        // Summary: our logging object, used for diagnostic logs.
        private readonly ILogger<RequestsService> _logger;
        // Summary: our API client, so we only open up some ports, rather than swamping the system.
        private readonly IHttpClientInstance _httpClient;

        // Summary: Our cache object
        private readonly IDistributedCache _cache;

        // Summary: our Lyft API client
        private readonly LyftAPI.Client.Api.UserApi _apiClient;

        private readonly LyftAPI.Client.Api.UserApi CacheEstimate;

        public RequestsService(ILogger<RequestsService> logger, IDistributedCache cache, IHttpClientInstance httpClient)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
            _apiClient = new LyftAPI.Client.Api.UserApi(httpClient.APIClientInstance, new LyftAPI.Client.Client.Configuration {});
            CacheEstimate = new LyftAPI.Client.Api.UserApi(httpClient.APIClientInstance, new LyftAPI.Client.Client.Configuration {});
        }

        /**
         * @brief Creates new Lyft Ride Request
         * @startuml
         * Alice -> Bob : Hello
         * @enduml
         */
        public override async Task<RideModel> PostRideRequest(PostRideRequestModel request, ServerCallContext context)
        {
            var SessionToken = context.AuthContext.PeerIdentityPropertyName;
            _logger.LogInformation("HTTP Context User: {User}", SessionToken);
            var encodedUserID = await _cache.GetAsync(SessionToken);

            if (encodedUserID == null)
            {
               throw new NotImplementedException();
            }
            var UserID = Encoding.UTF8.GetString(encodedUserID);

            var AccessToken = UserID;

            _apiClient.Configuration = new LyftAPI.Client.Client.Configuration 
            {
                AccessToken = AccessToken
            };

            var CacheEstimate = await _cache.GetAsync<EstimateCache>(request.EstimateId);

            LyftAPI.Client.Model.CreateRideRequest _request = new LyftAPI.Client.Model.CreateRideRequest()
            {
                Origin = new Location()
                {
                    Lat = CacheEstimate.GetEstimatesRequest.StartPoint.Latitude,
                    Lng = CacheEstimate.GetEstimatesRequest.StartPoint.Longitude
                },

                Destination = new Location()
                {
                    Lat = CacheEstimate.GetEstimatesRequest.EndPoint.Latitude,
                    Lng = CacheEstimate.GetEstimatesRequest.EndPoint.Longitude
                },
            };

            var ride = await _apiClient.RidesPostAsync(_request);

            var RideDetails = await _apiClient.RidesIdGetAsync(ride.RideId); 

            CacheEstimate.CancelationCost = new CurrencyModel()
            {
                Price = RideDetails.CancellationPrice.Amount,
                Currency = RideDetails.CancellationPrice.Currency
            };

            CacheEstimate.CancelationToken = new Guid (RideDetails.CancellationPrice.Token);

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

                RideStage = StagefromStatus (RideDetails.Status),

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
            var SessionToken = context.AuthContext.PeerIdentityPropertyName;
            _logger.LogInformation("HTTP Context User: {User}", SessionToken);
            var encodedUserID = await _cache.GetAsync(SessionToken); // TODO: Figure out if this is the correct token

            if (encodedUserID == null)
            {
                throw new NotImplementedException();
            }
            var UserID = Encoding.UTF8.GetString(encodedUserID);

            var AccessToken = UserID; // TODO: Get Access Token From DB

            // Create new API client (since it doesn't seem to allow dynamic loading of credentials)
            _apiClient.Configuration = new LyftAPI.Client.Client.Configuration 
            {
                AccessToken = AccessToken
            };

            string serviceName;
            ServiceIDs.serviceIDs.TryGetValue(request.RideId, out serviceName);
           
            // Get ride with parameters
            var ride = await _apiClient.RidesIdGetAsync(request.RideId);

            return (new RideModel
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

                RideStage = StagefromStatus (ride.Status),

                DriverLocation = new LocationModel
                {
                    Latitude = ride.Location.Lat,
                    Longitude = ride.Location.Lng,
                },
            });
        }

        public override async Task<CurrencyModel> DeleteRideRequest(DeleteRideRequestModel request, ServerCallContext context)
        {
            var SessionToken = context.AuthContext.PeerIdentityPropertyName;
            _logger.LogInformation("HTTP Context User: {User}", SessionToken);
            var encodedUserID = await _cache.GetAsync(SessionToken); // TODO: Figure out if this is the correct token

            if (encodedUserID == null)
            {
                throw new NotImplementedException();
            }
            var UserID = Encoding.UTF8.GetString(encodedUserID);

            var AccessToken = UserID; // TODO: Get Access Token From DB

            var CacheEstimate = await _cache.GetAsync<EstimateCache>(request.RideId);

            _apiClient.Configuration = new LyftAPI.Client.Client.Configuration 
            {
                AccessToken = AccessToken
            };

            string serviceName;
            ServiceIDs.serviceIDs.TryGetValue(request.RideId, out serviceName);

            return CacheEstimate.CancelationCost;
        }

        private Stage StagefromStatus (LyftAPI.Client.Model.RideStatusEnum? status) 
        {
            switch (status)
            {
                case LyftAPI.Client.Model.RideStatusEnum.Pending:
                    return Stage.Pending;
                
                case LyftAPI.Client.Model.RideStatusEnum.Arrived:
                case LyftAPI.Client.Model.RideStatusEnum.PickedUp:
                case LyftAPI.Client.Model.RideStatusEnum.Accepted:
                    return Stage.Accepted;

                case LyftAPI.Client.Model.RideStatusEnum.Canceled:
                    return Stage.Cancelled;

                case LyftAPI.Client.Model.RideStatusEnum.DroppedOff:
                    return Stage.Completed;

                default:
                    return Stage.Unknown;
            }
        }     
    }
}
