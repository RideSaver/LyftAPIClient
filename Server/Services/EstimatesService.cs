using Grpc.Core;
using InternalAPI;
using LyftClient.Interface;
using LyftClient.Extensions;
using LyftApiClient.Server.Models;
using LyftApiClient.Server.Extensions;
using LyftAPI.Client.Model;
using Microsoft.Extensions.Caching.Distributed;

using PublicApi = LyftAPI.Client.Api.PublicApi;
using APIConfig = LyftAPI.Client.Client.Configuration;

namespace LyftClient.Services
{
    // Summary: Handles all requests for estimates
    public class EstimatesService : Estimates.EstimatesBase
    {
        private readonly IDistributedCache _cache;
        private readonly IAccessTokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly PublicApi _apiClient;
        private readonly ILogger<EstimatesService> _logger;

        public EstimatesService(ILogger<EstimatesService> logger, IDistributedCache cache, IAccessTokenService accessToken, IHttpContextAccessor httpContextAccessor)
        {
            _apiClient = new PublicApi();
            _tokenService = accessToken;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _cache = cache;
        } 

        public override async Task GetEstimates(GetEstimatesRequest request, IServerStreamWriter<EstimateModel> responseStream, ServerCallContext context)
        {
            // ClientID recieved from the MockAPIs for authentication.
            string clientId = "al0I63Gjwk3Wsmhq_EL8_HxB8qWlO7yY";

            // Extract the JWT token from the request headers for the current-user.
            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"]; 
            if(SessionToken is null) { throw new ArgumentNullException(nameof(SessionToken)); }

            // Configure the redis-cache options.
            var redisOptions = new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24), SlidingExpiration = TimeSpan.FromHours(5) };

            // Iterate through the list of serviceIDs recieved in the request
            var servicesList = request.Services.ToList();
            foreach (var service in servicesList)
            {
                // Attempt to match the serviceID to a service name through the ServiceLinker.
                ServiceLinker.ServiceIDs.TryGetValue(service.ToUpper(), out string? serviceName);
                if (serviceName is null) continue;

                // Retrieve the user-access-token for from IdentityService for the current-user.
                _apiClient.Configuration = new APIConfig { AccessToken = await _tokenService!.GetAccessTokenAsync(SessionToken!, service!) };
                if(_apiClient.Configuration.AccessToken is null) { throw new ArgumentNullException(nameof(_apiClient.Configuration.AccessToken)); }

                // Request the EstimateInfo from the MockAPI.
                var estimateResponse = await _apiClient.EstimateAsync(request.StartPoint.Latitude, request.StartPoint.Longitude, serviceName, request.EndPoint.Latitude, request.EndPoint.Longitude);
                if(estimateResponse is null) { throw new ArgumentNullException(nameof(estimateResponse)); }

                // Generate the internal serviceID for the current estimate request.
                var internalServiceID = DataAccess.Services.ServiceID.CreateServiceID(service).ToString();

                // Iterate through the list of estimates recieved from the MockAPI
                foreach(var estimate in estimateResponse.CostEstimates)
                {
                    var estimateModel = new EstimateModel()
                    {
                        EstimateId = internalServiceID,
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

                    // Save the current Estimate instance to the cache & return the estimate-info to EstimatesAPI.
                    await _cache.SetAsync(internalServiceID, estimateCache, redisOptions);
                    await responseStream.WriteAsync(estimateModel);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }

        public override async Task<EstimateModel> GetEstimateRefresh(GetEstimateRefreshRequest request, ServerCallContext context)
        {
            // ClientID recieved from the MockAPIs for authentication.
            string clientId = "al0I63Gjwk3Wsmhq_EL8_HxB8qWlO7yY";

            // Extract the JWT token from the request headers for the current-user.
            var SessionToken = "" + _httpContextAccessor.HttpContext!.Request.Headers["token"];
            if (SessionToken is null) { throw new ArgumentNullException(nameof(SessionToken)); }

            // Get the EstimateID used as key for storage within the cache.
            var internalServiceID = request.EstimateId.ToString();
            if (internalServiceID is null) { throw new ArgumentNullException(nameof(internalServiceID)); }

            // Extract the EstimateInstance from the cache.
            var estimateCache = await _cache.GetAsync<EstimateCache>(internalServiceID); // Extract the EstimateInstance from the cache
            if(estimateCache is null) { throw new ArgumentNullException(nameof(estimateCache)); }

            var estimateInstance = estimateCache!.GetEstimatesRequest;  // Estimate Instance
            var serviceID = estimateCache.ProductId.ToString(); // Service ID

            // Match the current serviceID with a valid Service name through the service linker.
            ServiceLinker.ServiceIDs.TryGetValue(serviceID.ToUpper(), out string? serviceName);
            if(serviceName is null) { throw new ArgumentNullException(nameof(serviceName)); }

            // Configure the redis-cache options.
            var redisOptions = new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24), SlidingExpiration = TimeSpan.FromHours(5) };

            // Retrieve the user-access-token from IdentityService for the current user.
            _apiClient.Configuration = new APIConfig { AccessToken = await _tokenService!.GetAccessTokenAsync(SessionToken!, serviceID) };
            if (_apiClient.Configuration.AccessToken is null) { throw new ArgumentNullException(nameof(_apiClient.Configuration.AccessToken)); }

            // Make the EstimateRefresh request from the MockAPI.
            var estimateResponse = await _apiClient.EstimateAsync(estimateInstance!.StartPoint.Latitude, estimateInstance.StartPoint.Longitude, serviceName, estimateInstance.EndPoint.Latitude, estimateInstance.EndPoint.Longitude);
            if (estimateResponse is null) { throw new ArgumentNullException(nameof(estimateResponse)); }

            // Create an EstimateModel to be sent back to the EstimatesAPI
            var estimateModel = new EstimateModel()
            {
                EstimateId = internalServiceID,
                CreatedTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now.ToUniversalTime()),
                InvalidTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now.AddMinutes(5).ToUniversalTime()),
                Distance = (int)estimateResponse.CostEstimates[0].EstimatedDistanceMiles,
                Seats = estimateInstance.Seats,
                DisplayName = estimateResponse.CostEstimates[0].DisplayName,
                WayPoints = { { estimateInstance.StartPoint }, { estimateInstance.EndPoint }, },
                RequestUrl = $"https://lyft.mock/client_id={clientId}&action=setPickup&pickup[latitude]={estimateInstance.StartPoint.Latitude}&pickup[longitude]={estimateInstance.StartPoint.Longitude}&dropoff[latitude]={estimateInstance.EndPoint.Latitude}&dropoff[longitude]={estimateInstance.EndPoint.Longitude}&product_id={serviceID}",
                PriceDetails = new CurrencyModel
                {
                    Price = estimateResponse.CostEstimates[0].EstimatedCostCentsMax / 100,
                    Currency = estimateResponse.CostEstimates[0].Currency
                },
            };

            // Save the EstimateInstance back into the cache.
            var cacheInstance = new EstimateCache()
            {
                Cost = new Cost((int)estimateModel.PriceDetails.Price, estimateModel.PriceDetails.Currency.ToString(), "ETA Cost Breakdown"),
                GetEstimatesRequest = estimateInstance,
                ProductId = Guid.Parse(serviceID)
            };

            // Save the EstimateInstance back into the cache & return the estimate to EstimatesAPI.
            await _cache.SetAsync(internalServiceID, cacheInstance, redisOptions);
            return estimateModel;
        }
    }
}
