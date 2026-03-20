using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ISupportTicketRepository
    {
        Task<int> InsertTicketAsync(SupportTicket ticket);

        Task<List<SupportTicketVM>> GetTicketsAsync(
            string   status      = null,
            string   intent      = null,
            string   senderEmail = null,
            DateTime? dateFrom   = null,
            DateTime? dateTo     = null,
            int      page        = 1,
            int      pageSize    = 25);
    }
}
