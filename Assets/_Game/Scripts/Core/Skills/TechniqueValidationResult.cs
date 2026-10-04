namespace Game.Core.Skills
{
    public struct TechniqueValidationResult
    {
        public TechniqueValidationError Error;
        public string Message;

        public bool IsValid => Error == TechniqueValidationError.None;

        public static TechniqueValidationResult Ok()
        {
            return new TechniqueValidationResult { Error = TechniqueValidationError.None, Message = null };
        }

        public static TechniqueValidationResult Fail(TechniqueValidationError error, string message)
        {
            return new TechniqueValidationResult { Error = error, Message = message };
        }
    }
}
