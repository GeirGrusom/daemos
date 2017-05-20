grammar MuteGrammar;

@header {
	using System;
	using System.Linq;
	using Daemos.Mute.Expressions;
}
/*
 * Parser Rules
 */

compileUnit returns [ModuleExpression module]
	: moduleStatement ';' (statements += statement)* EOF { $module = new ModuleExpression($moduleStatement.mod, ($statements).Select(x => x.expr), $ctx); }
	| moduleStatement ';' EOF { $module = new ModuleExpression($moduleStatement.mod, null, $ctx); }
	| EOF { $module = null; }
	;


moduleStatement  returns [string mod]
	: MODULE IDENTIFIER { $mod = $IDENTIFIER.text; PushScope(); }
	;

primaryExpression returns [Expression expr]
	: literalExpression { $expr = $literalExpression.expr; }
	| variableExpression { $expr = $variableExpression.expr; }
	| castExpression { $expr = $castExpression.expr; }
	| '(' expression ')' { $expr = $expression.expr; }
	;

statement returns [Expression expr]
	: declaration ';' { $expr = $declaration.expr; }
	| using ';' { $expr = $using.expr; }
	| expression ';' { $expr = $expression.expr; }
	| whileExpression { $expr = $whileExpression.expr; }
	| ifExpression { $expr = $ifExpression.expr; }
	| tryExpression { $expr = $tryExpression.expr; }
	| RETRY ';' { $expr = new RetryExpression($ctx); }
	| THROW ';'
	;

expression returns [Expression expr]
	: assignmentExpression {$expr = $assignmentExpression.expr; }
	| '(' innerExpression = expression ')' { $expr = $innerExpression.expr; }
	;

assignmentExpression returns [Expression expr]
	: lhs = variableExpression ASSIGN rhs = orExpression {
		$expr = Assign($lhs.expr, $rhs.expr, $ctx);
	}
	| operand = orExpression { $expr = $operand.expr; }
	;

orExpression returns [Expression expr]
	: lhs = orExpression OR rhs = xorExpression { $expr = Or($lhs.expr, $rhs.expr, $ctx); }
	| operand = xorExpression { $expr = $operand.expr; }
	;

xorExpression returns [Expression expr]
	: lhs = xorExpression XOR rhs = andExpression { $expr = Xor($lhs.expr, $rhs.expr, $ctx); }
	| operand = andExpression { $expr = $operand.expr; }
	;


andExpression returns [Expression expr]
	: lhs = andExpression AND rhs = equalityExpression { $expr = And($lhs.expr, $rhs.expr, $ctx); }
	| operand = equalityExpression { $expr = $operand.expr; }
	;

equalityExpression returns [Expression expr]
	: lhs = equalityExpression EQ rhs = comparisonExpression { $expr = Eq($lhs.expr, $rhs.expr, $ctx); }
	| operand = comparisonExpression { $expr = $operand.expr; }
	;

comparisonExpression returns [Expression expr]
	: lhs = comparisonExpression GREATER_THAN rhs = addExpression { $expr = Greater($lhs.expr, $rhs.expr, $ctx); }
	| lhs = comparisonExpression GREATER_THAN_OR_EQ rhs = addExpression { $expr = GreaterOrEqual($lhs.expr, $rhs.expr, $ctx); }
	| lhs = comparisonExpression LESS_THAN rhs = addExpression { $expr = Less($lhs.expr, $rhs.expr, $ctx); }
	| lhs = comparisonExpression LESS_THAN_OR_EQ rhs = addExpression { $expr = LessOrEqual($lhs.expr, $rhs.expr, $ctx); }
	| operand = addExpression { $expr = $operand.expr; }
	;

addExpression returns [Expression expr]
	: lhs = addExpression ADD rhs = mulExpression { $expr = Add($lhs.expr, $rhs.expr, $ctx); }
	| lhs = addExpression SUB rhs = mulExpression { $expr = Sub($lhs.expr, $rhs.expr, $ctx); }
	| operand = mulExpression { $expr = $operand.expr; }
	;

