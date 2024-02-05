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
        public string mobileNumber { get; set; }
        public string panNumber { get; set; }
        public string CandidateEmail { get; set; }
    }
    public class SignzyEmployeeResponse
    {
        public SignzyEmployeeResponse()
        {
            uanDetails1 = new List<UanDetails>();
        }
        public EmploymentSummary summary { get; set; }
        //public EmploymentSummary employmentSummary { get; set; }
        public List<UanDetails> uanDetails1 { get; set; }
        public string[] uanNumbers { get; set; }
        public string[] uan { get; set; }
        public string CandidateEmail { get; set; }
        public string MobileNumber { get; set; }
        public string message { get; set; }
        public SignzyEmployeeResponseerror error { get; set; }
    }
    public class EmploymentSummary
    {
        //public Recent_employer_data recent_employer_data { get; set; }
        public Recent_employer_data recentEmployerData { get; set; }
        public bool isEmployed { get; set; }
        public int uanCount { get; set; }
    }
    public class Recent_employer_data
    {
        public string establishmentName { get; set; }
        public string matchingUan { get; set; }
        public string dateOfJoining { get; set; }
        public string dateOfExit { get; set; }
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
        public DateTime? JoiningDate { get; set; }
        public DateTime? ExitDate { get; set; }
        public int TotalUAN { get; set; }
        public bool Isemployed { get; set; }
        public string UANNumbers { get; set; }
        public DateTime? DOB { get; set; }
        public string message { get; set; }
        public bool isVerified { get; set; }
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
    public class SignzyEmployeeResponseerror
    {
        public string message { get; set; }
    }
    public class SignzyEVResponseResult
    {
        public SignzyEVResponse_Result result { get; set; }
    }
    public class SignzyEVResponse_Result
    {
        public SignzyEmployeeResponse result { get; set; }
    }
}
