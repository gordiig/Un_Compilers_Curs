using Antlr4.Runtime;
using TestANTLR.Scopes;

namespace TestANTLR.Generators.Expressions
{
    public class PostfixExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            // Primary expr only
            if (context is MiniCParser.PrimaryExpContext primaryExpression)
            {
                var primaryExprGen = new PrimaryExpressionGenerator();
                currentCode = primaryExprGen.GenerateCodeForContext(primaryExpression, currentCode);
            }
            // Indexing (a[i])
            else if (context is MiniCParser.ArrayReadContext arrayReadContext)
            {
                currentCode.AddComment("Getting braces value");
                
                // Вычисление значения в скобках
                var ternaryExpression = arrayReadContext.ternaryExpression();
                var ternaryExpressionGen = new TernaryExpressionGenerator();
                currentCode = ternaryExpressionGen.GenerateCodeForContext(ternaryExpression, currentCode);
                var inBracesValueRegister = currentCode.LastAssignedRegister;
                
                // Привод типа в скобках, если надо
                var typeToConvert = currentCode.Conversions.Get(ternaryExpression);
                if (typeToConvert != null) 
                    currentCode.ConvertRegisterToType(inBracesValueRegister, inBracesValueRegister, typeToConvert);

                // Получение адреса переменной
                var postfixExpression = arrayReadContext.postfixExpression();
                var postfixExpressionGen = new PostfixExpressionGenerator();
                currentCode = postfixExpressionGen.GenerateCodeForContext(postfixExpression, currentCode);
                var variableAddressRegister = currentCode.LastReferencedAddressRegister;
                var variableType = currentCode.LastReferencedAddressRegisterType;

                // Вычисление смещения
                currentCode.AddComment("Getting indexed value");
                var intType = SymbolType.GetType("int");
                var varSizeRegister = currentCode.GetFreeRegister();
                currentCode.AddValueToRegisterAssign(varSizeRegister, variableType.Size.ToString(), intType);
                var offsetRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterMpyRegister(offsetRegister, inBracesValueRegister, varSizeRegister, intType);

                // Получение значения по индексу
                currentCode.AddAddingRegisterToRegister(variableAddressRegister, variableAddressRegister, 
                    offsetRegister, intType);
                
                // Освобождение регистров
                currentCode.FreeRegister(offsetRegister);
                currentCode.FreeRegister(varSizeRegister);
                currentCode.FreeRegister(inBracesValueRegister);
            }
            // Function call (f(*))
            else if (context is MiniCParser.FunctionCallContext functionCallContext)
            {
                // TODO FUNCTIONS
            }
            // Dotting (a.b)
            else if (context is MiniCParser.StructReadContext structReadContext)
            {
                // Вычисление lvalue
                var postfixExpression = structReadContext.postfixExpression();
                var postfixEpressionGen = new PostfixExpressionGenerator();
                currentCode = postfixEpressionGen.GenerateCodeForContext(postfixExpression, currentCode);
                var lValueAddressRegister = currentCode.LastReferencedAddressRegister;
                var lValueType = currentCode.LastReferencedAddressRegisterType;
                
                // Вычисляем offset для переменной структуры
                var structSymbol = currentCode.GlobalScope.FindStruct(lValueType);
                var structOffset = structSymbol.VariableOffsetFromStartAddress(structReadContext.Identifier().GetText());
                
                // Запись offset в регистр
                var intType = SymbolType.GetType("int");
                var offsetRegister = currentCode.GetFreeRegister();
                currentCode.AddValueToRegisterAssign(offsetRegister, structOffset.ToString(), intType);
                
                // Получаем адрес нужной переменной в структуре
                currentCode.AddAddingRegisterToRegister(lValueAddressRegister, lValueAddressRegister, offsetRegister, intType);
                currentCode.LastReferencedAddressRegisterType = structSymbol.VariableType(structReadContext.Identifier().GetText());
                
                // Чистка регистров
                currentCode.FreeRegister(offsetRegister);
            }
            
            return currentCode;
        }
    }
}