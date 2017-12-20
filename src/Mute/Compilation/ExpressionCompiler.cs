// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Compilation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using Antlr4.Runtime;
    using Daemos.Mute.Expressions;
    using Daemos.Scripting;

    /// <summary>
    /// Compiles an expression into a executable delegate
    /// </summary>
    public class ExpressionCompiler : Visitor
    {
        private static readonly MethodInfo GetServiceMethod = typeof(IDependencyResolver).GetMethod(nameof(IDependencyResolver.GetService), new Type[] { typeof(string) });

        private static readonly MethodInfo LoadStageMethod = typeof(IStateDeserializer).GetMethod(nameof(IStateDeserializer.ReadStage), Array.Empty<Type>());

        private static readonly Dictionary<Type, MethodInfo> SerializeMethods;

        private static readonly MethodInfo SerializeNullMethod = typeof(IStateSerializer).GetMethod(nameof(IStateSerializer.SerializeNull), new[] { typeof(string), typeof(Type) });

        private static readonly MethodInfo SaveStageMethod = typeof(IStateSerializer).GetMethod(nameof(IStateSerializer.WriteStage), new[] { typeof(int) });

        private static readonly MethodInfo GetDependencyMethod;

        private readonly Stack<List<VariableDeclarationExpression>> variables;

        private readonly Dictionary<VariableExpression, VariableInfo> variableIndices;

        private readonly Stack<Label> retryLabelStack;

        private List<KeyValuePair<UnaryAwaitExpression, Label>> awaitEntryPoints;

        private ILGenerator il;
        private DynamicMethod method;
        private LocalBuilder @this;
        private LocalBuilder now;
        private LocalBuilder expired;
        private Label endOfScript;

        static ExpressionCompiler()
        {
            var methods = typeof(IDependencyResolver).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            GetDependencyMethod = methods.Single(x => x.Name == "GetService" && x.GetParameters().Length == 1);

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

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionCompiler"/> class.
        /// </summary>
        public ExpressionCompiler()
        {
            this.variables = new Stack<List<VariableDeclarationExpression>>();

            this.variableIndices = new Dictionary<VariableExpression, VariableInfo>();
            this.retryLabelStack = new Stack<Label>();
        }

        /// <summary>
        /// Produces a partial compile. This oversteps some initialization processes in order to get a more
        /// concise compiliation. This is intended for debugging purposes.
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="expression">Expression to compile</param>
        /// <returns>Compiled delegate</returns>
        public Func<IStateSerializer, IStateDeserializer, IDependencyResolver, T> PartialCompile<T>(Expression expression)
        {
            return this.PartialCompile<T>(this.OnVisit, expression);
        }

        /// <summary>
        /// Produces a partial compile. This oversteps some initialization processes in order to get a more
        /// concise compiliation. This is intended for debugging purposes.
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="visitor">Delegate used to invoke visitiation</param>
        /// <param name="expression">Expression to compile</param>
        /// <returns>Compiled delegate</returns>
        public Func<IStateSerializer, IStateDeserializer, IDependencyResolver, T> PartialCompile<T>(Action<Expression> visitor, Expression expression)
        {
            this.method = new DynamicMethod($"DynamicExpression_Main", typeof(T), new[] { typeof(IStateSerializer), typeof(IStateDeserializer), typeof(IDependencyResolver) });
            this.il = this.method.GetILGenerator();
            this.@this = this.il.DeclareLocal(typeof(Transaction));
            this.il.Emit(OpCodes.Ldarg_2); // this = di.GetService<Transaction>()
            this.il.Emit(OpCodes.Ldnull);
            this.il.EmitCall(OpCodes.Callvirt, GetServiceMethod.MakeGenericMethod(typeof(Transaction)), null);
            this.il.Emit(OpCodes.Stloc, this.@this);

            EntryPointVisitor entrypoints = new EntryPointVisitor(this.il);
            entrypoints.Visit(expression);
            this.awaitEntryPoints = entrypoints.Awaits;

            visitor(expression);

            this.il.Emit(OpCodes.Ret);
            return (Func<IStateSerializer, IStateDeserializer, IDependencyResolver, T>)this.method.CreateDelegate(typeof(Func<IStateSerializer, IStateDeserializer, IDependencyResolver, T>));
        }

        /// <summary>
        /// Compiles an expression into a delegate
        /// </summary>
        /// <param name="expression">Expression to compile</param>
        /// <returns>Compiled code entry point as a delegate</returns>
        public Func<IStateSerializer, IStateDeserializer, IDependencyResolver, int> Compile(ModuleExpression expression)
        {
            this.method = new DynamicMethod($"{expression.Name}_Main", typeof(int), new[] { typeof(IStateSerializer), typeof(IStateDeserializer), typeof(IDependencyResolver) });
            this.il = this.method.GetILGenerator();

            this.@this = this.il.DeclareLocal(typeof(Transaction));
            this.now = this.il.DeclareLocal(typeof(DateTime));
            this.expired = this.il.DeclareLocal(typeof(DateTime));
            var stageLocal = this.il.DeclareLocal(typeof(int));

            this.endOfScript = this.il.DefineLabel();
            this.il.Emit(OpCodes.Ldarg_1);
            this.il.EmitCall(OpCodes.Callvirt, LoadStageMethod, null);
            this.il.Emit(OpCodes.Stloc, stageLocal);

            // Initialization code
            this.il.Emit(OpCodes.Ldarg_2); // this = di.GetService<Transaction>()
            this.il.Emit(OpCodes.Ldnull);
            this.il.EmitCall(OpCodes.Callvirt, GetServiceMethod.MakeGenericMethod(typeof(Transaction)), null);
            this.il.Emit(OpCodes.Stloc, this.@this);

            // now = GetService<ITimeService>().Now();
            this.il.Emit(OpCodes.Ldarg_2);
            this.il.Emit(OpCodes.Ldnull);
            this.il.Emit(OpCodes.Callvirt, GetServiceMethod.MakeGenericMethod(typeof(ITimeService)));
            this.il.Emit(OpCodes.Callvirt, typeof(ITimeService).GetMethod(nameof(ITimeService.Now)));
            this.il.Emit(OpCodes.Stloc, this.now);

            EntryPointVisitor entrypoints = new EntryPointVisitor(this.il);
            entrypoints.Visit(expression);
            this.awaitEntryPoints = entrypoints.Awaits;
            var startLabel = this.il.DefineLabel();

            this.il.Emit(OpCodes.Ldloc, stageLocal);
            this.il.Emit(OpCodes.Brfalse, startLabel);

            // Stage jumps
            for (int i = 0; i < this.awaitEntryPoints.Count; ++i)
            {
                this.il.Emit(OpCodes.Ldloc, stageLocal);
                this.il.Emit(OpCodes.Ldc_I4, i + 1);
                this.il.Emit(OpCodes.Beq, this.awaitEntryPoints[i].Value);
            }

            this.il.Emit(OpCodes.Ldloc, stageLocal);
            this.il.Emit(OpCodes.Ldc_I4_M1); // if state == -1 then goto end of script. The script has already completed. This is probably a error condition, but hard to catch.
            this.il.Emit(OpCodes.Beq, this.endOfScript);

            this.il.MarkLabel(startLabel);

            this.Visit(expression);

            // Script ran to the end.
            this.il.Emit(OpCodes.Ldc_I4_M1);
            this.il.Emit(OpCodes.Stloc, stageLocal);

            this.il.MarkLabel(this.endOfScript);

            this.il.Emit(OpCodes.Ldloc, stageLocal);
            this.il.Emit(OpCodes.Ret);

            return (Func<IStateSerializer, IStateDeserializer, IDependencyResolver, int>)this.method.CreateDelegate(typeof(Func<IStateSerializer, IStateDeserializer, IDependencyResolver, int>));
        }

        /// <inheritdoc/>
        public override void OnVisit(BinaryAndExpression exp)
        {
            base.OnVisit(exp);
            this.il.Emit(OpCodes.And);
        }

        /// <inheritdoc/>
        public override void OnVisit(BinaryOrExpression exp)
        {
            base.OnVisit(exp);
            this.il.Emit(OpCodes.Or);
        }

        /// <inheritdoc/>
        public override void OnVisit(BinaryXorExpression exp)
        {
            base.OnVisit(exp);
            this.il.Emit(OpCodes.Xor);
        }

        /// <inheritdoc/>
        public override void OnVisit(BinaryAddExpression exp)
        {
            if (exp.Method == null)
            {
                this.Visit(exp.Left);
                this.Visit(exp.Right);

                this.il.Emit(OpCodes.Add);
            }
            else
            {
                if (exp.Method.IsStatic || !exp.Left.Type.ClrType.GetTypeInfo().IsValueType)
                {
                    this.Visit(exp.Left);
                }
                else
                {
                    var tmp = this.il.DeclareLocal(exp.Left.Type.ClrType);
                    this.Visit(exp.Left);
                    this.il.Emit(OpCodes.Stloc, tmp);
                    this.il.Emit(OpCodes.Ldloca, tmp);
                }

                this.Visit(exp.Right);
                this.il.Emit(OpCodes.Call, exp.Method);
            }
        }

        /// <inheritdoc/>
        public override void OnVisit(MemberExpression exp)
        {
            if (exp.Member is PropertyInfo property)
            {
                if (exp.Instance.Type.ClrType.GetTypeInfo().IsValueType)
                {
                    if (exp.Instance is VariableExpression v)
                    {
                        this.il.Emit(OpCodes.Ldloc, v.Name == "this" ? this.@this : this.variableIndices[v].Local);
                    }
                    else
                    {
                        var tmp = this.il.DeclareLocal(exp.Instance.Type.ClrType);
                        this.OnVisit(exp.Instance);
                        this.il.Emit(OpCodes.Stloc, tmp);
                        this.il.Emit(OpCodes.Ldloca, tmp);
                    }
                }
                else
                {
                    this.OnVisit(exp.Instance);
                }

                this.il.Emit(OpCodes.Call, property.GetMethod);
                return;
            }

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void OnVisit(BinarySubtractExpression exp)
        {
            this.Visit(exp.Left);
            this.Visit(exp.Right);
            if (exp.Method == null)
            {
                this.il.Emit(OpCodes.Sub);
            }
            else
            {
                this.il.Emit(OpCodes.Call, exp.Method);
            }
        }

        /// <inheritdoc/>
        public override void OnVisit(BinaryMultiplyExpression exp)
        {
            this.Visit(exp.Left);
            this.Visit(exp.Right);
            if (exp.Method == null)
            {
                this.il.Emit(OpCodes.Mul);
            }
            else
            {
                this.il.Emit(OpCodes.Call, exp.Method);
            }
        }

        /// <inheritdoc/>
        public override void OnVisit(BinaryDivideExpression exp)
        {
            this.Visit(exp.Left);
            this.Visit(exp.Right);
            if (exp.Method == null)
            {
                this.il.Emit(OpCodes.Div);
            }
            else
            {
                this.il.Emit(OpCodes.Call, exp.Method);
            }
        }

        /// <inheritdoc/>
        public override void OnVisit(BinaryEqualExpression exp)
        {
            this.Visit(exp.Left);
            this.Visit(exp.Right);
            this.il.Emit(OpCodes.Ceq);
        }

        /// <inheritdoc/>
        public override void OnVisit(BinaryNotEqualExpression exp)
        {
            this.Visit(exp.Left);
            this.Visit(exp.Right);
            this.il.Emit(OpCodes.Ceq);
            this.il.Emit(OpCodes.Ldc_I4_0);
            this.il.Emit(OpCodes.Newobj);
            this.il.Emit(OpCodes.Ceq);
        }

        /// <inheritdoc/>
        public override void OnVisit(BinaryLessExpression exp)
        {
            this.Visit(exp.Left);
            this.Visit(exp.Right);
            this.il.Emit(OpCodes.Clt);
        }

        /// <inheritdoc/>
        public override void OnVisit(BinaryLessOrEqualExpression exp)
        {
            this.Visit(exp.Left);
            this.Visit(exp.Right);
            this.il.Emit(OpCodes.Cgt);
            this.il.Emit(OpCodes.Ldc_I4_0);
            this.il.Emit(OpCodes.Ceq);
        }

        /// <inheritdoc/>
        public override void OnVisit(BinaryGreaterExpression exp)
        {
            this.Visit(exp.Left);
            this.Visit(exp.Right);
            this.il.Emit(OpCodes.Cgt);
        }

        /// <inheritdoc/>
        public override void OnVisit(BinaryGreaterOrEqualExpression exp)
        {
            this.Visit(exp.Left);
            this.Visit(exp.Right);
            this.il.Emit(OpCodes.Clt);
            this.il.Emit(OpCodes.Ldc_I4_0);
            this.il.Emit(OpCodes.Ceq);
        }

        /// <inheritdoc/>
        public override void OnVisit(ConditionalExpression exp)
        {
            var exitLabel = this.il.DefineLabel();
            var ifLabel = this.il.DefineLabel();

            var elseLabel = exp.ElseValue != null ? this.il.DefineLabel() : exitLabel;

            this.OnVisit(exp.Condition);
            this.il.Emit(OpCodes.Brtrue_S, ifLabel);
            this.il.Emit(OpCodes.Br, elseLabel);
            this.il.MarkLabel(ifLabel);
            this.OnVisit(exp.IfValue);
            if (exp.ElseValue != null)
            {
                this.il.Emit(OpCodes.Br, exitLabel);
                this.il.MarkLabel(elseLabel);
                this.OnVisit(exp.ElseValue);
            }

            this.il.MarkLabel(exitLabel);
        }

        /// <inheritdoc/>
        public override void OnVisit(UnaryAddExpression exp)
        {
            this.OnVisit(exp.Operand);
        }

        /// <inheritdoc/>
        public override void OnVisit(UnarySubtractExpression exp)
        {
            this.OnVisit(exp.Operand);
            this.il.Emit(OpCodes.Neg);
        }

        /// <inheritdoc/>
        public override void OnVisit(UnaryNotExpression exp)
        {
            if (exp.Type.ClrType != typeof(bool))
            {
                throw new InvalidOperationException("Not is only legal on boolean operands.");
            }

            this.OnVisit(exp.Operand);
            if (exp.Type.Nullable)
            {
                throw new NotImplementedException("Not is not yet supported for nullable booleans.");
            }

            this.il.Emit(OpCodes.Not);
        }

        /// <inheritdoc/>
        public override void OnVisit(UnaryConvertExpression exp)
        {
            if (exp.Type == exp.Operand.Type)
            {
                return;
            }

            if (!exp.Type.Nullable && exp.Operand.Type.Nullable && exp.Operand.Type.ClrType.GetTypeInfo().IsValueType)
            {
                if (exp.Type.ClrType.GetTypeInfo().IsValueType && exp.Operand.Type.ClrType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    this.OnVisit(exp.Operand);
                    var tmp = this.il.DeclareLocal(exp.Operand.Type.ClrType);
                    this.il.Emit(OpCodes.Stloc, tmp);
                    this.il.Emit(OpCodes.Ldloca, tmp);
                    this.il.Emit(OpCodes.Call, exp.Operand.Type.ClrType.GetProperty("Value").GetMethod);
                    return;
                }

                this.OnVisitNotNullUnaryExpression(exp);

                return;
            }

            if (exp.Type.Equals(DataType.NullString))
            {
                this.OnVisit(exp.Operand);
                if (exp.Operand is ConstantExpression ce && ce.Value == null)
                {
                    return;
                }

                var finishedLabel = this.il.DefineLabel();
                if (exp.Operand.Type.Nullable)
                {
                    var isNullLabel = this.il.DefineLabel();
                    var local = this.il.DeclareLocal(exp.Operand.Type.ClrType);
                    this.il.Emit(OpCodes.Stloc, local);
                    this.il.Emit(OpCodes.Ldloc, local);
                    this.il.Emit(OpCodes.Brfalse, isNullLabel);

                    if (exp.Operand.Type.ClrType.GetTypeInfo().IsValueType)
                    {
                        this.il.Emit(OpCodes.Box);
                    }

                    var methd = exp.Operand.Type.ClrType.GetMethod("ToString", new Type[0]);
                    this.il.Emit(OpCodes.Callvirt, methd);
                    this.il.Emit(OpCodes.Br_S, finishedLabel);

                    this.il.MarkLabel(isNullLabel);
                    this.il.Emit(OpCodes.Ldnull);
                }

                this.il.MarkLabel(finishedLabel);
                return;
            }

            if (exp.Type.Equals(DataType.NonNullString))
            {
                this.OnVisit(exp.Operand);
                if (exp.Operand.Type.ClrType.GetTypeInfo().IsValueType)
                {
                    this.il.Emit(OpCodes.Box);
                }

                var methd = exp.Operand.Type.ClrType.GetMethod("ToString", new Type[0]);
                this.il.Emit(OpCodes.Callvirt, methd);
                return;
            }

            if (exp.Type == DataType.NonNullInt)
            {
                this.OnVisit(exp.Operand);

                // Don't care if the target is nullable or not.
                if (exp.Operand.Type.ClrType == typeof(string))
                {
                    this.EmitParse<int>();
                }
                else if (exp.Operand.Type.ClrType == typeof(object))
                {
                    this.il.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ToInt32), new[] { typeof(object) }));
                }
                else
                {
                    this.il.Emit(OpCodes.Conv_I4);
                }
            }
            else if (exp.Type == DataType.NonNullLong)
            {
                this.OnVisit(exp.Operand);
                if (exp.Operand.Type.ClrType == typeof(string))
                {
                    this.EmitParse<long>();
                }
                else if (exp.Operand.Type.ClrType == typeof(object))
                {
                    this.il.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ToInt64), new[] { typeof(object) }));
                }
                else
                {
                    this.il.Emit(OpCodes.Conv_I8);
                }
            }
            else if (exp.Type == DataType.NonNullFloat)
            {
                this.OnVisit(exp.Operand);
                if (exp.Operand.Type.ClrType == typeof(string))
                {
                    this.EmitParse<float>();
                }
                else if (exp.Operand.Type.ClrType == typeof(object))
                {
                    this.il.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ToSingle), new[] { typeof(object) }));
                }
                else
                {
                    this.il.Emit(OpCodes.Conv_R4);
                }
            }
            else if (exp.Type == DataType.NonNullDouble)
            {
                this.OnVisit(exp.Operand);
                if (exp.Operand.Type.ClrType == typeof(string))
                {
                    this.EmitParse<double>();
                }
                else if (exp.Operand.Type.ClrType == typeof(object))
                {
                    this.il.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ToDouble), new[] { typeof(object) }));
                }
                else
                {
                    this.il.Emit(OpCodes.Conv_R8);
                }
            }
            else if (exp.Type == DataType.NonNullDecimal)
            {
                this.OnVisit(exp.Operand);
                if (exp.Operand.Type.ClrType == typeof(string))
                {
                    this.EmitParse<decimal>();
                }
                else if (exp.Operand.Type.ClrType == typeof(object))
                {
                    this.il.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ToDecimal), new[] { typeof(object) }));
                }
                else
                {
                    var ctor = typeof(decimal).GetConstructor(new Type[] { exp.Operand.Type.ClrType });
                    this.il.Emit(OpCodes.Newobj, ctor);
                }
            }
            else
            {
                this.OnVisit(exp.Operand);
                var tmp = this.il.DeclareLocal(exp.Type.ClrType);
                this.il.Emit(OpCodes.Stloc, tmp);
                this.il.Emit(OpCodes.Ldloc, tmp);
            }
        }

        /// <inheritdoc/>
        public override void OnVisit(TryExpression exp)
        {
            var retryLabel = this.il.DefineLabel();
            var finallyLabel = exp.Finally != null ? this.il.DefineLabel() : default(Label);
            var endLabel = this.il.DefineLabel();

            var catchBlocks = new List<(Label label, CatchExpression @catch)>();

            if (exp.CatchExpressions.Count != 0)
            {
                this.il.BeginExceptionBlock();
            }

            this.retryLabelStack.Push(retryLabel);
            this.il.MarkLabel(retryLabel);
            this.OnVisit(exp.Body);
            if (exp.Finally != null)
            {
                this.il.Emit(OpCodes.Br, finallyLabel);
            }

            foreach (var catchExpression in exp.CatchExpressions)
            {
                this.il.BeginCatchBlock(catchExpression.Exception ?? typeof(Exception));
                var label = this.il.DefineLabel();
                catchBlocks.Add((label, catchExpression));
                this.il.Emit(OpCodes.Leave, label);
            }

            if (exp.CatchExpressions.Count != 0 || exp.Finally != null)
            {
                this.il.EndExceptionBlock();
            }

            this.il.Emit(OpCodes.Br, endLabel);

            foreach (var item in catchBlocks)
            {
                this.il.MarkLabel(item.label);
                this.OnVisit(item.@catch);
                if (exp.Finally != null)
                {
                    this.il.Emit(OpCodes.Br, finallyLabel);
                }
                else
                {
                    this.il.Emit(OpCodes.Br, endLabel);
                }
            }

            this.retryLabelStack.Pop();

            if (exp.Finally != null)
            {
                this.il.MarkLabel(finallyLabel);
                this.OnVisit(exp.Finally);
            }

            this.il.MarkLabel(endLabel);
        }

        /// <inheritdoc/>
        public override void OnVisit(RetryExpression exp)
        {
            this.il.Emit(OpCodes.Leave, this.retryLabelStack.Peek());
        }

        /// <inheritdoc/>
        public override void OnVisit(UnaryAwaitExpression exp)
        {
            int stage = 0;
            Label stageLabel;
            bool foundLabel = false;
            for (int i = 0; i < this.awaitEntryPoints.Count; ++i)
            {
                if (this.awaitEntryPoints[i].Key == exp)
                {
                    stage = i + 1;
                    stageLabel = this.awaitEntryPoints[i].Value;
                    foundLabel = true;
                    break;
                }
            }

            if (!foundLabel)
            {
                throw new InvalidOperationException("Could not locate the specified stage...");
            }

            this.il.Emit(OpCodes.Ldarg_0);
            this.il.Emit(OpCodes.Ldc_I4, stage);
            this.il.EmitCall(OpCodes.Callvirt, SaveStageMethod, null);
            var tmpLocal = this.il.DeclareLocal(exp.Type.ClrType);
            this.Visit(exp.Operand);
            this.il.Emit(OpCodes.Stloc, tmpLocal);

            // Stores state
            foreach (var item in this.variableIndices.Where(x => !x.Value.IsImport).OrderBy(x => x.Value.Index))
            {
                if (!SerializeMethods.TryGetValue(item.Key.Type.ClrType, out MethodInfo meth))
                {
                    // Default to BinaryFormatter serialization.
                    meth = typeof(StateSerializer).GetMethods().Single(x => x.Name == "Serialize" && x.IsGenericMethodDefinition).MakeGenericMethod(item.Key.Type.ClrType);
                }

                this.il.Emit(OpCodes.Ldarg_0); // Load StateSerializer
                this.il.Emit(OpCodes.Ldstr, item.Key.Name); // Load key name (first argument in serializer method)

                var nextLabel = this.il.DefineLabel();

                // If type is nullable we need to do a null-check and call SerializeNull if it is null.
                if (item.Key.Type.Nullable)
                {
                    var isNotNull = this.il.DefineLabel();
                    if (!item.Key.Type.ClrType.GetTypeInfo().IsValueType)
                    {
                        this.il.Emit(OpCodes.Ldloc, item.Value.Local);
                        this.il.Emit(OpCodes.Brtrue, isNotNull);
                    }
                    else
                    {
                        this.il.Emit(OpCodes.Ldloc, item.Value.Local);
                        this.il.Emit(OpCodes.Ldnull);
                        this.il.Emit(OpCodes.Ceq);
                        this.il.Emit(OpCodes.Brfalse, isNotNull);
                    }

                    this.il.Emit(OpCodes.Ldtoken, item.Value.Local.LocalType);
                    this.il.Emit(OpCodes.Callvirt, SerializeNullMethod);
                    this.il.Emit(OpCodes.Br, nextLabel);
                    this.il.MarkLabel(isNotNull);
                }

                this.il.Emit(OpCodes.Ldloc, item.Value.Local);
                this.il.Emit(OpCodes.Callvirt, meth);
                this.il.MarkLabel(nextLabel);
            }

            this.il.Emit(OpCodes.Ldarg_0);
            this.il.EmitCall(OpCodes.Callvirt, typeof(IStateSerializer).GetMethod(nameof(IStateSerializer.WriteEndStage)), null);

            this.il.Emit(OpCodes.Ldarg_0);
            this.il.EmitCall(OpCodes.Callvirt, typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose)), null);

            this.il.Emit(OpCodes.Ldloc, tmpLocal);
            this.il.Emit(OpCodes.Brfalse, this.endOfScript);
            this.il.Emit(OpCodes.Ldloc, tmpLocal);
            this.il.Emit(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Storage)).GetMethod);
            this.il.Emit(OpCodes.Ldloc, tmpLocal);
            this.il.Emit(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Id)).GetMethod);
            this.il.Emit(OpCodes.Ldloc, tmpLocal);
            this.il.Emit(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Revision)).GetMethod);
            this.il.Emit(OpCodes.Ldarg_0);
            this.il.Emit(OpCodes.Callvirt, typeof(IStateSerializer).GetMethod(nameof(IStateSerializer.GetState)));
            this.il.Emit(OpCodes.Callvirt, typeof(ITransactionStorage).GetMethod(nameof(ITransactionStorage.SaveTransactionState)));

            this.il.Emit(OpCodes.Br, this.endOfScript);
            this.il.MarkLabel(stageLabel);

            var deserializerMethod = typeof(IStateDeserializer).GetMethods().Single(x => x.Name == nameof(IStateDeserializer.Deserialize) && x.IsGenericMethodDefinition);
            foreach (var item in this.variableIndices.OrderBy(x => x.Value.Index))
            {
                if (item.Value.IsImport)
                {
                    var loadMethod = GetDependencyMethod.MakeGenericMethod(item.Key.Type.ClrType);
                    this.il.Emit(OpCodes.Ldarg_2); // Load IDependencyResolver
                    this.il.Emit(OpCodes.Ldnull);
                    this.il.EmitCall(OpCodes.Callvirt, loadMethod, null);
                    this.il.Emit(OpCodes.Stloc, item.Value.Index);
                }
                else
                {
                    var loadMethod = deserializerMethod.MakeGenericMethod(item.Key.Type.ClrType);
                    this.il.Emit(OpCodes.Ldarg_1); // Load IStateDeserializer
                    this.il.Emit(OpCodes.Ldstr, item.Key.Name);
                    this.il.EmitCall(OpCodes.Callvirt, loadMethod, null);
                    this.il.Emit(OpCodes.Stloc, item.Value.Index);
                }
            }

            this.il.Emit(OpCodes.Ldnull);
        }

        /// <inheritdoc/>
        public override void OnVisit(CallExpression exp)
        {
            if (exp.Method is MethodInfo meth)
            {
                if (exp.Instance != null)
                {
                    if (exp.Instance is MemberExpression mex)
                    {
                        this.OnVisit(mex.Instance);
                    }
                    else
                    {
                        this.OnVisit(exp.Instance);
                    }

                    // If the call-target is dynamic, a type check must be performed.
                    if (exp.Instance.Type == DataType.Dynamic)
                    {
                        var castOk = this.il.DefineLabel();
                        var tmp = this.il.DeclareLocal(exp.Instance.Type.ClrType);
                        this.il.Emit(OpCodes.Stloc, tmp);
                        this.il.Emit(OpCodes.Ldloc, tmp);
                        this.il.Emit(OpCodes.Castclass, typeof(IDictionary<string, object>));
                        this.il.Emit(OpCodes.Brtrue);
                        this.il.Emit(OpCodes.Newobj, typeof(InvalidCastException).GetConstructor(new Type[0]));
                        this.il.Emit(OpCodes.Throw);
                        this.il.MarkLabel(castOk);
                        this.il.Emit(OpCodes.Ldloc, tmp);
                    }
                }

                if (exp.IsNamedArguments)
                {
                    this.LoadNamedArguments(exp.Arguments.Cast<NamedArgument>().ToList(), exp.Method);
                }
                else
                {
                    this.LoadArguments(exp.Arguments, exp.Method);
                }

                if (exp.Instance == null)
                {
                    this.il.Emit(OpCodes.Call, meth);
                }
                else
                {
                    this.il.Emit(OpCodes.Callvirt, meth);
                }
            }
            else if (exp.Method is ConstructorInfo ctor)
            {
                if (exp.IsNamedArguments)
                {
                    this.LoadNamedArguments(exp.Arguments.Cast<NamedArgument>().ToList(), exp.Method);
                }
                else
                {
                    this.LoadArguments(exp.Arguments, exp.Method);
                }

                this.il.Emit(OpCodes.Newobj, ctor);
            }
        }

        /// <inheritdoc/>
        public override void OnVisit(ConstantExpression exp)
        {
            if (exp.Value == null)
            {
                this.il.Emit(OpCodes.Ldnull);
            }
            else if (true.Equals(exp.Value))
            {
                this.il.Emit(OpCodes.Ldc_I4_1);
            }
            else if (false.Equals(exp.Value))
            {
                this.il.Emit(OpCodes.Ldc_I4_0);
            }
            else if (exp.Type.Equals(DataType.NonNullInt))
            {
                this.il.Emit(OpCodes.Ldc_I4, (int)exp.Value);
            }
            else if (exp.Type.Equals(DataType.NonNullLong))
            {
                this.il.Emit(OpCodes.Ldc_I8, (long)exp.Value);
            }
            else if (exp.Type.Equals(DataType.NonNullFloat))
            {
                this.il.Emit(OpCodes.Ldc_R4, (float)exp.Value);
            }
            else if (exp.Type.Equals(DataType.NonNullDouble))
            {
                this.il.Emit(OpCodes.Ldc_R8, (double)exp.Value);
            }
            else if (exp.Type.Equals(DataType.NonNullString))
            {
                this.il.Emit(OpCodes.Ldstr, (string)exp.Value);
            }
            else if (exp.Type.Equals(DataType.NonNullDateTime))
            {
                var ctor = typeof(DateTime).GetConstructor(new[] { typeof(long), typeof(DateTimeKind) });
                if (ctor == null)
                {
                    throw new InvalidOperationException();
                }

                var dt = (DateTime)exp.Value;
                this.il.Emit(OpCodes.Ldc_I8, dt.Ticks);
                this.il.Emit(OpCodes.Ldc_I4_S, (int)DateTimeKind.Utc);
                this.il.Emit(OpCodes.Newobj, ctor);
            }
            else if (exp.Type.Equals(DataType.NonNullTransactionState))
            {
                this.il.Emit(OpCodes.Ldc_I4, (int)(TransactionStatus)exp.Value);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc/>
        public override void OnVisit(NotNullExpression exp)
        {
            this.OnVisitNotNullUnaryExpression(exp);
        }

        /// <summary>
        /// Performs a null check for a unary expression
        /// </summary>
        /// <param name="exp">Expression to null check</param>
        public void OnVisitNotNullUnaryExpression(UnaryExpression exp)
        {
            var isNotNull = this.il.DefineLabel();
            var ctor = typeof(NullReferenceException).GetConstructor(Array.Empty<Type>());
            if (exp.Operand is VariableExpression var)
            {
                this.OnVisit(var);
                this.il.Emit(OpCodes.Brtrue_S, isNotNull);
                this.il.Emit(OpCodes.Newobj, ctor);
                this.il.Emit(OpCodes.Throw);
                this.il.MarkLabel(isNotNull);
                this.OnVisit(var);
            }
            else
            {
                var tmp = this.il.DeclareLocal(exp.Type.ClrType);
                this.Visit(exp.Operand);
                this.il.Emit(OpCodes.Stloc, tmp);
                this.il.Emit(OpCodes.Ldloc, tmp);
                this.il.Emit(OpCodes.Brtrue_S, isNotNull);
                this.il.Emit(OpCodes.Newobj, ctor);
                this.il.Emit(OpCodes.Throw);
                this.il.MarkLabel(isNotNull);
                this.il.Emit(OpCodes.Ldloc, tmp);
            }
        }

        /// <inheritdoc/>
        public override void OnVisit(ModuleExpression exp)
        {
            this.OnVisit(exp.Body);
        }

        /// <inheritdoc/>
        public override void OnVisit(BinaryAssignExpression exp)
        {
            var variable = this.variableIndices[(VariableExpression)exp.Left];

            this.Visit(exp.Right);
            this.il.Emit(OpCodes.Stloc, variable.Index);
            this.il.Emit(OpCodes.Ldloc, variable.Index);
        }

        /// <inheritdoc/>
        public override void OnVisit(CommitTransactionExpression exp)
        {
            LocalBuilder originalTransaction;
            var nextTransaction = this.il.DeclareLocal(typeof(Transaction));

            if (exp.Transaction is WithExpression withExp)
            {
                if (withExp.Left is VariableExpression varexp)
                {
                    originalTransaction = this.GetLocalForVariableExpression(varexp);
                }
                else
                {
                    originalTransaction = this.il.DeclareLocal(typeof(Transaction));
                    this.OnVisit(withExp.Left);
                    this.il.Emit(OpCodes.Stloc, originalTransaction);
                }

                this.OnVisit(withExp, true);
                this.il.Emit(OpCodes.Stloc, nextTransaction);
            }
            else
            {
                var data = this.il.DeclareLocal(typeof(TransactionData));
                if (exp.Transaction is VariableExpression varexp)
                {
                    originalTransaction = this.GetLocalForVariableExpression(varexp);
                }
                else
                {
                    originalTransaction = this.il.DeclareLocal(typeof(Transaction));
                    this.OnVisit(exp.Transaction);
                    this.il.Emit(OpCodes.Stloc, originalTransaction);
                }

                this.il.Emit(OpCodes.Ldloc, originalTransaction);
                this.il.EmitCall(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Data)).GetMethod, null);
                this.il.Emit(OpCodes.Stloc, data);

                // Increment revision
                this.il.Emit(OpCodes.Ldloca, data);
                this.il.Emit(OpCodes.Dup);
                this.il.Emit(OpCodes.Call, typeof(TransactionData).GetProperty(nameof(TransactionData.Revision)).GetMethod);
                this.il.Emit(OpCodes.Ldc_I4_1);
                this.il.Emit(OpCodes.Add);
                this.il.Emit(OpCodes.Call, typeof(TransactionData).GetProperty(nameof(TransactionData.Revision)).SetMethod);

                var ctor = typeof(Transaction).GetConstructor(new[] { typeof(TransactionData).MakeByRefType(), typeof(ITransactionStorage) });

                this.il.Emit(OpCodes.Ldloca, data);
                this.il.Emit(OpCodes.Ldloc, originalTransaction);
                this.il.EmitCall(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Storage)).GetMethod, null);
                this.il.Emit(OpCodes.Newobj, ctor);
                this.il.Emit(OpCodes.Stloc, nextTransaction);
            }

            this.il.Emit(OpCodes.Ldloc, originalTransaction);
            this.il.EmitCall(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Storage)).GetMethod, null);
            this.il.Emit(OpCodes.Ldloc, originalTransaction);
            this.il.Emit(OpCodes.Ldloc, nextTransaction);
            var commitTransaction = typeof(ITransactionStorage).GetMethod(nameof(ITransactionStorage.CommitTransactionDelta));
            this.il.Emit(OpCodes.Callvirt, commitTransaction);
        }

        /// <summary>
        /// Produces a copy of a transaction with an incremented revision
        /// </summary>
        /// <param name="tr">Transaction to copy</param>
        /// <returns>Transaction with incremented revision</returns>
        public Transaction IncrementTransactionRevision(Transaction tr)
        {
            var data = tr.Data;
            ++data.Revision;
            var result = new Transaction(data, tr.Storage);
            return result;
        }

        /// <summary>
        /// Visits a WithExpression optionally increments the revision of the target object
        /// </summary>
        /// <param name="exp">Expression to visit</param>
        /// <param name="incrementRevision">Value indicatng whether the object revision should be incremented</param>
        public void OnVisit(WithExpression exp, bool incrementRevision)
        {
            if (exp.Type.ClrType != typeof(Transaction))
            {
                throw new NotSupportedException();
            }

            LocalBuilder tr;

            var transactionData = this.il.DeclareLocal(typeof(TransactionData));
            if (exp.Left is VariableExpression varexp)
            {
                tr = this.GetLocalForVariableExpression(varexp);
            }
            else
            {
                tr = this.il.DeclareLocal(typeof(Transaction));
                this.OnVisit(exp.Left);
                this.il.Emit(OpCodes.Stloc, tr);
            }

            this.il.Emit(OpCodes.Ldloc, tr);
            this.il.Emit(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Data)).GetMethod);
            this.il.Emit(OpCodes.Stloc, transactionData);

            if (incrementRevision)
            {
                this.il.Emit(OpCodes.Ldloca, transactionData);
                this.il.Emit(OpCodes.Dup);
                this.il.Emit(OpCodes.Call, typeof(TransactionData).GetProperty(nameof(TransactionData.Revision)).GetMethod);
                this.il.Emit(OpCodes.Ldc_I4_1);
                this.il.Emit(OpCodes.Add);
                this.il.Emit(OpCodes.Call, typeof(TransactionData).GetProperty(nameof(TransactionData.Revision)).SetMethod);
            }

            foreach (var mem in ((ObjectExpression)exp.Right).Members)
            {
                var prop = typeof(TransactionData).GetProperty(mem.Name);
                this.il.Emit(OpCodes.Ldloca, transactionData);

                if (exp.Left.Type.ClrType == typeof(Transaction) && mem.Name == "Payload")
                {
                    this.VisitPayloadWith(new MemberExpression(exp.Left, prop, ParserRuleContext.EmptyContext), (ObjectExpression)mem.Value);
                }
                else
                {
                    this.OnVisit(mem);
                }

                if (prop.PropertyType.GetTypeInfo().IsGenericType &&
                    prop.PropertyType.GetTypeInfo().GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    this.il.Emit(OpCodes.Newobj, prop.PropertyType.GetConstructor(new[] { prop.PropertyType.GetGenericArguments()[0] }));
                }

                this.il.Emit(OpCodes.Call, prop.SetMethod);
            }

            var ctor = typeof(Transaction).GetConstructor(new[] { typeof(TransactionData).MakeByRefType(), typeof(ITransactionStorage) });
            this.il.Emit(OpCodes.Ldloca, transactionData);
            this.il.Emit(OpCodes.Ldloc, tr);
            this.il.Emit(OpCodes.Call, typeof(Transaction).GetProperty(nameof(Transaction.Storage)).GetMethod);
            this.il.Emit(OpCodes.Newobj, ctor);
        }

        /// <inheritdoc/>
        public override void OnVisit(WithExpression exp)
        {
            this.OnVisit(exp, false);
        }

        /// <summary>
        /// Registers a variable
        /// </summary>
        /// <param name="expr">VariableExpression to register</param>
        /// <param name="isImport">Defines whether or not the variavble is imported</param>
        /// <param name="importAlias">Defines the alias used for the import (only used if isImport is true)</param>
        public void RegisterVariable(VariableExpression expr, bool isImport, string importAlias)
        {
            var builder = this.il.DeclareLocal(expr.Type.ClrType);
            this.variableIndices.Add(expr, new VariableInfo(builder, isImport, importAlias));
        }

        /// <summary>
        /// Registers a variable and produces IL and optionally produces import code for it
        /// </summary>
        /// <param name="expr">VariableExpression to register</param>
        /// <param name="isImport">Defines whether or not the variavble is imported</param>
        /// <param name="importAlias">Defines the alias used for the import (only used if isImport is true)</param>
        public void RegisterVariableExtern(VariableExpression expr, bool isImport, string importAlias)
        {
            var builder = this.il.DeclareLocal(expr.Type.ClrType);
            if (isImport)
            {
                this.il.Emit(OpCodes.Ldarg_2); // this = di.GetService<Transaction>()
                this.il.Emit(OpCodes.Ldnull);
                this.il.Emit(OpCodes.Callvirt, GetServiceMethod.MakeGenericMethod(expr.Type.ClrType));
                this.il.Emit(OpCodes.Stloc, builder);
            }

            this.variableIndices.Add(expr, new VariableInfo(builder, isImport, importAlias));
        }

        /// <inheritdoc/>
        public override void OnVisit(BlockExpression exp)
        {
            foreach (var expression in exp.Body)
            {
                this.OnVisit(expression);
                if (expression.Type.ClrType != typeof(void))
                {
                    this.il.Emit(OpCodes.Pop);
                }
            }
        }

        /// <inheritdoc/>
        public override void OnVisit(ImportExpression exp)
        {
            var loadMethod = GetDependencyMethod.MakeGenericMethod(exp.Type.ClrType);
            this.il.Emit(OpCodes.Ldarg_2);
            if (exp.Name == null)
            {
                this.il.Emit(OpCodes.Ldnull);
            }
            else
            {
                this.il.Emit(OpCodes.Ldstr, exp.Name);
            }

            this.il.EmitCall(OpCodes.Callvirt, loadMethod, null);
        }

        /// <inheritdoc/>
        public override void OnVisit(VariableExpression exp)
        {
            this.OnVisit(exp, false);
        }

        /// <summary>
        /// Visits a variable expression and optionally handles the variable as a reference
        /// </summary>
        /// <param name="exp">VariableExpression to visit</param>
        /// <param name="refValueType">Value indicating whether this should be loaded as a reference value</param>
        public void OnVisit(VariableExpression exp, bool refValueType)
        {
            OpCode opcode;
            LocalBuilder local;
            if (exp.Type.ClrType.GetTypeInfo().IsValueType && refValueType)
            {
                opcode = OpCodes.Ldloca;
            }
            else
            {
                opcode = OpCodes.Ldloc;
            }

            if (exp.Name == "this")
            {
                local = this.@this;
            }
            else if (exp.Name == "expired")
            {
                local = this.expired;
            }
            else
            {
                local = this.variableIndices[exp].Local;
            }

            this.il.Emit(opcode, local);
        }

        /// <inheritdoc/>
        public override void OnVisit(CatchExpression exp)
        {
            if (exp.ExceptionValue != null)
            {
                var loc = this.il.DeclareLocal(exp.ExceptionValue.Type.ClrType);
                this.variableIndices.Add(exp.ExceptionValue, new VariableInfo(loc, false, null));
                this.il.Emit(OpCodes.Stloc, loc);
            }

            this.OnVisit(exp.Body);
        }

        /// <inheritdoc/>
        public override void OnVisit(VariableDeclarationExpression exp)
        {
            if (exp.Assignment != null)
            {
                var imp = exp.Assignment as ImportExpression;
                this.RegisterVariable(exp.Variable, imp != null, imp?.Name);

                this.OnVisit(exp.Assignment);
                this.il.Emit(OpCodes.Stloc, this.variableIndices[exp.Variable].Index);
                this.il.Emit(OpCodes.Ldloc, this.variableIndices[exp.Variable].Index);
            }
            else
            {
                this.RegisterVariable(exp.Variable, isImport: false, importAlias: null);
            }
        }

        /// <inheritdoc/>
        public override void OnVisit(WhileExpression exp)
        {
            if (!exp.DoWhile)
            {
                var startLabel = this.il.DefineLabel();
                var endLabel = this.il.DefineLabel();
                this.il.MarkLabel(startLabel);
                if (exp.Condition != null || exp.Condition == ConstantExpression.TrueExpression)
                {
                    this.OnVisit(exp.Condition);
                    this.il.Emit(OpCodes.Brfalse, endLabel);
                }

                this.OnVisit(exp.Contents);
                this.il.Emit(OpCodes.Br, startLabel);
                this.il.MarkLabel(endLabel);
            }
            else
            {
                var startLabel = this.il.DefineLabel();
                var endLabel = this.il.DefineLabel();
                this.il.MarkLabel(startLabel);
                this.OnVisit(exp.Contents);
                if (exp.Condition != null || exp.Condition == ConstantExpression.TrueExpression)
                {
                    this.OnVisit(exp.Condition);
                    this.il.Emit(OpCodes.Brfalse, endLabel);
                }

                this.il.Emit(OpCodes.Br, startLabel);
                this.il.MarkLabel(endLabel);
            }
        }

        private LocalBuilder GetLocalForVariableExpression(VariableExpression exp)
        {
            if (exp.Name == "this")
            {
                return this.@this;
            }

            return this.variableIndices[exp].Local;
        }

        private void VisitPayloadWith(Expression payload, ObjectExpression expression)
        {
            var dictType = typeof(IDictionary<string, object>);
            var dict = this.il.DeclareLocal(dictType);
            var notNull = this.il.DefineLabel();

            this.il.Emit(OpCodes.Newobj, typeof(System.Dynamic.ExpandoObject).GetConstructor(new Type[0]));
            this.il.Emit(OpCodes.Stloc, dict);
            this.il.MarkLabel(notNull);

            var put = dictType.GetMethod("set_Item");
            foreach (var mem in expression.Members)
            {
                this.il.Emit(OpCodes.Ldloc, dict);
                this.il.Emit(OpCodes.Ldstr, mem.Name);
                this.OnVisit(mem.Value);
                if (mem.Value.Type.ClrType.GetTypeInfo().IsValueType)
                {
                    this.il.Emit(OpCodes.Box, mem.Value.Type.ClrType);
                }

                this.il.Emit(OpCodes.Callvirt, put);
            }

            this.il.Emit(OpCodes.Ldloc, dict);
        }

        private void LoadConstantObject(object value, Type expectedType)
        {
            if (value == null)
            {
                if (expectedType.GetTypeInfo().IsValueType && expectedType.GetTypeInfo().GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    this.il.Emit(OpCodes.Initobj, expectedType);
                }
                else
                {
                    this.il.Emit(OpCodes.Ldnull);
                }
            }
            else if (value is bool boolv)
            {
                if (boolv)
                {
                    this.il.Emit(OpCodes.Ldc_I4_1);
                }
                else
                {
                    this.il.Emit(OpCodes.Ldc_I4_0);
                }
            }
            else if (value is byte)
            {
                this.il.Emit(OpCodes.Ldc_I4, (byte)value);
            }
            else if (value is short)
            {
                this.il.Emit(OpCodes.Ldc_I4, (short)value);
            }
            else if (value is int)
            {
                this.il.Emit(OpCodes.Ldc_I4, (int)value);
            }
            else if (value is long)
            {
                this.il.Emit(OpCodes.Ldc_I8, (long)value);
            }
            else if (value is float)
            {
                this.il.Emit(OpCodes.Ldc_R4, (float)value);
            }
            else if (value is double)
            {
                this.il.Emit(OpCodes.Ldc_R8, (double)value);
            }
            else if (value is string)
            {
                this.il.Emit(OpCodes.Ldstr, (string)value);
            }
            else
            {
                throw new NotSupportedException();
            }

            if (expectedType.GetTypeInfo().IsValueType && expectedType.GetTypeInfo().GetGenericTypeDefinition() ==
                typeof(Nullable<>))
            {
                var ctor = expectedType.GetConstructor(new[] { expectedType.GetGenericArguments()[0] });
                this.il.Emit(OpCodes.Newobj, ctor);
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
                {
                    return i;
                }
            }

            throw new InvalidOperationException();
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
                this.LoadArguments(namedArguments, method);
                return;
            }

            var locals = new LocalBuilder[parameters.Length];

            for (int i = 0; i < namedArguments.Count; ++i)
            {
                int localIndex = this.FindParameterIndex(namedArguments[i].Argument, parameters);

                if (namedArguments[i].Value is VariableExpression vex)
                {
                    locals[localIndex] = this.GetLocalForVariableExpression(vex);
                }
                else
                {
                    locals[localIndex] = this.il.DeclareLocal(parameters[i].ParameterType);

                    this.OnVisit(namedArguments[i]);

                    this.il.Emit(OpCodes.Stloc, locals[localIndex]);
                }
            }

            for (int i = 0; i < parameters.Length; ++i)
            {
                if (this.ContainsNamedArgument(parameters[i].Name, namedArguments))
                {
                    continue;
                }

                this.LoadConstantObject(parameters[i].DefaultValue, parameters[i].ParameterType);
                this.il.Emit(OpCodes.Stloc, locals[i]);
            }

            for (int i = 0; i < locals.Length; ++i)
            {
                if (parameters[i].ParameterType.IsByRef)
                {
                    this.il.Emit(OpCodes.Ldloca, locals[i]);
                }
                else
                {
                    this.il.Emit(OpCodes.Ldloc, locals[i]);
                }
            }
        }

        private void LoadArguments(IEnumerable<Expression> arguments, MethodBase method)
        {
            var parameters = method.GetParameters();
            int count = 0;
            foreach (var exp in arguments)
            {
                this.OnVisit(exp);
                if (parameters[count].ParameterType.IsByRef)
                {
                    var local = this.il.DeclareLocal(parameters[count].ParameterType);
                    this.il.Emit(OpCodes.Stloc, local);
                    this.il.Emit(OpCodes.Ldloca, local);
                }

                ++count;
            }

            for (int i = count; i < parameters.Length; ++i)
            {
                this.LoadConstantObject(parameters[i].DefaultValue, parameters[i].ParameterType);
            }
        }

        private void EmitParse<T>()
        {
            var parseMethod = typeof(T).GetMethod("Parse", new[] { typeof(string), typeof(IFormatProvider) });
            var invProp = typeof(CultureInfo).GetProperty("InvariantCulture").GetMethod;

            this.il.Emit(OpCodes.Call, invProp);
            this.il.Emit(OpCodes.Call, parseMethod);
        }

        private struct VariableInfo
        {
            public VariableInfo(LocalBuilder local, bool isImport, string importAlias)
            {
                this.Local = local;
                this.IsImport = isImport;
                this.ImportAlias = importAlias;
            }

            public int Index => this.Local.LocalIndex;

            public bool IsImport { get; }

            public string ImportAlias { get; }

            public LocalBuilder Local { get; }
        }
    }
}
