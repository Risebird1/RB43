namespace Risebird.Entities.ViewModel
{
    public class SaveOperationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public SaveOperationResult()
        {
            Success = false;
            ErrorMessage = string.Empty;
        }

        public SaveOperationResult(bool success, string errorMessage = "")
        {
            Success = success;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public static SaveOperationResult SuccessResult()
        {
            return new SaveOperationResult(true);
        }

        public static SaveOperationResult FailureResult(string errorMessage)
        {
            return new SaveOperationResult(false, errorMessage);
        }
    }
}
