using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TestANTLR.Generators;
using TestANTLR.Scopes;

namespace TestANTLR
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SymbolType.AddTypeRange("void", "char", "int", "float");

            using (StreamReader file = new StreamReader("../../../test.txt"))
            {
                AntlrInputStream inputStream = new AntlrInputStream(file.ReadToEnd());
                MiniCLexer testLexer = new MiniCLexer(inputStream);
                testLexer.AddErrorListener(ErrorListenerLex.Instance);
                CommonTokenStream commonTokenStream = new CommonTokenStream(testLexer);
                MiniCParser testParser = new MiniCParser(commonTokenStream);
                testParser.AddErrorListener(ErrorListener.Instance);
                var tree = testParser.compilationUnit();

                ParseTreeWalker walker = new ParseTreeWalker();
                SymbolTableSemanticListener def = new SymbolTableSemanticListener();
                walker.Walk(def, tree);
            }
        }
    }
}
