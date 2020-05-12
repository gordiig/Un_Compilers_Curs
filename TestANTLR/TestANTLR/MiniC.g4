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
    :   declaration
	|	definition
    ;

declaration 
    :   varDeclaration
	|	structDeclaration
    ;

definition
	:	functionDefinition
	|	varDefinition
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
	:	varHeader (LeftBracket RightBracket)? Assign initializer Semi
	;

initializer
    :   ternaryExpression
    |   LeftBrace initializerList RightBrace
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
    :   logicalOrExpression (Question ternaryExpression Colon ternaryExpression)?
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
    :   primaryExpression												#PrimaryExp
    |   postfixExpression LeftBracket ternaryExpression RightBracket	#ArrayRead
    |   postfixExpression Dot Identifier								#StructRead
	|   Identifier LeftParen parameterList? RightParen					#FunctionCall  
    ;

parameterList
	:	ternaryExpression
	|	parameterList Comma ternaryExpression
	;

primaryExpression
    :   Identifier														#VarRead
    |   constant														#ConstRead
    |   LeftParen ternaryExpression RightParen							#Parens
    ;

lValueExpression
	:	Identifier
	|	lValueExpression LeftBracket ternaryExpression RightBracket
	|	lValueExpression Dot Identifier
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
    :   If LeftParen ternaryExpression RightParen statement (Else statement)?
    ;

iterationStatement
    :   While LeftParen ternaryExpression RightParen statement
    |   Do statement While LeftParen ternaryExpression RightParen Semi
    |   For LeftParen expression? Semi ternaryExpression? Semi expression? RightParen statement
    ;

jumpStatement
    :   Continue Semi
    |   Break Semi
    |   Return ternaryExpression? Semi
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
    :   FractionalConstant ExponentPart?
    |   DigitSequence ExponentPart
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

CharacterConstant
    :   '\'' CChar '\''
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