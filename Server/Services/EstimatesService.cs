using Grpc.Core;
using InternalAPI;
using LyftAPI.Client.Model;
using Microsoft.Extensions.Caching.Distributed;
using LyftApiClient.Server.Models;
using LyftApiClient.Server.Extensions;
using LyftClient.Interface;
using LyftClient.Extensions;

using PublicApi = LyftAPI.Client.Api.PublicApi;
using APIConfig = LyftAPI.Client.Client.Configuration;

namespace LyftClient.Services
{
    // Summary: Handles all requests for estimates
    public class EstimatesService : Estimates.EstimatesBase
    {
        private readonly IDistributedCache _cache;
        private readonly IAccessTokenService _accessToken;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly PublicApi _apiClient;
        private readonly ILogger<EstimatesService> _logger;

        public EstimatesService(ILogger<EstimatesService> logger, IDistributedCache cache, IAccessTokenService accessToken, IHttpContextAccessor httpContextAccessor)
        {
            _accessToken = accessToken;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _cache = cache;

            _apiClient = new PublicApi();
        } 

        public override async Task GetEstimates(GetEstimatesRequest request, IServerStreamWriter<EstimateModel> responseStream, ServerCallContext context)
        {
            string clientId = "al0I63Gjwk3Wsmhq_EL8_HxB8qWlO7yY"; // Mimick the clientID recieved from the MockAPI

            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"]; // Extract the JWT token from the request headers.
            if(SessionToken is null) { throw new ArgumentNullException(nameof(SessionToken)); }

            DistributedCacheEntryOptions options = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24), SlidingExpiration = TimeSpan.FromHours(5) };

            // Iterate through the list of serviceIDs recieved in the request
            var servicesList = request.Services.ToList();
            foreach (var service in servicesList)
            {
                ServiceLinker.ServiceIDs.TryGetValue(service.ToUpper(), out string? serviceName);
                if (serviceName is null) continue;

                _apiClient.Configuration = new APIConfig { AccessToken = await _accessToken!.GetAccessTokenAsync(SessionToken!, service!) };
   
                var estimateResponse = await _apiClient.EstimateAsync(request.StartPoint.Latitude, request.StartPoint.Longitude, serviceName, request.EndPoint.Latitude, request.EndPoint.Longitude);
                if(estimateResponse is null) { throw new ArgumentNullException(nameof(estimateResponse)); }
                var estimateID = DataAccess.Services.ServiceID.CreateServiceID(service).ToString();

                // Iterate through the list of estimates recieved from the MockAPI
                foreach(var estimate in estimateResponse.CostEstimates)
                {
                    var estimateModel = new EstimateModel()
                    {
                        EstimateId = estimateID,
                        CreatedTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now.ToUniversalTime()),
                        InvalidTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now.AddMinutes(5).ToUniversalTime()),
                        Distance = (int)estimate.EstimatedDistanceMiles,
                        Seats = request.Seats,
                        DisplayName = estimate.DisplayName,
                        WayPoints = { { request.StartPoint }, { request.EndPoint }, },
                        RequestUrl = $"https://lyft.mock/client_id={clientId}&action=setPickup&pickup[latitude]={request.StartPoint.Latitude}&pickup[longitude]={request.StartPoint.Longitude}&dropoff[latitude]={request.EndPoint.Latitude}&dropoff[longitude]={request.EndPoint.Longitude}&product_id={service}",
                        PriceDetails = new CurrencyModel
                        {
                            Price = estimate.EstimatedCostCentsMax,
                            Currency = estimate.Currency
                        }
                    };

                    // Create a new instance of EstimateCache to be added to the RedisCache.
                    var estimateCache = new EstimateCache()
                    {
                        Cost = new Cost((int)estimateModel.PriceDetails.Price, estimateModel.PriceDetails.Currency.ToString(), "RideEstimate Cost Breakdown"),
                        GetEstimatesRequest = request,  // Original Request
                        ProductId = Guid.Parse(service) // ServiceID 
                    };

                    await _cache.SetAsync(estimateID, estimateCache, options);
                    await responseStream.WriteAsync(estimateModel);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }

        public override async Task<EstimateModel> GetEstimateRefresh(GetEstimateRefreshRequest request, ServerCallContext context)
        {
            string clientId = "al0I63Gjwk3Wsmhq_EL8_HxB8qWlO7yY";

            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"];
            if (SessionToken is null) { throw new ArgumentNullException(nameof(SessionToken)); }

            var estimateCache = await _cache.GetAsync<EstimateCache>(request.EstimateId.ToString()); // Extract the EstimateInstance from the cache
            if(estimateCache is null) { throw new ArgumentNullException(nameof(estimateCache)); }

            var estimateInstance = estimateCache!.GetEstimatesRequest;  // Estimate Instance
            var estimateID = request.EstimateId.ToString(); // EstimateInstance Generated Service ID
            var serviceID = estimateCache.ProductId.ToString(); // RideType Service ID

            ServiceLinker.ServiceIDs.TryGetValue(serviceID.ToUpper(), out string? serviceName);
            if(serviceName is null) { throw new ArgumentNullException(nameof(serviceName)); }

            DistributedCacheEntryOptions options = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24), SlidingExpiration = TimeSpan.FromHours(5) };
            _apiClient.Configuration = new APIConfig { AccessToken = await _accessToken!.GetAccessTokenAsync(SessionToken!, serviceID) };

            var estimateResponse = await _apiClient.EstimateAsync(estimateInstance!.StartPoint.Latitude, estimateInstance.StartPoint.Longitude, serviceName, estimateInstance.EndPoint.Latitude, estimateInstance.EndPoint.Longitude);
            if (estimateResponse is null) { throw new ArgumentNullException(nameof(estimateResponse)); }

            // Create an EstimateModel to be sent back to the EstimatesAPI
            var estimateModel = new EstimateModel()
            {
                EstimateId = estimateID,
                CreatedTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now.ToUniversalTime()),
                InvalidTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now.AddMinutes(5).ToUniversalTime()),
                Distance = (int)estimateResponse.CostEstimates[0].EstimatedDistanceMiles,
                Seats = estimateInstance.Seats,
                DisplayName = estimateResponse.CostEstimates[0].DisplayName,
                WayPoints = { { estimateInstance.StartPoint }, { estimateInstance.EndPoint }, },
                RequestUrl = $"https://lyft.mock/client_id={clientId}&action=setPickup&pickup[latitude]={estimateInstance.StartPoint.Latitude}&pickup[longitude]={estimateInstance.StartPoint.Longitude}&dropoff[latitude]={estimateInstance.EndPoint.Latitude}&dropoff[longitude]={estimateInstance.EndPoint.Longitude}&product_id={estimateResponseId}",
                PriceDetails = new CurrencyModel
                {
                    Price = estimateResponse.CostEstimates[0].EstimatedCostCentsMax,
                    Currency = estimateResponse.CostEstimates[0].Currency
                },
            };

            // Save the EstimateInstance back into the cache.
            var cacheInstance = new EstimateCache()
            {
                Cost = new Cost((int)estimateModel.PriceDetails.Price, estimateModel.PriceDetails.Currency.ToString(), "RideEstimate Cost Breakdown"),
                GetEstimatesRequest = estimateInstance,
                ProductId = Guid.Parse(serviceID)
            };

            await _cache.SetAsync(estimateID, cacheInstance, options);
            return estimateModel;
        }
    }
}
