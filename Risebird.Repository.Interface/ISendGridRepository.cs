using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ISendGridRepository
    {
        Task<bool> SaveSendGridEventDetails(SendgridEventData model);
    }
}
