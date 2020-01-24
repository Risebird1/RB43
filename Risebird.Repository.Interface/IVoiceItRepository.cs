using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Repository.Interface
{
    public interface IVoiceItRepository
    {
        VoiceItResponse CreateUser();
        string EnrollUser(string userId, string fileUrl, string phrase);
    }
}