mulExpression returns [Expression expr]
	: lhs = mulExpression MUL rhs = withExpression { $expr = Mul($lhs.expr, $rhs.expr, $ctx); }
	| lhs = mulExpression DIV rhs = withExpression { $expr = Div($lhs.expr, $rhs.expr, $ctx); }
	| lhs = mulExpression MOD rhs = withExpression { $expr = Mod($lhs.expr, $rhs.expr, $ctx); }
	| operand = withExpression { $expr = $operand.expr; }
	;

withExpression returns [Expression expr]
	: lhs = withExpression WITH rhs = objectExpression { $expr = With($lhs.expr, (ObjectExpression)$rhs.expr, $ctx); }
	| operand = unaryExpression { $expr = $operand.expr; }
	;

unaryExpression returns [Expression expr]
	: postfixExpression { $expr = $postfixExpression.expr; }
	| NOT operand = unaryExpression { $expr = Not($operand.expr, $ctx); }
	| SUB operand = unaryExpression { $expr = Neg($operand.expr, $ctx); }
	| ADD operand = unaryExpression { $expr = Add($operand.expr, $ctx); }
	| NOT_NULL operand = unaryExpression { $expr = NotNull($operand.expr, $ctx); }
	| AWAIT awaitOperand = unaryExpression { $expr = Await($awaitOperand.expr, $ctx); }
	| commitTransaction { $expr = $commitTransaction.expr; }
	;

postfixExpression returns [Expression expr]
	: primaryExpression { $expr = $primaryExpression.expr; }
	| lhs = postfixExpression '.' IDENTIFIER { $expr = Member($lhs.expr, $IDENTIFIER.text, $ctx); }
	| lhs = postfixExpression '[' expression ']'
	| lhs = postfixExpression '(' callList ')' { $expr = Call($lhs.expr, $callList.results, $ctx); }
	| lhs = postfixExpression '(' namedCallList ')' { $expr = Call($lhs.expr, $namedCallList.results, $ctx); }
	| id = (IDENTIFIER | TIMESPAN_TYPE) '(' callList ')' { $expr = Call($id.text, $callList.results, $ctx); }
	| id = (IDENTIFIER | TIMESPAN_TYPE) '(' namedCallList ')' { $expr = Call($id.text, $namedCallList.results, $ctx); }
	;

using returns [Expression expr]
	: USING namespace += IDENTIFIER ('.' namespace += IDENTIFIER)* ( '{' (members += IDENTIFIER (',' members += IDENTIFIER)*)| all = '*' '}')?
	;

commitTransaction returns [Expression expr]
	: COMMIT CHILD variableExpression { $expr = CommitTransactionChild($variableExpression.expr, $ctx); } // Creates a new child transaction
	| COMMIT variableExpression { $expr = CommitTransaction($variableExpression.expr, $ctx); }// Commits an exisiting transaction
	| COMMIT withExpression { $expr = CommitTransaction($withExpression.expr, $ctx); } // Commits an exisiting transaction
	;

importExpression returns [Expression expr]
	: IMPORT dataType { $expr = Import($dataType.type, $ctx); }
	;

ifExpression returns [ConditionalExpression expr]
	: IF '(' condition = expression ')' ifValue = statementBody { $expr = If($condition.expr, $ifValue.expr, $ctx); }
	| IF '(' condition = expression ')' ifValue = statementBody ELSE elseValue = statementBody { $expr = If($condition.expr, $ifValue.expr, $elseValue.expr, $ctx); }
	| IF '(' condition = expression ')' ifValue = statementBody elseIfExpression { $expr = If($condition.expr, $ifValue.expr, $elseIfExpression.expr, $ctx); }
	;

elseIfExpression returns [ConditionalExpression expr]
	: ELSEIF '(' condition = expression ')' ifValue = statementBody { $expr = If($condition.expr, $ifValue.expr, $ctx); }
	| ELSEIF '(' condition = expression ')' ifValue = statementBody ELSE elseValue = statementBody { $expr = If($condition.expr, $ifValue.expr, $elseValue.expr, $ctx); }
	| ELSEIF '(' condition = expression ')' ifValue = statementBody elseIfExpression { $expr = If($condition.expr, $ifValue.expr, $elseIfExpression.expr, $ctx); }
	;

objectExpression returns [ObjectExpression expr]
	: '{' objectMemberList? '}' { $expr = Object($objectMemberList.members, $ctx); }
	;

