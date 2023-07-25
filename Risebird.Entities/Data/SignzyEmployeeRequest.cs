using Risebird.Entities.Enum;
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
        public string CandidateEmail { get; set; }
    }
    public class SignzyEmployeeResponse
    {
        public SignzyEmployeeResponse()
        {
            uanDetails1 = new List<UanDetails>();
        }
        public EmploymentSummary employmentSummary { get; set; }
        public List<UanDetails> uanDetails1 { get; set; }
        public string[] uanNumbers { get; set; }
        public string CandidateEmail { get; set; }
        public string MobileNumber { get; set; }
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
        public string uanNumber { get; set; }
        public string name { get; set; }
        public string gender { get; set; }
        public string date_of_birth { get; set; }
        public string establishment_name { get; set; }
        public string date_of_joining { get; set; }
    }
    public class EmploymentDetailsDM
    {
        public int id { get; set; }
        public string MobileNumber { get; set; }
        public string Name { get; set; }
        public string CompanyName { get; set; }
        public string CandidateEmail { get; set; }
        public string UAN { get; set; }
        public DateTime JoiningDate { get; set; }
        public DateTime ExitDate { get; set; }
        public int TotalUAN { get; set; }
        public bool Isemployed { get; set; }
        public string UANNumbers { get; set; }
        public DateTime DOB { get; set; }
    }
    public class UANDetailsDM
    {
        public int id { get; set; }
        public int EmploymentId { get; set; }
        public string UAN { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public DateTime DOB { get; set; }
        public string CompanyName { get; set; }
        public DateTime JoiningDate { get; set; }
    }
}
