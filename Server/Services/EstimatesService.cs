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
            string clientId = "al0I63Gjwk3Wsmhq_EL8_HxB8qWlO7yY";

            DistributedCacheEntryOptions options = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24), SlidingExpiration = TimeSpan.FromHours(5) };

            //----------------------------------------------------------[DEBUG]---------------------------------------------------------------//
            _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimates] HTTP Context Session Token : {SessionToken}", SessionToken);
            _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimates] Request: START: {request.StartPoint} END: {request.EndPoint} SEATS: {request.Seats} ");
            foreach (var service in request.Services)
            {
                ServiceLinker.ServiceIDs.TryGetValue(service.ToUpper(), out string? logging_name);
                _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimates] Request: ServiceID: {service} - Service Name: {logging_name}");
            }
            //--------------------------------------------------------------------------------------------------------------------------------//

            var servicesList = request.Services.ToList();


            foreach (var service in servicesList)
            {
                ServiceLinker.ServiceIDs.TryGetValue(service.ToUpper(), out string? serviceName);
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

                var estimateResponse = await _apiClient.EstimateAsync(request.StartPoint.Latitude, request.StartPoint.Longitude, serviceName, request.EndPoint.Latitude, request.EndPoint.Longitude);
                var estimateResponseId = DataAccess.Services.ServiceID.CreateServiceID(service).ToString();

                _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimates] Received (CostEstimateResponse) from MockAPI...");

                foreach(var estimate in estimateResponse.CostEstimates)
                {
                    _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimates] Translating (CostEstimate) for gRPC... \n{estimate}");

                    var estimateModel = new EstimateModel()
                    {
                        EstimateId = estimateResponseId,
                        CreatedTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now.ToUniversalTime()),
                        InvalidTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now.AddMinutes(5).ToUniversalTime()),
                        PriceDetails = new CurrencyModel
                        {
                            Price = estimate.EstimatedCostCentsMax,
                            Currency = estimate.Currency
                        },
                        Distance = (int)estimate.EstimatedDistanceMiles,
                        Seats = request.Seats,
                        RequestUrl = $"https://lyft.mock/client_id={clientId}&action=setPickup&pickup[latitude]={request.StartPoint.Latitude}&pickup[longitude]={request.StartPoint.Longitude}&dropoff[latitude]={request.EndPoint.Latitude}&dropoff[longitude]={request.EndPoint.Longitude}&product_id={service}",
                        DisplayName = estimate.DisplayName,
                        WayPoints = { { request.StartPoint }, { request.EndPoint }, }
                    };

                    var estimateCache = new EstimateCache()
                    {
                        Cost = new Cost((int)estimateModel.PriceDetails.Price, estimateModel.PriceDetails.Currency.ToString(), "Estimate price details"),
                        GetEstimatesRequest = request,
                        ProductId = Guid.Parse(estimateResponseId)
                    };

                    _logger.LogInformation($"[UberClient::EstimatesService::GetEstimates] Adding (EstimateCache) to the cache... \n{estimateCache}");

                    await _cache.SetAsync(estimateResponseId, estimateCache, options);

                    _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimates] Sending (EstimateModel) back to caller...");

                    await responseStream.WriteAsync(estimateModel);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }
        
        public override async Task<EstimateModel> GetEstimateRefresh(GetEstimateRefreshRequest request, ServerCallContext context)
        {
            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"];
            string clientId = "al0I63Gjwk3Wsmhq_EL8_HxB8qWlO7yY";

            DistributedCacheEntryOptions options = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24), SlidingExpiration = TimeSpan.FromHours(5) };

            var estimateCache = await _cache.GetAsync<EstimateCache>(request.EstimateId.ToString());

            if(estimateCache is null) { _logger.LogError($"[LyftClient::EstimatesService::GetEstimateRefresh] Failed to get (EstimateCache) from cache"); }

            var estimateInstance = estimateCache!.GetEstimatesRequest;
            var estimateResponseId = estimateCache.ProductId.ToString();

            ServiceLinker.ServiceIDs.TryGetValue(estimateResponseId.ToUpper(), out string? serviceName);

            _apiClient.Configuration = new LyftAPI.Client.Client.Configuration { AccessToken = await _accessToken!.GetAccessTokenAsync(SessionToken!, serviceID!) };
    
            _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimateRefresh] Requesting data from the MockAPI...");

            var estimateResponse = await _apiClient.EstimateAsync(estimateInstance!.StartPoint.Latitude, estimateInstance.StartPoint.Longitude, serviceName, estimateInstance.EndPoint.Latitude, estimateInstance.EndPoint.Longitude);

            _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimateRefresh] Received (CostEstimateResponse) from MockAPI...");

            var estimateModel = new EstimateModel()
            {
                EstimateId = estimateResponseId,
                CreatedTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now.ToUniversalTime()),
                InvalidTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now.AddMinutes(5).ToUniversalTime()),
                PriceDetails = new CurrencyModel
                {
                    Price = estimateResponse.CostEstimates[0].EstimatedCostCentsMax,
                    Currency = estimateResponse.CostEstimates[0].Currency
                },
                Distance = (int)estimateResponse.CostEstimates[0].EstimatedDistanceMiles,
                Seats = estimateInstance.Seats,
                RequestUrl = $"https://lyft.mock/client_id={clientId}&action=setPickup&pickup[latitude]={estimateInstance.StartPoint.Latitude}&pickup[longitude]={estimateInstance.StartPoint.Longitude}&dropoff[latitude]={estimateInstance.EndPoint.Latitude}&dropoff[longitude]={estimateInstance.EndPoint.Longitude}&product_id={serviceID}",
                DisplayName = estimateResponse.CostEstimates[0].DisplayName,
                WayPoints = { { estimateInstance.StartPoint }, { estimateInstance.EndPoint }, }
            };

            var cacheInstance = new EstimateCache()
            {
                Cost = new Cost((int)estimateModel.PriceDetails.Price, estimateModel.PriceDetails.Currency.ToString(), "Estimate price details"),
                GetEstimatesRequest = estimateInstance,
                ProductId = Guid.Parse(estimateResponseId)
            };

            _logger.LogInformation($"[UberClient::EstimatesService::GetEstimateRefresh] Adding (EstimateCache) to the cache... \n{estimateCache}");

            await _cache.SetAsync(estimateResponseId, cacheInstance, options);

            _logger.LogInformation($"[LyftClient::EstimatesService::GetEstimateRefresh] Sending (EstimateModel) back to caller...");

            return estimateModel;
        }
    }
}
