namespace Risebird.Web.Services.Proctoring
{
    public interface IProctoringTokenService
    {
        string CreateToken(string email, int interviewId, string aiSessionId);
        ProctoringTokenPayload? ValidateToken(string token);
    }
}
