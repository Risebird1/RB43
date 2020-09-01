using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum SentimentType
    {
        [Description("Anger")]
        Anger = 1,
        [Description("Contempt")]
        Contempt = 2,
        [Description("Disgust")]
        Disgust = 3,
        [Description("Fear")]
        Fear = 4,
        [Description("Happiness")]
        Happiness = 5,
        [Description("Neutral")]
        Neutral = 6,
        [Description("Sadness")]
        Sadness = 7,
        [Description("Surprise")]
        Surprise = 8
    }
}
