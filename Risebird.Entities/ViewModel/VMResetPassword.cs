using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class VMResetPassword
    {
        public string subscriptionName { get; set; }
        public string ResourceGroupName { get; set; }
        [DisplayName("VM Name")]

        public string VmName { get; set; }
        [Required]
        [DisplayName("User Name")]
        public string userName { get; set; }
        [Required]
        [RegularExpression("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$", ErrorMessage = "Password must be eight characters including one uppercase & lowercase letter, one special character, one digit.")]
        public string Password { get; set; }
        [Required]
        [Compare("Password", ErrorMessage = "Password and Confirmation Password must match.")]
        [DisplayName("Confirm Password")]
        public string confirmPassword { get; set; }

    }
}
