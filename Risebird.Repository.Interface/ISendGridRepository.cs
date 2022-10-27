using Risebird.Entities.Data;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ISendGridRepository
    {
        Task<bool> SaveSendGridEventDetails(SendgridEventData model);
        Task<bool> SaveSentMailDetails(SentMailDetails model);
    }
}
