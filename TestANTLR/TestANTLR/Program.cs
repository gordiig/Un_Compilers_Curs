using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.IO;
using System.Text;

namespace TestANTLR
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (StreamReader file = new StreamReader("test.txt"))
            {
                AntlrInputStream inputStream = new AntlrInputStream(file.ReadToEnd());
                MiniCLexer testLexer = new MiniCLexer(inputStream);
                //testLexer.AddErrorListener(ErrorListener.Instance);
                CommonTokenStream commonTokenStream = new CommonTokenStream(testLexer);
                MiniCParser testParser = new MiniCParser(commonTokenStream);
                testParser.AddErrorListener(ErrorListener.Instance);
                int aa = testParser.NumberOfSyntaxErrors;
                commonTokenStream.Fill();
                var b = commonTokenStream.GetTokens();
                MiniCParser.CompilationUnitContext context = testParser.compilationUnit();
                string a = context.ToStringTree(testParser);
                var c = new ParseTreeWalker();
            }
        }
    }
}
