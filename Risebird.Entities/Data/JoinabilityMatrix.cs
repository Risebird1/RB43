using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
namespace Risebird.Entities.Data
{
   public class JoinabilityMatrix
    {
        public int id { get; set; }

        [Display(Name = "Name")]
        [Required(ErrorMessage = "Please Enter Joinability Matrix Name")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [Required(ErrorMessage = "Please Enter Joinability Matrix Description")]
        public string Description { get; set; }
    }
}
