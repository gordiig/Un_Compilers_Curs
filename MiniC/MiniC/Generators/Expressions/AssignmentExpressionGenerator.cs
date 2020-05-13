using System;
using Antlr4.Runtime;
using MiniC.Scopes;

namespace MiniC.Generators.Expressions
{
    public class AssignmentExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var assignmentExprCtx = context as MiniCParser.AssignmentExpressionContext;
            var ternaryExpression = assignmentExprCtx.ternaryExpression();
            var lValueExpression = assignmentExprCtx.lValueExpression();
            var assignmentOperator = assignmentExprCtx.assignmentOperator();
            
            var ternaryGen = new TernaryExpressionGenerator();
            // Ternary expr only
            if (lValueExpression == null)
            {
                currentCode = ternaryGen.GenerateCodeForContext(ternaryExpression, currentCode);
            }
            else
            // With lvalue expr
            {
                // Вычисляем rvalue
                currentCode = ternaryGen.GenerateCodeForContext(ternaryExpression, currentCode);
                var rValueRegister = getValueFromExpression(currentCode);
                
                // Привод типов если нужно
                convertTypeIfNeeded(currentCode, rValueRegister, ternaryExpression);

                // Вычисляем lvalue
                var lvalueGen = new LValueExpressionGenerator();
                lvalueGen.GenerateCodeForContext(lValueExpression, currentCode);
                var lValueAddressRegister = currentCode.LastReferencedAddressRegister;
                var lValueType = lValueAddressRegister.Type;

                // Чтение данных из адреса lvalue
                var lValueRegister = currentCode.GetFreeRegister();
                currentCode.AddMemToRegisterReading(lValueAddressRegister, lValueType, lValueRegister);
                
                // В зависимости от оператора присваивания производим вычисления
                currentCode.AddComment("Assigning with some assign operator");
                var type = SymbolType.GetType("int");    // TODO: TYPING
                if (assignmentOperator.Assign() != null) 
                    currentCode.AddRegisterToRegisterAssign(lValueRegister, rValueRegister);
                else if (assignmentOperator.PlusAssign() != null)
                    currentCode.AddAddingRegisterToRegister(lValueRegister, lValueRegister, rValueRegister);
                else if (assignmentOperator.MinusAssign() != null) 
                    currentCode.AddSubRegisterFromRegister(lValueRegister, lValueRegister, rValueRegister);
                else if (assignmentOperator.LeftShiftAssign() != null)
                    currentCode.AddRegisterLefShiftRegister(lValueRegister, lValueRegister, rValueRegister);
                else if (assignmentOperator.RightShiftAssign() != null)
                    currentCode.AddRegisterRightShiftRegister(lValueRegister, lValueRegister, rValueRegister);
                else if (assignmentOperator.AndAssign() != null) 
                    currentCode.AddRegisterAndRegister(lValueRegister, lValueRegister, rValueRegister);
                else if (assignmentOperator.XorAssign() != null)
                    currentCode.AddRegisterXorRegister(lValueRegister, lValueRegister, rValueRegister);
                else if (assignmentOperator.OrAssign() != null)
                    currentCode.AddRegisterOrRegister(lValueRegister, lValueRegister, rValueRegister);
                else if (assignmentOperator.StarAssign() != null) 
                    currentCode.AddRegisterMpyRegister(lValueRegister, lValueRegister, rValueRegister);
                else if (assignmentOperator.DivAssign() != null)
                    // TODO: DIV
                    throw new NotImplementedException("IMPLEMENT DIV");
                else if (assignmentOperator.ModAssign() != null)
                    // TODO: MOD
                    throw new NotImplementedException("IMPLEMENT MOD");
                else 
                    throw new ApplicationException("Can't be here");

                // Записываем в переменную
                currentCode.AddRegisterToMemWriting(lValueAddressRegister, lValueRegister);

                // Чистка регистров
                currentCode.FreeRegister(lValueRegister);
                currentCode.FreeRegister(rValueRegister);
                currentCode.FreeLastReferencedAddressRegister();
            }
            
            return currentCode;
        }
    }
}