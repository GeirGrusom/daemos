using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using Irony.Parsing;
using Transact;
namespace Transact.Api
{
    public class TransactionMatchCompiler
    {
        private readonly Parser _parser;

        private static readonly ParameterExpression _transactionParameter = Expression.Parameter(typeof (Transaction),
            "transaction");
        public TransactionMatchCompiler()
        {
            var grammar = new TransactionMatchGrammar();
            var languageData = new LanguageData(grammar);
            _parser = new Parser(languageData);
        }

        public Expression<Func<Transaction, bool>> BuildExpression(string input)
        {
            var tree = _parser.Parse(input);

            var exp = Visit(tree.Root);

            return Expression.Lambda<Func<Transaction, bool>>(exp, _transactionParameter);
        }

        private static Expression Visit(ParseTreeNode node)
        {
            if (node.Term.Name == "statementList")
                return VisitStatementList(node);
            if (node.Term.Name == "statement")
                return VisitStatement(node);
            if (node.Term.Name == "number")
                return VisitNumber(node);
            if (node.Term.Name == "datetime")
                return VisitDateTime(node);
            if (node.Term.Name == "expr")
                return VisitExpr(node);
            if (node.Term.Name == "term")
                return VisitTerm(node);
            if(node.Term.Name == "binExpr")
                return VisitBinExpr(node);
            if (node.Term.Name == "bool")
                return VisitBoolean(node);
            if (node.Term.Name == "string")
                return VisitString(node);
            if (node.Term.Name == "null")
                return VisitNull(node);
            if (node.Term.Name == "guid")
                return VisitGuid(node);
            if (node.Term.Name == "identifier")
                return VisitIdentifier(node);
            if (node.Term.Name == "arrayList")
                return VisitArray(node);
            
            throw new NotImplementedException();
        }

        private static class EmptyArray<T>
        {
            public static T[] Instance { get; } = new T[0];
        }

        private static class EmptyHashSet<T>
        {
            public static HashSet<T> Instance { get; } = new HashSet<T>(); 
        }

        public static class Empty
        {
            public static T[] Array<T>()
            {
                return EmptyArray<T>.Instance;
            }

            public static HashSet<T> HashSet<T>()
            {
                return EmptyHashSet<T>.Instance;
            } 
        }
        private static Expression VisitArray(ParseTreeNode node)
        {
            var children = node.ChildNodes.Select(Visit).ToArray();

            if (children.Length == 0)
            {
                return Expression.Constant(Empty.HashSet<object>());
            }

            Type elementType = children[0].Type;
            if(children.Any(x => x.Type != elementType))
                throw new InvalidOperationException("All types must be the same in an array.");

            var hashSetType = typeof (HashSet<>).MakeGenericType(elementType);
            dynamic hashSet = Activator.CreateInstance(hashSetType);
            var addMethod = hashSetType.GetMethod("Add");

            foreach (var item in children)
            {
                addMethod.Invoke(hashSet, new [] {((ConstantExpression) item).Value});
            }

            return Expression.Constant(hashSet, hashSetType);
        }

        private static Expression VisitString(ParseTreeNode node)
        {
            return Expression.Constant(node.Token.ValueString);
        }

        private static Expression VisitGuid(ParseTreeNode node)
        {
            string value = node.FindTokenAndGetText();
            return Expression.Constant(Guid.ParseExact(value, "N"));
        }

        private static Expression VisitNull(ParseTreeNode node)
        {
            return Expression.Constant(null);
        }

        private static Expression VisitBoolean(ParseTreeNode node)
        {
            switch (node.FindTokenAndGetText())
            {
                case "true":
                    return Expression.Constant("true");
                case "false":
                    return Expression.Constant("false");
            }
            throw new NotSupportedException();
        }

