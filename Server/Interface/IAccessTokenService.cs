namespace LyftClient.Interface
{
    public interface IAccessTokenService
    {
        Task<string> GetAccessTokenAsync(string SessionToken, string ServiceId);
    }
}
