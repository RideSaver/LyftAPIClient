using Grpc.Core;
using InternalAPI;
using LyftAPI.Client.Model;
using Microsoft.Extensions.Caching.Distributed;

namespace LyftClient.Services
{
    // Summary: Handles all requests for estimates
    public class EstimatesService : Estimates.EstimatesBase
    {
        // Summary: our logging object, used for diagnostic logs.
        private readonly ILogger<EstimatesService> _logger;
        // Summary: our API client, so we only open up some ports, rather than swamping the system.
        private HttpClient apiClient;
        // Summary: Our cache object
        private readonly IDistributedCache _cache;

        public EstimatesService(ILogger<EstimatesService> logger, IDistributedCache cache)
        {
            _logger = logger;
            _cache = cache;
            apiClient = new HttpClient(new HttpClientHandler {
                MaxConnectionsPerServer = 2 // Make sure we only open up a maximum of 2 connections per server (i.e. Lyft.com)
            });
        }
        
        [Authorize]
        public override async Task GetEstimates(GetEstimatesRequest request, IServerStreamWriter<EstimateModel> responseStream, ServerCallContext context)
        {
            var encodedUserID = await _cache.GetAsync(context.GetHttpContext().Password);

            if (encodedUserID == null)
            {
                return;
            }
            UserID = Encoding.UTF8.GetString(encodedUserID);

            var AccessToken = UserID; // TODO: Get Access Token From DB

            // Create new API client (since it doesn't seem to allow dynamic loading of credentials)
            var apiClient = new LyftAPI.Client.Api.PublicApi(this.apiClient, new LyftAPI.Client.Client.Configuration {
                AccessToken = AccessToken
            });
            // Loop through all the services in the request
            foreach (var service in request.Services)
            {
                string serviceName;
                ServiceIDs.serviceIDs.TryGetValue(service, out serviceName);
                if(serviceName == null) continue;
                // Get estimate with parameters
                var estimate = await apiClient.EstimateAsync(
                    request.StartPoint.Latitude,
                    request.StartPoint.Longitude,
                    serviceName,
                    request.EndPoint.Latitude,
                    request.EndPoint.Longitude
                );
                // Write an InternalAPI model back
                await responseStream.WriteAsync(new EstimateModel
                {
                    // TODO: populate most of this data with data from the estimate.
                    EstimateId = "NEW ID GENERATOR",
                    PriceDetails = new CurrencyModel
                    {
                        Price = (double)estimate.CostEstimates[0].EstimatedCostCentsMax,
                        Currency = estimate.CostEstimates[0].Currency
                    },
                    Distance = (int)estimate.CostEstimates[0].EstimatedDistanceMiles
                });
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
