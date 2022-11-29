using Grpc.Core;
using InternalAPI;
using LyftAPI.Client.Model;
using LyftClient.HTTPClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using LyftApiClient.Server.Models;
using LyftApiClient.Server.Extensions;
using DataAccess;

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

        public EstimatesService(ILogger<EstimatesService> logger, IDistributedCache cache, IHttpClientInstance httpClient)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
            _apiClient = new LyftAPI.Client.Api.PublicApi(httpClient.APIClientInstance, new LyftAPI.Client.Client.Configuration {});
        }
        
        [Authorize]
        public override async Task GetEstimates(GetEstimatesRequest request, IServerStreamWriter<EstimateModel> responseStream, ServerCallContext context)
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
            _apiClient.Configuration = new LyftAPI.Client.Client.Configuration {
                AccessToken = AccessToken
            };
            // Loop through all the services in the request
            foreach (var service in request.Services)
            {
                string serviceName;
                ServiceIDs.serviceIDs.TryGetValue(service, out serviceName);
                if(serviceName == null) continue;
                // Get estimate with parameters
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

        public override Task<EstimateModel> GetEstimateRefresh(GetEstimateRefreshRequest request, ServerCallContext context)
        {
            var estimateRefresh = new EstimateModel();
            // TBA: Invoke the web-client API to get the information from the Lyft-api, then send it to the microservice.

            return Task.FromResult(estimateRefresh);
        }
    }
}
