using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Antlr4.Runtime;
using Daemos.Mute.Expressions;
using Daemos.Scripting;

namespace Daemos.Mute
{
    partial class MuteGrammarParser
    {

        public class ModuleInfo
        {
            public string Name { get; }

            public ModuleInfo(string name)
            {
                Name = name;
            }
        }

        private static readonly Type[] ArithmeticTypes =
        {
            typeof(byte),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(float),
            typeof(double)
        };

        private static bool IsArithmetic(Type t)
        {
            return ArithmeticTypes.Contains(t);
        }

        private static bool IsArithmetic<T>()
        {
            return ArithmeticTypes.Contains(typeof(T));
        }

        public List<CompilationMessage> Messages { get; } = new List<CompilationMessage>();

        public Type TypeLookup(string name)
        {
            if (UsingTypes.TryGetValue(name, out Type resultType))
            {
                return resultType;
            }
            return Type.GetType(name, throwOnError: true, ignoreCase: false);
        }

        protected readonly Stack<List<VariableExpression>> VariableScope = new Stack<List<VariableExpression>>();
        protected List<VariableExpression> LastPoppedScope;

        public Dictionary<string, Type> UsingTypes { get; set; }

        public NamespaceLookup NamespaceLookup { get; set; }

        protected void PushScope()
        {
            VariableScope.Push(new List<VariableExpression>());
        }

        protected void PopScope()
        {
            LastPoppedScope = VariableScope.Pop();
        }

        protected VariableExpression Lookup(string variableName, ParserRuleContext context)
        {
            var variable = VariableScope.SelectMany(x => x).Reverse().FirstOrDefault(x => x.Name == variableName);
            if (variable == null)
            {
                AddSyntaxError($"The variable '{variableName}' is not accessible in this scope.", context);
            }
            return variable;
        }

        protected VariableExpression AddVariable(VariableExpression expression)
        {
            VariableScope.Peek().Add(expression);
            return expression;
        }

        protected Expression And(Expression lhs, Expression rhs, ParserRuleContext context)
        {
            if (lhs == null || rhs == null)
            {
                return null;
            }
            return new BinaryAndExpression(lhs, rhs, context);
        }

        protected Expression Or(Expression lhs, Expression rhs, ParserRuleContext context)
        {
            if (lhs == null || rhs == null)
            {
                return null;
            }
            return new BinaryOrExpression(lhs, rhs, context);
        }

        protected Expression Xor(Expression lhs, Expression rhs, ParserRuleContext context)
        {
            if (lhs == null || rhs == null)
            {
                return null;
            }
            return new BinaryXorExpression(lhs, rhs, context);
        }

        protected T Binary<T>(Expression lhs, Expression rhs, string opName, ParserRuleContext context)
            where T : BinaryExpression
        {
            if (lhs == null || rhs == null)
            {
                return null;
            }

            if (lhs.Type != rhs.Type)
            {
                AddSyntaxError("Types mismatch. Are you missing a cast?", context);
                return null;
            }

            if (IsArithmetic(lhs.Type.ClrType))
            {
                return (T)Activator.CreateInstance(typeof(T), lhs, rhs, context);
            }
            else
            {
                var op = FindOperator(lhs.Type.ClrType, "Add", context);
                if (op == null)
                {
                    return null;
                }
                return (T)Activator.CreateInstance(typeof(T), lhs, rhs, op, context);
            }
        }

        protected Expression Add(Expression lhs, Expression rhs, ParserRuleContext context)
        {
            if (lhs.Type == DataType.NonNullDateTime && rhs.Type == DataType.NonNullTimeSpan)
            {
                return new BinaryAddExpression(lhs, rhs, typeof(DateTime).GetMethod(nameof(System.DateTime.Add)), context);
            }
            if (lhs.Type != rhs.Type)
            {
                AddSyntaxError("Types mismatch", context);
                return null;
            }
            return Binary<BinaryAddExpression>(lhs, rhs, "Add", context);
        }

        protected Expression Using(IList<string> namespaceList, IList<string> typeList, ParserRuleContext context)
        {
            var result = new UsingExpression(namespaceList, typeList, context);

            if (!NamespaceLookup.TryGetNamespace(result.Namespace, out ImportedNamespace types))
            {
                AddSyntaxError($"Could not locate the namespace {result.Namespace}.", context);
                return null;
            }

            foreach (var item in typeList)
            {
                var type = types.GetType(item);
                if (type == null)
                {
                    AddSyntaxError($"Could not locate the type '{item}' in the namespace '{result.Namespace}'.", context);
                }
                UsingTypes.Add("item", type);
            }
            return result;
        }

