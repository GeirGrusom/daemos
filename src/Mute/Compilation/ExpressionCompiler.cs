using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Daemos.Mute.Expressions;
using Daemos.Scripting;

namespace Daemos.Mute.Compilation
{
    public class Output
    {
        public DateTime? Expires { get; set; }
    }



    public class ExpressionCompiler : Visitor
    {
        private ILGenerator il;
        private DynamicMethod method;

        private struct VariableInfo
        {
            public int Index => Local.LocalIndex;
            public bool IsImport { get; }
            public LocalBuilder Local { get; }

            public VariableInfo(LocalBuilder local, bool isImport)
            {
                Local = local;
                IsImport = isImport;
            }
        }

        private readonly Stack<List<VariableDeclarationExpression>> _variables;

        private readonly Dictionary<VariableExpression, VariableInfo> _variableIndices;

        private readonly Stack<Label> _retryLabelStack;

        private List<KeyValuePair<UnaryAwaitExpression, Label>> _awaitEntryPoints;

        private LocalBuilder _this;


        public ExpressionCompiler()
        {
            _variables = new Stack<List<VariableDeclarationExpression>>();

            

            _variableIndices = new Dictionary<VariableExpression, VariableInfo>();
            _retryLabelStack = new Stack<Label>();
        }

        private void PushScope()
        {
            _variables.Push(new List<VariableDeclarationExpression>());
        }

        private void PopScope()
        {
            _variables.Pop();
        }

        private VariableDeclarationExpression GetVariable(VariableExpression expression)
        {
            return _variables.Reverse().SelectMany(x => x).FirstOrDefault(x => x.Variable.Name == expression.Name);
        }

        private static readonly MethodInfo GetServiceMethod = typeof(IDependencyResolver).GetMethod(nameof(IDependencyResolver.GetService), new Type[] { });
        private static readonly MethodInfo LoadStageMethod = typeof(IStateDeserializer).GetMethod(nameof(IStateDeserializer.ReadStage), Array.Empty<Type>());
        private Label endOfScript;

        public Func<IStateSerializer, IStateDeserializer, IDependencyResolver, T> PartialCompile<T>(Expression expression)
        {
            return PartialCompile<T>(OnVisit, expression);
        }

        public Func<IStateSerializer, IStateDeserializer, IDependencyResolver, T> PartialCompile<T>(Action<Expression> visitor, Expression expression)
        {
            method = new DynamicMethod($"DynamicExpression_Main", typeof(T), new[] { typeof(IStateSerializer), typeof(IStateDeserializer), typeof(IDependencyResolver) });
            il = method.GetILGenerator();

            _this = il.DeclareLocal(typeof(Transaction));
            il.Emit(OpCodes.Ldarg_2); // this = di.GetService<Transaction>()
            il.EmitCall(OpCodes.Callvirt, GetServiceMethod.MakeGenericMethod(typeof(Transaction)), null);
            il.Emit(OpCodes.Stloc, _this);
            

            EntryPointVisitor entrypoints = new EntryPointVisitor(il);
            entrypoints.Visit(expression);
            _awaitEntryPoints = entrypoints.Awaits;

            visitor(expression);

            il.Emit(OpCodes.Ret);
            return (Func<IStateSerializer, IStateDeserializer, IDependencyResolver, T>)method.CreateDelegate(typeof(Func<IStateSerializer, IStateDeserializer, IDependencyResolver, T>));
        }

        public Func<IStateSerializer, IStateDeserializer, IDependencyResolver, int> Compile(ModuleExpression expression)
        {
            method = new DynamicMethod($"{expression.Name}_Main", typeof(int), new[] { typeof(IStateSerializer), typeof(IStateDeserializer), typeof(IDependencyResolver) });
            il = method.GetILGenerator();

            _this = il.DeclareLocal(typeof(Transaction));
            var stageLocal = il.DeclareLocal(typeof(int));

            endOfScript = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_1);
            il.EmitCall(OpCodes.Callvirt, LoadStageMethod, null);
            il.Emit(OpCodes.Stloc, stageLocal);

            // Initialization code
            il.Emit(OpCodes.Ldarg_2); // this = di.GetService<Transaction>()
            il.EmitCall(OpCodes.Callvirt, GetServiceMethod.MakeGenericMethod(typeof(Transaction)), null);
            il.Emit(OpCodes.Stloc, _this);

            EntryPointVisitor entrypoints = new EntryPointVisitor(il);
            entrypoints.Visit(expression);
            _awaitEntryPoints = entrypoints.Awaits;
            var startLabel = il.DefineLabel();

            il.Emit(OpCodes.Ldloc, stageLocal);
            il.Emit(OpCodes.Brfalse, startLabel);

