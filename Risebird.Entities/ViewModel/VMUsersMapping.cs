using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class VMUsersMapping
    {
        public int VmId { get; set; }

        [Display(Name = "VM Name")]
        public string VmName { get; set; }
        public IList<TransVMUser> UsersList { get; set; }
        public SelectList ProfileList { get; set; }

        [Display(Name = "Profile")]
        public int ProfileId { get; set; }

    }
}