        protected Expression UsingAll(IList<string> namespaceList, ParserRuleContext context)
        {
            var result = new UsingExpression(namespaceList, null, context);

            if (!NamespaceLookup.TryGetNamespace(result.Namespace, out ImportedNamespace types))
            {
                AddSyntaxError($"Could not locate the namespace {result.Namespace}.", context);
                return null;
            }

            foreach (var item in types)
            {
                UsingTypes.Add(item.Key, item.Value);
            }

            return result;
        }

        private MethodInfo FindOperator(Type type, string operatorName, ParserRuleContext context)
        {
            var res = type.GetMethod(operatorName, new Type[] { type, type });

            if(res == null || !res.IsStatic)
            {
                AddSyntaxError($"There is no suitably defined '{operatorName}' operator defined for the type {type.Name}.", context);
            }
            return res;
        }

        protected Expression Sub(Expression lhs, Expression rhs, ParserRuleContext context)
        {
            return Binary<BinarySubtractExpression>(lhs, rhs, "Subtract", context);
        }

        protected Expression Mul(Expression lhs, Expression rhs, ParserRuleContext context)
        {
            return Binary<BinaryMultiplyExpression>(lhs, rhs, "Multiply", context);
        }

        protected Expression Div(Expression lhs, Expression rhs, ParserRuleContext context)
        {
            return Binary<BinaryDivideExpression>(lhs, rhs, "Divide", context);
        }

        protected Expression Mod(Expression lhs, Expression rhs, ParserRuleContext context)
        {
            return Binary<BinaryRemainderExpression>(lhs, rhs, "Remainder", context);
        }

        protected bool IsComparable(Type t, ParserRuleContext context)
        {
            if (!IsArithmetic(t))
            {
                var comparableInterface = typeof(IComparable<>).MakeGenericType(t);
                if (!t.GetInterfaces().Contains(comparableInterface))
                {
                    AddSyntaxError($"The type {t.Name} is not comparable.", context);
                    return false;
                }
            }
            return true;

        }

        protected T BinaryCompare<T>(Expression lhs, Expression rhs, ParserRuleContext context)
            where T : BinaryExpression
        {
            if (lhs == null || rhs == null)
            {
                return null;
            }

            if(lhs.Type != rhs.Type)
            {
                AddSyntaxError("Types mismatch. Are you missing a cast?", context);
                return null;
            }

            var t = lhs.Type.ClrType;
            if (!IsArithmetic(t))
            {
                var comparableInterface = typeof(IComparable<>).MakeGenericType(t);
                if (!t.GetInterfaces().Contains(comparableInterface))
                {
                    AddSyntaxError($"The type {t.Name} is not comparable.", context);
                    return null;
                }
                else
                {
                    return (T)Activator.CreateInstance(typeof(T), lhs, rhs, t.GetMethod("CompareTo", new Type[] { t }), context);
                }
            }
            return (T)Activator.CreateInstance(typeof(T), lhs, rhs, context);
        }

        protected BinaryGreaterExpression Greater(Expression lhs, Expression rhs, ParserRuleContext context)
        {
            return BinaryCompare<BinaryGreaterExpression>(lhs, rhs, context);
        }

        protected BinaryGreaterOrEqualExpression GreaterOrEqual(Expression lhs, Expression rhs, ParserRuleContext context)
        {
            return BinaryCompare<BinaryGreaterOrEqualExpression>(lhs, rhs, context);
        }

        protected BinaryLessExpression Less(Expression lhs, Expression rhs, ParserRuleContext context)
        {
            return BinaryCompare<BinaryLessExpression>(lhs, rhs, context);
        }

        protected BinaryLessOrEqualExpression LessOrEqual(Expression lhs, Expression rhs, ParserRuleContext context)
        {
            return BinaryCompare<BinaryLessOrEqualExpression>(lhs, rhs, context);
        }

        protected BinaryEqualExpression Eq(Expression lhs, Expression rhs, ParserRuleContext context)
        {
            if (rhs == null || lhs == null)
            {
                return null;
            }

            if(lhs.Type != rhs.Type)
            {
                AddSyntaxError("Types mismatch. Are you missing a cast?", context);
                return null;
            }

            if(!IsArithmetic(lhs.Type.ClrType))
            {
                var eqType = typeof(IEquatable<>).MakeGenericType(lhs.Type.ClrType);

                if(lhs.Type.ClrType.GetInterfaces().Contains(eqType))
                {
                    AddSyntaxError($"There is no defined equality operator for the type {lhs.Type.ClrType.Name}.", context);
                    return null;
                }

                var meth = lhs.Type.ClrType.GetMethod("Equals", new[] { lhs.Type.ClrType });

                return new BinaryEqualExpression(lhs, rhs, meth, context);
            }

            return new BinaryEqualExpression(lhs, rhs, context);
        }