            // Stage jumps
            for (int i = 0; i < _awaitEntryPoints.Count; ++i)
            {
                il.Emit(OpCodes.Ldloc, stageLocal);
                il.Emit(OpCodes.Ldc_I4, i + 1);
                il.Emit(OpCodes.Beq, _awaitEntryPoints[i].Value);
            }

            il.Emit(OpCodes.Ldloc, stageLocal);
            il.Emit(OpCodes.Ldc_I4_M1); // if state == -1 then goto end of script. The script has already completed. This is probably a error condition, but hard to catch.
            il.Emit(OpCodes.Beq, endOfScript);

            il.MarkLabel(startLabel);

            Visit(expression);

            // Script ran to the end.
            il.Emit(OpCodes.Ldc_I4_M1);
            il.Emit(OpCodes.Stloc, stageLocal);

            il.MarkLabel(endOfScript);

            il.Emit(OpCodes.Ldloc, stageLocal);
            il.Emit(OpCodes.Ret);            
            
            return (Func<IStateSerializer, IStateDeserializer, IDependencyResolver, int>)method.CreateDelegate(typeof(Func<IStateSerializer, IStateDeserializer, IDependencyResolver, int>));
        }

        public override void OnVisit(BinaryAndExpression exp)
        {
            base.OnVisit(exp);
            il.Emit(OpCodes.And);
        }

        public override void OnVisit(BinaryOrExpression exp)
        {
            base.OnVisit(exp);
            il.Emit(OpCodes.Or);
        }
        public override void OnVisit(BinaryXorExpression exp)
        {
            base.OnVisit(exp);
            il.Emit(OpCodes.Xor);
        }


        public override void OnVisit(BinaryAddExpression exp)
        {

            /*if (exp.Left.Type == DataType.NonNullDateTime && exp.Right.Type == DataType.NonNullTimeSpan)
            {
                il.Emit(OpCodes.Call, typeof(DateTime).GetMethod(nameof(DateTime.Add)));
                return;
            }*/


            if (exp.Method == null)
            {
                Visit(exp.Left);
                Visit(exp.Right);

                il.Emit(OpCodes.Add);
            }
            else
            {
                if (exp.Method.IsStatic || !exp.Left.Type.ClrType.GetTypeInfo().IsValueType)
                {
                    Visit(exp.Left);
                    Visit(exp.Right);
                }
                else
                {

                    var tmp = il.DeclareLocal(exp.Left.Type.ClrType);
                    Visit(exp.Left);
                    il.Emit(OpCodes.Stloc, tmp);
                    il.Emit(OpCodes.Ldloca, tmp);
                    Visit(exp.Right);
                }
                il.Emit(OpCodes.Call, exp.Method);
            }
        }

        public override void OnVisit(BinarySubtractExpression exp)
        {
            Visit(exp.Left);
            Visit(exp.Right);
            if (exp.Method == null)
            {
                il.Emit(OpCodes.Sub);
            }
            else
            {
                il.Emit(OpCodes.Call, exp.Method);
            }
        }

        public override void OnVisit(BinaryMultiplyExpression exp)
        {
            Visit(exp.Left);
            Visit(exp.Right);
            if (exp.Method == null)
            {
                il.Emit(OpCodes.Mul);
            }
            else
            {
                il.Emit(OpCodes.Call, exp.Method);
            }
        }

        public override void OnVisit(BinaryDivideExpression exp)
        {
            Visit(exp.Left);
            Visit(exp.Right);
            if (exp.Method == null)
            {
                il.Emit(OpCodes.Div);
            }
            else
            {
                il.Emit(OpCodes.Call, exp.Method);
            }
        }

        public override void OnVisit(BinaryEqualExpression exp)
        {
            Visit(exp.Left);
            Visit(exp.Right);
            il.Emit(OpCodes.Ceq);
        }

