using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IWhatsappRepository
    {
        Task<bool> SaveSentWhatsappMessageDetails(SentWhatsAppMessageDetails model);
    }
}
