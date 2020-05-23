using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class Mail
    {
        public string From { get; set; }
        public string TO { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string CC { get; set; }
    }
}
