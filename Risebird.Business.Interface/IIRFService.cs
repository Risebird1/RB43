using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Business.Interface
{
    public interface IIRFService
    {
        bool AddIRFDetails(IRFVm irfDetails);
    }
}
