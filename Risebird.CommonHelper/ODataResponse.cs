using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class ODataResponse
    {
        protected B2CClientService _client;

        [JsonProperty("odata.error")]
        public ODataErrorDto Error { get; set; }

        public static ODataResponse Parse(string responseText)
        {
            var checkForError = JsonConvert.DeserializeObject<ODataResponse>(responseText);
            return checkForError;
        }
    }

    public class ODataResponseDto<T> : ODataResponse
    {

        public static ODataResponseDto<T> Parse(string responseText)
        {
            var checkForError = JsonConvert.DeserializeObject<ODataResponseDto<T>>(responseText);
            if (checkForError?.Error == null)
            {
                return new ODataResponseDto<T>
                {
                    Value = JsonConvert.DeserializeObject<T>(responseText)
                };
            }
            return checkForError;
        }

        [JsonProperty("value")]
        public T Value { get; set; }


        [JsonProperty("odata.nextLink")]
        public string NextLink { get; set; }

        internal void PreparePaging(B2CClientService client)
        {
            _client = client;
        }

        public async Task<ODataResponseDto<T>> GetNextPage()
        {
            if (!string.IsNullOrEmpty(NextLink))
            {
                var path = $"/{NextLink.Substring(0, NextLink.IndexOf("?"))}";
                var query = NextLink.Substring(NextLink.IndexOf("?") + 1);
                return await _client.List<T>(path, query);
            }

            return null;
        }
    }

    public class ODataErrorDto
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public ODataErrorMessageDto Message { get; set; }

        [JsonProperty("requestId")]
        public Guid RequestId { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("values")]
        public IEnumerable<ODataErrorValueDto> Values { get; set; }
    }

    public class ODataErrorMessageDto
    {
        [JsonProperty("lang")]
        public string Lang { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class ODataErrorValueDto
    {
        [JsonProperty("item")]
        public string Item { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
