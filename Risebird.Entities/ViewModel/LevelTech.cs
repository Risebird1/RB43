using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class LevelTech
    {
        public int id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
    public class ExperienceRange
    {
        public int id { get; set; }
        public string Description { get; set; }
    }
    public class JobFunction
    {
        public int id { get; set; }
        public string Description { get; set; }
    }
    public class Industry
    {
        public int id { get; set; }
        public string Description { get; set; }
    }
}
