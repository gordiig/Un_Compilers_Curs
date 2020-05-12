grammar OldMiniC;

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
    :   declaration
	|	definition
    ;

declaration 
    :   functionDeclaration
	|	varDeclaration
	|	structDeclaration
    ;

definition
	:	functionDefinition
	|	varDefinition
	;

functionDeclaration
	:	functionHeader Semi
	;

varDeclaration
	:	varHeader (LeftBracket IntegerConstant RightBracket)? Semi
	;

structDeclaration
	:	Struct Identifier LeftBrace varDeclaration+ RightBrace Semi
	;

varHeader
	:	typeQualifier? typeSpecifier Identifier
	;

functionDefinition
    :   functionHeader compoundStatement
    ;

functionHeader
	:	typeSpecifier Identifier LeftParen (Void | parameterDeclarationList)? RightParen
	;

varDefinition
	:	varHeader (LeftBracket RightBracket)? assignmentOperator initializer Semi
	;

initializer
    :   ternaryExpression
    |   LeftBrace initializerList RightBrace
    |   LeftBrace initializerList Comma RightBrace
    ;

initializerList
    :   initializer
    |   initializerList Comma initializer
    ;

parameterDeclarationList
    :   parameterDeclaration
    |   parameterDeclarationList Comma parameterDeclaration
    ;

parameterDeclaration
    :   typeQualifier? typeSpecifier Identifier (LeftBracket RightBracket)? 
    ;

typeSpecifier 
    :   Void
    |   Char
    |   Int
    |   Float
    |   Struct Identifier
    ;

typeQualifier 
    :   Const
    ;

compoundStatement
    :   LeftBrace (varDeclaration | varDefinition)* statement* RightBrace
    ;

expression
    :   assignmentExpression
    ;

assignmentExpression
    :   (lValueExpression assignmentOperator)? ternaryExpression
    ;

ternaryExpression
    :   logicalOrExpression (Question expression Colon ternaryExpression)?
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
    |   unaryOperator unaryExpression
    ;

postfixExpression
    :   primaryExpression
    |   postfixExpression LeftBracket ternaryExpression RightBracket
	|   postfixExpression LeftParen ternaryExpression* RightParen
    |   postfixExpression Dot postfixExpression
    ;

primaryExpression
    :   Identifier
    |   constant
    |   LeftParen expression RightParen
    ;

lValueExpression
	:	Identifier
	|	lValueExpression LeftBracket ternaryExpression RightBracket
	|	lValueExpression Dot lValueExpression
	;

statement
    :   expressionStatement
    |   compoundStatement
    |   ifStatement
    |   iterationStatement
    |   jumpStatement
    ;

expressionStatement
    :   expression? Semi
    ;

ifStatement
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

assignmentOperator
    :   Assign | StarAssign | DivAssign | ModAssign | PlusAssign | MinusAssign 
    |   LeftShiftAssign | RightShiftAssign | AndAssign | XorAssign | OrAssign
    ;

unaryOperator
    :   Plus | Minus | Tilde | Not
    ;

constant
    :   IntegerConstant
    |   FloatingConstant
    |   CharacterConstant
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
Minus : '-';
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

fragment
DigitSequence
    :   Digit+
    ;

fragment
FloatingSuffix
    :   'f' | 'l' | 'F' | 'L'
    ;

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
        -> channel(HIDDEN)
    ;

Newline
    :   (   '\r' '\n'?
        |   '\n'
        )
        -> channel(HIDDEN)
    ;