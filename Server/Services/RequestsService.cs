using Grpc.Core;
using InternalAPI;
<<<<<<< Updated upstream
=======
using LyftClient.HTTPClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.ComponentModel;
using Microsoft.Bot.Schema;
using LyftAPI;
using LyftAPI.Client.Repository;
using LyftApiClient.Server.Models;
using LyftAPI.Client.Model;
using LyftApiClient.Server.Extensions;
>>>>>>> Stashed changes

namespace LyftClient.Services
{
    public class RequestsService : Requests.RequestsBase // TBA
    {
        private readonly ILogger<RequestsService> _logger;

        public RequestsService(ILogger<RequestsService> logger)
        {
            _logger = logger;
<<<<<<< Updated upstream
=======
            _cache = cache;
            _apiClient = new LyftAPI.Client.Api.UserApi(httpClient.APIClientInstance, new LyftAPI.Client.Client.Configuration {});
            CacheEstimate = new LyftAPI.Client.Api.UserApi(httpClient.APIClientInstance, new LyftAPI.Client.Client.Configuration {});
            _accessController = accessController;
        }

        /**
         * @brief Creates new Lyft ride request
         * @startuml
         * State Diagram
         * < = Input
         * : = state
         * then = denotes branch
         * if =  if statement
         * endif = end if statement
         * @enduml
         * @startuml
         * Sequence Diagram
         * participant 
         * example -> example1
         * ++: = activation
         * return 
         * alt = alternative scenario
         */
        public override async Task<RideModel> PostRideRequest(PostRideRequestModel request, ServerCallContext context)
        {
            var SessionToken = context.AuthContext.PeerIdentityPropertyName;
            _logger.LogInformation("HTTP Context User: {User}", SessionToken);
            var CacheEstimate = await _cache.GetAsync<EstimateCache>(request.EstimateId);

            LyftAPI.Client.Model.Ride _request = new LyftAPI.Client.Model.Ride()
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

            _apiClient.Configuration = new LyftAPI.Client.Client.Configuration 
            {
                AccessToken = await _accessController.GetAccessTokenAsync(SessionToken, CacheEstimate.ProductId.ToString())
            };

            var ride = await _apiClient.RidesPostAsync(_request);
            _apiClient.Configuration = new LyftAPI.Client.Client.Configuration 
            {
                AccessToken = await _accessController.GetAccessTokenAsync(SessionToken, CacheEstimate.ProductId.ToString())
            };

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
        
        /**
        * @brief Gets new Lyft ride request 
        * @startuml
        * 
        * @enduml
        */
        public override async Task<RideModel> GetRideRequest(GetRideRequestModel request, ServerCallContext context)
        {
            var SessionToken = context.AuthContext.PeerIdentityPropertyName;
            _logger.LogInformation("HTTP Context User: {User}", SessionToken);
            var CacheEstimate = await _cache.GetAsync<EstimateCache>(request.RideId);
            _apiClient.Configuration = new LyftAPI.Client.Client.Configuration 
            {
                AccessToken = await _accessController.GetAccessTokenAsync(SessionToken, CacheEstimate.ProductId.ToString()),
            };
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
>>>>>>> Stashed changes
        }
    }
}
