grammar TransactQuery;

options {
	
}

@header {
	using System.Linq.Expressions;
	using System.Linq;
	using static System.Linq.Expressions.Expression;
	using System.Reflection;
}

@members {
	public ParameterExpression Transaction { get; } = Parameter(typeof(Transaction));
    public static MemberExpression GetPropertyCI(Expression owner, string name)
    {
        return Property(owner, owner.Type.GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public));
    }

	private static Expression In(Expression lhs, Expression rhs)
	{
		var method = rhs.Type.GetMethod("Contains", new [] { lhs.Type });

		return Call(rhs, method, lhs);
	}

	private string Unescape(string input, char tokenBarrier)
	{
		var builder = new System.Text.StringBuilder(input.Length);

		bool lastWasSlash = false;
		int lastCodePoint = 0;

		for (int i = 0; i < input.Length; ++i)
		{
			char c = input[i];
			if (c == '\\')
			{
				lastWasSlash = !lastWasSlash;
			}
			else if (lastWasSlash)
			{
				int copyLen = i - lastCodePoint - 1;
				builder.Append(input.Substring(lastCodePoint, copyLen));
			
				if (c == 'n')
				{
					builder.Append('\n');
				}
				else if (c == 'r')
				{
					builder.Append('\r');
				}
				else if (c == tokenBarrier)
				{
					builder.Append(tokenBarrier);
				}
				else if (c == 't')
				{
					builder.Append('\t');
				}
				lastCodePoint = i + 1;
				lastWasSlash = false;
			}
		
		}

		if (lastCodePoint < input.Length)
		{
			builder.Append(input.Substring(lastCodePoint));
		}
		
		return builder.ToString();
	}
}

/*
 * Parser Rules
 */

compileUnit returns [Expression expr]
	: expression { $expr = $expression.expr; } EOF
	| EOF
	|;

logicalExpression returns [Expression expr]
	: containmentExpression { $expr = $containmentExpression.expr; }
	;

containmentExpression returns [Expression expr] 
	: lhs = containmentExpression IN rhs = orExpression { $expr = In($lhs.expr, $rhs.expr); }
	| orExpression { $expr = $orExpression.expr; }
	;

orExpression returns [Expression expr]
	: lhs = orExpression OR rhs = xorExpression { $expr = OrElse($lhs.expr, $rhs.expr); }
	| xorExpression { $expr = $xorExpression.expr; }
	;

xorExpression returns [Expression expr]
	: lhs = xorExpression XOR rhs = andExpression { $expr = ExclusiveOr($lhs.expr, $rhs.expr); }
	| andExpression { $expr = $andExpression.expr; }
	;

andExpression returns [Expression expr]
	: lhs = andExpression AND rhs = equalityExpression { $expr = AndAlso($lhs.expr, $rhs.expr); }
	| equalityExpression { $expr = $equalityExpression.expr; }
	;

equalityExpression returns [Expression expr]
	: lhs = equalityExpression EQ rhs = comparisonExpression {
		if($lhs.expr.Type == typeof(JsonValue) || $rhs.expr.Type == typeof(JsonValue)) {
			$expr = Equal($lhs.expr, $rhs.expr, false,  typeof(JsonValue).GetMethod("Equals", new [] { $lhs.expr.Type, $rhs.expr.Type }));
		}
		else { 
			$expr = Equal($lhs.expr, $rhs.expr);
		}
				
	}
	| lhs = equalityExpression NOT_EQ rhs = comparisonExpression {
					$expr = NotEqual($lhs.expr, $rhs.expr);
	}
	| comparisonExpression { $expr = $comparisonExpression.expr; }
	;