        private static Expression VisitDateTime(ParseTreeNode node)
        {
            var text = node.FindTokenAndGetText().Trim('\'');
            return Expression.Constant(DateTime.ParseExact(text, new[]
            {
                "yyyy'-'MM'-'dd'Z'",
                "yyyy'-'MM'-'dd'Z'zzz",
                "yyyy'-'MM'-'ddTHH':'mm':'ss'Z'",
                "yyyy'-'MM'-'ddTHH':'mm':'ss'Z'zzz",
            }, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
        }

        private static Expression VisitIdentifier(ParseTreeNode node)
        {
            var value = node.FindTokenAndGetText();

            var prop = typeof (Transaction).GetProperty(value,
                BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public);
            return Expression.Property(_transactionParameter, prop);
            
        }

        private static Expression VisitMemberIdentifier(Expression parent, ParseTreeNode node)
        {
            var value = node.FindTokenAndGetText();
            switch (value)
            {
                case "true":
                    return Expression.Constant(true);
                case "false":
                    return Expression.Constant(false);
                case "null":
                    return Expression.Constant(null);
                default:
                    return Expression.Property(_transactionParameter, value);
            }
            throw new NotImplementedException();
        }

        private static Expression VisitTerm(ParseTreeNode node)
        {
            return Visit(node.ChildNodes.Single());
        }

        private static Expression ParseEnumString(Type enumType, ConstantExpression str)
        {
            object[] parameters = 
            {
                str.Value, true, null
            };

            var tryParseGeneric = typeof (Enum).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Single(x => x.Name == "TryParse" && x.GetParameters().Length == 3);
            var tryParse = tryParseGeneric.MakeGenericMethod(enumType);
            if ((bool) tryParse.Invoke(null, parameters))
                return Expression.Constant(parameters[2], enumType);

            throw new InvalidOperationException();
        }

        private static Expression VisitEqual(Expression left, Expression right)
        {
            if (left.Type.IsEnum && right is ConstantExpression && right.Type == typeof (string))
            {
                right = ParseEnumString(left.Type, (ConstantExpression) right);
            }
            if (right.Type.IsEnum && left is ConstantExpression && left.Type == typeof (string))
            {
                left = ParseEnumString(right.Type, (ConstantExpression)left);
            }
            return Expression.Equal(left, right);
        }

        private static Expression VisitNotEqual(Expression left, Expression right)
        {
            if (left.Type.IsEnum && right is ConstantExpression && right.Type == typeof(string))
            {
                right = ParseEnumString(left.Type, (ConstantExpression)right);
            }
            if (right.Type.IsEnum && left is ConstantExpression && left.Type == typeof(string))
            {
                left = ParseEnumString(right.Type, (ConstantExpression)left);
            }
            return Expression.NotEqual(left, right);
        }

        private static Expression VisitBinExpr(ParseTreeNode node)
        {
            var left = Visit(node.ChildNodes[0]);
            var op = node.ChildNodes[1];
            var right = Visit(node.ChildNodes[2]);
            
            switch (op.FindTokenAndGetText())
            {
                case "+":
                    return Expression.Add(left, right);
                case "-":
                    return Expression.Subtract(left, right);
                case "*":
                    return Expression.Multiply(left, right);
                case "/":
                    return Expression.Divide(left, right);
                case "=":
                    return VisitEqual(left, right);
                case "!=":
                    return VisitNotEqual(left, right);
                case ">":
                    return Expression.GreaterThan(left, right);
                case ">=":
                    return Expression.GreaterThanOrEqual(left, right);
                case "<":
                    return Expression.LessThan(left, right);
                case "<=":
                    return Expression.LessThanOrEqual(left, right);
                case "in":
                    return VisitIn(left, right);

            }
            throw new NotImplementedException();
        }

        private static Expression VisitIn(Expression left, Expression right)
        {
            if (left.Type.IsEnum && right is ConstantExpression)
            {
                var rightConst = (ConstantExpression) right;
                var items = (IEnumerable) rightConst.Value;
                var hashSetType = typeof(HashSet<>).MakeGenericType(left.Type);
                dynamic hashSet = Activator.CreateInstance(hashSetType);
                var addMethod = hashSetType.GetMethod("Add");
                foreach (var item in items)
                {
                    addMethod.Invoke(hashSet, new [] { Enum.Parse(left.Type, (string) item, true) });
                }
                right = Expression.Constant(hashSet, hashSetType);
            }
            var containsMethod = right.Type.GetMethod("Contains");
            return Expression.Call(right, containsMethod, left);
        }

        private static Expression VisitExpr(ParseTreeNode node)
        {
            return Visit(node.ChildNodes.Single());
        }

        private static Expression VisitNumber(ParseTreeNode node)
        {
            return Expression.Constant(decimal.Parse(node.FindTokenAndGetText()));
        }

        private static Expression VisitStatement(ParseTreeNode node)
        {
            return Visit(node.ChildNodes.Single());
        }

        private static Expression VisitStatementList(ParseTreeNode node)
        {
            if (node.ChildNodes.Count == 1)
            {
                return Visit(node.ChildNodes[0]);
            }
            var en = node.ChildNodes.GetEnumerator();
            en.MoveNext();
            return RecurseOrElse(en);

        }

        

        private static Expression RecurseOrElse(IEnumerator<ParseTreeNode> nodes)
        {
            var item = Visit(nodes.Current);
            if (nodes.MoveNext())
            {
                return Expression.OrElse(item, RecurseOrElse(nodes));
            }
            return item;
        }
    }

    public class TreeWalker
    {
        
    }
}