objectMemberList returns [IEnumerable<ObjectMember> members]
	: mem += objectMember (',' mem += objectMember)* { $members = ($mem).Select(x => x.item); }
	;

objectMember returns [ObjectMember item]
	: IDENTIFIER ':' expression { $item = ObjectMember($IDENTIFIER.text, $expression.expr, $ctx); }
	;

variableExpression returns [VariableExpression expr]
	: IDENTIFIER { $expr = Lookup($IDENTIFIER.text, $ctx); }
	;

castExpression returns [Expression expr]
	: dataType '!' '(' expression ')' { $expr = Convert($dataType.type, $expression.expr, $ctx); }
	;


callList returns [List<Expression> results]
	: (args += expression (',' args += expression)*)? { $results = ($args)?.Select(x => x.expr).ToList() ?? new List<Expression>(); }
	;

namedCallList returns [List<NamedArgument> results]
	: args += namedCallListElement (',' args += namedCallListElement)* { $results = ($args).Select(x => x.expr).ToList(); }
	;

namedCallListElement returns [NamedArgument expr]
	: IDENTIFIER ':' expression { $expr = new NamedArgument($IDENTIFIER.text, $expression.expr, $ctx); }
	;


literalExpression returns [Expression expr]
	: 'true' { $expr = ConstantExpression.TrueExpression; }
	| 'false' { $expr = ConstantExpression.FalseExpression; }
	| NULL { $expr = ConstantExpression.NullExpression; }
	| INTEGER { $expr = new ConstantExpression(DataType.NonNullInt, $INTEGER.int, $ctx); }
	| THIS { $expr = new VariableExpression("this", false, new DataType(typeof(Daemos.Transaction), false), $ctx); }
	| DATETIME { $expr = DateTime($DATETIME.text, $ctx); }
	| quotedString { $expr = new ConstantExpression(DataType.NonNullString, $quotedString.value, $ctx); }
	| singleQuotedString { $expr = new ConstantExpression(DataType.NonNullString, $singleQuotedString.value, $ctx); }
	| TRANSACTION_STATE { $expr = TransactionState($TRANSACTION_STATE.text, $ctx); }
	;

whileExpression returns [Expression expr]
	: WHILE '(' expression ')' statementBody { $expr = While($expression.expr, $statementBody.expr, $ctx); }
	| DO statementBody WHILE '(' expression ')' { $expr = DoWhile($expression.expr, $statementBody.expr, $ctx); }
	;

quotedString returns [string value]: QUOTED_STRING { $value = Unescape($QUOTED_STRING.text.Substring(1, $QUOTED_STRING.text.Length - 2), '"'); } ;
singleQuotedString returns [string value]: SINGLE_QUOTED_STRING { $value = Unescape($SINGLE_QUOTED_STRING.text.Substring(1, $SINGLE_QUOTED_STRING.text.Length - 2), '\''); };


tryExpression returns [Expression expr]
	: TRY statementBody { $expr = Try($statementBody.expr, $ctx); }
	| TRY statementBody (catches += catchExpression)+ { $expr = Try($statementBody.expr, ($catches).Select(x => x.expr), $ctx); }
	| TRY statementBody finallyExpression { $expr = Try($statementBody.expr, $finallyExpression.expr, $ctx); }
	| TRY statementBody (catches += catchExpression)+ finallyExpression { $expr = Try($statementBody.expr, ($catches).Select(x => x.expr), $finallyExpression.expr, $ctx); }
	;
	
catchExpression returns [CatchExpression expr]
	: FAILURE statementBody { $expr = Catch($statementBody.expr, $ctx);  }
	| FAILURE '<' dataType '>' statementBody { $expr = Catch($statementBody.expr, $dataType.type, $ctx); } 
	;

finallyExpression returns [Expression expr]
	: FINALLY statementBody { $expr = $statementBody.expr; }
	;



