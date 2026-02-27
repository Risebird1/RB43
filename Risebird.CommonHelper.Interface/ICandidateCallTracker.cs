namespace Risebird.CommonHelper.Interface
{
    /// <summary>
    /// Tracks Vapi call attempts per candidate.
    /// Kept in CommonHelper.Interface so VapiService can depend on it
    /// without taking a direct reference to the Repository layer.
    /// </summary>
    public interface ICandidateCallTracker
    {
        /// <summary>
        /// Atomically increments Candidate.VapiCallAttempts for the given candidate.
        /// </summary>
        System.Threading.Tasks.Task IncrementVapiCallAttemptsAsync(int candidateId);
    }
}
