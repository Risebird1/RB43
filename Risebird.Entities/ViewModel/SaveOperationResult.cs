namespace Risebird.Entities.ViewModel
{
    public class SaveOperationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int? Id { get; set; }

        public SaveOperationResult()
        {
            Success = false;
            ErrorMessage = string.Empty;
        }

        public SaveOperationResult(bool success, string errorMessage = "", int? id = null)
        {
            Success = success;
            ErrorMessage = errorMessage ?? string.Empty;
            Id = id;
        }

        public static SaveOperationResult SuccessResult(int? id = null)
        {
            return new SaveOperationResult(true, id: id);
        }

        public static SaveOperationResult FailureResult(string errorMessage)
        {
            return new SaveOperationResult(false, errorMessage);
        }
    }
}