        protected Expression Not(Expression operand, ParserRuleContext context)
        {
            if (operand == null)
            {
                return null;
            }
            return new UnaryNotExpression(operand, context);
        }

        protected Expression Neg(Expression operand, ParserRuleContext context)
        {
            if (operand == null)
            {
                return null;
            }
            return new UnarySubtractExpression(operand, context);
        }

        protected Expression Add(Expression operand, ParserRuleContext context)
        {
            if (operand == null)
            {
                return null;
            }

            return new UnaryAddExpression(operand, context);
        }

        protected Expression Await(Expression operand, ParserRuleContext context)
        {
            if (operand == null)
            {
                return null;
            }

            return new UnaryAwaitExpression(operand, context);
        }

        protected void AddSyntaxError(string message, ParserRuleContext context)
        {
            Messages.Add(new CompilationMessage(message, context.start.Line, context.start.Column, MessageSeverity.Error));
        }
        protected void AddSyntaxWarning(string message, ParserRuleContext context)
        {
            Messages.Add(new CompilationMessage(message, context.start.Line, context.start.Column, MessageSeverity.Warning ));
        }

        protected Expression Convert(DataType type, Expression operand, ParserRuleContext context)
        {
            if (type.ClrType == null || operand == null)
            {
                return null;
            }
            if (type == operand.Type)
            {
                AddSyntaxWarning($"Redundant conversion by trying to convert {type.Name} to itself.", context);
            }
            return new UnaryConvertExpression(type, operand, context);
        }

        protected Expression Assign(VariableExpression lhs, Expression value, ParserRuleContext context)
        {

            if (lhs == null || value == null)
            {
                return null;
            }

            if (lhs.Type != value.Type)
            {
                AddSyntaxError($"Cannot assign a '{value.Type}' to the variable '{lhs.Name}'({lhs.Type.Name}). Consider casting the value.", context);
                return null;
            }
            if (!lhs.Type.Nullable && value.Type.Nullable)
            {
                AddSyntaxError($"Cannot assign a nullable value to the non-nullable variable '{lhs.Name}'.", context);
                return null;
            }
            if (lhs == value)
            {
                AddSyntaxError($"Cannot assign '{lhs.Name}' to itself.", context);
                return null;
            }

            if (!lhs.Mutable)
            {
                AddSyntaxError($"The variable '{lhs.Name}' is not mutable and can therefore not be the target of an assignment.", context);
                return null;
            }
            if (!lhs.Type.Nullable)
            {
                if (value == ConstantExpression.NullExpression)
                {
                    AddSyntaxError($"Cannot assign null to the non-nullable variable '{lhs.Name}'.", context);
                }
            }
            return new BinaryAssignExpression(lhs, value, context);
        }

        protected Expression Valid<T>(Expression exp)
        {
            if (exp == null)
            {
                return null;
            }
            if (typeof(T) != exp.Type.ClrType)
            {
                throw new ArgumentException($"Expression has to return a {typeof(T).Name} but instead is {exp.Type.Name}.");
            }
            return exp;
        }

        protected Expression Member(Expression instance, string name, ParserRuleContext context)
        {
            if (instance == null || name == null)
            {
                return null;
            }

            if (instance.Type == DataType.Dynamic)
            {
                return new CallExpression(typeof(IDictionary<string, object>).GetMethod("get_Item", new [] { typeof(string) }), instance, new Expression[] { new ConstantExpression(DataType.NonNullString, name, context) }, context);
            }

            var type = instance.Type.ClrType;
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(x => x.Name == name);

            var member = members.SingleOrDefault();
            if (member == null)
            {
                AddSyntaxError($"The member {name} was not found on the type {instance.Type.ClrType.Name}.", context);
                return null;
            }

            if (instance.Type.ClrType == typeof(Transaction) && name == nameof(Transaction.Payload))
            {
                return new UnaryConvertExpression(DataType.Dynamic, new MemberExpression(instance, member, context), context);
            }

            return new MemberExpression(instance, member, context);
        }
        
        protected WithExpression With(Expression lhs, ObjectExpression rhs, ParserRuleContext context)
        {
            if (lhs.Type.ClrType == typeof(Transaction))
            {
                ValidateTransactionWithExpression(rhs, context);
            }
            return new WithExpression(lhs, rhs, context);
        }

