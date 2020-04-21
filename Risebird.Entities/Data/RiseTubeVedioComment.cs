using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations.Schema;

namespace Risebird.Entities.Data
{
    class RiseTubeVedioComment
    {
        public int id { get; set; }
        public int userId { get; set; }

        public int interviewId { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; }

    }
}
