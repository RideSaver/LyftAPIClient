namespace LyftClient.Services
{
    // Summary: Stores all services with relation to their internal id
    public class ServiceIDs
    {
        public static readonly Dictionary<string, string> serviceIDs = new Dictionary<string, string> {
            {"2B2225AD-9D0E-45E0-85FB-378FE2B521E0", "Lyft"},
            {"52648E86-B617-44FD-B753-295D5CE9D9DC", "LyftShared"},
            {"B47A0993-DE35-4F86-8DD8-C6462F16F5E8", "LyftLUX"},
            {"BB331ADE-E379-4F12-9AB0-A68AF94D5813", "LyftXL"}
        };
    };
}
