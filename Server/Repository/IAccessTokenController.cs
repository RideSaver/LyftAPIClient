namespace LyftAPI.Client.Repository
{
    public interface IAccessTokenController
    {
        Task<string> GetAccessTokenAsync(string SessionToken, string ServiceId);
    }
}
