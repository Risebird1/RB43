using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class TransJoinability
    {
        public int Id { get; set; }
        public int IRFId { get; set; }
        public int LevelId { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public int Weightage { get; set; }
    }
}
