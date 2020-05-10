using Antlr4.Runtime;
using TestANTLR.Exceptions;
using TestANTLR.Scopes;

namespace TestANTLR.Generators.Expressions
{
    public class LValueExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var lvalExprCtx = context as MiniCParser.LValueExpressionContext;
            var identifier = lvalExprCtx.Identifier();
            var ternaryExpr = lvalExprCtx.ternaryExpression();
            var lvalExpr = lvalExprCtx.lValueExpression();
            // Variable name (a ...)
            if (identifier != null && lvalExprCtx.Dot() == null)
            {
                currentCode.AddComment($"Getting variable \"{identifier.GetText()}\" for lvalue");
                
                // Получаем тип из таблицы
                var curScope = currentCode.GetCurrentScope();
                var symbol = curScope.FindSymbol(identifier.GetText()) as VarSymbol;
                if (symbol == null) 
                    throw new CodeGenerationException($"Unknown symbol {identifier.GetText()}");

                // Запись в регистр
                var register = currentCode.GetFreeRegister();
                currentCode.AddVariableToRegisterReading(symbol, register);
            }
            // Braces (a[] ...)
            else if (ternaryExpr != null)
            {
                currentCode.AddComment("Getting braces value");
                
                // Вычисления в скобках
                var terExprGen = new TernaryExpressionGenerator();
                currentCode = terExprGen.GenerateCodeForContext(ternaryExpr, currentCode);
                var inBracesValueRegister = currentCode.LastAssignedRegister;
                
                // Вычисление lValue
                var lvalExprGen = new LValueExpressionGenerator();
                currentCode = lvalExprGen.GenerateCodeForContext(lvalExpr, currentCode);
                var lValueRegister = currentCode.LastAssignedRegister;
                var arrayVar = currentCode.LastReferencedVariable;
                
                // Вычисление оффсета для массива
                currentCode.AddComment("Getting indexed value for lvalue");
                var intType = SymbolType.GetType("int");
                var varSizeRegister = currentCode.GetFreeRegister();
                currentCode.AddValueToRegisterAssign(varSizeRegister, arrayVar.Type.Size.ToString(), intType);
                var offsetRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterMpyRegister(offsetRegister, inBracesValueRegister, varSizeRegister, intType);

                // Само действие
                var lhsRegister = currentCode.GetFreeRegister();
                currentCode.AddVariableToRegisterReadingWithOffset(arrayVar, lhsRegister, offsetRegister);
                
                // Чистка регистров
                currentCode.FreeRegister(varSizeRegister);
                currentCode.FreeRegister(inBracesValueRegister);
                currentCode.FreeRegister(lValueRegister);
            }
            // Dot (a.x ...)
            else
            {
                currentCode.AddComment($"Getting dot value (.{identifier.GetText()})");
                
                // Вычисление lvalue
                var lvalExprGen = new LValueExpressionGenerator();
                currentCode = lvalExprGen.GenerateCodeForContext(lvalExpr, currentCode);
                var valueRegister = currentCode.LastAssignedRegister;
                var prevVariableInDotChain = currentCode.LastReferencedVariable;
                var prevVariableInDotChainType = prevVariableInDotChain.Type;

                // Вычисление offset для переменной структуры
                var structSymbol = currentCode.GlobalScope.FindStruct(prevVariableInDotChainType);
                var structOffset = structSymbol.VariableOffsetFromStartAddress(identifier.GetText());
                
                // Получаем значение
                var resultRegister = currentCode.GetFreeRegister();
                currentCode.AddVariableToRegisterReadingWithOffset(prevVariableInDotChain, resultRegister, structOffset.ToString());
                
                // Чиска регистров
                currentCode.FreeRegister(valueRegister);
            }
            
            return currentCode;
        }
    }
}