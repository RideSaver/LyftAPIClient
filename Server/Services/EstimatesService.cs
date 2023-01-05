using Grpc.Core;
using InternalAPI;
using LyftAPI.Client.Model;
using Microsoft.Extensions.Caching.Distributed;
using LyftApiClient.Server.Models;
using LyftApiClient.Server.Extensions;
using LyftClient_API = LyftAPI.Client.Api.PublicApi;
using LyftClient.Interface;
using LyftClient.Extensions;

namespace LyftClient.Services
{
    // Summary: Handles all requests for estimates
    public class EstimatesService : Estimates.EstimatesBase
    {

        private readonly IHttpClientFactory _clientFactory;
        private readonly IDistributedCache _cache;
        private readonly IAccessTokenService _accessToken;
        private readonly ILogger<EstimatesService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly HttpClient _httpClient;
        private readonly LyftClient_API _apiClient;

        public EstimatesService(ILogger<EstimatesService> logger, IDistributedCache cache, IAccessTokenService accessToken, IHttpClientFactory clientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _clientFactory= clientFactory;
            _httpClient = _clientFactory.CreateClient();
            _logger = logger;
            _cache = cache;
            _apiClient = new LyftClient_API(_httpClient, new LyftAPI.Client.Client.Configuration {});
            _accessToken = accessToken;
            _httpContextAccessor = httpContextAccessor;
        }
   
        public override async Task GetEstimates(GetEstimatesRequest request, IServerStreamWriter<EstimateModel> responseStream, ServerCallContext context)
        {
            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"];

            //----------------------------------------------------------[DEBUG]---------------------------------------------------------------//
            _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimates] HTTP Context Session Token : {SessionToken}", SessionToken);
            _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimates] Request: START: {request.StartPoint} END: {request.EndPoint} SEATS: {request.Seats} ");
            foreach(var service in request.Services)
            {
                _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimates] Request: SERVICE ID: {service}");
                ServiceIDs.serviceIDs.TryGetValue(service.ToUpper(), out string? logging_name);
                _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimates] Request: SERVICE NAME: {logging_name}");
            }
            //--------------------------------------------------------------------------------------------------------------------------------//

            foreach (var service in request.Services)
            {
                ServiceIDs.serviceIDs.TryGetValue(service.ToUpper(), out string? serviceName);
                if (serviceName is null) continue;

                if(_accessToken is null)
                {
                    _logger.LogInformation("[LyftClient::EstimatesService::GetEstimates] AccessToken is null.");
                    continue;
                }

                _apiClient.Configuration = new LyftAPI.Client.Client.Configuration // Get estimate with parameters
                {
                    AccessToken = await _accessToken!.GetAccessTokenAsync(SessionToken!, service!)
                };

                _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimates] Requesting data from the MockAPI...");

                var estimate = await _apiClient.EstimateAsync(request.StartPoint.Latitude, request.StartPoint.Longitude, serviceName, request.EndPoint.Latitude, request.EndPoint.Longitude);
                var estimateId = DataAccess.Services.ServiceID.CreateServiceID(service);

                _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimates] MockAPI Partial Response Data: {estimate.CostEstimates[0].DisplayName}");

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
