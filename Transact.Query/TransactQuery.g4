grammar TransactQuery;


options {
	
}

@header {
	using System.Linq.Expressions;
	using static System.Linq.Expressions.Expression;
	using System.Reflection;
}

@members {
	public ParameterExpression Transaction { get; } = Parameter(typeof(Transaction));
    public static MemberExpression GetPropertyCI(Expression owner, string name)
    {
        return Property(owner, owner.Type.GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public));
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
	: additiveExpression { $expr = $additiveExpression.expr; }
	|	lhs = logicalExpression op = ('or' | 'and' | 'xor') rhs = additiveExpression {
		switch($op.text) {
			case "or":
				$expr = OrElse($lhs.expr, $rhs.expr);
				break;
			case "and":
				$expr = AndAlso($lhs.expr, $rhs.expr);
				break;
			case "xor":
				$expr = ExclusiveOr($lhs.expr, $rhs.expr);
				break;
			default:
				throw new System.NotSupportedException();
		}
	}
	;


additiveExpression returns [Expression expr]
	: comparisonExpression { $expr = $comparisonExpression.expr; }
	|lhs = additiveExpression op = ('+' | '-') rhs = comparisonExpression {
		switch($op.text) {
			case "+":
				$expr = AddChecked($lhs.expr, $rhs.expr);
				break;
			case "-":
				$expr = SubtractChecked($lhs.expr, $rhs.expr);
				break;
			default:
				throw new System.NotSupportedException();
		}
	}
	;

comparisonExpression returns [Expression expr]
	: multiplicativeExpression { $expr = $multiplicativeExpression.expr; }
	| lhs = comparisonExpression op = ('=' | '!=' | '<>' | '>' | '<' | '>=' | '<=')  rhs = multiplicativeExpression {
		switch($op.text) {
			case "=":
				if($lhs.expr.Type == typeof(JsonValue) || $rhs.expr.Type == typeof(JsonValue)) {
					$expr = Equal($lhs.expr, $rhs.expr, false,  typeof(JsonValue).GetMethod("Equals", new [] { $lhs.expr.Type, $rhs.expr.Type }));
				}
				else { 
					$expr = Equal($lhs.expr, $rhs.expr);
				}
				break;
			case ">":
				$expr = GreaterThan($lhs.expr, $rhs.expr);
				break;
			case ">=":
				$expr = GreaterThanOrEqual($lhs.expr, $rhs.expr);
				break;
			case "<":
				$expr = LessThan($lhs.expr, $rhs.expr);
				break;
			case "<=":
				$expr = LessThanOrEqual($lhs.expr, $rhs.expr);
				break;
			case "!=":
			case "<>":
				$expr = NotEqual($lhs.expr, $rhs.expr);
				break;
			default:
				throw new System.NotSupportedException();
		}
	}
	;


multiplicativeExpression returns [Expression expr]
	: unaryExpression { $expr = $unaryExpression.expr; }
	|lhs = multiplicativeExpression op = ('*' | '/' | '%') rhs = unaryExpression {
		switch($op.text) {
			case "*":
				$expr = Multiply($lhs.expr, $rhs.expr);
				break;
			case "/":
				$expr = Divide($lhs.expr, $rhs.expr);
				break;
			case "%":
				$expr = Modulo($lhs.expr, $rhs.expr);
				break;
			default:
				throw new System.NotSupportedException();
		}
	}
	;
	

unaryExpression returns [Expression expr]
	: op = ('not' | '!' | '-') operand = expression { 
		switch($op.text) {
			case "not":
			case "!":
				$expr = Not($operand.expr);
				break;
			case "-":
				$expr = Negate($operand.expr);
				break;
			default:
				throw new System.NotSupportedException();
		}
	}
	| '(' operand = expression { $expr = $expression.expr; } ')'
	| literalExpression { $expr = $literalExpression.expr; }
	| identifierChain { $expr = $identifierChain.expr; }
	;

	

expression returns [Expression expr] : logicalExpression { $expr = $logicalExpression.expr; };

literalExpression returns [Expression expr]
	: float { $expr = Constant($float.value, typeof(float));}
	| integer { $expr = Constant($integer.value, typeof(int)); }
	| quotedString { $expr = Constant($quotedString.value, typeof(string)); }
	| singleQuotedString { $expr = Constant($singleQuotedString.value, typeof(string)); }
	| boolean { $expr = Constant($boolean.value, typeof(bool)); }
	| null { $expr = Constant(null); }
	| date { $expr = Constant($date.value, typeof(System.DateTime)); }
	| guid { $expr = Constant($guid.value, typeof(System.Guid)); }
	;

identifierChain returns [Expression expr] :
	(identifiers += identifier ('.' identifiers += identifier)*) { 
		if($ctx._identifiers.Count == 1) {
			$expr = GetPropertyCI(Transaction, $ctx._identifiers[0].value);
		} else if($ctx._identifiers.Count == 2) {
			string dynamicObject = $ctx._identifiers[0].value;
			string memberName = $ctx._identifiers[1].value;
			var ctor = typeof(Transact.JsonValue).GetConstructor(new[] { typeof(IDictionary<string, object>), typeof(string), typeof(string) });
			$expr =  New(ctor, Expression.Convert(GetPropertyCI(Transaction, dynamicObject), typeof(IDictionary<string, object>)), Constant(dynamicObject), Constant(memberName));
		} else {
			throw new System.NotImplementedException();
		}
		
};

identifier returns [string value]
	:'@' quotedString { $value = $quotedString.value; }
	|'@' singleQuotedString { $value = $singleQuotedString.value; }
	| ID { $value = $ID.text; }
	;

quotedString returns [string value]: QUOTED_STRING { $value = $QUOTED_STRING.text.Substring(1, $QUOTED_STRING.text.Length - 2); } ;
singleQuotedString returns [string value]: SINGLE_QUOTED_STRING { $value = $SINGLE_QUOTED_STRING.text.Substring(1, $SINGLE_QUOTED_STRING.text.Length - 2); };


float returns [double value]
	: INT '.' INT  exponent { $value = double.Parse($ctx.GetText(), System.Globalization.CultureInfo.InvariantCulture); }
	| '.' INT exponent { $value = double.Parse($ctx.GetText(), System.Globalization.CultureInfo.InvariantCulture); }
	| INT exponent { $value = double.Parse($ctx.GetText(), System.Globalization.CultureInfo.InvariantCulture); }
	| INT '.' INT { $value = double.Parse($ctx.GetText(), System.Globalization.CultureInfo.InvariantCulture); }
	| '.' INT { $value = double.Parse($ctx.GetText(), System.Globalization.CultureInfo.InvariantCulture); }
	;

integer returns [int value]: INT { $value = int.Parse($INT.text, System.Globalization.CultureInfo.InvariantCulture); } ; 

exponent: 'e' ('+' | '-') INT; 

boolean returns [bool value] : token = TRUE { $value = true; } | FALSE { $value = false; };

null returns [object value]: NULL { $value = null; };

guid returns [System.Guid value]: GUID_SLASH | GUID { $value = System.Guid.Parse($ctx.GetText()); };

date returns [System.DateTime value]
	: DATE { $value = System.DateTime.ParseExact($DATE.text, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture); }
	| DATETIME { $value = System.DateTime.ParseExact($DATETIME.text, new [] { "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm:ss.fff" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal); }
	;
									

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
ID: ID_HEAD ID_TAIL*;
NULL: 'null';
TRUE: 'true';
FALSE: 'false';
AND: 'and';
OR:'or';
XOR: 'xor';
fragment FLOAT_INT: [0-9]+;

fragment YEAR: [0-9][0-9][0-9][0-9];
fragment MONTH: [0-1][0-9];
fragment DAY: [0-3][0-9];

fragment HOUR: [0-2][0-9];
fragment MINUTE: [0-5][0-9];
fragment SECOND: [0-5][0-9];
fragment MILLISECOND: [0-9][0-9][0-9];


DATE: YEAR '-' MONTH '-' DAY;
TIME: HOUR ':' MINUTE ':' SECOND ('.' MILLISECOND)? 'Z';

DATETIME: DATE 'T' TIME;