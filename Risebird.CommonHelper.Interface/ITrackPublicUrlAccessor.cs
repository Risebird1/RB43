namespace Risebird.CommonHelper.Interface
{
    public interface ITrackPublicUrlAccessor
    {
        void SendTelemetry(string emailId, string requestedUrl);
    }
}
