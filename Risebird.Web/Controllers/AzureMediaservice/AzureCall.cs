using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;

namespace Risebird.Web.Controllers.AzureMediaservice
{

    public class AzureCall
    {
        IConfiguration _configuration;
        public AzureCall(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string CallApi(IFormFile file, string filename, long filesize)
        {
            Reservation reservation = new Reservation();
            reservation.filename = filename;
            reservation.fileSize = filesize;
            byte[] chunk = new byte[file.Length];
            MemoryStream stream;
            using (stream = new MemoryStream())
            {
                file.CopyToAsync(stream);
                chunk = stream.ToArray();
            }
            reservation.chunk = chunk;
            //string _chunk = Convert.ToBase64String(chunk);
            var baseurl = _configuration["Azure:ApiCall"];
            string UserDetails = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes("FaceApi:FaceApi"));
            RestClient _client = new RestClient(baseurl);
            var request1 = new RestRequest("api/Azure", Method.POST) { RequestFormat = DataFormat.Json };
            request1.AddHeader("Authorization", "Basic " + UserDetails);
            request1.AddBody(reservation);
            request1.AddHeader("content-type", "application/json");

            var response = _client.Execute(request1);
            return response.Content;
        }
    }
    public class Reservation
    {
        public byte[] chunk { get; set; }
        public string filename { get; set; }
        public long fileSize { get; set; }

    }
}
