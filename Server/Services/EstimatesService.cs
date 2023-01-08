using Grpc.Core;
using InternalAPI;
using LyftAPI.Client.Model;
using Microsoft.Extensions.Caching.Distributed;
using LyftApiClient.Server.Models;
using LyftApiClient.Server.Extensions;
using LyftClient.Interface;
using LyftClient.Extensions;

using PublicApi = LyftAPI.Client.Api.PublicApi;

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
        private readonly PublicApi _apiClient;

        public EstimatesService(ILogger<EstimatesService> logger, IDistributedCache cache, IAccessTokenService accessToken, IHttpClientFactory clientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _accessToken = accessToken;
            _httpContextAccessor = httpContextAccessor;
            _clientFactory = clientFactory;
            _httpClient = _clientFactory.CreateClient();
            _logger = logger;
            _cache = cache;
            _apiClient = new PublicApi(_httpClient, new LyftAPI.Client.Client.Configuration {});
        }
   
        public override async Task GetEstimates(GetEstimatesRequest request, IServerStreamWriter<EstimateModel> responseStream, ServerCallContext context)
        {
            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"];

            DistributedCacheEntryOptions options = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24), SlidingExpiration = TimeSpan.FromHours(5) };

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

            var servicesList = request.Services.ToList();

            foreach (var service in servicesList)
            {
                ServiceIDs.serviceIDs.TryGetValue(service.ToUpper(), out string? serviceName);
                if (serviceName is null) continue;

                if(_accessToken is null)
                {
                    _logger.LogError("[LyftClient::EstimatesService::GetEstimates] AccessToken is NULL.");
                    continue;
                }

                _apiClient.Configuration = new LyftAPI.Client.Client.Configuration // Get estimate with parameters
                {
                    AccessToken = await _accessToken!.GetAccessTokenAsync(SessionToken!, service!)
                };

                _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimates] Requesting data from the MockAPI...");

                var estimate = await _apiClient.EstimateAsync(request.StartPoint.Latitude, request.StartPoint.Longitude, serviceName, request.EndPoint.Latitude, request.EndPoint.Longitude);
                var estimateId = DataAccess.Services.ServiceID.CreateServiceID(service).ToString();

                _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimates] Received (CostEstimateResponse) from MockAPI... \n{estimate}");

                // Write an InternalAPI model back
                var estimateModel = new EstimateModel()
                {
                    EstimateId = estimateId,
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

                _logger.LogInformation($"[UberClient::EstimatesService::GetEstimates] Adding (EstimateCache) to the cache...");

                await _cache.SetAsync(estimateId, new EstimateCache
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
                    ProductId = Guid.Parse(estimateId)
                }, options);

                await responseStream.WriteAsync(estimateModel);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
        
        public override Task<EstimateModel> GetEstimateRefresh(GetEstimateRefreshRequest request, ServerCallContext context)
        {
            throw new NotImplementedException();
        }
    }
}
