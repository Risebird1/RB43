using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Data
{
    public class Client
    {
        public int id { get; set; }
      
        [Display(Name = "Client Name")]
        [Required(ErrorMessage = "Please Enter Client Name")]
        public string Name { get; set; }
        [Display(Name = "Description")]
        [Required(ErrorMessage = "Please Enter Client Description")]
        public string Description { get; set; }
        [Display(Name = "GST")]
       
        public string GST { get; set; }
        [Display(Name = "OfficeAddress")]
      
        public string OfficeAddress { get; set; }
    }
}
