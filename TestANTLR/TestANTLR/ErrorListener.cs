using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
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

    public class ErrorListenerLex : IAntlrErrorListener<int>
    {
        public static readonly ErrorListenerLex Instance = new ErrorListenerLex();

        public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] int offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
        {
            throw new Exception("line " + line + ":" + charPositionInLine + " " + msg);
        }
    }
}