        private static readonly HashSet<string> MutableTransactionProperties = new HashSet<string>
        {
            nameof(Transaction.State),
            nameof(Transaction.Expires),
            nameof(Transaction.Handler),
            nameof(Transaction.Payload),
            nameof(Transaction.Script),
        };

        protected ConstantExpression TransactionState(string state, ParserRuleContext ctx)
        {
            if(state == "commit")
            {
                AddSyntaxError("commit is not a valid transaction state.", ctx);
                return null;
            }
            switch(state)
            {
                case "initialize":
                    return new ConstantExpression(Daemos.TransactionState.Initialized, ctx);
                case "authorize":
                    return new ConstantExpression(Daemos.TransactionState.Authorized, ctx);
                case "complete":
                    return new ConstantExpression(Daemos.TransactionState.Completed, ctx);
                case "fail":
                    return new ConstantExpression(Daemos.TransactionState.Failed, ctx);
                case "cancel":
                    return new ConstantExpression(Daemos.TransactionState.Cancelled, ctx);
                default:
                    AddSyntaxError($"Transaction state '{state}' is not recognized.", ctx);
                    return null;
            }
        }

        protected Expression NotNull(Expression expr, ParserRuleContext ctx)
        {
            if(expr == null)
            {
                return null;
            }
            if(!expr.Type.Nullable)
            {
                AddSyntaxError($"The expression '{ctx.GetChild(1).GetText()}' is not nullable.", ctx);
                return null;
            }
            Type clrType;
            if (expr.Type.ClrType.GetTypeInfo().IsValueType)
            {
                clrType = expr.Type.ClrType.GetGenericArguments()[0];
            }
            else
            {
                clrType = expr.Type.ClrType;
            }
            return new UnaryConvertExpression(new DataType(clrType, false),  expr, ctx);
        }

        protected void ValidateTransactionWithExpression(ObjectExpression exp, ParserRuleContext context)
        {
            if (exp == null)
            {
                return;
            }
            foreach(var member in exp.Members)
            {
                if(!MutableTransactionProperties.Contains(member.Name))
                {
                    AddSyntaxError($"The property '{member.Name}' either does not exist or is not mutable.", member.Context);
                    continue;
                }
                if(member.Name == nameof(Transaction.Payload))
                {
                    ValidatePayloadMembers(member);
                    continue;
                }
                if(member.Name == nameof(Transaction.State))
                {
                    if(member.Type.Nullable || (member.Type.ClrType != typeof(TransactionState) && member.Type.ClrType != typeof(string)))
                    {
                        AddSyntaxError($"Expected a transaction state; got {member.Type}.", member.Context);
                    }
                    continue;
                }
                if(member.Name == nameof(Transaction.Expires))
                {
                    if(member.Type.ClrType != typeof(DateTime))
                    {
                        AddSyntaxError($"Expires must be a datetime.", member.Context);
                    }
                    continue;
                }
                if(member.Name == nameof(Transaction.Script))
                {
                    if(member.Type.ClrType != typeof(string))
                    {
                        AddSyntaxError("Script must be a string.", member.Context);
                    }
                    continue;
                }
                if (member.Name == nameof(Transaction.Handler))
                {
                    if (member.Type.ClrType != typeof(string))
                    {
                        AddSyntaxError("Handler must be a string.", member.Context);
                    }
                    continue;
                }
            }
        }

        protected void ValidatePayloadMembers(ObjectMember payload)
        {
            if (!(payload.Value is ObjectExpression))
            {
                AddSyntaxError("Payload must be an object expression.", payload.Context);
            }
        }

        private T GetMethodForNamedArguments<T>(Type type, List<NamedArgument> arguments, T[] methods)
            where T : MethodBase
        {
            foreach (var method in methods)
            {
                if (method.DeclaringType == typeof(object))
                {
                    continue;
                }

                var parameters = method.GetParameters().ToList();

                if (parameters.Count > arguments.Count)
                {
                    continue;
                }

                for (int i = 0; i < arguments.Count; ++i)
                {
                    var arg = arguments[i];
                    int removeIndex = -1;
                    for (int j = 0; j < parameters.Count; ++j)
                    {
                        if (parameters[j].Name == arg.Argument && parameters[j].ParameterType == arg.Type.ClrType)
                        {
                            removeIndex = j;
                            break;
                        }
                    }
                    if (removeIndex == -1)
                        break;
                    parameters.RemoveAt(removeIndex);
                }

                if (parameters.Any(x => !x.HasDefaultValue))
                    continue;

                return method;
            }

            return null;
        }

