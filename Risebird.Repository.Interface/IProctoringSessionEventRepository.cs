using Risebird.Entities.Data;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IProctoringSessionEventRepository
    {
        /// <summary>Inserts one row; returns identity or 0 on failure.</summary>
        Task<long> InsertAsync(ProctoringSessionEvent row);
    }
}
