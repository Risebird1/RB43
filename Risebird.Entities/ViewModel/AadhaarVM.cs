namespace Risebird.Entities.ViewModel
{
    public class AadhaarVM
    {
        public AadhaarVM()
        {
            headers = new AadhaarHeader();
            request = new AadhaarRequest();
        }
        public AadhaarHeader headers { get; set; }
        public AadhaarRequest request { get; set; }
    }
    public class AadhaarHeader
    {
        public string client_code { get; set; }
        public string sub_client_code { get; set; }
        public string channel_code { get; set; }
        public string channel_version { get; set; }
        public string stan { get; set; }
        public string client_ip { get; set; }
        public string transmission_datetime { get; set; }
        public string operation_mode { get; set; }
        public string run_mode { get; set; }
        public string actor_type { get; set; }
        public string user_handle_type { get; set; }
        public string user_handle_value { get; set; }
        public string location { get; set; }
        public string function_code { get; set; }
        public string function_sub_code { get; set; }
    }
    public class AadhaarRequest
    {
        public string api_key { get; set; }
        public string request_id { get; set; }
        public string hash { get; set; }
        public string uuid { get; set; }
        public string aadhaar { get; set; }
        public string captcha { get; set; }
        public string verification_type { get; set; }
        public string consent { get; set; }
        public string otp { get; set; }
        public string share_code { get; set; }
    }
    public class AadhaarResponse
    {
        public AadhaarData response_data { get; set; }
        public AadhaarStatus response_status { get; set; }
    }
    public class AadhaarData
    {
        public string captcha { get; set; }
        public string uuid { get; set; }
        public string message { get; set; }
        public string doc_type { get; set; }
        public string document_id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string gender { get; set; }
        public string dob { get; set; }
        public string doc_face { get; set; }
        public string father_name { get; set; }
        public string mother_name { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string verified_by { get; set; }
        public string verified_using { get; set; }
        public string reference_id { get; set; }
        public string status { get; set; }
    }
    public class AadhaarStatus
    {
        public string code { get; set; }
        public string message { get; set; }
        public string status { get; set; }
    }
    public class AadhaarDetails
    {
        public string EmailId { get; set; }
        public bool AadhaarVerified { get; set; }
    }
    public class candidateVerification
    {
        public candidateVerification()
        {
            document = new verifydocument();
            phone = new verifyphone();
        }
        public string reference { get; set; }
        public string callback_url { get; set; }
        public string email { get; set; }
        public string country { get; set; }
        public string language { get; set; }
        public face face { get; set; }
        public string verification_mode { get; set; }
        public verifydocument document { get; set; }
        public verifyphone phone { get; set; }
    }
    public class face
    {
        public string proof { get; set; }
    }
    public class verifyphone
    {
        public string phone_number { get; set; }
        public string random_code { get; set; }
        public string text { get; set; }
    }
    public class verifydocument
    {
        public verifydocument()
        {
            name = new verifyname();
        }
        public string proof { get; set; }
        public string[] supported_types { get; set; }
        public verifyname name { get; set; }
    }
    public class verifyname
    {
        public string first_name { get; set; }
        public string middle_name { get; set; }
        public string last_name { get; set; }
    }
    public class verifyResponse
    {
        public string verification_url { get; set; }
        public string reference { get; set; }
    }
}