        private ConstructorInfo GetConstructorForNamedArguments(Type type, List<NamedArgument> arguments)
        {
            var constructors = type.GetConstructors();

            return GetMethodForNamedArguments(type, arguments, constructors);

        }

        private MethodInfo GetMethodForNamedArguments(Type type, List<NamedArgument> arguments, string name)
        {
            var methods = type.GetMethods().Where(x => x.Name == name).ToArray();

            return GetMethodForNamedArguments(type, arguments, methods);
        }

        private MethodInfo GetMethodForArguments(Type type, List<Expression> arguments, string name)
        {
            var methods = type.GetMethods().Where(x => x.Name == name).ToArray();

            return GetMethodForArguments(type, arguments, methods);
        }

        private ConstructorInfo GetConstructorForArguments(Type type, List<Expression> arguments)
        {
            var methods = type.GetConstructors();

            return GetMethodForArguments(type, arguments, methods);
        }

        private T GetMethodForArguments<T>(Type type, List<Expression> arguments, T[] methods) where T : MethodBase
        {
            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (arguments.Count > parameters.Length)
                    continue;
                bool failed = false;
                for(int i = 0; i < arguments.Count; ++i)
                {
                    if (arguments[i].Type.ClrType != parameters[i].ParameterType)
                    {
                        failed = true;
                        break;
                    }
                }

                if (failed)
                    continue;

                for (int i = arguments.Count; i < parameters.Length; ++i)
                {
                    if (!parameters[i].HasDefaultValue)
                        failed = true;
                }
                if (failed)
                    continue;

                return method;
            }
            return null;
        }

        protected CallExpression Call(string methodName, List<NamedArgument> arguments, ParserRuleContext context)
        {

            if (arguments == null)
            {
                return null;
            }
            if (arguments.Any(x => x == null))
            {
                return null;
            }

            MethodInfo method = null;

            if (methodName == "print")
            {
                method = typeof(Console).GetMethod("WriteLine", arguments.Select(x => x.Type.ClrType).ToArray());
            }

            if (UsingTypes.TryGetValue(methodName, out Type typeToConstruct))
            {
                var ctor = GetConstructorForNamedArguments(typeToConstruct, arguments);
                if (ctor == null)
                {
                    AddSyntaxError("Could not find a constructor that fit the type arguments.", context);
                    return null;
                }
                return new CallExpression(ctor, arguments, context);
            }


            if (method == null)
            {
                AddSyntaxError($"Could not locate the method {methodName}.", context);
                return null;
            }

            return new CallExpression(method, null, arguments, context);
        }
        protected CallExpression Call(string methodName, List<Expression> arguments, ParserRuleContext context)
        {
            
            if (arguments == null)
            {
                return null;
            }
            if (arguments.Any(x => x == null))
            {
                return null;
            }

            MethodInfo method = null;

            if (methodName == "print")
            {
                method = typeof(Console).GetMethod("WriteLine", arguments.Select(x => x.Type.ClrType).ToArray());
            }
                
            if (UsingTypes.TryGetValue(methodName, out Type typeToConstruct))
            {
                var ctor = GetConstructorForArguments(typeToConstruct, arguments);
                if (ctor == null)
                {
                    AddSyntaxError("Could not find a constructor that fit the type arguments.", context);
                    return null;
                }
                return new CallExpression(ctor, arguments, context);
            }


            if (method == null)
            {
                AddSyntaxError($"Could not locate the method {methodName}.", context);
                return null;
            }

            return new CallExpression(method, null, arguments, context);
        }

        protected CallExpression Call(Expression instance, List<Expression> arguments, ParserRuleContext context)
        {

            if (arguments == null|| arguments.Any(x => x == null))
            {
                return null;
            }

            if (!(instance is MemberExpression mem))
            {
                AddSyntaxError("Unrecognized call expression", context);
                return null;
            }
            if (!(mem.Member is MethodInfo))
            {
                AddSyntaxError($"{mem.Member.Name} is not a method.", context);
                return null;
            }

            var method = GetMethodForArguments(mem.Instance.Type.ClrType, arguments, mem.Member.Name);

            if (method == null)
            {
                AddSyntaxError($"Could not locate a suitable overload of {mem.Member.Name}.", context);
            }


            return new CallExpression(method, instance, arguments, context);
        }

