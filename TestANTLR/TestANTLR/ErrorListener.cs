using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestANTLR
{
    public class ErrorListener : BaseErrorListener
    {
        public static readonly ErrorListener Instance = new ErrorListener();

        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            throw new Exception("line " + line + ":" + charPositionInLine + " " + msg);
        }
    }
}
