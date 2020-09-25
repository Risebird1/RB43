using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.CommonHelper.Interface
{
    public interface IAzureHelper
    {
        AzureCredentials GetAzureCredential();
    }
}
