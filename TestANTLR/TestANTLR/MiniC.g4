grammar MiniC;

/*
 * Parser Rules
 */

 compilationUnit
    :   translationUnit? EOF
    ;

translationUnit
    :   externalDeclaration*
    ;

externalDeclaration
    :   functionDefinition
    |   declaration
    ;

functionDefinition
    :   declarationSpecifier* declarator declaration* compoundStatement
    ;

declarationSpecifier
    :   typeSpecifier
    |   typeQualifier
    ;

typeSpecifier 
    :   Void
    |   Char
    |   Int
    |   Float
    |   structSpecifier
    ;

structSpecifier 
    :   Struct Identifier? LeftBrace structDeclaration+ RightBrace
    |   Struct Identifier
    ;

structDeclaration 
    :   specifierQualifier* structDeclaratorList
    ;

specifierQualifier 
    :   typeSpecifier
    |   typeQualifier
    ;

structDeclaratorList 
    :   structDeclarator
    |   structDeclaratorList Comma structDeclarator
    ;

structDeclarator 
    :   declarator
    |   declarator? Colon constantExpression
    ;

declarator
    :   directDeclarator
    ;

typeQualifier 
    :   Const
    ;

directDeclarator 
    :   Identifier
    |   LeftParen declarator RightParen
    |   directDeclarator LeftBracket constantExpression? RightBracket
    |   directDeclarator LeftParen parameterList  RightParen
    |   directDeclarator LeftParen Identifier* RightParen
    ;

constantExpression
    :   conditionalExpression
    ;

conditionalExpression
    :   logicalOrExpression (Question expression Colon conditionalExpression)?
    ;

logicalOrExpression
    :   logicalAndExpression
    |   logicalOrExpression OrOr logicalAndExpression
    ;

logicalAndExpression
    :   inclusiveOrExpression
    |   logicalAndExpression AndAnd inclusiveOrExpression
    ;

inclusiveOrExpression
    :   exclusiveOrExpression
    |   inclusiveOrExpression Or exclusiveOrExpression
    ;

exclusiveOrExpression
    :   andExpression
    |   exclusiveOrExpression Caret andExpression
    ;

andExpression
    :   equalityExpression
    |   andExpression And equalityExpression
    ;

equalityExpression
    :   relationalExpression
    |   equalityExpression Equal relationalExpression
    |   equalityExpression NotEqual relationalExpression
    ;

relationalExpression
    :   shiftExpression
    |   relationalExpression Less shiftExpression
    |   relationalExpression Greater shiftExpression
    |   relationalExpression LessEqual shiftExpression
    |   relationalExpression GreaterEqual shiftExpression
    ;

shiftExpression
    :   additiveExpression
    |   shiftExpression LeftShift additiveExpression
    |   shiftExpression RightShift additiveExpression
    ;

additiveExpression
    :   multiplicativeExpression
    |   additiveExpression Plus multiplicativeExpression
    |   additiveExpression Minus multiplicativeExpression
    ;

multiplicativeExpression
    :   unaryExpression
    |   multiplicativeExpression Star unaryExpression
    |   multiplicativeExpression Div unaryExpression
    |   multiplicativeExpression Mod unaryExpression
    ;

unaryExpression
    :   postfixExpression
    |   PlusPlus unaryExpression
    |   MinusMinus unaryExpression
    |   unaryOperator unaryExpression
    ;

postfixExpression
    :   primaryExpression
    |   postfixExpression LeftBracket expression RightBracket
    |   postfixExpression LeftParen assignmentExpression* RightParen
    |   postfixExpression Dot Identifier
    |   postfixExpression PlusPlus
    |   postfixExpression MinusMinus
    ;

primaryExpression
    :   Identifier
    |   Constant
    |   StringLiteral+
    |   LeftParen expression RightParen
    ;

expression
    :   assignmentExpression
    |   expression Comma assignmentExpression
    ;

assignmentExpression
    :   conditionalExpression
    |   unaryExpression assignmentOperator assignmentExpression
    ;

assignmentOperator
    :   Assign | StarAssign | DivAssign | ModAssign | PlusAssign | MinusAssign 
    |   LeftShiftAssign | RightShiftAssign | AndAssign | XorAssign | OrAssign
    ;

unaryOperator
    :   And | Star | Plus | Minus | Tilde | Not
    ;

typeName
    :   specifierQualifier+ directAbstractDeclarator?
    ;
        
parameterList
    :   parameterDeclaration
    |   parameterList Comma parameterDeclaration
    ;

parameterDeclaration
    :   declarationSpecifier+ declarator
    |   declarationSpecifier+ directAbstractDeclarator?
    ;

directAbstractDeclarator
    :   LeftParen directAbstractDeclarator RightParen
    |   LeftBracket constantExpression? RightBracket
    |   LeftParen parameterList? RightParen
    |   directAbstractDeclarator LeftBracket constantExpression? RightBracket
    |   directAbstractDeclarator LeftParen parameterList? RightParen
    ;

declaration
    :   declarationSpecifier+ initDeclarator* Semi
    ;

initDeclarator
    :   declarator
    |   declarator Assign initializer
    ;

