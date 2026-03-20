using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ISupportTicketRepository
    {
        Task<int> InsertTicketAsync(SupportTicket ticket);
        Task<List<SupportTicketVM>> GetAllTicketsAsync();
    }
}