comparisonExpression returns [Expression expr]
	: lhs = comparisonExpression GREATER  rhs = additiveExpression {
		$expr = GreaterThan($lhs.expr, $rhs.expr);
	}
	| lhs = comparisonExpression GREATER_OR_EQUAL  rhs = additiveExpression {
		$expr = GreaterThanOrEqual($lhs.expr, $rhs.expr);
	}
	| lhs = comparisonExpression LESS  rhs = additiveExpression {
		$expr = LessThan($lhs.expr, $rhs.expr);
	}
	| lhs = comparisonExpression LESS_OR_EQUAL  rhs = additiveExpression {
		$expr = LessThanOrEqual($lhs.expr, $rhs.expr);
	}
	| additiveExpression { $expr = $additiveExpression.expr; }
	;

additiveExpression returns [Expression expr]
	: lhs = additiveExpression ADD rhs = multiplicativeExpression {
		$expr = Add($lhs.expr, $rhs.expr);
	}
	|  lhs = additiveExpression SUB rhs = multiplicativeExpression {
		$expr = Subtract($lhs.expr, $rhs.expr);
	}
	| multiplicativeExpression { $expr = $multiplicativeExpression.expr; }
	;




multiplicativeExpression returns [Expression expr]
	: lhs = multiplicativeExpression MUL rhs = unaryExpression {
		$expr = Multiply($lhs.expr, $rhs.expr);
	}
	| lhs = multiplicativeExpression DIV rhs = unaryExpression {
		$expr = Divide($lhs.expr, $rhs.expr);
	}
	| lhs = multiplicativeExpression MOD rhs = unaryExpression {
		$expr = Modulo($lhs.expr, $rhs.expr);
	}
	| unaryExpression { $expr = $unaryExpression.expr; }
	;
	

unaryExpression returns [Expression expr]
	: NOT unaryExpression { 
		$expr = Not($unaryExpression.expr);
	}
	| SUB unaryExpression {
		$expr = Negate($unaryExpression.expr);
	}
	| '(' expression { $expr = $expression.expr; } ')'
	| literalExpression { $expr = $literalExpression.expr; }
	;

	

expression returns [Expression expr] : logicalExpression { $expr = $logicalExpression.expr; };

literalExpression returns [Expression expr]
	: float { $expr = Constant($float.value, typeof(float));}
	| integer { $expr = Constant($integer.value, typeof(int)); }
	| quotedString { $expr = Constant($quotedString.value, typeof(string)); }
	| singleQuotedString { $expr = Constant($singleQuotedString.value, typeof(string)); }
	| date { $expr = Constant($date.value, typeof(System.DateTime)); }
	| period { $expr = Constant($period.value, typeof(System.TimeSpan)); }
	| guid { $expr = Constant($guid.value, typeof(System.Guid)); }
	| identifierChain { $expr = $identifierChain.expr; }
	| array { $expr = $array.expr; }
	;

identifierChain returns [Expression expr] 
	: constant { $expr = $constant.expr; }
	| identifier {
		$expr = GetPropertyCI(Transaction, $identifier.value);
	}
	| owner = identifier '.' member = identifier {
			string dynamicObject = $owner.value;
			string memberName = $member.value;
			var ctor = typeof(Transact.JsonValue).GetConstructor(new[] { typeof(IDictionary<string, object>), typeof(string), typeof(string) });
			$expr =  New(ctor, Convert(GetPropertyCI(Transaction, dynamicObject), typeof(IDictionary<string, object>)), Constant(dynamicObject), Constant(memberName));
	}
	;

constant returns [Expression expr]
	: NULL { $expr = Constant(null); }
	| TRUE { $expr = Constant(true); }
	| FALSE { $expr = Constant(false); }
	| INITIALIZED { $expr = Constant(TransactionState.Initialized); }
	| AUTHORIZED { $expr = Constant(TransactionState.Authorized); }
	| COMPLETED {$expr = Constant(TransactionState.Completed); }
	| CANCELLED { $expr = Constant(TransactionState.Cancelled); }
	| FAILED { $expr = Constant(TransactionState.Failed); }
	;