initializer
    :   assignmentExpression
    |   LeftBrace initializerList RightBrace
    |   LeftBrace initializerList Comma RightBrace
    ;

initializerList
    :   initializer
    |   initializerList Comma initializer
    ;

compoundStatement
    :   LeftBrace declaration* statement* RightBrace
    ;

statement
    :   expressionStatement
    |   compoundStatement
    |   selectionStatement
    |   iterationStatement
    |   jumpStatement
    ;

expressionStatement
    :   expression? Semi
    ;

selectionStatement
    :   If LeftParen expression RightParen statement (Else statement)?
    ;

iterationStatement
    :   While LeftParen expression RightParen statement
    |   Do statement While LeftParen expression RightParen Semi
    |   For LeftParen expression? Semi expression? Semi expression? RightParen statement
    ;

jumpStatement
    :   Continue Semi
    |   Break Semi
    |   Return expression? Semi
    ;

/*
 * Lexer Rules
 */

Break : 'break';
Char : 'char';
Const : 'const';
Continue : 'continue';
Do : 'do';
Else : 'else';
Float : 'float';
For : 'for';
If : 'if';
Int : 'int';
Return : 'return';
Struct : 'struct';
Void : 'void';
While : 'while';

LeftParen : '(';
RightParen : ')';
LeftBracket : '[';
RightBracket : ']';
LeftBrace : '{';
RightBrace : '}';

Less : '<';
LessEqual : '<=';
Greater : '>';
GreaterEqual : '>=';
LeftShift : '<<';
RightShift : '>>';

Plus : '+';
PlusPlus : '++';
Minus : '-';
MinusMinus : '--';
Star : '*';
Div : '/';
Mod : '%';

And : '&';
Or : '|';
AndAnd : '&&';
OrOr : '||';
Caret : '^';
Not : '!';
Tilde : '~';

Question : '?';
Colon : ':';
Semi : ';';
Comma : ',';

Assign : '=';
// '*=' | '/=' | '%=' | '+=' | '-=' | '<<=' | '>>=' | '&=' | '^=' | '|='
StarAssign : '*=';
DivAssign : '/=';
ModAssign : '%=';
PlusAssign : '+=';
MinusAssign : '-=';
LeftShiftAssign : '<<=';
RightShiftAssign : '>>=';
AndAssign : '&=';
XorAssign : '^=';
OrAssign : '|=';

Equal : '==';
NotEqual : '!=';

Dot : '.';

Identifier
    :   Nondigit
        (   Nondigit
        |   Digit
        )*
    ;

fragment
Nondigit
    :   [a-zA-Z_]
    ;

fragment
Digit
    :   [0-9]
    ;

Constant
    :   IntegerConstant
    |   FloatingConstant
    |   CharacterConstant
    ;

fragment
IntegerConstant
    :   DecimalConstant
    |   OctalConstant
    |   HexadecimalConstant
    ;

fragment
DecimalConstant
    :   NonzeroDigit Digit*
    ;

fragment
OctalConstant
    :   '0' OctalDigit*
    ;

fragment
HexadecimalConstant
    :   HexadecimalPrefix HexadecimalDigit+
    ;

fragment
HexadecimalPrefix
    :   '0' [xX]
    ;

fragment
NonzeroDigit
    :   [1-9]
    ;

fragment
OctalDigit
    :   [0-7]
    ;

fragment
HexadecimalDigit
    :   [0-9a-fA-F]
    ;

fragment
FloatingConstant
    :   DecimalFloatingConstant
    ;

fragment
DecimalFloatingConstant
    :   FractionalConstant ExponentPart? FloatingSuffix?
    |   DigitSequence ExponentPart FloatingSuffix?
    ;

fragment
FractionalConstant
    :   DigitSequence? '.' DigitSequence
    |   DigitSequence '.'
    ;

fragment
ExponentPart
    :   'e' Sign? DigitSequence
    |   'E' Sign? DigitSequence
    ;

fragment
Sign
    :   '+' | '-'
    ;

DigitSequence
    :   Digit+
    ;

fragment
FloatingSuffix
    :   'f' | 'l' | 'F' | 'L'
    ;

fragment
CharacterConstant
    :   '\'' CCharSequence '\''
    ;

fragment
CCharSequence
    :   CChar+
    ;

fragment
CChar
    :   ~['\\\r\n]
    |   EscapeSequence
    ;

fragment
EscapeSequence
    :   '\\' ['"?abfnrtv\\]
    ;

StringLiteral
    :   '"' SCharSequence? '"'
    ;

fragment
SCharSequence
    :   SChar+
    ;

fragment
SChar
    :   ~["\\\r\n]
    |   EscapeSequence
    |   '\\\n'   // Added line
    |   '\\\r\n' // Added line
    ;

LineComment
    :   '//' ~[\r\n]*
        -> skip
    ;

BlockComment
    :   '/*' .*? '*/'
        -> skip
    ;

Whitespace
    :   [ \t]+
        -> skip
    ;

Newline
    :   (   '\r' '\n'?
        |   '\n'
        )
        -> skip
    ;