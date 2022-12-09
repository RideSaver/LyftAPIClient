using Grpc.Core;
using InternalAPI;
using LyftAPI.Client.Model;
using LyftAPI.Client.Repository;
using LyftClient.HTTPClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.ComponentModel;
using Microsoft.Bot.Schema;
using LyftApiClient.Server.Models;
using LyftApiClient.Server.Extensions;

/** Requests Service class
/**
* Lyft client class which sends requests from PostRideRequest, GetRideRequest, and DeleteRideRequest.
* These three methods work similarly. It first gets a access token in order to make calls to the Lyft
* API/Server. After token is recieved gRPC will make a call to the Lyft Client to the get the RideId.
* With that Id the client can recieve data from the protocol buffer. Then the client will invoke the
* necessary methods. Then the Lyft API/Server will send back data as parameters that can then be sent
* to our services.
*/
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

        private readonly IAccessTokenController _accessController;

        public RequestsService(ILogger<RequestsService> logger, IDistributedCache cache, IHttpClientInstance httpClient, IAccessTokenController accessController)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
            _apiClient = new LyftAPI.Client.Api.UserApi(httpClient.APIClientInstance, new LyftAPI.Client.Client.Configuration {});
            CacheEstimate = new LyftAPI.Client.Api.UserApi(httpClient.APIClientInstance, new LyftAPI.Client.Client.Configuration {});
            _accessController = accessController;
        }

        /**
        * @brief Creates new Lyft ride request
        * @startuml
        * state "Access Token Get" as GetToken
        * state "gRPC calls Lyft Client" as gRPC
        * gRPC: RideId
        * state "Get RideId from cache" as CacheId
        * state "Lyft Client receives data from protocol buffer" as ProtoData
        * state "Add Authentication Token to Lyft Client" as AuthToken
        * state "Client makes GetRideRequest call to Lyft API/Server" as RideReq
        * RideReq : Requested objects are parameters
        * state "Lyft API/Server sends back requested data" as LyftSend
        * state "Lyft Client receives Data/Response Objects" as ClientData
        * ClientData : Iterates through instances and adds them to RideId
        * state "Update RideId to Cache" as RideCache
        * state "Lyft Client sends data to services" as ServiceData
        *
        * [*] --> GetToken
        * GetToken --> gRPC
        * gRPC --> CacheId
        * CacheId --> ProtoData
        * ProtoData --> AuthToken : Deserialization to standard model
        * AuthToken --> RideReq : Serialization to Lyft Model
        * RideReq --> LyftSend
        * LyftSend --> ClientData
        * ClientData --> RideCache: Serialization to protocol buffer
        * RideCache --> ServiceData
        * ServiceData --> [*]
        * @enduml
         */
        public override async Task<RideModel> PostRideRequest(PostRideRequestModel request, ServerCallContext context)
        {
            var SessionToken = context.AuthContext.PeerIdentityPropertyName;
            _logger.LogInformation("HTTP Context User: {User}", SessionToken);
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
        * state "Access Token Get" as GetToken
        * state "gRPC calls Lyft Client" as gRPC
        * gRPC: RideId
        * state "Get RideId from cache" as CacheId
        * state "Lyft Client receives data from protocol buffer" as ProtoData
        * state "Add Authentication Token to Lyft Client" as AuthToken
        * state "Client makes GetRideRequest call to Lyft API/Server" as RideReq
        * RideReq : Requested objects are parameters
        * state "Lyft API/Server sends back requested data" as LyftSend
        * state "Lyft Client receives Data/Response Objects" as ClientData
        * ClientData : Iterates through instances and adds them to RideId
        * state "Lyft Client sends data to services" as ServiceData
        *
        * [*] --> GetToken
        * GetToken --> gRPC
        * gRPC --> CacheId
        * CacheId --> ProtoData
        * ProtoData --> AuthToken : Deserialization to standard model
        * AuthToken --> RideReq : Serialization to Lyft Model
        * RideReq --> LyftSend
        * LyftSend --> ClientData
        * ClientData --> ServiceData : Serialization to protocol buffer
        * ServiceData --> [*]
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
        }

        /**
        * @brief Deletes Lyft ride request
        * @startuml
        * state "Access Token Get" as GetToken
        * state "gRPC calls Lyft Client" as gRPC
        * gRPC: RideId
        * state "Get RideId from cache" as CacheId
        * state "Lyft Client receives data from protocol buffer" as ProtoData
        * state "Add Authentication Token to Lyft Client" as AuthToken
        * state "Client makes DeleteRideRequest call to Lyft API/Server" as RideReq
        * RideReq : Requested objects are parameters
        * state "Lyft API/Server sends back cancelation token and cancellation price" as CancelToken
        *
        * [*] --> GetToken
        * GetToken --> gRPC
        * gRPC --> CacheId
        * CacheId --> ProtoData
        * ProtoData --> AuthToken : Deserialization to standard model
        * AuthToken --> RideReq : Serialization to Lyft Model
        * RideReq --> CancelToken
        * CancelToken --> [*]
        * @enduml
        */
        public override async Task<CurrencyModel> DeleteRideRequest(DeleteRideRequestModel request, ServerCallContext context)
        {
            var SessionToken = context.AuthContext.PeerIdentityPropertyName;
            _logger.LogInformation("HTTP Context User: {User}", SessionToken);
            var CacheEstimate = await _cache.GetAsync<EstimateCache>(request.RideId);

            _apiClient.Configuration = new LyftAPI.Client.Client.Configuration
            {
                AccessToken = await _accessController.GetAccessTokenAsync(SessionToken, CacheEstimate.ProductId.ToString())
            };

            await _apiClient.RidesIdCancelPostAsync(CacheEstimate.CancelationToken.ToString());

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
