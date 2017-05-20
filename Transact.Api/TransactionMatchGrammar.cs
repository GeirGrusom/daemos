using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Parsing;
using Irony.Ast;

namespace Transact.Api
{

    public class TransactionMatchGrammar : Irony.Parsing.Grammar
    {
        public TransactionMatchGrammar()
        {
            var number = new Irony.Parsing.NumberLiteral("number", NumberOptions.AllowSign | NumberOptions.AllowStartEndDot);
            var identifier  = new Irony.Parsing.IdentifierTerminal("identifier");
            
            //var guid = new Irony.Parsing.RegexBasedTerminal("guid", @"\{a-fA-F0-9\}");
            var strDouble = new Irony.Parsing.StringLiteral("string", "\"", StringOptions.AllowsAllEscapes);
            
            

            const string dateRegex ="([0-9]{4}-[0-9]{2}-[0-9]{2})";
            const string timeRegex = "(T[0-9]{2}:[0-9]{2}:[0-9]{2})";
            const string timeZone = @"(\+[0-9]{2}:[0-9]{2})";
            var dateTime = new Irony.Parsing.RegexBasedTerminal("datetime", $@"'{dateRegex}{timeRegex}?Z{timeZone}?'");
            var boolean = new NonTerminal("bool");
            var nullObj = new NonTerminal("null");

            var payloadMember = new NonTerminal("payloadMember");

            var exp = new NonTerminal("expr");
            var term = new NonTerminal("term");
            var binExpr = new NonTerminal("binExpr");
            var parExpr = new NonTerminal("parExpr"); 
            var unExpr = new NonTerminal("unExpr");
            var unOp = new NonTerminal("unOp");
            var binOp = new NonTerminal("binOp");
            var arrExpr = new NonTerminal("array");
            var arrList = new NonTerminal("arrayList");

            var statement = new NonTerminal("statement");
            var statementList = new NonTerminal("statementList");
            
            var guid = new NonTerminal("guid");
            guid.Rule = "{" + new RegexBasedTerminal("[a-fA-F0-9]{8}(-[a-fA-F0-9]{4}){3}-[a-fA-F0-9]{12}") + "}";

            exp.Rule = term | unExpr | binExpr;
            term.Rule = number | nullObj | boolean | dateTime | strDouble | guid | identifier | parExpr | arrExpr | payloadMember;
            parExpr.Rule = "(" + exp + ")";
            unExpr.Rule = unOp + term;
            unOp.Rule = ToTerm("+") | "-" | "not";
            binExpr.Rule = exp + binOp + exp;
            binOp.Rule = ToTerm("+") | "-" | "*" | "/" | "." | ">" | "<" | ">="| "<=" | "=" | "!=" | "in" | "and" | "or";
            statement.Rule = exp;
            statementList.Rule = MakeStarRule(statementList, ToTerm(";"), statement);
            nullObj.Rule = ToTerm("null");
            boolean.Rule = ToTerm("true") | "false";

            payloadMember.Rule = "$" + identifier + "." + (identifier | strDouble);

            arrList.Rule = MakeListRule(arrList, ToTerm(","), term, TermListOptions.AllowTrailingDelimiter | TermListOptions.AllowEmpty | TermListOptions.AddPreferShiftHint);
            arrExpr.Rule = "[" + arrList + "]";
            this.Root = statementList;

            var operators = new []
            {
                new [] { "or" },
                new [] { "and"},
                new [] { "=", "!="},
                new [] { "<", ">", "<=", ">=", "in" },
                new [] { "+", "-"},
                new [] { "*", "/", "%"}
            };


            foreach (var item in operators.Select((strings, i) => new { strings, i} ))
            {
                RegisterOperators(item.i + 1, item.strings);
            }
            RegisterBracePair("(", ")");
            RegisterBracePair("[", "]");
            MarkPunctuation("(", ")", ";", "[", "]");
            MarkTransient(term, exp, statement, unOp, binOp, parExpr, arrExpr);
            
            //RegisterBracePair("(", ")");
            //MarkPunctuation(";");
            this.LanguageFlags = LanguageFlags.Default;
        }
    }
}
