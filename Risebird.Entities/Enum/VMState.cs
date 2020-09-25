using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum VMState
    {
        [Description("deallocated")]
        deallocated = 0,
        [Description("stopped")]
        stopped = 1,
        [Description("started")]
        started = 2,
        [Description("unknown")]
        unknown = 3,
        [Description("queued")]
        queued = 4,
        [Description("processing")]
        processing = 5,
        [Description("provisioning")]
        provisioning = 6,
        [Description("running")]
        running = 7,
        [Description("failed")]
        failed = 8,
        [Description("deallocating")]
        deallocating = 9,
        [Description("creating")]
        creating = 10,
        [Description("updating")]
        updating = 11,
        [Description("starting")]
        starting = 12,
        [Description("stopping")]
        stopping = 13,
        [Description("deleting")]
        deleting = 14,
        [Description("provisioning failed")]
        provisioningfailed = 15,
        [Description("checking")]
        checking = 16,
        [Description("succeeded")]
        succeeded = 17,
        [Description("removing")]
        removing = 18,
        [Description("restarting")]
        restarting = 19
    }
}
