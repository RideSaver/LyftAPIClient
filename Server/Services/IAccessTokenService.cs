namespace LyftClient.Services
{
    public interface IAccessTokenService
    {
        Task<string> GetAccessTokenAsync(string SessionToken, string ServiceId);
    }
}
