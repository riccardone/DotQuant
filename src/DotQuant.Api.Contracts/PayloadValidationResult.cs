namespace DotQuant.Api.Contracts
{
    public class PayloadValidationResult 
    {
        /// <summary>
        /// Success or failure status 
        /// </summary>
        public bool IsValid { get; }
        /// <summary>
        /// Returns list of validation error messages
        /// </summary>
        public IList<string> ErrorMessages { get; private set; }

        public PayloadValidationResult(bool isValid)
        {
            IsValid = isValid;
        }

        public PayloadValidationResult(bool isValid, string errorMessage)
        {
            IsValid = isValid;
            ErrorMessages = new List<string>() { errorMessage };
        }

        public PayloadValidationResult(bool isValid, IList<string> errorMessages)
        {
            IsValid = isValid;
            ErrorMessages = errorMessages; 
        }

        /// <summary>
        /// Adds an
        /// </summary>
        /// <param name="errorMessage"></param>
        public void AddErrorMessage(string errorMessage)
        {
            ErrorMessages.Add(errorMessage);
        }
    }
}
