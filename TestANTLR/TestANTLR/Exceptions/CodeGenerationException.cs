using System;

namespace TestANTLR.Exceptions
{
    public class CodeGenerationException: Exception
    {
        public CodeGenerationException()
        {
        }

        public CodeGenerationException(string message) : base(message)
        {
        }

        public CodeGenerationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}