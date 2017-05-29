# Daemos

[![Build status](https://ci.appveyor.com/api/projects/status/9ga4p126ysglbh6a/branch/master?svg=true)](https://ci.appveyor.com/project/GeirGrusom/daemos/branch/master)

## License

This is licensed under the [MIT license](https://github.com/GeirGrusom/daemos/blob/master/LICENSE)

## Introduction

Daemos is a transaction engine for handling time and action based operations. Basically each transaction may have a script and / or an expiration time. When the expiration
time is hit the script will get executed. The script may produce a new transaction to be executed in the future.




## MuteScript

The transaction script language is a language called MuteScript (Mute). It's a relatively simple language, but it allows the execution to be suspended and continued at any point in time
by storing the transaction stage.

### Syntax

Note that any syntax described in this document is intended to explain the expressions; they're edited for brevity.

Generally Mute is a imperative programming language. It does not support user-defined types or functions currently (although user defined functions are planned, and some syntax for it exists).
Statements are terminated with `;` and is generally in the C camp of syntax.

```antlr4
statementBody
    : '{' statement* '}'
	;
```

#### Module

Each script is a module and module is currently mandatory (although planned for removal). So each script starts with the following:

```
module myModule;
```

#### Data type

Datatypes can be either nullable or non-nullable. The default is non-nullable. To make a datatype nullable append `?`.

```antlr4
dataType
    : (identifier) '?'?
	;
```

#### Variable declaration
Variables are declared with either `let` (readonly) or `var` (mutable). Some expressions *require* that you use `let` (such as importExpression).

```antlr4
declaration
    : ('let'|'var') identifier (':' dataType)? ('<-' (expression))
    | 'let' identifier '<-' 'import' dataType
	;
```

Import expressions are readonly since they denote unserializable dependencies for the script. This may be changed in the future.

#### Conditional expressions

Conditional execution is done using the `if` expression or statement. An `if` without braces covering all execution paths is an expression, but without braces is a statement.

```antlr4
ifExpression
    : 'if' '(' expression ')' expression ('elseif' '(' expression ')' expression)* ('else' expression)?
	;
```

#### Function call

Functions on dependencies (or other objects) can be called using familiar C-style syntax although it also includes named arguments.

```antlr4
argument:
    : expression
	;
namedArgument:
    : identifier ':' expression
	;
functionCall
    : expression '(' (argument)* ')'
	| expression '(' (namedArgument)+ ')'
	;
```

#### Cast expressions

Cast's are currently done using the cast operator, but will use function call syntax in the future.

```antlr4
cast
	: dataType '!' '(' expression ')'
	;


#### Literal expressions

```antlr4
literalExpression
	: ('true'|'false')
	| 'null'
	| [0-9]+
	| 'this'
	| '@' dateTime // Datetime follows ISO-8601 date format
	| '"' doubleQuotedStringContents '"'
	| '\'' singleQuotedStringContents '\''
	| ('initialized'|'authorized'|'completed'|'failed','cancelled')
	;
```

#### While loop

Follows regular C `while` and `do-while` expressions.

```antlr4
while
    : 'while' '(' expression ')' statementBody
	| 'do' statementBody 'while' '(' expression ')'
	;
```

#### Commit transaction

Commits the specified transaction to its storage engine.

```antlr4
commitExpression
    : 'commit' expression
	;
```

#### Await transaction

Awaits a committed transaction. This stores the scripts state and exits.

```antlr4
awaitExpression
    : 'await' expression
	;
```

#### Object expression

Object expressions are general object structure definitions. It more or less follows JSON syntax.

```antlr4
objectExpression
	: '{' (identifier ':' expression)* '}'
	;
```

#### With expression

Generally used to alter a transaction since transactions are immutable.

```antl4
withExpression
	: expression 'with' objectExpression
	;
```

## Usage

Run the console application using `dotnet`. It requires PostgreSQL server to run properly although an in-memory options is available.

### Command line arguments

Switch | Shorthand | Description
-------|------------|------------
--database-type | -d | Selects the database provider. Can be either `postgresql` or `memory`.
--connection-string | -c | Database provider connection string.
--port | -p | Specifies HTTP listening port