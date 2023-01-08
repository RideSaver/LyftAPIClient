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
                        Cost = new Cost()
                        {
                            Currency = estimateModel.PriceDetails.Currency.ToString(),
                            Amount = (int)estimateModel.PriceDetails.Price,
                            Description = "Estimate price details",
                        },
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
        
        public override Task<EstimateModel> GetEstimateRefresh(GetEstimateRefreshRequest request, ServerCallContext context)
        {
            throw new NotImplementedException();
        }
    }
}
