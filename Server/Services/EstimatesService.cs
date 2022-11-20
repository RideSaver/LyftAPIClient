using Grpc.Core;
using InternalAPI;
using LyftAPI.Client.Model;
using LyftClient.HTTPClient;
using Microsoft.Extensions.Caching.Distributed;

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
            var encodedUserID = await _cache.GetAsync(context.GetHttpContext().Password);

            if (encodedUserID == null)
            {
                return;
            }
            UserID = Encoding.UTF8.GetString(encodedUserID);

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