identifier returns [string value]
	:'$' quotedString { $value = $quotedString.value; }
	|'$' singleQuotedString { $value = $singleQuotedString.value; }
	| ID { $value = $ID.text; }
	;

quotedString returns [string value]: QUOTED_STRING { $value = Unescape($QUOTED_STRING.text.Substring(1, $QUOTED_STRING.text.Length - 2), '"'); } ;
singleQuotedString returns [string value]: SINGLE_QUOTED_STRING { $value = Unescape($SINGLE_QUOTED_STRING.text.Substring(1, $SINGLE_QUOTED_STRING.text.Length - 2), '\''); };


float returns [double value]
	: INT '.' INT  exponent { $value = double.Parse($ctx.GetText(), System.Globalization.CultureInfo.InvariantCulture); }
	| '.' INT exponent { $value = double.Parse($ctx.GetText(), System.Globalization.CultureInfo.InvariantCulture); }
	| INT exponent { $value = double.Parse($ctx.GetText(), System.Globalization.CultureInfo.InvariantCulture); }
	| INT '.' INT { $value = double.Parse($ctx.GetText(), System.Globalization.CultureInfo.InvariantCulture); }
	| '.' INT { $value = double.Parse($ctx.GetText(), System.Globalization.CultureInfo.InvariantCulture); }
	;

integer returns [int value]: INT { $value = int.Parse($INT.text, System.Globalization.CultureInfo.InvariantCulture); } ; 

exponent: 'e' (ADD | SUB) INT; 

boolean returns [bool value] : TRUE { $value = true; } | FALSE { $value = false; };

null returns [object value]: NULL { $value = null; };

guid returns [System.Guid value]
: GUID_SLASH { $value = System.Guid.Parse($ctx.GetText()); }
| GUID { $value = System.Guid.Parse($ctx.GetText()); }
;