declaration returns [VariableDeclarationExpression expr]
	: MUTABLE_DECLARE IDENTIFIER ASSIGN expression 
	{ 
		$expr = Declare($IDENTIFIER.text, true, $expression.expr, $ctx);
	}
	| MUTABLE_DECLARE IDENTIFIER ':' dataType
	{
		$expr = Declare($IDENTIFIER.text, true, $dataType.type, $ctx);
	}
	| IMMUTABLE_DECLARE IDENTIFIER ASSIGN expression 
	{ 
		$expr = Declare($IDENTIFIER.text, false, $expression.expr, $ctx);
	}
	| IMMUTABLE_DECLARE IDENTIFIER ':' dataType
	{
		$expr = Declare($IDENTIFIER.text, false, $dataType.type, $ctx);
	}
	| IMMUTABLE_DECLARE IDENTIFIER ASSIGN importExpression
	{
		$expr = Declare($IDENTIFIER.text, false, $importExpression.expr, $ctx);
	}
	;

functionDeclaration: FUNCTION '(' argumentList ')' expression ';'
	| FUNCTION '(' argumentList ')' statementBody ';'
	;

argumentList: (args += argument (',' args += argument)*)?;

argument returns [VariableExpression expr] : IDENTIFIER ':' dataType { $expr = new VariableExpression($IDENTIFIER.text, mutable: false, type: $dataType.type, context: $ctx); };

scopeStart: '{' { PushScope(); };

scopeEnd: '}' { PopScope(); };

nullable returns [bool result]: NULLABLE? { $result = $NULLABLE.text == "?"; };

statementBody returns [BlockExpression expr]
	: scopeStart (expressions += statement)* scopeEnd { $expr = new BlockExpression(($expressions).Select(x => x.expr), LastPoppedScope, $ctx); } ;

dataType returns [DataType type]
	: INT_TYPE nullable { $type = new DataType(typeof(int), $nullable.result); }
	| LONG_TYPE nullable { $type = new DataType(typeof(long), $nullable.result); }
	| FLOAT_TYPE nullable { $type = new DataType(typeof(double), $nullable.result); }
	| STRING_TYPE nullable { $type = new DataType(typeof(string), $nullable.result); }
	| BOOL_TYPE nullable { $type = new DataType(typeof(bool), $nullable.result); }
	| CURRENCY_TYPE nullable { $type = new DataType(typeof(decimal), $nullable.result); }
	| DATETIME_TYPE nullable { $type = new DataType(typeof(DateTime), $nullable.result); }
	| TIMESPAN_TYPE nullable { $type = new DataType(typeof(TimeSpan), $nullable.result); }
	| TRANSACTION_TYPE nullable { $type = new DataType(typeof(Daemos.Transaction), $nullable.result); }
	| IDENTIFIER ('.' IDENTIFIER)* nullable { $type = new DataType(TypeLookup($ctx.GetText()), $nullable.result); }
	;



/*
 * Lexer Rules
 */



WS : [ \n\r\t] -> skip;

fragment ESCAPED_QUOTE: '\\"';
fragment QUOTED_STRING_BODY: (ESCAPED_QUOTE | ~('\n'|'\r'))*?;
QUOTED_STRING: '"' QUOTED_STRING_BODY '"';

fragment ESCAPED_SINGLE_QUOTE: '\\\'';
fragment SINGLE_QUOTED_STRING_BODY: (ESCAPED_SINGLE_QUOTE | ~('\n'|'\r'))*?;
SINGLE_QUOTED_STRING: '\'' SINGLE_QUOTED_STRING_BODY '\'';

USING: 'using';

IF: 'if';
ELSEIF: 'else-if';
ELSE: 'else';
MODULE: 'module';
FUNCTION: 'fun';
OR: 'or';
XOR: 'xor';
AND: 'and';
TRY: 'try';
FAILURE: 'catch';
FINALLY: 'finally';
IMPORT: 'import';
AS: 'as';
INCREMENT: '++';
DECREMENT: '--';
ASSIGN: '<-';
ADD: '+';
SUB: '-';
MUL: '*';
DIV: '/';
MOD: '%';
NOT: 'not';
EQ: '=';
NEQ: '!=';

fragment DATETIME_PREFIX: '@';
fragment DATE_YEAR: [0-9][0-9][0-9][0-9];
fragment DATE_MONTH: [0-9][0-9];
fragment DATE_DAY: [0-9][0-9];
fragment DATE_SEPARATOR: '-';


