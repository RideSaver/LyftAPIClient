using Grpc.Core;
using InternalAPI;
using LyftAPI.Client.Model;
using LyftClient.HTTPClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace LyftClient.Services
{
    public class RequestsService : Requests.RequestsBase // TBA
    {
        // Summary: our logging object, used for diagnostic logs.
        private readonly ILogger<RequestsService> _logger;
        // Summary: our API client, so we only open up some ports, rather than swamping the system.
        private readonly IHttpClientInstance _httpClient;

        // Summary: Our cache object
        private readonly IDistributedCache _cache;

        // Summary: our Lyft API client
        private readonly LyftAPI.Client.Api.PublicApi _apiClient;

        public RequestsService(ILogger<RequestsService> logger, IDistributedCache cache, IHttpClientInstance httpClient)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
            _apiClient = new LyftAPI.Client.Api.PublicApi(httpClient.APIClientInstance, new LyftAPI.Client.Client.Configuration {});
        }

        // Post Ride Request 
        public override async Task<RideModel> PostRideRequest(PostRideRequestModel request, ServerCallContext context)
        {
            // TODO: Add in post request 
            var postRide = new RideModel();
            return Task.FromResult(postRide);
        }

        public override async Task<RideModel> GetRideRequest(GetRideRequestModel request, IServerStreamWriter<RideModel> responseStream, ServerCallContext context)
        {
            var SessionToken = context.AuthContext.PeerIdentityPropertyName;
            _logger.LogInformation("HTTP Context User: {User}", SessionToken);
            var encodedUserID = await _cache.GetAsync(SessionToken); // TODO: Figure out if this is the correct token

            if (encodedUserID == null)
            {
                return new NotImplementedException();
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
                // Write an InternalAPI model back
                await responseStream.WriteAsync(new EstimateModel
                {
                    // TODO: populate most of this data with data from the estimate.
                    EstimateId = "NEW ID GENERATOR",
                    EstimatedTimeOfArrival = DateTime.Now,
                    PriceDetails = new CurrencyModel
                    {
                        Price = (double)estimate.CostEstimates[0].EstimatedCostCentsMax,
                        Currency = estimate.CostEstimates[0].Currency
                    },
                    WayPoints = new Location[0] {request.StartPoint, request.EndPoint},
                    VehicleDetail = new VehicleDetail
                    {
                        Model,
                        Year,
                        LicensePlate,
                        Color,
                        ImageUrl = "",
                    },
                    DisplayName = estimate.CostEstimates[0].DisplayName
                });
            }
        }

        public override Task<CurrencyModel> DeleteRideRequest(DeleteRideRequestModel request, ServerCallContext context)
        {
            var deleteRide = new CurrencyModel();
            // TBA: Invoke the web-client API to get the information from the Lyft-api, then send it to the microservice.

            return Task.FromResult(deleteRide);
        }
    }
}
