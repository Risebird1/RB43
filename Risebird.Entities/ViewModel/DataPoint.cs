using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Risebird.Entities.ViewModel
{

    [DataContract]
    public class DataPoint
    {
        public DataPoint(string label, double y,string color)
        {
            this.Label = label;
            this.Y = y;
            this.Color = color;
        }

        //Explicitly setting the name to be used while serializing to JSON.
        [DataMember(Name = "label")]
        public string Label = "";

        //Explicitly setting the name to be used while serializing to JSON.
        [DataMember(Name = "y")]
        public Nullable<double> Y = null;

        [DataMember(Name = "color")]
        public string Color = "";
    }
}
