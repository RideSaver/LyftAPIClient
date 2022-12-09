using Grpc.Core;
using InternalAPI;
using LyftAPI.Client.Model;
using LyftAPI.Client.Repository;
using LyftClient.HTTPClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using LyftApiClient.Server.Models;
using LyftApiClient.Server.Extensions;
using DataAccess;

//**Estimates Service class
/**
* Lyft client class which sends requests from GetEstimate, and GetEstimate Refresh.
* These two methods work similarly. It first gets a access token in order to make calls to the Lyft
* API/Server. After token is recieved gRPC will make a call to the Lyft Client to the get the RideId.
* With that Id the client can recieve data from the protocol buffer. Then the client will invoke the
* necessary methods. Then the Lyft API/Server will send back data as parameters that can then be sent
* to our services.
*/
namespace LyftClient.Services
{
    // Summary: Handles all requests for estimates
    public class EstimatesService : Estimates.EstimatesBase
    {
        // Summary: our logging object, used for diagnostic logs.
        private readonly ILogger<EstimatesService> _logger;
        // Summary: our API client, so we only open up some ports, rather than swamping the system.

        private readonly IHttpClientInstance _httpClient;

        // Summary: Our cache object
        private readonly IDistributedCache _cache;

        // Summary: our Lyft API client
        private readonly LyftAPI.Client.Api.PublicApi _apiClient;

        private readonly IAccessTokenController _accessController;

        public EstimatesService(ILogger<EstimatesService> logger, IDistributedCache cache, IHttpClientInstance httpClient, IAccessTokenController accessController)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
            _apiClient = new LyftAPI.Client.Api.PublicApi(httpClient.APIClientInstance, new LyftAPI.Client.Client.Configuration {});
            _accessController = accessController;
        }

        [Authorize]
        /**
        * @startuml
        * state "Get Access Token" as AccTok
        * state "gRPC calls Lyft Client" as gRPC
        * gRPC : Estimate Values
        * state "Lyft Client receives protocol buffer data" as ProtoData
        * state "Add authentication token to Lyft Client" as  AuthToken
        * state "LyftAPI Third Party Call" as LyftCall
        * LyftCall: Request object as Parameter
        * state "Lyft Server sends back data of requested estimates" as DataSend
        * state "Lyft Client receives response object" as DataReci
        * DataReci: Loops through all services then adds to EstimatedId
        * state "EstimateId sent to cache" as IDCahce
        * state "Send Estimate object to Service" as ServiObj

        * [*] --> AccTok
        * AccTok --> gRPC
        * gRPC --> ProtoData
        * ProtoData --> AuthToken : Deserialization to the standard model
        * AuthToken --> LyftCall : Serialization to Lyft Model
        * LyftCall --> DataSend
        * DataSend --> DataReci
        * DataReci --> IDCahce : Serialization to protocol buffer data
        * IDCahce --> AccTok
        * IDCahce --> ServiObj
        * ServiObj  --> [*]
        * @enduml
        */
        public override async Task GetEstimates(GetEstimatesRequest request, IServerStreamWriter<EstimateModel> responseStream, ServerCallContext context)
        {
            var SessionToken = context.AuthContext.PeerIdentityPropertyName;
            _logger.LogInformation("HTTP Context User: {User}", SessionToken);
            // Loop through all the services in the request
            foreach (var service in request.Services)
            {
                string serviceName;
                ServiceIDs.serviceIDs.TryGetValue(service, out serviceName);
                if(serviceName == null) continue;
                // Get estimate with parameters
                _apiClient.Configuration = new LyftAPI.Client.Client.Configuration {
                    AccessToken = await _accessController.GetAccessTokenAsync(SessionToken, service)
                };

                var estimate = await _apiClient.EstimateAsync(
                    request.StartPoint.Latitude,
                    request.StartPoint.Longitude,
                    serviceName,
                    request.EndPoint.Latitude,
                    request.EndPoint.Longitude
                );

                var EstimateId = DataAccess.Services.ServiceID.CreateServiceID(service);

                // Write an InternalAPI model back
                var estimateModel = new EstimateModel()
                {
                    EstimateId = "NEW ID GENERATOR",
                    CreatedTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now),
                    PriceDetails = new CurrencyModel
                    {
                        Price = (double)estimate.CostEstimates[0].EstimatedCostCentsMax,
                        Currency = estimate.CostEstimates[0].Currency
                    },
                    Distance = (int)estimate.CostEstimates[0].EstimatedDistanceMiles,
                    Seats = request.Seats,// TODO: Lookup table non shared services
                    RequestUrl  = "",
                    DisplayName = estimate.CostEstimates[0].DisplayName

                };

                estimateModel.WayPoints.Add(request.StartPoint);
                estimateModel.WayPoints.Add(request.EndPoint);

                await responseStream.WriteAsync(estimateModel);

                _cache.SetAsync<EstimateCache>(estimateModel.EstimateId, new EstimateCache
                {

                    Cost = new Cost()
                    {
                        Currency = estimateModel.PriceDetails.Currency,
                        Amount = (int)estimateModel.PriceDetails.Price
                    },
                    GetEstimatesRequest = new GetEstimatesRequest()
                    {
                        StartPoint = estimateModel.WayPoints[0],
                        EndPoint = estimateModel.WayPoints[1],
                        Seats = estimateModel.Seats
                    },
                    ProductId = Guid.Parse(service)

                },new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2) });
            }
        }

        /**
        * @startuml
        * tate "New Estimate Model" as NewModel
        * state "Return Data From EstimateRefresh" as Data
        *
        * [*] --> NewModel
        * NewModel --> Data
        * Data--> [*]
        * @enduml
        */
        public override Task<EstimateModel> GetEstimateRefresh(GetEstimateRefreshRequest request, ServerCallContext context)
        {
            var estimateRefresh = new EstimateModel();
            // TBA: Invoke the web-client API to get the information from the Lyft-api, then send it to the microservice.

            return Task.FromResult(estimateRefresh);
        }
    }
}