fragment DATETIME_TIME: 'T';
fragment TIME_SEPARATOR: ':';
fragment TIME_HOUR: [0-9][0-9];
fragment TIME_MINUTE: [0-9][0-9];
fragment TIME_SECOND: [0-9][0-9];
fragment TIME_UTC: 'Z';
fragment TIME_OFFSET: [+\-];
fragment TIME_OFFSET_HOUR: [0-9][0-9];
fragment TIME_OFFSET_MINUTE: [0-9][0-9];

DATETIME
	: '@' DATE_YEAR '-' DATE_MONTH '-' DATE_DAY 'T' TIME_HOUR ':' TIME_MINUTE ':' TIME_SECOND ('+' | '-') TIME_OFFSET_HOUR ':' TIME_OFFSET_MINUTE 
	| '@' DATE_YEAR '-' DATE_MONTH '-' DATE_DAY 'T' TIME_HOUR ':' TIME_MINUTE ':' TIME_SECOND ('+' | '-') TIME_OFFSET_HOUR
	| '@' DATE_YEAR '-' DATE_MONTH '-' DATE_DAY 'T' TIME_HOUR ':' TIME_MINUTE ':' TIME_SECOND 'Z'
	| '@' DATE_YEAR '-' DATE_MONTH '-' DATE_DAY 'T' TIME_HOUR ':' TIME_MINUTE ':' TIME_SECOND
	| '@' DATE_YEAR '-' DATE_MONTH '-' DATE_DAY 'T' TIME_HOUR ':' TIME_MINUTE ('+' | '-') TIME_OFFSET_HOUR ':' TIME_OFFSET_MINUTE 
	| '@' DATE_YEAR '-' DATE_MONTH '-' DATE_DAY 'T' TIME_HOUR ':' TIME_MINUTE ('+' | '-') TIME_OFFSET_HOUR
	| '@' DATE_YEAR '-' DATE_MONTH '-' DATE_DAY 'T' TIME_HOUR ':' TIME_MINUTE 'Z'
	| '@' DATE_YEAR '-' DATE_MONTH '-' DATE_DAY 'T' TIME_HOUR ':' TIME_MINUTE 
	| '@' DATE_YEAR '-' DATE_MONTH '-' DATE_DAY ('+' | '-') TIME_OFFSET_HOUR ':' TIME_OFFSET_MINUTE 
	| '@' DATE_YEAR '-' DATE_MONTH '-' DATE_DAY ('+' | '-') TIME_OFFSET_HOUR
	| '@' DATE_YEAR '-' DATE_MONTH '-' DATE_DAY 'Z'
	| '@' DATE_YEAR '-' DATE_MONTH '-' DATE_DAY ;

	// P1Y2M10DT2H30M
//TIMESPAN
//	: '@' 'P' [0-9]+ 'Y' [0-9]+ 'M' [0-9]+ 'D' 'T' [0-9]+ 'H' [-9]+ 'M'
	
CHILD: 'child';
TRANSACTION_STATE: 'initialize' | 'authorize' | 'complete' | 'cancel' | 'fail';
COMMIT: 'commit';
GREATER_THAN_OR_EQ: '>=';
GREATER_THAN: '>';
LESS_THAN_OR_EQ: '<=';
LESS_THAN: '<';
IMMUTABLE_DECLARE: 'let';
MUTABLE_DECLARE: 'var';
NULLABLE: '?';
NOT_NULL: '!!'; 
INT_TYPE: 'int';
LONG_TYPE: 'long';
FLOAT_TYPE: 'float';
STRING_TYPE: 'string';
BOOL_TYPE: 'bool';
CURRENCY_TYPE: 'currency';
TRANSACTION_TYPE: 'transaction';
DATETIME_TYPE: 'datetime';
TIMESPAN_TYPE: 'timespan';
THROW: 'throw';
RETRY: 'retry';
NULL: 'null';
DO: 'do';
WHILE: 'while';
AWAIT: 'await';
WITH: 'with';
THIS: 'this';

HEX: '0x' [0-9a-fA-F]+;
BIN: '0b' [01]+;
INTEGER: [0-9]+;
fragment EXPONENT: 'e' '-'? INTEGER;
NUMBER: [0-9]* '.' [0-9]+ EXPONENT?;


fragment ID_HEAD: [a-zA-Z_];
fragment ID_TAIL: [a-zA-Z_0-9];

IDENTIFIER: ID_HEAD ID_TAIL*;