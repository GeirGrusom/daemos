grammar MuteGrammar;

@header {
	using System;
	using System.Linq;
	using Markurion.Mute.Expressions;
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


statement returns [Expression expr]
	: declaration ';' { $expr = $declaration.expr; }
	| expression ';' { $expr = $expression.expr; }
	| whileExpression { $expr = $whileExpression.expr; }
	| ifExpression { $expr = $ifExpression.expr; }
	| tryExpression { $expr = $tryExpression.expr; }
	| RETRY ';' { $expr = new RetryExpression($ctx); }
	| THROW ';'
	;

expression returns [Expression expr]
	: assignmentExpression {$expr = $assignmentExpression.expr; }
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

memberExpression returns [Expression expr]
	: lhs = memberExpression '.' IDENTIFIER { $expr = Member($lhs.expr, $IDENTIFIER.text, $ctx); }
	| variableExpression '.' IDENTIFIER { $expr = Member($variableExpression.expr, $IDENTIFIER.text, $ctx); }
	;

unaryExpression returns [Expression expr]
	: NOT operand = unaryExpression { $expr = Not($operand.expr, $ctx); }
	| SUB operand = unaryExpression { $expr = Neg($operand.expr, $ctx); }
	| ADD operand = unaryExpression { $expr = Add($operand.expr, $ctx); }
	| NOT_NULL operand = unaryExpression { $expr = NotNull($operand.expr, $ctx); }
	| AWAIT awaitOperand = commitTransaction { $expr = Await($awaitOperand.expr, $ctx); }
	| '(' innerExpression = expression ')' { $expr = $innerExpression.expr; }
	| literalExpression { $expr = $literalExpression.expr; }
	| variableExpression { $expr = $variableExpression.expr; }
	| castExpression { $expr = $castExpression.expr; }
	| commitTransaction { $expr = $commitTransaction.expr; }
	| memberExpression { $expr = $memberExpression.expr; }
	| functionCall { $expr = $functionCall.expr; }
	;

commitTransaction returns [Expression expr]
	: TRANSACTION_STATE CHILD variableExpression { $expr = CommitTransactionChild($TRANSACTION_STATE.text, $variableExpression.expr, $ctx); } // Creates a new child transaction
	| TRANSACTION_STATE variableExpression { $expr = CommitTransaction($TRANSACTION_STATE.text, $variableExpression.expr, $ctx); }// Commits an exisiting transaction
	| TRANSACTION_STATE withExpression { $expr = CommitTransaction($TRANSACTION_STATE.text, $withExpression.expr, $ctx); } // Commits an exisiting transaction
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

functionCall returns [Expression expr]
	: IDENTIFIER '(' callList ')' { $expr = Call(null, $IDENTIFIER.text, $callList.results, $ctx); }
	| IDENTIFIER '(' ')' { $expr = Call(null, $IDENTIFIER.text, Enumerable.Empty<Expression>(), $ctx); }
	;

callList returns [List<Expression> results]
	: args += expression (',' args += expression)* { $results = ($args).Select(x => x.expr).ToList(); }
	;

literalExpression returns [Expression expr]
	: 'true' { $expr = ConstantExpression.TrueExpression; }
	| 'false' { $expr = ConstantExpression.FalseExpression; }
	| NULL { $expr = ConstantExpression.NullExpression; }
	| INTEGER { $expr = new ConstantExpression(DataType.NonNullInt, $INTEGER.int, $ctx); }
	| THIS { $expr = new VariableExpression("this", false, new DataType(typeof(Markurion.Transaction), false), $ctx); }
	| futureTime { $expr = $futureTime.expr; }
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


futureTime returns [Expression expr]: 'in' years? months? weeks? days? hours? minutes? seconds?;

years returns [Expression value]: expression 'years' { $value = Valid<int>($expression.expr); } ;
months returns [Expression value]: expression 'months' { $value = Valid<int>($expression.expr); } ;
weeks returns [Expression value]: expression 'weeks' { $value = Valid<int>($expression.expr); };
days returns [Expression value]: expression 'days' { $value = Valid<int>($expression.expr); } ;
hours returns [Expression value]: expression 'hours' { $value = Valid<int>($expression.expr); } ;
minutes returns [Expression value]: expression 'minutes' { $value = Valid<int>($expression.expr); } ;
seconds returns [Expression value]: expression 'seconds' { $value = Valid<int>($expression.expr); } ;

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
| FUNCTION '(' argumentList ')' statementBody ';';

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
	| TRANSACTION_TYPE nullable { $type = new DataType(typeof(Markurion.Transaction), $nullable.result); }
	| IDENTIFIER ('.' IDENTIFIER)* nullable { $type = new DataType(TypeLookupFunction($ctx.GetText()), $nullable.result); }
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
CHILD: 'child';
TRANSACTION_STATE: 'commit' | 'initialize' | 'authorize' | 'complete' | 'cancel' | 'fail';
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