        protected CallExpression Call(Expression instance, List<NamedArgument> arguments, ParserRuleContext context)
        {

            if (arguments == null || arguments.Any(x => x == null))
            {
                return null;
            }
            if (!(instance is MemberExpression mem))
            {
                AddSyntaxError("Unrecognized call expression", context);
                return null;
            }
            if (!(mem.Member is MethodInfo))
            {
                AddSyntaxError($"{mem.Member.Name} is not a method.", context);
                return null;
            }

            var method = GetMethodForNamedArguments(instance.Type.ClrType, arguments, mem.Member.Name);

            if (method == null)
            {
                AddSyntaxError($"Could not locate a suitable overload of {mem.Member.Name}.", context);
            }


            return new CallExpression(method, instance, arguments, context);
        }

        protected string Unescape(string input, char tokenBarrier)
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


        private void ValidateDate(int year, int month, int day, ParserRuleContext ctx)
        {
            if (year <= 0 || year >= 9999 || month <= 0 || month > 12 || day <= 0 || day > CultureInfo.CurrentCulture.Calendar.GetDaysInMonth(year, month))
            {
                AddSyntaxError("The specified date is outside of valid values.", ctx);
            }
        }

        private void ValidateTime(int hour, int minute, int second, ParserRuleContext ctx)
        {
            if (hour >= 24 || minute >= 60 || second >= 60)
            {
                AddSyntaxError("The specified date is outside of valid values.", ctx);
            }
        }

        private void ValidateOffset(int hour, int minute, ParserRuleContext ctx)
        {
            if (hour >= 24)
            {
                AddSyntaxError("The specified date is outside of valid values.", ctx);
            }
        }


        protected ConstantExpression DateTime(int year, int month, int day, int hour, int minute, int second,
            string offsetType, int offsetHour, int offsetMinute, ParserRuleContext context)
        {
            ValidateDate(year, month, day, context);
            ValidateTime(hour, minute, second, context);
            ValidateOffset(offsetHour, offsetMinute, context);
            var ts = offsetType == "+" ? new TimeSpan(offsetHour, offsetMinute, 0) : -new TimeSpan(offsetHour, offsetMinute, 0);
            return new ConstantExpression(DataType.NonNullDateTime, new DateTimeOffset(year, month, day, hour, minute, second, new TimeSpan(offsetHour, offsetMinute, 0)).UtcDateTime, context);
        }
        
        protected ConstantExpression DateTime(int year, int month, int day, int hour, int minute, int second,
            string offsetType, int offsetHour, ParserRuleContext context)
        {
            ValidateDate(year, month, day, context);
            ValidateTime(hour, minute, second, context);
            ValidateOffset(offsetHour, 0, context);
            var ts = offsetType == "+" ? new TimeSpan(offsetHour, 0, 0) : -new TimeSpan(offsetHour, 0, 0);
            return new ConstantExpression(DataType.NonNullDateTime, new DateTimeOffset(year, month, day, hour, minute, second, ts).UtcDateTime, context);
        }

        protected ConstantExpression DateTime(int year, int month, int day, int hour, int minute,
            string offsetType, int offsetHour, int offsetMinute, ParserRuleContext context)
        {
            ValidateDate(year, month, day, context);
            ValidateTime(hour, minute, 0, context);
            ValidateOffset(offsetHour, offsetMinute, context);

            var ts = offsetType == "+" ? new TimeSpan(offsetHour, offsetMinute, 0) : -new TimeSpan(offsetHour, offsetMinute, 0);
            return new ConstantExpression(DataType.NonNullDateTime, new DateTimeOffset(year, month, day, hour, minute, 0, new TimeSpan(offsetHour, offsetMinute, 0)).UtcDateTime, context);
        }

        protected ConstantExpression DateTime(int year, int month, int day, 
            string offsetType, int offsetHour, ParserRuleContext context)
        {
            ValidateDate(year, month, day, context);
            ValidateOffset(offsetHour, 0, context);

            var ts = offsetType == "+" ? new TimeSpan(offsetHour, 0, 0) : -new TimeSpan(offsetHour, 0, 0);
            return new ConstantExpression(DataType.NonNullDateTime, new DateTimeOffset(year, month, day, 0, 0, 0, ts).UtcDateTime, context);
        }

        protected ConstantExpression DateTime(int year, int month, int day, 
            string offsetType, int offsetHour, int offsetMinute, ParserRuleContext context)
        {
            ValidateDate(year, month, day, context);
            ValidateOffset(offsetHour, offsetMinute, context);

            var ts = offsetType == "+" ? new TimeSpan(offsetHour, offsetMinute, 0) : -new TimeSpan(offsetHour, offsetMinute, 0);
            return new ConstantExpression(DataType.NonNullDateTime, new DateTimeOffset(year, month, day, 0, 0, 0, new TimeSpan(offsetHour, offsetMinute, 0)).UtcDateTime, context);
        }

