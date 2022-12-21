using Grpc.Core;
using InternalAPI;
using LyftAPI.Client.Model;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;
using LyftApiClient.Server.Models;
using LyftApiClient.Server.Extensions;

using LyftClient_API = LyftAPI.Client.Api.PublicApi;

namespace LyftClient.Services
{
    // Summary: Handles all requests for estimates
    public class EstimatesService : Estimates.EstimatesBase
    {

        private readonly IHttpClientFactory _clientFactory;
        private readonly IDistributedCache _cache;
        private readonly IAccessTokenService _accessToken;
        private readonly ILogger<EstimatesService> _logger;

        private readonly HttpClient _httpClient;
        private readonly LyftClient_API _apiClient;

 
        public EstimatesService(ILogger<EstimatesService> logger, IDistributedCache cache, IAccessTokenService accessToken, IHttpClientFactory clientFactory)
        {
            _clientFactory= clientFactory;
            _httpClient = _clientFactory.CreateClient();
            _logger = logger;
            _cache = cache;
            _apiClient = new LyftClient_API(_httpClient, new LyftAPI.Client.Client.Configuration {});
            _accessToken = accessToken;
        }
   
        public override async Task GetEstimates(GetEstimatesRequest request, IServerStreamWriter<EstimateModel> responseStream, ServerCallContext context)
        {
            var SessionToken = context.AuthContext.FindPropertiesByName("token").ToString();

            _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimates] HTTP Context session token : {SessionToken}", SessionToken);

            // Loop through all the services in the request
            foreach (var service in request.Services)
            {
                ServiceIDs.serviceIDs.TryGetValue(service, out string? serviceName);
                if (serviceName is null) continue;

                if(_accessToken is null)
                {
                    _logger.LogInformation("[LyftClient::EstimatesService::GetEstimates] AccessToken is null.");
                    break;
                }

                _apiClient.Configuration = new LyftAPI.Client.Client.Configuration // Get estimate with parameters
                {
                    AccessToken = await _accessToken!.GetAccessTokenAsync(SessionToken!, service!)
                };
                   
                var estimate = await _apiClient.EstimateAsync(request.StartPoint.Latitude, request.StartPoint.Longitude, serviceName, request.EndPoint.Latitude, request.EndPoint.Longitude);
                var estimateId = DataAccess.Services.ServiceID.CreateServiceID(service);

                // Write an InternalAPI model back
                var estimateModel = new EstimateModel()
                {
                    EstimateId = estimateId.ToString(),
                    CreatedTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now),

                    PriceDetails = new CurrencyModel
                    {
                        Price = estimate.CostEstimates[0].EstimatedCostCentsMax,
                        Currency = estimate.CostEstimates[0].Currency
                    },

                    Distance = (int)estimate.CostEstimates[0].EstimatedDistanceMiles,
                    Seats = request.Seats,
                    RequestUrl  = "",
                    DisplayName = estimate.CostEstimates[0].DisplayName
                    
                };

                estimateModel.WayPoints.Add(request.StartPoint);
                estimateModel.WayPoints.Add(request.EndPoint);

                await responseStream.WriteAsync(estimateModel);

                await _cache.SetAsync(estimateModel.EstimateId, new EstimateCache
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
                    
                }, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2) });
            }
        }
        
        public override Task<EstimateModel> GetEstimateRefresh(GetEstimateRefreshRequest request, ServerCallContext context)
        {
            throw new NotImplementedException();
        }
    }
}
