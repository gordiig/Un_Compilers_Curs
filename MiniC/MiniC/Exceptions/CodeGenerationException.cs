using System;

namespace MiniC.Exceptions
{
    public class CodeGenerationException: Exception
    {
        private static string errorPrefix = "Code generation error\n";
        
        public CodeGenerationException()
        {
        }

        public CodeGenerationException(string message) : base(errorPrefix + message)
        {
        }

        public CodeGenerationException(string message, Exception innerException) : base(errorPrefix + message, innerException)
        {
        }
    }
}