        protected ConstantExpression DateTime(int year, int month, int day, int hour, int minute,
            string offsetType, int offsetHour, ParserRuleContext context)
        {
            ValidateDate(year, month, day, context);
            ValidateTime(hour, minute, 0, context);
            ValidateOffset(offsetHour, 0, context);

            var ts = offsetType == "+" ? new TimeSpan(offsetHour, 0, 0) : -new TimeSpan(offsetHour, 0, 0);
            return new ConstantExpression(DataType.NonNullDateTime, new DateTimeOffset(year, month, day, hour, minute, 0, ts).UtcDateTime, context);
        }

        protected ConstantExpression DateTime(int year, int month, int day, int hour, int minute, int second, ParserRuleContext context)
        {
            ValidateDate(year, month, day, context);
            ValidateTime(hour, minute, second, context);

            return new ConstantExpression(DataType.NonNullDateTime, new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local).ToUniversalTime(), context);
        }

        protected ConstantExpression DateTimeUtc(int year, int month, int day, int hour, int minute, int second, ParserRuleContext context)
        {
            ValidateDate(year, month, day, context);
            ValidateTime(hour, minute, second, context);

            return new ConstantExpression(DataType.NonNullDateTime, new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc), context);
        }


        protected ConstantExpression DateTime(int year, int month, int day, int hour, int minute, ParserRuleContext context)
        {
            ValidateDate(year, month, day, context);
            ValidateTime(hour, minute, 0, context);

            return new ConstantExpression(DataType.NonNullDateTime, new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Local).ToUniversalTime(), context);
        }

        protected ConstantExpression DateTimeUtc(int year, int month, int day, int hour, int minute, ParserRuleContext context)
        {
            ValidateDate(year, month, day, context);
            ValidateTime(hour, minute, 0, context);

            return new ConstantExpression(DataType.NonNullDateTime, new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc), context);
        }


        protected ConstantExpression DateTime(int year, int month, int day, int hour, ParserRuleContext context)
        {
            ValidateDate(year, month, day, context);
            ValidateTime(hour, 0, 0, context);

            return new ConstantExpression(DataType.NonNullDateTime, new DateTime(year, month, day, hour, 0, 0, DateTimeKind.Local).ToUniversalTime(), context);
        }

        protected ConstantExpression DateTimeUtc(int year, int month, int day, int hour, ParserRuleContext context)
        {
            ValidateDate(year, month, day, context);
            ValidateTime(hour, 0, 0, context);

            return new ConstantExpression(DataType.NonNullDateTime, new DateTime(year, month, day, hour, 0, 0, DateTimeKind.Utc), context);
        }


        protected ConstantExpression DateTime(int year, int month, int day, ParserRuleContext context)
        {
            ValidateDate(year, month, day, context);

            return new ConstantExpression(DataType.NonNullDateTime, new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local).ToUniversalTime(), context);
        }

        protected ConstantExpression DateTimeUtc(int year, int month, int day, ParserRuleContext context)
        {
            ValidateDate(year, month, day, context);

            return new ConstantExpression(DataType.NonNullDateTime, new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc), context);
        }

        private static readonly string[] DateTimeFormats =
        {
            "'@'yyyy'-'MM'-'dd'T'HH':'mm':'ssK",
            "'@'yyyy'-'MM'-'dd'T'HH':'mm':'ss",
            "'@'yyyy'-'MM'-'dd'T'HH':'mmK",
            "'@'yyyy'-'MM'-'dd'T'HH':'mm",
            "'@'yyyy'-'MM'-'ddK",
            "'@'yyyy'-'MM'-'dd",
        };

        protected ConstantExpression DateTime(string input, ParserRuleContext context)
        {
            if (!System.DateTime.TryParseExact(input, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime result))
            {
                AddSyntaxError("The specified datetime is not in a valid format.", context);
                return null;
            }
            return new ConstantExpression(DataType.NonNullDateTime, result, context);
        }

        protected CommitTransactionExpression CommitTransaction(Expression commitValue, ParserRuleContext context)
        {
            return new CommitTransactionExpression(commitValue, false, context);
        }

        protected CommitTransactionExpression CommitTransactionChild(Expression commitValue, ParserRuleContext context)
        {
            return new CommitTransactionExpression(commitValue, true, context);
        }


        protected VariableDeclarationExpression Declare(string name, bool mutable, DataType type, ParserRuleContext context)
        {
            if (name == null || type.Equals(default(DataType)))
            {
                return null;
            }
            if (type.Nullable && type.ClrType.GetTypeInfo().IsValueType)
            {
                type = new DataType(typeof(Nullable<>).MakeGenericType(type.ClrType), true);
            }
            var variable = AddVariable(new VariableExpression(name, mutable, type, context));
            return new VariableDeclarationExpression(variable, null, context);
        }

        protected VariableDeclarationExpression Declare(string name, bool mutable, Expression assignment, ParserRuleContext context)
        {
            if (name == null || assignment == null)
            {
                return null;
            }
            var variable = AddVariable(new VariableExpression(name, mutable, assignment.Type, context));
            return new VariableDeclarationExpression(variable, assignment, context);
        }

        public ImportExpression Import(DataType type, ParserRuleContext context)
        {
            if (type.Equals(default(DataType)))
            {
                return null;
            }
            return new ImportExpression(type, null, context);
        }

        public ImportExpression Import(DataType type, string name, ParserRuleContext context)
        {
            if (type.Equals(default(DataType)))
            {
                return null;
            }
            return new ImportExpression(type, name, context);
        }

        protected ConditionalExpression If(Expression condition, Expression ifValue, ParserRuleContext context)
        {
            if (condition == null || ifValue == null)
            {
                return null;
            }
            return new ConditionalExpression(condition, ifValue, null, context);
        }

        protected ConditionalExpression If(Expression condition, Expression ifValue, Expression elseValue, ParserRuleContext context)
        {
            if (condition == null || ifValue == null || elseValue == null)
            {
                return null;
            }
            return new ConditionalExpression(condition, ifValue, elseValue, context);
        } 

        protected ObjectMember ObjectMember(string name, Expression value, ParserRuleContext context)
        {
            if (name == null || value == null)
            {
                return null;
            }
            return new ObjectMember(name, value, context);
        }

        protected ObjectExpression Object(IEnumerable<ObjectMember> members, ParserRuleContext context)
        {
            var mem = members.ToArray();
            if (mem.Any(x => x == null))
            {
                return null;
            }
            return new ObjectExpression(mem, context);
        }

        protected WhileExpression While(Expression condition, BlockExpression body, ParserRuleContext context)
        {
            if (condition == null || body == null)
            {
                return null;
            }
            return new WhileExpression(condition, body, false, context);
        }

        protected WhileExpression DoWhile(Expression condition, BlockExpression body, ParserRuleContext context)
        {
            if (condition == null || body == null)
            {
                return null;
            }
            return new WhileExpression(condition, body, true, context);
        }

        protected CatchExpression Catch(Expression body, DataType exception, ParserRuleContext context)
        {
            if (exception.Nullable)
            {
                AddSyntaxWarning($"The exception '{exception.Name}' will never be null.", context);
            }
            if (body == null || exception.ClrType == null)
            {
                return null;
            }
            return new CatchExpression(body, exception.ClrType, context);
        }

        protected CatchExpression Catch(Expression body, ParserRuleContext context)
        {
            if (body == null)
            {
                return null;
            }
            return new CatchExpression(body, null, context);
        }

        protected TryExpression Try(Expression body, ParserRuleContext context)
        {
            if (body == null)
            {
                return null;
            }
            return new TryExpression(body, Enumerable.Empty<CatchExpression>(), null, context);
        }

        protected TryExpression Try(Expression body, IEnumerable<CatchExpression> catchExpressions, Expression finallyExpression, ParserRuleContext context)
        {
            var catches = catchExpressions.ToArray();
            if (body == null || catches.Any(x => x == null) || finallyExpression == null)
            {
                return null;
            }
            return new TryExpression(body, catches, finallyExpression, context);
        }

        protected TryExpression Try(Expression body, Expression finallyExpression, ParserRuleContext context)
        {
            if (body == null || finallyExpression == null)
            {
                return null;
            }
            return new TryExpression(body, Enumerable.Empty<CatchExpression>(), finallyExpression, context);
        }

        protected TryExpression Try(Expression body, IEnumerable<CatchExpression> catchExpressions, ParserRuleContext context)
        {
            var expressions = catchExpressions as CatchExpression[] ?? catchExpressions.ToArray();
            if (body == null || expressions.Any(x => x == null))
            {
                return null;
            }
            return new TryExpression(body, expressions, null, context);
        }
    }
}
