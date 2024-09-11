using System;

namespace Risebird.Entities.Data
{
    public class InterviewerAvailabilityVM
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public DateTime AvailabilityDate { get; set; }
        public bool Hour_00_TO_01 { get; set; }
        public bool Hour_01_TO_02 { get; set; }
        public bool Hour_02_TO_03 { get; set; }
        public bool Hour_03_TO_04 { get; set; }
        public bool Hour_04_TO_05 { get; set; }
        public bool Hour_05_TO_06 { get; set; }
        public bool Hour_06_TO_07 { get; set; }
        public bool Hour_07_TO_08 { get; set; }
        public bool Hour_08_TO_09 { get; set; }
        public bool Hour_09_TO_10 { get; set; }
        public bool Hour_10_TO_11 { get; set; }
        public bool Hour_11_TO_12 { get; set; }
        public bool Hour_12_TO_13 { get; set; }
        public bool Hour_13_TO_14 { get; set; }
        public bool Hour_14_TO_15 { get; set; }
        public bool Hour_15_TO_16 { get; set; }
        public bool Hour_16_TO_17 { get; set; }
        public bool Hour_17_TO_18 { get; set; }
        public bool Hour_18_TO_19 { get; set; }
        public bool Hour_19_TO_20 { get; set; }
        public bool Hour_20_TO_21 { get; set; }
        public bool Hour_21_TO_22 { get; set; }
        public bool Hour_22_TO_23 { get; set; }
        public bool Hour_23_TO_00 { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedBy { get; set; }
    }
}