        public override void OnVisit(BinaryNotEqualExpression exp)
        {
            Visit(exp.Left);
            Visit(exp.Right);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Newobj);
            il.Emit(OpCodes.Ceq);
        }

        public override void OnVisit(BinaryLessExpression exp)
        {
            Visit(exp.Left);
            Visit(exp.Right);
            il.Emit(OpCodes.Clt);
        }

        public override void OnVisit(BinaryLessOrEqualExpression exp)
        {
            Visit(exp.Left);
            Visit(exp.Right);
            il.Emit(OpCodes.Cgt);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ceq);
        }

        public override void OnVisit(BinaryGreaterExpression exp)
        {
            Visit(exp.Left);
            Visit(exp.Right);
            il.Emit(OpCodes.Cgt);
        }

        public override void OnVisit(BinaryGreaterOrEqualExpression exp)
        {
            Visit(exp.Left);
            Visit(exp.Right);
            il.Emit(OpCodes.Clt);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ceq);
        }

        public override void OnVisit(ConditionalExpression exp)
        {
            var exitLabel = il.DefineLabel();
            var ifLabel = il.DefineLabel();

            var elseLabel = exp.ElseValue != null ? il.DefineLabel() : exitLabel;

            OnVisit(exp.Condition);
            il.Emit(OpCodes.Brtrue_S, ifLabel);
            il.Emit(OpCodes.Br, elseLabel);
            il.MarkLabel(ifLabel);
            OnVisit(exp.IfValue);
            if (exp.ElseValue != null)
            {
                il.Emit(OpCodes.Br, exitLabel);
                il.MarkLabel(elseLabel);
                OnVisit(exp.ElseValue);
            }
            il.MarkLabel(exitLabel);
        }

        public override void OnVisit(UnaryAddExpression exp)
        {
            OnVisit(exp.Operand);
        }

        public override void OnVisit(UnarySubtractExpression exp)
        {
            OnVisit(exp.Operand);
            il.Emit(OpCodes.Neg);
        }

        public override void OnVisit(UnaryNotExpression exp)
        {
            if (exp.Type.ClrType != typeof(bool))
            {
                throw new InvalidOperationException("Not is only legal on boolean operands.");
            }
            OnVisit(exp.Operand);
            if (exp.Type.Nullable)
            {
                throw new NotImplementedException("Not is not yet supported for nullable booleans.");
            }
            il.Emit(OpCodes.Not);
        }

        public override void OnVisit(UnaryConvertExpression exp)
        {
            OnVisit(exp.Operand);

            if (exp.Type == exp.Operand.Type)
            {
                return;
            }
            if(exp.Type.Equals(DataType.NullString))
            {
                if(exp.Operand is ConstantExpression ce && ce.Value == null)
                {
                    return;
                }

                var finishedLabel = il.DefineLabel();
                if(exp.Operand.Type.Nullable)
                {
                    var isNullLabel = il.DefineLabel();
                    var local = il.DeclareLocal(exp.Operand.Type.ClrType);
                    il.Emit(OpCodes.Stloc, local);
                    il.Emit(OpCodes.Ldloc, local);
                    il.Emit(OpCodes.Brfalse, isNullLabel);

                    if (exp.Operand.Type.ClrType.GetTypeInfo().IsValueType)
                    {
                        il.Emit(OpCodes.Box);
                    }
                    var methd = exp.Operand.Type.ClrType.GetMethod("ToString", new Type[0]);
                    il.Emit(OpCodes.Callvirt, methd);
                    il.Emit(OpCodes.Br_S, finishedLabel);

                    il.MarkLabel(isNullLabel);
                    il.Emit(OpCodes.Ldnull);
                }

                il.MarkLabel(finishedLabel);
                return;
            }
            if (exp.Type.Equals(DataType.NonNullString))
            {
                if (exp.Operand.Type.ClrType.GetTypeInfo().IsValueType)
                {
                    il.Emit(OpCodes.Box);
                }
                var methd = exp.Operand.Type.ClrType.GetMethod("ToString", new Type[0]);
                il.Emit(OpCodes.Callvirt, methd);
                return;
            }
            if (exp.Type == DataType.NonNullInt)
            {
                if (exp.Operand.Type.ClrType == typeof(string)) // Don't care if the target is nullable or not.
                {
                    EmitParse<int>();
                }
                else
                {
                    il.Emit(OpCodes.Conv_I4);
                }
            }
            else if (exp.Type == DataType.NonNullLong)
            {
                if (exp.Operand.Type.ClrType == typeof(string))
                {
                    EmitParse<long>();
                }
                else
                {
                    il.Emit(OpCodes.Conv_I8);
                }
            }
            else if (exp.Type == DataType.NonNullFloat)
            {
                if (exp.Operand.Type.ClrType == typeof(string))
                {
                    EmitParse<float>();
                }
                else
                {
                    il.Emit(OpCodes.Conv_R4);
                }
            }
            else if (exp.Type == DataType.NonNullDouble)
            {
                if (exp.Operand.Type.ClrType == typeof(string))
                {
                    EmitParse<double>();
                }
                else
                {
                    il.Emit(OpCodes.Conv_R8);
                }
            }
            else
            {
                throw new InvalidOperationException("Unsupported type conversion.");
            }
        }

        private void EmitParse<T>()
        {
            var parseMethod = typeof(T).GetMethod("Parse", new[] { typeof(string), typeof(IFormatProvider) });
            var invProp = typeof(CultureInfo).GetProperty("InvariantCulture").GetMethod;

            il.Emit(OpCodes.Call, invProp);
            il.Emit(OpCodes.Call, parseMethod);
        }
        static ExpressionCompiler()
        {
            var methods = typeof(IDependencyResolver).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            GetDependencyMethod = methods.Single(x => x.Name == "GetService" && x.GetParameters().Length == 0);

            SerializeMethods = new Dictionary<Type, MethodInfo>();

            var types = new[]
            {
                typeof(bool),
                typeof(byte),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(float),
                typeof(double),
                typeof(decimal),
                typeof(DateTime),
                typeof(DateTimeOffset),
                typeof(TimeSpan),
                typeof(string),
                typeof(Transaction),
                typeof(bool?),
                typeof(byte?),
                typeof(short?),
                typeof(int?),
                typeof(long?),
                typeof(float?),
                typeof(double?),
                typeof(decimal?),
                typeof(DateTime?),
                typeof(DateTimeOffset?),
                typeof(TimeSpan?)
            };
            var genSerialize = typeof(IStateSerializer).GetMethods().Single(m => m.Name == "Serialize" && m.IsGenericMethodDefinition);
            foreach (var item in types)
            {
                var method = genSerialize.MakeGenericMethod(item);
                SerializeMethods.Add(item, method);
            }
        }

        private static readonly Dictionary<Type, MethodInfo> SerializeMethods;

        private static readonly MethodInfo SerializeNullMethod = typeof(IStateSerializer).GetMethod(nameof(IStateSerializer.SerializeNull), new[] {typeof(string), typeof(Type)});

        private static readonly MethodInfo SaveStageMethod = typeof(IStateSerializer).GetMethod(nameof(IStateSerializer.WriteStage),new[] {typeof(int)});

        public override void OnVisit(TryExpression exp)
        {
            var retryLabel = il.DefineLabel();
            il.MarkLabel(retryLabel);
            _retryLabelStack.Push(retryLabel);
            if (exp.CatchExpressions.Count != 0 || exp.Finally != null)
            {
                il.BeginExceptionBlock();
            }
            OnVisit(exp.Body);
            foreach (var catchExpression in exp.CatchExpressions)
            {
                il.BeginCatchBlock(catchExpression.Type.ClrType ?? typeof(Exception));
                OnVisit(catchExpression);
            }
            if (exp.Finally != null)
            {
                il.BeginFinallyBlock();

                OnVisit(exp.Finally);
            }
            if (exp.CatchExpressions.Count != 0 || exp.Finally != null)
            {
                il.EndExceptionBlock();
            }
            _retryLabelStack.Pop();
        }

        public override void OnVisit(RetryExpression exp)
        {
            il.Emit(OpCodes.Br, _retryLabelStack.Peek());
        }


        private static readonly MethodInfo GetDependencyMethod;

        public override void OnVisit(UnaryAwaitExpression exp)
        {
            int stage = 0;
            Label stageLabel;
            bool foundLabel = false;
            for (int i = 0; i < _awaitEntryPoints.Count; ++i)
            {
                if (_awaitEntryPoints[i].Key == exp)
                {
                    stage = i + 1;
                    stageLabel = _awaitEntryPoints[i].Value;
                    foundLabel = true;
                    break;
                }
            }
            if(!foundLabel)
            {
                throw new InvalidOperationException("Could not locate the specified stage...");
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, stage);
            il.EmitCall(OpCodes.Callvirt, SaveStageMethod, null);
            var tmpLocal = il.DeclareLocal(exp.Type.ClrType);
            Visit(exp.Operand);
            il.Emit(OpCodes.Stloc, tmpLocal);
            // Stores state
            foreach (var item in _variableIndices.Where(x => !x.Value.IsImport).OrderBy(x => x.Value.Index))
            {
                if (!SerializeMethods.TryGetValue(item.Key.Type.ClrType, out MethodInfo meth))
                {
                    // Default to BinaryFormatter serialization.
                    meth = typeof(IStateSerializer).GetMethod(nameof(IStateSerializer.Serialize), new[] { typeof(string), typeof(object) });
                }
                il.Emit(OpCodes.Ldarg_0); // Load StateSerializer
                il.Emit(OpCodes.Ldstr, item.Key.Name); // Load key name (first argument in serializer method)

                var nextLabel = il.DefineLabel();

                if (item.Key.Type.Nullable) // If type is nullable we need to do a null-check and call SerializeNull if it is null.
                {
                    var isNotNull = il.DefineLabel();
                    if (!item.Key.Type.ClrType.GetTypeInfo().IsValueType)
                    {
                        il.Emit(OpCodes.Ldloc, item.Value.Local);
                        il.Emit(OpCodes.Brtrue, isNotNull);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldloc, item.Value.Local);
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Brfalse, isNotNull);
                    }
                    il.Emit(OpCodes.Ldtoken, item.Value.Local.LocalType);
                    il.Emit(OpCodes.Callvirt, SerializeNullMethod);
                    il.Emit(OpCodes.Br, nextLabel);
                    il.MarkLabel(isNotNull);
                }

                il.Emit(OpCodes.Ldloc, item.Value.Local);
                il.Emit(OpCodes.Callvirt, meth);
                il.MarkLabel(nextLabel);
            }

            il.Emit(OpCodes.Ldarg_0);
            il.EmitCall(OpCodes.Callvirt, typeof(IStateSerializer).GetMethod(nameof(IStateSerializer.WriteEndStage)), null);

            il.Emit(OpCodes.Ldarg_0);
            il.EmitCall(OpCodes.Callvirt, typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose)), null);

            
            //il.EmitWriteLine(tmpLocal);
            il.Emit(OpCodes.Ldloc, tmpLocal);
            il.Emit(OpCodes.Brfalse, endOfScript);
            il.Emit(OpCodes.Ldloc, tmpLocal);
            il.Emit(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Storage)).GetMethod);
            il.Emit(OpCodes.Ldloc, tmpLocal);
            il.Emit(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Id)).GetMethod);
            il.Emit(OpCodes.Ldloc, tmpLocal);
            il.Emit(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Revision)).GetMethod);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, typeof(IStateSerializer).GetMethod(nameof(IStateSerializer.GetState)));
            il.Emit(OpCodes.Callvirt, typeof(ITransactionStorage).GetMethod(nameof(ITransactionStorage.SaveTransactionState)));

            il.Emit(OpCodes.Br, endOfScript);
            il.MarkLabel(stageLabel);

            var deserializerMethod = typeof(IStateDeserializer).GetMethods().Single(x => x.Name == nameof(IStateDeserializer.Deserialize) && x.IsGenericMethodDefinition);
            foreach (var item in _variableIndices.OrderBy(x => x.Value.Index))
            {
                if (item.Value.IsImport)
                {
                    var loadMethod = GetDependencyMethod.MakeGenericMethod(item.Key.Type.ClrType);
                    il.Emit(OpCodes.Ldarg_2); // Load IDependencyResolver
                    il.EmitCall(OpCodes.Callvirt, loadMethod, null);
                    il.Emit(OpCodes.Stloc, item.Value.Index);
                }
                else
                {
                    var loadMethod = deserializerMethod.MakeGenericMethod(item.Key.Type.ClrType);
                    il.Emit(OpCodes.Ldarg_1); // Load IStateDeserializer
                    il.Emit(OpCodes.Ldstr, item.Key.Name);
                    il.EmitCall(OpCodes.Callvirt, loadMethod, null);
                    il.Emit(OpCodes.Stloc, item.Value.Index);
                }
            }
            il.Emit(OpCodes.Ldnull);
        }

        public override void OnVisit(CallExpression exp)
        {
            if (exp.Method is MethodInfo meth)
            {
                if (exp.Instance != null)
                {
                    if (exp.Instance is MemberExpression mex)
                    {
                        OnVisit(mex.Instance);
                    }
                    else
                    {
                        OnVisit(exp.Instance);
                    }
                }

                if (exp.IsNamedArguments)
                {
                    LoadNamedArguments(exp.Arguments.Cast<NamedArgument>().ToList(), exp.Method);
                }
                else
                {
                    LoadArguments(exp.Arguments, exp.Method);
                }

                if (exp.Instance == null)
                {
                    il.Emit(OpCodes.Call, meth);
                }
                else
                {
                    il.Emit(OpCodes.Call, meth);
                }
            }
            else if (exp.Method is ConstructorInfo ctor)
            {
                if (exp.IsNamedArguments)
                {
                    LoadNamedArguments(exp.Arguments.Cast<NamedArgument>().ToList(), exp.Method);
                }
                else
                {
                    LoadArguments(exp.Arguments, exp.Method);
                }
                il.Emit(OpCodes.Newobj, ctor);
            }
        }

        private void LoadNamedArguments(List<NamedArgument> namedArguments, MethodBase method)
        {
            var parameters = method.GetParameters();
            bool needAlteredOrder = false;
            for (int i = 0; i < namedArguments.Count; ++i)
            {
                if (namedArguments[i].Argument != parameters[i].Name)
                {
                    needAlteredOrder = true;
                    break;
                }
            }
            if (!needAlteredOrder)
            {
                LoadArguments(namedArguments, method);
                return;
            }

            //var locals = parameters.Select(x => il.DeclareLocal(x.ParameterType)).ToArray();
            var locals = new LocalBuilder[parameters.Length];

            for (int i = 0; i < namedArguments.Count; ++i)
            {
                int localIndex = FindParameterIndex(namedArguments[i].Argument, parameters);

                if (namedArguments[i].Value is VariableExpression vex)
                {
                    locals[localIndex] = GetLocalForVariableExpression(vex);
                }
                else
                {
                    locals[localIndex] = il.DeclareLocal(parameters[i].ParameterType);

                    OnVisit(namedArguments[i]);

                    il.Emit(OpCodes.Stloc, locals[localIndex]);
                }
            }

            for (int i = 0; i < parameters.Length; ++i)
            {
                if(ContainsNamedArgument(parameters[i].Name, namedArguments))
                    continue;
                LoadConstantObject(parameters[i].DefaultValue, parameters[i].ParameterType);
                il.Emit(OpCodes.Stloc, locals[i]);
            }

            for (int i = 0; i < locals.Length; ++i)
            {
                if (parameters[i].ParameterType.IsByRef)
                    il.Emit(OpCodes.Ldloca, locals[i]);
                else
                    il.Emit(OpCodes.Ldloc, locals[i]);
            }
        }

        private void LoadArguments(IEnumerable<Expression> arguments, MethodBase method)
        {
            var parameters = method.GetParameters();
            int count = 0;
            foreach (var exp in arguments)
            {
                
                OnVisit(exp);
                if (parameters[count].ParameterType.IsByRef)
                {
                    var local = il.DeclareLocal(parameters[count].ParameterType);
                    il.Emit(OpCodes.Stloc, local);
                    il.Emit(OpCodes.Ldloca, local);
                }
                ++count;
            }

            for (int i = count; i < parameters.Length; ++i)
            {
                LoadConstantObject(parameters[i].DefaultValue, parameters[i].ParameterType);
            }
        }

        private void LoadConstantObject(object value, Type expectedType)
        {
            if (value == null)
            {
                if (expectedType.GetTypeInfo().IsValueType && expectedType.GetTypeInfo().GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    il.Emit(OpCodes.Initobj, expectedType);
                }
                else
                {
                    il.Emit(OpCodes.Ldnull);
                }
            }
            else if (value is bool boolv)
            {
                if(boolv)
                    il.Emit(OpCodes.Ldc_I4_1);
                else
                    il.Emit(OpCodes.Ldc_I4_0);
            }
            else if (value is byte)
            {
                il.Emit(OpCodes.Ldc_I4, (byte) value);
            }
            else if (value is short)
            {
                il.Emit(OpCodes.Ldc_I4, (short)value);
            }
            else if (value is int)
            {
                il.Emit(OpCodes.Ldc_I4, (int)value);
            }
            else if (value is long)
            {
                il.Emit(OpCodes.Ldc_I8, (long)value);
            }
            else if (value is float)
            {
                il.Emit(OpCodes.Ldc_R4, (float)value);
            }
            else if (value is double)
            {
                il.Emit(OpCodes.Ldc_R8, (double)value);
            }
            else if (value is string)
            {
                il.Emit(OpCodes.Ldstr, (string) value);
            }
            else
            {
                throw new NotSupportedException();
            }
            if (expectedType.GetTypeInfo().IsValueType && expectedType.GetTypeInfo().GetGenericTypeDefinition() ==
                typeof(Nullable<>))
            {
                var ctor = expectedType.GetConstructor(new[] {expectedType.GetGenericArguments()[0]});
                il.Emit(OpCodes.Newobj, ctor);
            }
        }

        private bool ContainsNamedArgument(string name, List<NamedArgument> arguments)
        {
            for (int i = 0; i < arguments.Count; ++i)
            {
                if (arguments[i].Argument == name)
                {
                    return true;
                }
            }
            return false;
        }
        private int FindParameterIndex(string name, ParameterInfo[] parameters)
        {
            for (int i = 0; i < parameters.Length; ++i)
            {
                if (parameters[i].Name == name)
                    return i;
            }
            throw new InvalidOperationException();
        }

        
        public override void OnVisit(ConstantExpression exp)
        {
            if (exp.Value == null)
            {
                il.Emit(OpCodes.Ldnull);
            }
            else if (true.Equals(exp.Value))
            {
                il.Emit(OpCodes.Ldc_I4_1);
            }
            else if (false.Equals(exp.Value))
            {
                il.Emit(OpCodes.Ldc_I4_0);
            }
            else if (exp.Type.Equals(DataType.NonNullInt))
            {
                il.Emit(OpCodes.Ldc_I4,(int)exp.Value);
            }
            else if (exp.Type.Equals(DataType.NonNullLong))
            {
                il.Emit(OpCodes.Ldc_I8, (long)exp.Value);
            }
            else if (exp.Type.Equals(DataType.NonNullFloat))
            {
                il.Emit(OpCodes.Ldc_R4, (float) exp.Value);
            }
            else if (exp.Type.Equals(DataType.NonNullDouble))
            {
                il.Emit(OpCodes.Ldc_R8, (double)exp.Value);
            }
            else if (exp.Type.Equals(DataType.NonNullString))
            {
                il.Emit(OpCodes.Ldstr, (string) exp.Value);
            }
            else if (exp.Type.Equals(DataType.NonNullDateTime))
            {
                var ctor = typeof(DateTime).GetConstructor(new[] {typeof(long), typeof(DateTimeKind)});
                if (ctor == null)
                {
                    throw new InvalidOperationException();
                }
                var dt = (DateTime)exp.Value;
                il.Emit(OpCodes.Ldc_I8, dt.Ticks);
                il.Emit(OpCodes.Ldc_I4_S, (int)DateTimeKind.Utc);
                il.Emit(OpCodes.Newobj, ctor);
            }
            else if (exp.Type.Equals(DataType.NonNullTransactionState))
            {
                il.Emit(OpCodes.Ldc_I4, (int) (TransactionState) exp.Value);
            }
            else
            {
                throw new NotImplementedException();
            }

        }

        public override void OnVisit(NotNullExpression exp)
        {
            
            var isNotNull = il.DefineLabel();
            var ctor = typeof(NullReferenceException).GetConstructor(Array.Empty<Type>());
            if (exp.Operand is VariableExpression var)
            {
                OnVisit(var);
                il.Emit(OpCodes.Brtrue_S, isNotNull);
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Throw);
                il.MarkLabel(isNotNull);
                OnVisit(var);
            }
            else
            {
                var tmp = il.DeclareLocal(exp.Type.ClrType);
                Visit(exp.Operand);
                il.Emit(OpCodes.Stloc, tmp);
                il.Emit(OpCodes.Ldloc, tmp);
                il.Emit(OpCodes.Brtrue_S, isNotNull);
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Throw);
                il.MarkLabel(isNotNull);
                il.Emit(OpCodes.Ldloc, tmp);
            }
        }

        public override void OnVisit(ModuleExpression exp)
        {
            OnVisit(exp.Body);
        }

        public override void OnVisit(BinaryAssignExpression exp)
        {
            var variable = _variableIndices[(VariableExpression) exp.Left];
            
            Visit(exp.Right);
            il.Emit(OpCodes.Stloc, variable.Index);
            il.Emit(OpCodes.Ldloc, variable.Index);
        }

        public override void OnVisit(CommitTransactionExpression exp)
        {
            LocalBuilder originalTransaction;
            var nextTransaction = il.DeclareLocal(typeof(Transaction));            

            if (exp.Transaction is WithExpression withExp)
            {
                if (withExp.Left is VariableExpression varexp)
                {
                    originalTransaction = GetLocalForVariableExpression(varexp);
                }
                else
                {
                    originalTransaction = il.DeclareLocal(typeof(Transaction));
                    OnVisit(withExp.Left);                    
                    il.Emit(OpCodes.Stloc, originalTransaction);
                }

                OnVisit(withExp, true);
                il.Emit(OpCodes.Stloc, nextTransaction);
            }
            else
            {
                
                var data = il.DeclareLocal(typeof(TransactionData));
                if (exp.Transaction is VariableExpression varexp)
                {
                    originalTransaction = GetLocalForVariableExpression(varexp);
                }
                else
                {
                    originalTransaction = il.DeclareLocal(typeof(Transaction));
                    OnVisit(exp.Transaction);                    
                    il.Emit(OpCodes.Stloc, originalTransaction);
                }
                il.Emit(OpCodes.Ldloc, originalTransaction);
                il.EmitCall(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Data)).GetMethod, null);
                il.Emit(OpCodes.Stloc, data);

                // Increment revision
                il.Emit(OpCodes.Ldloca, data);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Call, typeof(TransactionData).GetProperty(nameof(TransactionData.Revision)).GetMethod);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Call, typeof(TransactionData).GetProperty(nameof(TransactionData.Revision)).SetMethod);

                var ctor = typeof(Transaction).GetConstructor(new[] { typeof(TransactionData).MakeByRefType(), typeof(ITransactionStorage) });

                il.Emit(OpCodes.Ldloca, data);
                il.Emit(OpCodes.Ldloc, originalTransaction);
                il.EmitCall(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Storage)).GetMethod, null);
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Stloc, nextTransaction);

            }



            il.Emit(OpCodes.Ldloc, originalTransaction);
            il.EmitCall(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Storage)).GetMethod, null);
            il.Emit(OpCodes.Ldloc, originalTransaction);
            il.Emit(OpCodes.Ldloc, nextTransaction);
            var commitTransaction = typeof(ITransactionStorage).GetMethod(nameof(ITransactionStorage.CommitTransactionDelta));
            il.Emit(OpCodes.Callvirt, commitTransaction);
        }

        public Transaction IncrementTransactionRevision(Transaction tr)
        {
            var data = tr.Data;
            ++data.Revision;
            var result = new Transaction(data, tr.Storage);
            return result;
        }
        
        private LocalBuilder GetLocalForVariableExpression(VariableExpression exp)
        {
            if(exp.Name == "this")
            {
                return _this;
            }
            return _variableIndices[exp].Local;
        }

        public void OnVisit(WithExpression exp, bool incrementRevision)
        {
            if (exp.Type.ClrType != typeof(Transaction))
            {
                throw new NotSupportedException();
            }

            LocalBuilder tr;

            var transactionData = il.DeclareLocal(typeof(TransactionData));
            if (exp.Left is VariableExpression varexp)
            {
                tr = GetLocalForVariableExpression(varexp);
            }
            else
            {
                tr = il.DeclareLocal(typeof(Transaction));
                OnVisit(exp.Left);
                il.Emit(OpCodes.Stloc, tr);
            }

            il.Emit(OpCodes.Ldloc, tr);
            il.Emit(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Data)).GetMethod);
            il.Emit(OpCodes.Stloc, transactionData);

            if (incrementRevision)
            {
                il.Emit(OpCodes.Ldloca, transactionData);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Call, typeof(TransactionData).GetProperty(nameof(TransactionData.Revision)).GetMethod);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Call, typeof(TransactionData).GetProperty(nameof(TransactionData.Revision)).SetMethod);
            }

            foreach (var mem in ((ObjectExpression)exp.Right).Members)
            {
                var prop = typeof(TransactionData).GetProperty(mem.Name);
                il.Emit(OpCodes.Ldloca, transactionData);
                OnVisit(mem);

                if (prop.PropertyType.GetTypeInfo().IsGenericType &&
                    prop.PropertyType.GetTypeInfo().GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    il.Emit(OpCodes.Newobj, prop.PropertyType.GetConstructor(new [] {prop.PropertyType.GetGenericArguments()[0]}));
                }

                il.Emit(OpCodes.Call, prop.SetMethod);
            }
            var ctor = typeof(Transaction).GetConstructor(new[] { typeof(TransactionData).MakeByRefType(), typeof(ITransactionStorage) });
            il.Emit(OpCodes.Ldloca, transactionData);
            il.Emit(OpCodes.Ldloc, tr);
            il.Emit(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Storage)).GetMethod);
            il.Emit(OpCodes.Newobj, ctor);
        }

        public override void OnVisit(WithExpression exp)
        {
            OnVisit(exp, false);
        }

        public void RegisterVariable(VariableExpression expr, bool isImport)
        {
            var builder = il.DeclareLocal(expr.Type.ClrType);
            //builder.SetLocalSymInfo(expr.Name);
            _variableIndices.Add(expr, new VariableInfo(builder, isImport));
        }

        public override void OnVisit(BlockExpression exp)
        {
            foreach (var expression in exp.Body)
            {
                OnVisit(expression);
                if (expression.Type.ClrType != typeof(void))
                {
                    il.Emit(OpCodes.Pop);
                }
            }
        }

        public override void OnVisit(ImportExpression exp)
        {
            var loadMethod = GetDependencyMethod.MakeGenericMethod(exp.Type.ClrType);
            il.Emit(OpCodes.Ldarg_2);
            il.EmitCall(OpCodes.Callvirt, loadMethod, null);
        }

        public override void OnVisit(VariableExpression exp)
        {
            if(exp.Name == "this")
            {
                il.Emit(OpCodes.Ldloc, _this);
                return;
            }
            il.Emit(OpCodes.Ldloc, _variableIndices[exp].Index);
        }

        public override void OnVisit(VariableDeclarationExpression exp)
        {
            if (exp.Assignment != null)
            {
                RegisterVariable(exp.Variable, exp.Assignment is ImportExpression);
                
                OnVisit(exp.Assignment);
                il.Emit(OpCodes.Stloc, _variableIndices[exp.Variable].Index);
                il.Emit(OpCodes.Ldloc, _variableIndices[exp.Variable].Index);
            }
            else
            {
                RegisterVariable(exp.Variable, isImport: false);
            }
        }

        public override void OnVisit(WhileExpression exp)
        {
            if (!exp.DoWhile)
            {
                var startLabel = il.DefineLabel();
                var endLabel = il.DefineLabel();
                il.MarkLabel(startLabel);
                if (exp.Condition != null || exp.Condition == ConstantExpression.TrueExpression)
                {
                    OnVisit(exp.Condition);
                    il.Emit(OpCodes.Brfalse, endLabel);
                }
                OnVisit(exp.Contents);
                il.Emit(OpCodes.Br, startLabel);
                il.MarkLabel(endLabel);
            }
            else
            {
                var startLabel = il.DefineLabel();
                var endLabel = il.DefineLabel();
                il.MarkLabel(startLabel);
                OnVisit(exp.Contents);
                if (exp.Condition != null || exp.Condition == ConstantExpression.TrueExpression)
                {
                    OnVisit(exp.Condition);
                    il.Emit(OpCodes.Brfalse, endLabel);
                }
                il.Emit(OpCodes.Br, startLabel);
                il.MarkLabel(endLabel);
            }
        }
    }
}
