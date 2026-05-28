namespace Risebird.Entities.Data
{
    public class CapCheckResult
    {
        public bool Allowed { get; set; }
        public bool IsLocked { get; set; }
        public int Assigned { get; set; }
        public int Capacity { get; set; }

        public static CapCheckResult AllowedResult => new CapCheckResult { Allowed = true };
    }
}