date returns [System.DateTime value]
	: '@' DATE { $value = System.DateTime.ParseExact($DATE.text, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture); }
	| '@' DATETIME { $value = System.DateTime.ParseExact($DATETIME.text, new [] { "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm:ss.fff" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal); }
	;

period returns [System.TimeSpan value]
	: 'P' P_YEARS? P_MONTHS? P_DAYS? ('T' P_HOURS? P_MINUTES? P_SECONDS?) {
		System.TimeSpan timeSpan = new System.TimeSpan();
		if($P_YEARS != null) {
			timeSpan = new System.TimeSpan(int.Parse($P_YEARS.text.Substring(1)) * 365, 0, 0, 0);
		}
		if($P_MONTHS != null) {
			timeSpan = timeSpan.Add(new System.TimeSpan(int.Parse($P_MONTHS.text.Substring(1)) * 30, 0, 0, 0));
		}
		if($P_DAYS != null) {
			timeSpan = timeSpan.Add(new System.TimeSpan(int.Parse($P_DAYS.text.Substring(1)), 0, 0, 0));
		}
		if($P_HOURS != null) {
			timeSpan = timeSpan.Add(new System.TimeSpan(int.Parse($P_HOURS.text.Substring(1)), 0, 0));
		}
		if($P_MINUTES != null) {
			timeSpan = timeSpan.Add(new System.TimeSpan(0, int.Parse($P_MINUTES.text.Substring(1)), 0));
		}
		if($P_SECONDS != null) {
			timeSpan = timeSpan.Add(new System.TimeSpan(0, 0, int.Parse($P_SECONDS.text.Substring(1))));
		}
	 }
	;

array returns [Expression expr] : '[' (members += expression (',' members += expression)*)? ']' {
	var type = ($members)[0].expr.Type;
	$expr = NewArrayInit(type, ($members).Select(x => x.expr));
};

/*
 * Lexer Rules
 */

WS : ' ' -> skip;

fragment ESCAPED_QUOTE: '\\"';
fragment QUOTED_STRING_BODY: (ESCAPED_QUOTE | ~('\n'|'\r'))*?;
QUOTED_STRING: '"' QUOTED_STRING_BODY '"';

fragment ESCAPED_SINGLE_QUOTE: '\\\'';
fragment SINGLE_QUOTED_STRING_BODY: (ESCAPED_SINGLE_QUOTE | ~('\n'|'\r'))*?;
SINGLE_QUOTED_STRING: '\'' SINGLE_QUOTED_STRING_BODY '\'';

fragment HEX_DIGIT: [0-9a-fA-F];
fragment HEX_DIGIT_2: HEX_DIGIT HEX_DIGIT;
fragment HEX_DIGIT_4: HEX_DIGIT_2 HEX_DIGIT_2;
fragment HEX_DIGIT_8: HEX_DIGIT_4 HEX_DIGIT_4;

GUID_SLASH: '{' HEX_DIGIT_8 '-' HEX_DIGIT_4 '-' HEX_DIGIT_4 '-' HEX_DIGIT_4 '-' HEX_DIGIT_8 HEX_DIGIT_4 '}';
GUID: '{' HEX_DIGIT_8 HEX_DIGIT_4 HEX_DIGIT_4 HEX_DIGIT_4 HEX_DIGIT_8 HEX_DIGIT_4 '}';

INT: [0-9]+;
fragment ID_HEAD: [a-zA-Z_];
fragment ID_TAIL: [a-zA-Z_0-9];
NULL: [Nn][Uu][Ll][Ll];
TRUE: [Tt][Rr][Uu][Ee];
FALSE: [Ff][Aa][Ll][Ss][Ee];
AND: [Aa][Nn][Dd];
OR:[Oo][Rr];
XOR: [Xx][Oo][Rr];
IN: [Ii][Nn];
INITIALIZED: [Ii][Nn][Ii][Tt][Ii][Aa][Ll][Ii][Zz][Ee][Dd]; // Case insensitive Initialized
AUTHORIZED: [Aa][Uu][Tt][Hh][Oo][Rr][Ii][Zz][Ee][Dd]; // Case insensitive Autorized
COMPLETED: [Cc][Oo][Mm][Pp][Ll][Ee][Tt][Ee][Dd]; // Case insensitive Completed
CANCELLED: [Cc][Aa][Nn][Cc][Ee][Ll][Ll][Ee][Dd]; // Case insensitive Cancelled
FAILED: [Ff][Aa][Ii][Ll][Ee][Dd]; // Case insensitive Failed

P_YEARS: [0-9]+ 'Y';
P_MONTHS: [0-9]+ 'M';
P_DAYS: [0-9]+ 'D';
P_HOURS: [0-9]+ 'H';
P_MINUTES: [0-9]+ 'M';
P_SECONDS: [0-9]+ 'S';

fragment YEAR: [0-9][0-9][0-9][0-9];
fragment MONTH: [0-1][0-9];
fragment DAY: [0-3][0-9];

fragment HOUR: [0-2][0-9];
fragment MINUTE: [0-5][0-9];
fragment SECOND: [0-5][0-9];
fragment MILLISECOND: [0-9][0-9][0-9];


fragment DATE_FRAGMENT: YEAR '-' MONTH '-' DAY;
fragment TIME_FRAGMENT: HOUR ':' MINUTE ':' SECOND ('.' MILLISECOND)? 'Z';

DATETIME: DATE_FRAGMENT 'T' TIME_FRAGMENT;
DATE: DATE_FRAGMENT;

NOT_EQ: '!=' | '<>';
EQ: '=';

GREATER: '>';
GREATER_OR_EQUAL: '>=';
LESS: '<';
LESS_OR_EQUAL: '<=';

NOT: '!'|'not';
SUB: '-';
ADD: '+';
ID: ID_HEAD ID_TAIL*;

MUL: '*';
DIV: '/';
MOD: '%';