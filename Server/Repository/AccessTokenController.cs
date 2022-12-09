using Grpc.Core;
using Grpc.Net.Client;
using InternalAPI;

namespace LyftAPI.Client.Repository
{
    public class AccessTokenController : IAccessTokenController
    {
        public AccessTokenController()
        {
            _client = new Users.UsersClient(GrpcChannel.ForAddress($"https://users.api:7042"));
        }

        async public Task<string> GetAccessTokenAsync(string SessionToken, string ServiceId)
        {
            var headers = new Metadata();
            headers.Add("Authorization", $"Bearer {SessionToken}");
            
            var AccessTokenResponse = await _client.GetUserAccessTokenAsync(new GetUserAccessTokenRequest {
                ServiceId = ServiceId,
            }, headers);
            return AccessTokenResponse.AccessToken;
        }
        private Users.UsersClient _client { get; set; }
    }
}