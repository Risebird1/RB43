using Microsoft.AspNetCore.Http;
using System;

namespace Risebird.Entities.ViewModel
{
    public class RaiseBugsVM
    {
        public int id { get; set; }
        public string RequestType { get; set; }
        public string Comment { get; set; }
        public IFormFile Attachment { get; set; }
        public string RequestedBy { get; set; }
        public DateTime RequestedOn { get; set; }
        public string AttachmentURL { get; set; }
        public string Status { get; set; }
        public string Requester { get; set; }
        public string MobileNumber { get; set; }
        public string Name { get; set; }
    }
}
