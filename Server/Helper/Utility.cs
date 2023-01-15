using InternalAPI;
using LyftAPI.Client.Model;
using LyftClient.Extensions;

namespace LyftClient.Helper
{
    public static class Utility
    {
        public static RideTypeEnum RideTypeFromServiceID(string serviceID) // Utility: Extracts RidType from ServiceID.
        {
            ServiceLinker.ServiceIDs.TryGetValue(serviceID.ToUpper(), out string? serviceName);
            switch (serviceName)
            {
                case "lyft": return RideTypeEnum.Lyft;
                case "lyft_shared": return RideTypeEnum.LyftLine;
                case "lyft_lux": return RideTypeEnum.LyftPlus;
                case "lyft_suv": return RideTypeEnum.LyftSuv;
                default: return RideTypeEnum.Lyft;
            }
        }
        public static Location ConvertLocationModelToLocation(LocationModel locationModel) // Utility: LocationModel to Location
        {
            var location = new Location()
            {
                Lat = locationModel.Latitude,
                Lng = locationModel.Longitude,
                Address = "Exempt"
            };
            return location;
        }
        public static Stage StagefromStatus(RideStatusEnum? status) // Utility: Extracts RideStage from RideStatus
        {
            switch (status)
            {
                case RideStatusEnum.Pending: return Stage.Pending;
                case RideStatusEnum.Arrived: return Stage.Accepted;
                case RideStatusEnum.PickedUp: return Stage.Accepted;
                case RideStatusEnum.Accepted: return Stage.Accepted;
                case RideStatusEnum.Canceled: return Stage.Cancelled;
                case RideStatusEnum.DroppedOff: return Stage.Completed;
                default: return Stage.Unknown;
            }
        }
    }
}
