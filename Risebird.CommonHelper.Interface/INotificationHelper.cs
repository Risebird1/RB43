using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface INotificationHelper
    {
        string SendHelpsupportMail(Mail model);
    }
}
