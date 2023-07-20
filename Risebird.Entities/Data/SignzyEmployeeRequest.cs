using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class SignzyEmployeeRequest
    {
        public string mobile { get; set; }
        public string panNumber { get; set; }
    }
    public class SignzyEmployeeResponse
    {
        public EmploymentSummary employmentSummary { get; set; }
        public UanDetails uanDetails { get; set; }
        public string[] uanNumbers { get; set; }
    }
    public class EmploymentSummary
    {
        public Recent_employer_data recent_employer_data { get; set; }
        public bool is_employed { get; set; }
        public int uan_count { get; set; }
    }
    public class Recent_employer_data
    {
        public string establishment_name { get; set; }
        public string matching_uan { get; set; }
        public string date_of_joining { get; set; }
        public string date_of_exit { get; set; }
    }
    public class UanDetails
    {

    }
}
