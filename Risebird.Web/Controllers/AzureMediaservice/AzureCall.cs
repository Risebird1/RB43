using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.IO;

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
            //reservation.chunk = chunk;
            int _count = 1000000;
            var NoofBlock = chunk.Length / _count;
            if (chunk.Length % _count != 0)
            {
                NoofBlock++;
            }
            //string _chunk = Convert.ToBase64String(chunk);
            // create multiple chunks to upload data 
            reservation.noofblocks = NoofBlock;
            var baseurl = _configuration["Azure:ApiCall"];
            var username = _configuration["Azure:UserName"];
            var Password = _configuration["Azure:Password"];
            for (int i = 0; i < NoofBlock; i++)
            {
                byte[] chunk1;
                if (i == (NoofBlock - 1))
                {
                    chunk1 = new byte[chunk.Length % _count];
                }
                else
                {
                    chunk1 = new byte[_count];
                }
                for (int j = 0; j < _count; j++)
                {
                    if (((i * _count) + j) < chunk.Length)
                    {
                        chunk1[j] = chunk[(i * _count) + j];
                    }
                    else
                    {
                        break;
                    }
                }
                string UserDetails = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(username + ":" + Password));
                RestClient _client = new RestClient(baseurl);
                reservation.chunk = chunk1;
                reservation.id = i + 1;
                var request1 = new RestRequest("api/Azure", Method.Post) { RequestFormat = DataFormat.Json };
                request1.AddHeader("Authorization", "Basic " + UserDetails);
                request1.AddBody(reservation);
                request1.AddHeader("content-type", "application/json");

                var response = _client.Execute(request1);
                if (response.Content.Contains("/manifest"))
                {
                    return response.Content;
                }
            }

            return "";
        }
        public string CallApiUpload(IFormFile file)
        {
            var baseurl = _configuration["Azure:ApiCall"];
            var username = _configuration["Azure:UserName"];
            var Password = _configuration["Azure:Password"];
            Reservation reservation = new Reservation();
            reservation.filename = file.FileName;
            //byte[] chunk = new byte[file.Length];
            //MemoryStream stream;
            //using (stream = new MemoryStream())
            //{
            //    file.CopyToAsync(stream);
            //    chunk = stream.ToArray();
            //}
            //reservation.chunk = chunk;
            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FileName);
            using (var stream = new FileStream(path, FileMode.Create))
            {
                file.CopyTo(stream);
            }
            reservation.filename = path;
            string UserDetails = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(username + ":" + Password));
            RestClient _client = new RestClient(baseurl);
            var request1 = new RestRequest("VideoUpload/api/Upload", Method.Post) { RequestFormat = DataFormat.Json };
            request1.AddBody(reservation);
            //request1.AddFile("files", path);
            request1.AddHeader("content-type", "application/json");
            var response = _client.Execute(request1);
            System.IO.File.Delete(path);
            return response.Content;

        }
    }
    public class Reservation
    {
        public byte[] chunk { get; set; }
        public string filename { get; set; }
        public long fileSize { get; set; }
        public int noofblocks { get; set; }
        public int id { get; set; }

    }

}
