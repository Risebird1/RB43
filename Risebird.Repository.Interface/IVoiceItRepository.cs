using Risebird.Entities.Data;

namespace Risebird.Repository.Interface
{
    public interface IVoiceItRepository
    {
        VoiceItResponse CreateUser();
        string EnrollUser(string userId, string fileUrl, string phrase);
        VoiceItResponse VerifyUser(string userId, string fileUrl, string phrase);
        PhraseResponse GetPhrases();
    }
}
