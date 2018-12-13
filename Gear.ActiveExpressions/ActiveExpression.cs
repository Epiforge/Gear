using Gear.Components;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Gear.ActiveExpressions
{
    /// <summary>
    /// Provides the base class from which the classes that represent active expression tree nodes are derived; use <see cref="Create{TResult}(LambdaExpression, object[])"/> or one of its overloads to create an active expression
    /// </summary>
#pragma warning disable CS0660 // we can't override Equals in a meaningful way in this class, and besides, inheriting classes should be doing it anyway
#pragma warning disable CS0661 // we can't override GetHashCode in a meaningful way in this class, and besides, inheriting classes should be doing it anyway
    public abstract class ActiveExpression : OverridableSyncDisposablePropertyChangeNotifier
#pragma warning restore CS0660
#pragma warning restore CS0661
    {
        static readonly ConcurrentDictionary<MethodInfo, FastMethodInfo> compiledMethods = new ConcurrentDictionary<MethodInfo, FastMethodInfo>();
        static readonly ConcurrentDictionary<MethodInfo, PropertyInfo> propertyGetMethodToProperty = new ConcurrentDictionary<MethodInfo, PropertyInfo>();

        internal static ActiveExpression Create(Expression expression, ActiveExpressionOptions options, bool deferEvaluation)
        {
            ActiveExpression activeExpression = null;
            switch (expression)
            {
                case BinaryExpression binaryExpression:
                    activeExpression = ActiveBinaryExpression.Create(binaryExpression, options, deferEvaluation);
                    break;
                case ConditionalExpression conditionalExpression:
                    activeExpression = ActiveConditionalExpression.Create(conditionalExpression, options, deferEvaluation);
                    break;
                case ConstantExpression constantExpression:
                    activeExpression = ActiveConstantExpression.Create(constantExpression, options);
                    break;
                case IndexExpression indexExpression:
                    activeExpression = ActiveIndexExpression.Create(indexExpression, options, deferEvaluation);
                    break;
                case MemberExpression memberExpression:
                    activeExpression = ActiveMemberExpression.Create(memberExpression, options, deferEvaluation);
                    break;
                case MethodCallExpression methodCallExpressionForPropertyGet when propertyGetMethodToProperty.GetOrAdd(methodCallExpressionForPropertyGet.Method, GetPropertyFromGetMethod) is PropertyInfo property:
                    if (methodCallExpressionForPropertyGet.Arguments.Count > 0)
                        activeExpression = ActiveIndexExpression.Create(Expression.MakeIndex(methodCallExpressionForPropertyGet.Object, property, methodCallExpressionForPropertyGet.Arguments), options, deferEvaluation);
                    else
                        activeExpression = ActiveMemberExpression.Create(Expression.MakeMemberAccess(methodCallExpressionForPropertyGet.Object, property), options, deferEvaluation);
                    break;
                case MethodCallExpression methodCallExpression:
                    activeExpression = ActiveMethodCallExpression.Create(methodCallExpression, options, deferEvaluation);
                    break;
                case NewExpression newExpression:
                    activeExpression = ActiveNewExpression.Create(newExpression, options, deferEvaluation);
                    break;
                case UnaryExpression unaryExpression:
                    activeExpression = ActiveUnaryExpression.Create(unaryExpression, options, deferEvaluation);
                    break;
            }
            if (!deferEvaluation)
                activeExpression.EvaluateIfDeferred();
            return activeExpression;
        }

        static FastMethodInfo CreateFastMethodInfo(MethodInfo key) => new FastMethodInfo(key);

        protected static FastMethodInfo GetFastMethodInfo(MethodInfo methodInfo) => compiledMethods.GetOrAdd(methodInfo, CreateFastMethodInfo);

        /// <summary>
        /// Produces a human-readable representation of an expression
        /// </summary>
        /// <param name="expressionType">The type of expression</param>
        /// <param name="resultType">The type of the result when the expression is evaluated</param>
        /// <param name="operands">The operands (or arguments) of the expression</param>
        /// <returns>A human-readable representation of the expression</returns>
        public static string GetOperatorExpressionSyntax(ExpressionType expressionType, Type resultType, params object[] operands)
        {
            switch (expressionType)
            {
                case ExpressionType.Add:
                    return $"({operands[0]} + {operands[1]})";
                case ExpressionType.AddChecked:
                    return $"checked({operands[0]} + {operands[1]})";
                case ExpressionType.And:
                    return $"({operands[0]} & {operands[1]})";
                case ExpressionType.Convert:
                    return $"(({resultType.FullName}){operands[0]})";
                case ExpressionType.ConvertChecked:
                    return $"checked(({resultType.FullName}){operands[0]})";
                case ExpressionType.Decrement:
                    return $"({operands[0]} - 1)";
                case ExpressionType.Divide:
                    return $"({operands[0]} / {operands[1]})";
                case ExpressionType.Equal:
                    return $"({operands[0]} == {operands[1]})";
                case ExpressionType.ExclusiveOr:
                    return $"({operands[0]} ^ {operands[1]})";
                case ExpressionType.GreaterThan:
                    return $"({operands[0]} > {operands[1]})";
                case ExpressionType.GreaterThanOrEqual:
                    return $"({operands[0]} >= {operands[1]})";
                case ExpressionType.Increment:
                    return $"({operands[0]} + 1)";
                case ExpressionType.LeftShift:
                    return $"({operands[0]} << {operands[1]})";
                case ExpressionType.LessThan:
                    return $"({operands[0]} < {operands[1]})";
                case ExpressionType.LessThanOrEqual:
                    return $"({operands[0]} <= {operands[1]})";
                case ExpressionType.Modulo:
                    return $"({operands[0]} % {operands[1]})";
                case ExpressionType.Multiply:
                    return $"({operands[0]} * {operands[1]})";
                case ExpressionType.MultiplyChecked:
                    return $"checked({operands[0]} * {operands[1]})";
                case ExpressionType.Negate:
                    return $"(-{operands[0]})";
                case ExpressionType.NegateChecked:
                    return $"checked(-{operands[0]})";
                case ExpressionType.Not when operands[0] is bool || (operands[0] is ActiveExpression notOperand && (notOperand.Type == typeof(bool) || notOperand.Type == typeof(bool?))):
                    return $"(!{operands[0]})";
                case ExpressionType.Not:
                case ExpressionType.OnesComplement:
                    return $"(~{operands[0]})";
                case ExpressionType.NotEqual:
                    return $"({operands[0]} != {operands[1]})";
                case ExpressionType.Or:
                    return $"({operands[0]} | {operands[1]})";
                case ExpressionType.Power:
                    return $"{nameof(Math)}.{nameof(Math.Pow)}({operands[0]}, {operands[1]})";
                case ExpressionType.RightShift:
                    return $"({operands[0]} >> {operands[1]})";
                case ExpressionType.Subtract:
                    return $"({operands[0]} - {operands[1]})";
                case ExpressionType.SubtractChecked:
                    return $"checked({operands[0]} - {operands[1]})";
                case ExpressionType.UnaryPlus:
                    return $"(+{operands[0]})";
                default:
                    throw new ArgumentOutOfRangeException(nameof(expressionType));
            }
        }

        static PropertyInfo GetPropertyFromGetMethod(MethodInfo getMethod) => getMethod.DeclaringType.GetRuntimeProperties().FirstOrDefault(property => property.GetMethod == getMethod);

        /// <summary>
        /// Creates an active expression using a specified lambda expression and arguments
        /// </summary>
        /// <typeparam name="TResult">The type that <paramref name="lambdaExpression"/> returns</typeparam>
        /// <param name="lambdaExpression">The lambda expression</param>
        /// <param name="arguments">The arguments</param>
        /// <returns>The active expression</returns>
        public static ActiveExpression<TResult> Create<TResult>(LambdaExpression lambdaExpression, params object[] arguments) =>
            CreateWithOptions<TResult>(lambdaExpression, null, arguments);

        /// <summary>
        /// Creates an active expression using a specified lambda expression, options and arguments
        /// </summary>
        /// <typeparam name="TResult">The type that <paramref name="lambdaExpression"/> returns</typeparam>
        /// <param name="lambdaExpression">The lambda expression</param>
        /// <param name="options">Active expression options to use instead of <see cref="ActiveExpressionOptions.Default"/></param>
        /// <param name="arguments">The arguments</param>
        /// <returns>The active expression</returns>
        public static ActiveExpression<TResult> CreateWithOptions<TResult>(LambdaExpression lambdaExpression, ActiveExpressionOptions options, params object[] arguments)
        {
            options?.Freeze();
            return ActiveExpression<TResult>.Create(lambdaExpression, options, arguments);
        }

        /// <summary>
        /// Creates an active expression using a specified strongly-typed lambda expression with no arguments
        /// </summary>
        /// <typeparam name="TArg">The type of the argument.</typeparam>
        /// <typeparam name="TResult">The type that <paramref name="expression"/> returns</typeparam>
        /// <param name="expression">The strongly-typed lambda expression</param>
        /// <param name="options">Active expression options to use instead of <see cref="ActiveExpressionOptions.Default"/></param>
        /// <returns>The active expression</returns>
        public static ActiveExpression<TResult> Create<TResult>(Expression<Func<TResult>> expression, ActiveExpressionOptions options = null)
        {
            options?.Freeze();
            return ActiveExpression<TResult>.Create(expression, options);
        }

        /// <summary>
        /// Creates an active expression using a specified strongly-typed lambda expression and one argument
        /// </summary>
        /// <typeparam name="TArg">The type of the argument.</typeparam>
        /// <typeparam name="TResult">The type that <paramref name="expression"/> returns</typeparam>
        /// <param name="expression">The strongly-typed lambda expression</param>
        /// <param name="arg">The argument</param>
        /// <param name="options">Active expression options to use instead of <see cref="ActiveExpressionOptions.Default"/></param>
        /// <returns>The active expression</returns>
        public static ActiveExpression<TArg, TResult> Create<TArg, TResult>(Expression<Func<TArg, TResult>> expression, TArg arg, ActiveExpressionOptions options = null)
        {
            options?.Freeze();
            return ActiveExpression<TArg, TResult>.Create(expression, arg, options);
        }

        /// <summary>
        /// Creates an active expression using a specified strongly-typed lambda expression and two arguments
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument</typeparam>
        /// <typeparam name="TArg2">The type of the second argument</typeparam>
        /// <typeparam name="TResult">The type that <paramref name="expression"/> returns</typeparam>
        /// <param name="expression">The strongly-typed lambda expression</param>
        /// <param name="arg1">The first argument</param>
        /// <param name="arg2">The second argument</param>
        /// <param name="options">Active expression options to use instead of <see cref="ActiveExpressionOptions.Default"/></param>
        /// <returns>The active expression</returns>
        public static ActiveExpression<TArg1, TArg2, TResult> Create<TArg1, TArg2, TResult>(Expression<Func<TArg1, TArg2, TResult>> expression, TArg1 arg1, TArg2 arg2, ActiveExpressionOptions options = null)
        {
            options?.Freeze();
            return ActiveExpression<TArg1, TArg2, TResult>.Create(expression, arg1, arg2, options);
        }

        /// <summary>
        /// Creates an active expression using a specified strongly-typed lambda expression and three arguments
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument</typeparam>
        /// <typeparam name="TArg2">The type of the second argument</typeparam>
        /// <typeparam name="TArg3">The type of the third argument</typeparam>
        /// <typeparam name="TResult">The type that <paramref name="expression"/> returns</typeparam>
        /// <param name="expression">The strongly-typed lambda expression</param>
        /// <param name="arg1">The first argument</param>
        /// <param name="arg2">The second argument</param>
        /// <param name="arg3">The third argument</param>
        /// <param name="options">Active expression options to use instead of <see cref="ActiveExpressionOptions.Default"/></param>
        /// <returns>The active expression</returns>
        public static ActiveExpression<TArg1, TArg2, TArg3, TResult> Create<TArg1, TArg2, TArg3, TResult>(Expression<Func<TArg1, TArg2, TArg3, TResult>> expression, TArg1 arg1, TArg2 arg2, TArg3 arg3, ActiveExpressionOptions options = null)
        {
            options?.Freeze();
            return ActiveExpression<TArg1, TArg2, TArg3, TResult>.Create(expression, arg1, arg2, arg3, options);
        }

        protected static string GetValueLiteral(Exception fault, bool deferred, object value)
        {
            if (fault != null)
                return $"[{fault.GetType().Name}: {fault.Message}]";
            if (deferred)
                return "?";
            if (value == null)
                return "null";
            if (value is string str)
            {
                var sb = new StringBuilder(str);
                sb.Replace("\\", "\\\\");
                sb.Replace("\0", "\\0");
                sb.Replace("\a", "\\a");
                sb.Replace("\b", "\\b");
                sb.Replace("\f", "\\f");
                sb.Replace("\n", "\\n");
                sb.Replace("\r", "\\r");
                sb.Replace("\t", "\\t");
                sb.Replace("\v", "\\v");
                return $"\"{sb}\"";
            }
            if (value is char ch)
            {
                switch (ch)
                {
                    case '\\':
                        return "'\\\\'";
                    case '\0':
                        return "'\\0'";
                    case '\a':
                        return "'\\a'";
                    case '\b':
                        return "'\\b'";
                    case '\f':
                        return "'\\f'";
                    case '\n':
                        return "'\\n'";
                    case '\r':
                        return "'\\r'";
                    case '\t':
                        return "'\\t'";
                    case '\v':
                        return "'\\v'";
                    default:
                        return $"'{ch}'";
                }
            }
            if (value is DateTime dt)
                return $"new System.DateTime({dt.Ticks}, System.DateTimeKind.{dt.Kind})";
            if (value is TimeSpan ts)
                return $"new System.TimeSpan({ts.Ticks})";
            if (value is Guid guid)
                return $"new System.Guid(\"{guid}\")";
            return $"{value}";
        }

        internal static Expression ReplaceParameters(LambdaExpression lambdaExpression, params object[] arguments)
        {
            var parameterTranslation = new Dictionary<ParameterExpression, ConstantExpression>();
            lambdaExpression = (LambdaExpression)(Optimizer?.Invoke(lambdaExpression) ?? lambdaExpression);
            for (var i = 0; i < lambdaExpression.Parameters.Count; ++i)
            {
                var parameter = lambdaExpression.Parameters[i];
                var constant = Expression.Constant(arguments[i], parameter.Type);
                parameterTranslation.Add(parameter, constant);
            }
            return ReplaceParameters(parameterTranslation, lambdaExpression.Body);
        }

        static Expression ReplaceParameters(Dictionary<ParameterExpression, ConstantExpression> parameterTranslation, Expression expression)
        {
            switch (expression)
            {
                case BinaryExpression binaryExpression:
                    return Expression.MakeBinary(binaryExpression.NodeType, ReplaceParameters(parameterTranslation, binaryExpression.Left), ReplaceParameters(parameterTranslation, binaryExpression.Right), binaryExpression.IsLiftedToNull, binaryExpression.Method, binaryExpression.Conversion);
                case ConditionalExpression conditionalExpression:
                    return Expression.Condition(ReplaceParameters(parameterTranslation, conditionalExpression.Test), ReplaceParameters(parameterTranslation, conditionalExpression.IfTrue), ReplaceParameters(parameterTranslation, conditionalExpression.IfFalse), conditionalExpression.Type);
                case ConstantExpression constantExpression:
                    return constantExpression;
                case IndexExpression indexExpression:
                    return Expression.MakeIndex(ReplaceParameters(parameterTranslation, indexExpression.Object), indexExpression.Indexer, indexExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument)));
                case MemberExpression memberExpression:
                    return Expression.MakeMemberAccess(ReplaceParameters(parameterTranslation, memberExpression.Expression), memberExpression.Member);
                case MethodCallExpression methodCallExpression:
                    return methodCallExpression.Object == null ? Expression.Call(methodCallExpression.Method, methodCallExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument))) : Expression.Call(ReplaceParameters(parameterTranslation, methodCallExpression.Object), methodCallExpression.Method, methodCallExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument)));
                case NewExpression newExpression:
                    var newArguments = newExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument));
                    return newExpression.Members == null ? Expression.New(newExpression.Constructor, newArguments) : Expression.New(newExpression.Constructor, newArguments, newExpression.Members);
                case ParameterExpression parameterExpression:
                    return parameterTranslation[parameterExpression];
                case UnaryExpression unaryExpression:
                    return Expression.MakeUnary(unaryExpression.NodeType, ReplaceParameters(parameterTranslation, unaryExpression.Operand), unaryExpression.Type, unaryExpression.Method);
                case null:
                    return null;
                default:
                    throw new NotSupportedException($"Cannot replace parameters in {expression.GetType().Name}");
            }
        }

        /// <summary>
        /// Determines whether two active expression tree nodes are the same
        /// </summary>
        /// <param name="a">The first node to compare, or null</param>
        /// <param name="b">The second node to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is the same as <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator ==(ActiveExpression a, ActiveExpression b)
        {
            if (a is null && b is null)
                return true;
            if (a is null || b is null)
                return false;
            if (a is ActiveAndAlsoExpression andAlsoA && b is ActiveAndAlsoExpression andAlsoB)
                return andAlsoA == andAlsoB;
            if (a is ActiveCoalesceExpression coalesceA && b is ActiveCoalesceExpression coalesceB)
                return coalesceA == coalesceB;
            if (a is ActiveOrElseExpression orElseA && b is ActiveOrElseExpression orElseB)
                return orElseA == orElseB;
            if (a is ActiveBinaryExpression binaryA && b is ActiveBinaryExpression binaryB)
                return binaryA == binaryB;
            if (a is ActiveConditionalExpression conditionalA && b is ActiveConditionalExpression conditionalB)
                return conditionalA == conditionalB;
            if (a is ActiveConstantExpression constantA && b is ActiveConstantExpression constantB)
                return constantA == constantB;
            if (a is ActiveIndexExpression indexA && b is ActiveIndexExpression indexB)
                return indexA == indexB;
            if (a is ActiveMemberExpression memberA && b is ActiveMemberExpression memberB)
                return memberA == memberB;
            if (a is ActiveMethodCallExpression methodCallA && b is ActiveMethodCallExpression methodCallB)
                return methodCallA == methodCallB;
            if (a is ActiveNewExpression newA && b is ActiveNewExpression newB)
                return newA == newB;
            if (a is ActiveUnaryExpression unaryA && b is ActiveUnaryExpression unaryB)
                return unaryA == unaryB;
            return false;
        }

        /// <summary>
        /// Determines whether two active expression tree nodes are different
        /// </summary>
        /// <param name="a">The first node to compare, or null</param>
        /// <param name="b">The second node to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator !=(ActiveExpression a, ActiveExpression b)
        {
            if (a is null && b is null)
                return false;
            if (a is null || b is null)
                return true;
            if (a is ActiveAndAlsoExpression andAlsoA && b is ActiveAndAlsoExpression andAlsoB)
                return andAlsoA != andAlsoB;
            if (a is ActiveCoalesceExpression coalesceA && b is ActiveCoalesceExpression coalesceB)
                return coalesceA != coalesceB;
            if (a is ActiveOrElseExpression orElseA && b is ActiveOrElseExpression orElseB)
                return orElseA != orElseB;
            if (a is ActiveBinaryExpression binaryA && b is ActiveBinaryExpression binaryB)
                return binaryA != binaryB;
            if (a is ActiveConditionalExpression conditionalA && b is ActiveConditionalExpression conditionalB)
                return conditionalA != conditionalB;
            if (a is ActiveConstantExpression constantA && b is ActiveConstantExpression constantB)
                return constantA != constantB;
            if (a is ActiveIndexExpression indexA && b is ActiveIndexExpression indexB)
                return indexA != indexB;
            if (a is ActiveMemberExpression memberA && b is ActiveMemberExpression memberB)
                return memberA != memberB;
            if (a is ActiveMethodCallExpression methodCallA && b is ActiveMethodCallExpression methodCallB)
                return methodCallA != methodCallB;
            if (a is ActiveNewExpression newA && b is ActiveNewExpression newB)
                return newA != newB;
            if (a is ActiveUnaryExpression unaryA && b is ActiveUnaryExpression unaryB)
                return unaryA != unaryB;
            return true;
        }

        /// <summary>
        /// Gets/sets the method that will be invoked during the active expression creation process to optimize expressions (default is null)
        /// </summary>
        public static Func<Expression, Expression> Optimizer { get; set; }

        internal ActiveExpression(Type type, ExpressionType nodeType, ActiveExpressionOptions options, bool deferEvaluation)
        {
            Type = type;
            defaultValue = FastDefault.Get(type);
            val = defaultValue;
            valueEqualityComparer = FastEqualityComparer.Create(type);
            NodeType = nodeType;
            this.options = options;
            deferringEvaluation = deferEvaluation;
        }

        internal ActiveExpression(Type type, ExpressionType nodeType, ActiveExpressionOptions options, object value) : this(type, nodeType, options, false) => val = value;

        readonly object defaultValue;
        bool deferringEvaluation;
        readonly object deferringEvaluationLock = new object();
        Exception fault;
        protected readonly ActiveExpressionOptions options;
        object val;
        readonly FastEqualityComparer valueEqualityComparer;

        [ExcludeFromCodeCoverage]
        protected virtual void Evaluate()
        {
        }

        void EvaluateIfDeferred()
        {
            lock (deferringEvaluationLock)
            {
                if (deferringEvaluation)
                {
                    deferringEvaluation = false;
                    Evaluate();
                }
            }
        }

        protected void EvaluateIfNotDeferred()
        {
            lock (deferringEvaluationLock)
            {
                if (!deferringEvaluation)
                    Evaluate();
            }
        }

        protected bool TryGetUndeferredValue(out object value)
        {
            lock (deferringEvaluationLock)
            {
                if (deferringEvaluation)
                {
                    value = null;
                    return false;
                }
            }
            value = val;
            return true;
        }

        protected ActiveExpressionOptions ApplicableOptions => options ?? ActiveExpressionOptions.Default;

        protected string ToStringSuffix => $"/* {GetValueLiteral(fault, !TryGetUndeferredValue(out var value), value)} */";

        public Exception Fault
        {
            get
            {
                EvaluateIfDeferred();
                return fault;
            }
            protected set
            {
                SetBackedProperty(ref val, in defaultValue, nameof(Value));
                SetBackedProperty(ref fault, in value);
            }
        }

        public ExpressionType NodeType { get; }

        public Type Type { get; }

        public object Value
        {
            get
            {
                EvaluateIfDeferred();
                return val;
            }
            protected set
            {
                SetBackedProperty(ref fault, null, nameof(Fault));
                if (!valueEqualityComparer.Equals(value, val))
                {
                    OnPropertyChanging();
                    val = value;
                    OnPropertyChanged();
                }
            }
        }
    }

    /// <summary>
    /// Represents an active evaluation of a lambda expression
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the lambda expression upon which this active expression is based</typeparam>
    public class ActiveExpression<TResult> : OverridableSyncDisposablePropertyChangeNotifier
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<ActiveExpression, ActiveExpression<TResult>> instances = new Dictionary<ActiveExpression, ActiveExpression<TResult>>();

        internal static ActiveExpression<TResult> Create(LambdaExpression expression, ActiveExpressionOptions options, params object[] args)
        {
            var activeExpression = ActiveExpression.Create(ActiveExpression.ReplaceParameters(expression, args), options, false);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(activeExpression, out var instance))
                {
                    instance = new ActiveExpression<TResult>(activeExpression, options, args.ToImmutableArray());
                    instances.Add(activeExpression, instance);
                }
                ++instance.disposalCount;
                return instance;
            }
        }

        /// <summary>
        /// Determines whether two active expressions are the same
        /// </summary>
        /// <param name="a">The first expression to compare, or null</param>
        /// <param name="b">The second expression to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is the same as <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator ==(ActiveExpression<TResult> a, ActiveExpression<TResult> b) => a?.expression == b?.expression;

        /// <summary>
        /// Determines whether two active expressions are different
        /// </summary>
        /// <param name="a">The first expression to compare, or null</param>
        /// <param name="b">The second expression to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator !=(ActiveExpression<TResult> a, ActiveExpression<TResult> b) => a?.expression != b?.expression;

        ActiveExpression(ActiveExpression expression, ActiveExpressionOptions options, IReadOnlyList<object> arguments)
        {
            this.expression = expression;
            Options = options;
            Arguments = arguments;
            fault = this.expression.Fault;
            val = (TResult)this.expression.Value;
            this.expression.PropertyChanged += ExpressionPropertyChanged;
        }

        int disposalCount;
        readonly ActiveExpression expression;
        Exception fault;
        TResult val;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                instances.Remove(expression);
            }
            expression.PropertyChanged -= ExpressionPropertyChanged;
            expression.Dispose();
            return true;
        }

        public override bool Equals(object obj) => obj is ActiveExpression<TResult> other && expression.Equals(other.expression);

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = expression.Fault;
            else if (e.PropertyName == nameof(Value))
                Value = expression.Value is TResult typedValue ? typedValue : default;
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveExpression<TResult>), expression);

        public override string ToString() => expression.ToString();

        /// <summary>
        /// Gets the arguments that were passed to the lambda expression
        /// </summary>
        public IReadOnlyList<object> Arguments { get; }

        /// <summary>
        /// Gets the exception that was thrown while evaluating the lambda expression; <c>null</c> if there was no such exception
        /// </summary>
        public Exception Fault
        {
            get => fault;
            private set => SetBackedProperty(ref fault, in value);
        }

        /// <summary>
        /// Gets the options used when creating the active expression
        /// </summary>
        public ActiveExpressionOptions Options { get; }

        /// <summary>
        /// Gets the result of evaluating the lambda expression
        /// </summary>
        public TResult Value
        {
            get => val;
            private set => SetBackedProperty(ref val, in value);
        }
    }

    /// <summary>
    /// Represents an active evaluation of a strongly-typed lambda expression with a single argument
    /// </summary>
    /// <typeparam name="TArg">The type of the argument passed to the lambda expression</typeparam>
    /// <typeparam name="TResult">The type of the value returned by the expression upon which this active expression is based</typeparam>
    public class ActiveExpression<TArg, TResult> : OverridableSyncDisposablePropertyChangeNotifier
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<ActiveExpression, ActiveExpression<TArg, TResult>> instances = new Dictionary<ActiveExpression, ActiveExpression<TArg, TResult>>();

        internal static ActiveExpression<TArg, TResult> Create(LambdaExpression expression, TArg arg, ActiveExpressionOptions options = null)
        {
            var activeExpression = ActiveExpression.Create(ActiveExpression.ReplaceParameters(expression, arg), options, false);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(activeExpression, out var instance))
                {
                    instance = new ActiveExpression<TArg, TResult>(activeExpression, options, arg);
                    instances.Add(activeExpression, instance);
                }
                ++instance.disposalCount;
                return instance;
            }
        }

        /// <summary>
        /// Determines whether two active expressions are the same
        /// </summary>
        /// <param name="a">The first expression to compare, or null</param>
        /// <param name="b">The second expression to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is the same as <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator ==(ActiveExpression<TArg, TResult> a, ActiveExpression<TArg, TResult> b) => a?.expression == b?.expression;

        /// <summary>
        /// Determines whether two active expressions are different
        /// </summary>
        /// <param name="a">The first expression to compare, or null</param>
        /// <param name="b">The second expression to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator !=(ActiveExpression<TArg, TResult> a, ActiveExpression<TArg, TResult> b) => a?.expression != b?.expression;

        ActiveExpression(ActiveExpression expression, ActiveExpressionOptions options, TArg arg)
        {
            this.expression = expression;
            Options = options;
            Arg = arg;
            fault = this.expression.Fault;
            val = (TResult)this.expression.Value;
            this.expression.PropertyChanged += ExpressionPropertyChanged;
        }

        int disposalCount;
        readonly ActiveExpression expression;
        Exception fault;
        TResult val;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                instances.Remove(expression);
            }
            expression.PropertyChanged -= ExpressionPropertyChanged;
            expression.Dispose();
            return true;
        }

        public override bool Equals(object obj) => obj is ActiveExpression<TArg, TResult> other && expression.Equals(other.expression);

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = expression.Fault;
            else if (e.PropertyName == nameof(Value))
                Value = expression.Value is TResult typedValue ? typedValue : default;
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveExpression<TArg, TResult>), expression);

        public override string ToString() => expression.ToString();

        /// <summary>
        /// Gets the argument that was passed to the lambda expression
        /// </summary>
        public TArg Arg { get; }

        /// <summary>
        /// Gets the exception that was thrown while evaluating the lambda expression; <c>null</c> if there was no such exception
        /// </summary>
        public Exception Fault
        {
            get => fault;
            private set => SetBackedProperty(ref fault, in value);
        }

        /// <summary>
        /// Gets the options used when creating the active expression
        /// </summary>
        public ActiveExpressionOptions Options { get; }

        /// <summary>
        /// Gets the result of evaluating the lambda expression
        /// </summary>
        public TResult Value
        {
            get => val;
            private set => SetBackedProperty(ref val, in value);
        }
    }

    /// <summary>
    /// Represents an active evaluation of a strongly-typed lambda expression with two arguments
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument passed to the lambda expression</typeparam>
    /// <typeparam name="TArg2">The type of the second argument passed to the lambda expression</typeparam>
    /// <typeparam name="TResult">The type of the value returned by the expression upon which this active expression is based</typeparam>
    public class ActiveExpression<TArg1, TArg2, TResult> : OverridableSyncDisposablePropertyChangeNotifier
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<ActiveExpression, ActiveExpression<TArg1, TArg2, TResult>> instances = new Dictionary<ActiveExpression, ActiveExpression<TArg1, TArg2, TResult>>();

        internal static ActiveExpression<TArg1, TArg2, TResult> Create(LambdaExpression expression, TArg1 arg1, TArg2 arg2, ActiveExpressionOptions options = null)
        {
            var activeExpression = ActiveExpression.Create(ActiveExpression.ReplaceParameters(expression, arg1, arg2), options, false);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(activeExpression, out var instance))
                {
                    instance = new ActiveExpression<TArg1, TArg2, TResult>(activeExpression, options, arg1, arg2);
                    instances.Add(activeExpression, instance);
                }
                ++instance.disposalCount;
                return instance;
            }
        }

        /// <summary>
        /// Determines whether two active expressions are the same
        /// </summary>
        /// <param name="a">The first expression to compare, or null</param>
        /// <param name="b">The second expression to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is the same as <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator ==(ActiveExpression<TArg1, TArg2, TResult> a, ActiveExpression<TArg1, TArg2, TResult> b) => a?.expression == b?.expression;

        /// <summary>
        /// Determines whether two active expressions are different
        /// </summary>
        /// <param name="a">The first expression to compare, or null</param>
        /// <param name="b">The second expression to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator !=(ActiveExpression<TArg1, TArg2, TResult> a, ActiveExpression<TArg1, TArg2, TResult> b) => a?.expression != b?.expression;

        ActiveExpression(ActiveExpression expression, ActiveExpressionOptions options, TArg1 arg1, TArg2 arg2)
        {
            this.expression = expression;
            Options = options;
            Arg1 = arg1;
            Arg2 = arg2;
            fault = this.expression.Fault;
            val = (TResult)this.expression.Value;
            this.expression.PropertyChanged += ExpressionPropertyChanged;
        }

        int disposalCount;
        readonly ActiveExpression expression;
        Exception fault;
        TResult val;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                instances.Remove(expression);
            }
            expression.PropertyChanged -= ExpressionPropertyChanged;
            expression.Dispose();
            return true;
        }

        public override bool Equals(object obj) => obj is ActiveExpression<TArg1, TArg2, TResult> other && expression.Equals(other.expression);

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = expression.Fault;
            else if (e.PropertyName == nameof(Value))
                Value = expression.Value is TResult typedValue ? typedValue : default;
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveExpression<TArg1, TArg2, TResult>), expression);

        public override string ToString() => expression.ToString();

        /// <summary>
        /// Gets the first argument that was passed to the lambda expression
        /// </summary>
        public TArg1 Arg1 { get; }

        /// <summary>
        /// Gets the second argument that was passed to the lambda expression
        /// </summary>
        public TArg2 Arg2 { get; }

        /// <summary>
        /// Gets the exception that was thrown while evaluating the lambda expression; <c>null</c> if there was no such exception
        /// </summary>
        public Exception Fault
        {
            get => fault;
            private set => SetBackedProperty(ref fault, in value);
        }

        /// <summary>
        /// Gets the options used when creating the active expression
        /// </summary>
        public ActiveExpressionOptions Options { get; }

        /// <summary>
        /// Gets the result of evaluating the lambda expression
        /// </summary>
        public TResult Value
        {
            get => val;
            private set => SetBackedProperty(ref val, in value);
        }
    }

    /// <summary>
    /// Represents an active evaluation of a strongly-typed lambda expression with three arguments
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument passed to the lambda expression</typeparam>
    /// <typeparam name="TArg2">The type of the second argument passed to the lambda expression</typeparam>
    /// <typeparam name="TArg3">The type of the third argument passed to the lambda expression</typeparam>
    /// <typeparam name="TResult">The type of the value returned by the expression upon which this active expression is based</typeparam>
    public class ActiveExpression<TArg1, TArg2, TArg3, TResult> : OverridableSyncDisposablePropertyChangeNotifier
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<ActiveExpression, ActiveExpression<TArg1, TArg2, TArg3, TResult>> instances = new Dictionary<ActiveExpression, ActiveExpression<TArg1, TArg2, TArg3, TResult>>();

        internal static ActiveExpression<TArg1, TArg2, TArg3, TResult> Create(LambdaExpression expression, TArg1 arg1, TArg2 arg2, TArg3 arg3, ActiveExpressionOptions options = null)
        {
            var activeExpression = ActiveExpression.Create(ActiveExpression.ReplaceParameters(expression, arg1, arg2, arg3), options, false);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(activeExpression, out var instance))
                {
                    instance = new ActiveExpression<TArg1, TArg2, TArg3, TResult>(activeExpression, options, arg1, arg2, arg3);
                    instances.Add(activeExpression, instance);
                }
                ++instance.disposalCount;
                return instance;
            }
        }

        /// <summary>
        /// Determines whether two active expressions are the same
        /// </summary>
        /// <param name="a">The first expression to compare, or null</param>
        /// <param name="b">The second expression to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is the same as <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator ==(ActiveExpression<TArg1, TArg2, TArg3, TResult> a, ActiveExpression<TArg1, TArg2, TArg3, TResult> b) => a?.expression == b?.expression;

        /// <summary>
        /// Determines whether two active expressions are different
        /// </summary>
        /// <param name="a">The first expression to compare, or null</param>
        /// <param name="b">The second expression to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator !=(ActiveExpression<TArg1, TArg2, TArg3, TResult> a, ActiveExpression<TArg1, TArg2, TArg3, TResult> b) => a?.expression != b?.expression;

        ActiveExpression(ActiveExpression expression, ActiveExpressionOptions options, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            this.expression = expression;
            Options = options;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            fault = this.expression.Fault;
            val = (TResult)this.expression.Value;
            this.expression.PropertyChanged += ExpressionPropertyChanged;
        }

        int disposalCount;
        readonly ActiveExpression expression;
        Exception fault;
        TResult val;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                instances.Remove(expression);
            }
            expression.PropertyChanged -= ExpressionPropertyChanged;
            expression.Dispose();
            return true;
        }

        public override bool Equals(object obj) => obj is ActiveExpression<TArg1, TArg2, TArg3, TResult> other && expression.Equals(other.expression);

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = expression.Fault;
            else if (e.PropertyName == nameof(Value))
                Value = expression.Value is TResult typedValue ? typedValue : default;
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveExpression<TArg1, TArg2, TArg3, TResult>), expression);

        public override string ToString() => expression.ToString();

        /// <summary>
        /// Gets the first argument that was passed to the lambda expression
        /// </summary>
        public TArg1 Arg1 { get; }

        /// <summary>
        /// Gets the second argument that was passed to the lambda expression
        /// </summary>
        public TArg2 Arg2 { get; }

        /// <summary>
        /// Gets the third argument that was passed to the lambda expression
        /// </summary>
        public TArg3 Arg3 { get; }

        /// <summary>
        /// Gets the exception that was thrown while evaluating the lambda expression; <c>null</c> if there was no such exception
        /// </summary>
        public Exception Fault
        {
            get => fault;
            private set => SetBackedProperty(ref fault, in value);
        }

        /// <summary>
        /// Gets the options used when creating the active expression
        /// </summary>
        public ActiveExpressionOptions Options { get; }

        /// <summary>
        /// Gets the result of evaluating the lambda expression
        /// </summary>
        public TResult Value
        {
            get => val;
            private set => SetBackedProperty(ref val, in value);
        }
    }
}
