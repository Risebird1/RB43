namespace Risebird.Business.Interface
{
    public interface IRisebirdPayService
    {
        string DeductWalletPoints(int ClientId, string emailid, int points, string comment);
    }
}
