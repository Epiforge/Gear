using Gear.Components;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions
{
    /// <summary>
    /// Provides the base class from which the classes that represent active expression tree nodes are derived.
    /// Active expressions subscribe to notification events for values in each stage of evaluation and will re-evaluate dependent portions of the expression tree when a change occurs.
    /// Use <see cref="Create{TResult}(LambdaExpression, object[])"/> or one of its strongly-typed overloads to create an active expression.
    /// </summary>
    public abstract class ActiveExpression : OverridableSyncDisposablePropertyChangeNotifier
    {
        static readonly ConcurrentDictionary<MethodInfo, FastMethodInfo> compiledMethods = new ConcurrentDictionary<MethodInfo, FastMethodInfo>();

        static FastMethodInfo CreateFastMethodInfo(MethodInfo key) => new FastMethodInfo(key);

        protected static FastMethodInfo GetFastMethodInfo(MethodInfo methodInfo) => compiledMethods.GetOrAdd(methodInfo, CreateFastMethodInfo);

        internal static ActiveExpression Create(Expression expression)
        {
            switch (expression)
            {
                case BinaryExpression binaryExpression:
                    return ActiveBinaryExpression.Create(binaryExpression);
                case ConditionalExpression conditionalExpression:
                    return ActiveConditionalExpression.Create(conditionalExpression);
                case ConstantExpression constantExpression:
                    return ActiveConstantExpression.Create(constantExpression);
                case IndexExpression indexExpression:
                    return ActiveIndexExpression.Create(indexExpression);
                case MemberExpression memberExpression:
                    return ActiveMemberExpression.Create(memberExpression);
                case MethodCallExpression methodCallExpression:
                    return ActiveMethodCallExpression.Create(methodCallExpression);
                case NewExpression newExpression:
                    return ActiveNewExpression.Create(newExpression);
                case UnaryExpression unaryExpression:
                    return ActiveUnaryExpression.Create(unaryExpression);
                case null:
                    throw new ArgumentNullException(nameof(expression));
                default:
                    throw new NotSupportedException($"Cannot create an expression of type \"{expression.GetType().Name}\"");
            }
        }

        /// <summary>
        /// Creates an active expression using a specified lambda expression and arguments
        /// </summary>
        /// <typeparam name="TResult">The type that <paramref name="lambdaExpression"/> returns</typeparam>
        /// <param name="lambdaExpression">The lambda expression</param>
        /// <param name="arguments">The arguments</param>
        /// <returns>The active expression</returns>
        public static ActiveExpression<TResult> Create<TResult>(LambdaExpression lambdaExpression, params object[] arguments) =>
            ActiveExpression<TResult>.Create(lambdaExpression, arguments);

        /// <summary>
        /// Creates an active expression using a specified strongly-typed lambda expression with no arguments
        /// </summary>
        /// <typeparam name="TArg">The type of the argument.</typeparam>
        /// <typeparam name="TResult">The type that <paramref name="expression"/> returns</typeparam>
        /// <param name="expression">The strongly-typed lambda expression</param>
        /// <param name="arg">The argument</param>
        /// <returns>The active expression</returns>
        public static ActiveExpression<TResult> Create<TResult>(Expression<Func<TResult>> expression) =>
            ActiveExpression<TResult>.Create(expression);

        /// <summary>
        /// Creates an active expression using a specified strongly-typed lambda expression and one argument
        /// </summary>
        /// <typeparam name="TArg">The type of the argument.</typeparam>
        /// <typeparam name="TResult">The type that <paramref name="expression"/> returns</typeparam>
        /// <param name="expression">The strongly-typed lambda expression</param>
        /// <param name="arg">The argument</param>
        /// <returns>The active expression</returns>
        public static ActiveExpression<TArg, TResult> Create<TArg, TResult>(Expression<Func<TArg, TResult>> expression, TArg arg) =>
            ActiveExpression<TArg, TResult>.Create(expression, arg);

        /// <summary>
        /// Creates an active expression using a specified strongly-typed lambda expression and two arguments
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument</typeparam>
        /// <typeparam name="TArg2">The type of the second argument</typeparam>
        /// <typeparam name="TResult">The type that <paramref name="expression"/> returns</typeparam>
        /// <param name="expression">The strongly-typed lambda expression</param>
        /// <param name="arg1">The first argument</param>
        /// <param name="arg2">The second argument</param>
        /// <returns>The active expression</returns>
        public static ActiveExpression<TArg1, TArg2, TResult> Create<TArg1, TArg2, TResult>(Expression<Func<TArg1, TArg2, TResult>> expression, TArg1 arg1, TArg2 arg2) =>
            ActiveExpression<TArg1, TArg2, TResult>.Create(expression, arg1, arg2);

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
        /// <returns>The active expression</returns>
        public static ActiveExpression<TArg1, TArg2, TArg3, TResult> Create<TArg1, TArg2, TArg3, TResult>(Expression<Func<TArg1, TArg2, TArg3, TResult>> expression, TArg1 arg1, TArg2 arg2, TArg3 arg3) =>
            ActiveExpression<TArg1, TArg2, TArg3, TResult>.Create(expression, arg1, arg2, arg3);

        internal static Expression ReplaceParameters(LambdaExpression lambdaExpression, params object[] arguments)
        {
            var parameterTranslation = new Dictionary<ParameterExpression, ConstantExpression>();
            for (var i = 0; i < lambdaExpression.Parameters.Count; ++i)
            {
                var parameter = lambdaExpression.Parameters[i];
                parameterTranslation.Add(parameter, Expression.Constant(arguments[i], parameter.Type));
            }
            var expression = lambdaExpression.Body;
            while (expression?.CanReduce ?? false)
                expression = expression.ReduceAndCheck();
            return ReplaceParameters(parameterTranslation, expression);
        }

        static Expression ReplaceParameters(Dictionary<ParameterExpression, ConstantExpression> parameterTranslation, Expression expression)
        {
            switch (expression)
            {
                case BinaryExpression binaryExpression:
                    return Expression.MakeBinary(binaryExpression.NodeType, ReplaceParameters(parameterTranslation, binaryExpression.Left), ReplaceParameters(parameterTranslation, binaryExpression.Right), binaryExpression.IsLiftedToNull, binaryExpression.Method, (LambdaExpression)ReplaceParameters(parameterTranslation, binaryExpression.Conversion));
                case ConditionalExpression conditionalExpression:
                    return Expression.Condition(ReplaceParameters(parameterTranslation, conditionalExpression.Test), ReplaceParameters(parameterTranslation, conditionalExpression.IfTrue), ReplaceParameters(parameterTranslation, conditionalExpression.IfTrue), conditionalExpression.Type);
                case ConstantExpression constantExpression:
                    return constantExpression;
                case IndexExpression indexExpression:
                    return Expression.MakeIndex(ReplaceParameters(parameterTranslation, indexExpression.Object), indexExpression.Indexer, indexExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument)));
                case MemberExpression memberExpression:
                    return Expression.MakeMemberAccess(ReplaceParameters(parameterTranslation, memberExpression.Expression), memberExpression.Member);
                case MethodCallExpression methodCallExpression:
                    return methodCallExpression.Object == null ? Expression.Call(methodCallExpression.Method, methodCallExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument))) : Expression.Call(ReplaceParameters(parameterTranslation, methodCallExpression.Object), methodCallExpression.Method, methodCallExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument)));
                case NewExpression newExpression:
                    return Expression.New(newExpression.Constructor, newExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument)), newExpression.Members);
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

        public ActiveExpression(Type type, ExpressionType nodeType)
        {
            Type = type;
            NodeType = nodeType;
        }

        Exception fault;
        object val;

        public Exception Fault
        {
            get => fault;
            protected set
            {
                if (value != null)
                    Value = null;
                SetBackedProperty(ref fault, in value);
            }
        }

        public ExpressionType NodeType { get; }

        public Type Type { get; }

        public object Value
        {
            get => val;
            protected set
            {
                if (value != null)
                    Fault = null;
                SetBackedProperty(ref val, in value);
            }
        }
    }

    /// <summary>
    /// Represents an active evaluation of a lambda expression.
    /// <see cref="INotifyPropertyChanged"/>, <see cref="INotifyCollectionChanged"/>, and <see cref="INotifyDictionaryChanged"/> events raised by any value within the lambda expression will cause all dependent portions to be re-evaluated.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the lambda expression upon which this active expression is based</typeparam>
    public class ActiveExpression<TResult> : OverridableSyncDisposablePropertyChangeNotifier, IEquatable<ActiveExpression<TResult>>
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(string expressionString, EquatableList<object> arguments), ActiveExpression<TResult>> instances = new Dictionary<(string expressionString, EquatableList<object> arguments), ActiveExpression<TResult>>();

        internal static ActiveExpression<TResult> Create(LambdaExpression expression, params object[] args)
        {
            var expressionString = expression.ToString();
            var arguments = new EquatableList<object>(args);
            var key = (expressionString, arguments);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeExpression))
                {
                    activeExpression = new ActiveExpression<TResult>(expressionString, arguments, ActiveExpression.Create(ActiveExpression.ReplaceParameters(expression, args)));
                    instances.Add(key, activeExpression);
                }
                ++activeExpression.disposalCount;
                return activeExpression;
            }
        }

        public static bool operator ==(ActiveExpression<TResult> a, ActiveExpression<TResult> b) => a?.Equals(b) ?? b == null;

        public static bool operator !=(ActiveExpression<TResult> a, ActiveExpression<TResult> b) => !(a == b);

        protected ActiveExpression(string expressionString, EquatableList<object> arguments, ActiveExpression expression)
        {
            this.expressionString = expressionString;
            this.arguments = arguments;
            this.expression = expression;
            fault = this.expression.Fault;
            val = (TResult)this.expression.Value;
            this.expression.PropertyChanged += ExpressionPropertyChanged;
        }

        readonly EquatableList<object> arguments;
        int disposalCount;
        readonly ActiveExpression expression;
        readonly string expressionString;
        Exception fault;
        TResult val;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                expression.PropertyChanged -= ExpressionPropertyChanged;
                expression.Dispose();
                instances.Remove((expressionString, arguments));
                return true;
            }
        }

        public override bool Equals(object obj) => Equals(obj as ActiveExpression<TResult>);

        public bool Equals(ActiveExpression<TResult> other) => other?.arguments == arguments && other?.expression == expression;

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = expression.Fault;
            else if (e.PropertyName == nameof(Value))
                Value = expression.Value is TResult typedValue ? typedValue : default;
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveExpression<TResult>), arguments, expression);

        /// <summary>
        /// Gets the arguments that were passed to the lambda expression
        /// </summary>
        public IReadOnlyList<object> Arguments => arguments;

        /// <summary>
        /// Gets the exception that was thrown while evaluating the lambda expression; <c>null</c> if there was no such exception
        /// </summary>
        public Exception Fault
        {
            get => fault;
            private set => SetBackedProperty(ref fault, in value);
        }

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
    /// Represents an active evaluation of a strongly-typed lambda expression with a single argument.
    /// <see cref="INotifyPropertyChanged"/>, <see cref="INotifyCollectionChanged"/>, and <see cref="INotifyDictionaryChanged"/> events raised by any value within the lambda expression will cause all dependent portions to be re-evaluated.
    /// </summary>
    /// <typeparam name="TArg">The type of the argument passed to the lambda expression</typeparam>
    /// <typeparam name="TResult">The type of the value returned by the expression upon which this active expression is based</typeparam>
    public class ActiveExpression<TArg, TResult> : OverridableSyncDisposablePropertyChangeNotifier, IEquatable<ActiveExpression<TArg, TResult>>
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(string expressionString, TArg arg), ActiveExpression<TArg, TResult>> instances = new Dictionary<(string expressionString, TArg arg), ActiveExpression<TArg, TResult>>();

        internal static ActiveExpression<TArg, TResult> Create(LambdaExpression expression, TArg arg)
        {
            var expressionString = expression.ToString();
            var key = (expressionString, arg);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeExpression))
                {
                    activeExpression = new ActiveExpression<TArg, TResult>(expressionString, arg, ActiveExpression.Create(ActiveExpression.ReplaceParameters(expression, arg)));
                    instances.Add(key, activeExpression);
                }
                ++activeExpression.disposalCount;
                return activeExpression;
            }
        }

        public static bool operator ==(ActiveExpression<TArg, TResult> a, ActiveExpression<TArg, TResult> b) => a?.Equals(b) ?? b == null;

        public static bool operator !=(ActiveExpression<TArg, TResult> a, ActiveExpression<TArg, TResult> b) => !(a == b);

        protected ActiveExpression(string expressionString, TArg arg, ActiveExpression expression)
        {
            this.expressionString = expressionString;
            Arg = arg;
            this.expression = expression;
            fault = this.expression.Fault;
            val = (TResult)this.expression.Value;
            this.expression.PropertyChanged += ExpressionPropertyChanged;
        }

        int disposalCount;
        readonly ActiveExpression expression;
        readonly string expressionString;
        Exception fault;
        TResult val;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                expression.PropertyChanged -= ExpressionPropertyChanged;
                expression.Dispose();
                instances.Remove((expressionString, Arg));
                return true;
            }
        }

        public override bool Equals(object obj) => Equals(obj as ActiveExpression<TArg, TResult>);

        public bool Equals(ActiveExpression<TArg, TResult> other) => other != null && other.expression == expression && EqualityComparer<TArg>.Default.Equals(other.Arg, Arg);

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = expression.Fault;
            else if (e.PropertyName == nameof(Value))
                Value = expression.Value is TResult typedValue ? typedValue : default;
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveExpression<TArg, TResult>), expression, Arg);

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
        /// Gets the result of evaluating the lambda expression
        /// </summary>
        public TResult Value
        {
            get => val;
            private set => SetBackedProperty(ref val, in value);
        }
    }

    /// <summary>
    /// Represents an active evaluation of a strongly-typed lambda expression with two arguments.
    /// <see cref="INotifyPropertyChanged"/>, <see cref="INotifyCollectionChanged"/>, and <see cref="INotifyDictionaryChanged"/> events raised by any value within the lambda expression will cause all dependent portions to be re-evaluated.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument passed to the lambda expression</typeparam>
    /// <typeparam name="TArg2">The type of the second argument passed to the lambda expression</typeparam>
    /// <typeparam name="TResult">The type of the value returned by the expression upon which this active expression is based</typeparam>
    public class ActiveExpression<TArg1, TArg2, TResult> : OverridableSyncDisposablePropertyChangeNotifier, IEquatable<ActiveExpression<TArg1, TArg2, TResult>>
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(string expressionString, TArg1 arg1, TArg2 arg2), ActiveExpression<TArg1, TArg2, TResult>> instances = new Dictionary<(string expressionString, TArg1 arg1, TArg2 arg2), ActiveExpression<TArg1, TArg2, TResult>>();

        internal static ActiveExpression<TArg1, TArg2, TResult> Create(LambdaExpression expression, TArg1 arg1, TArg2 arg2)
        {
            var expressionString = expression.ToString();
            var key = (expressionString, arg1, arg2);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeExpression))
                {
                    activeExpression = new ActiveExpression<TArg1, TArg2, TResult>(expressionString, arg1, arg2, ActiveExpression.Create(ActiveExpression.ReplaceParameters(expression, arg1, arg2)));
                    instances.Add(key, activeExpression);
                }
                ++activeExpression.disposalCount;
                return activeExpression;
            }
        }

        public static bool operator ==(ActiveExpression<TArg1, TArg2, TResult> a, ActiveExpression<TArg1, TArg2, TResult> b) => a?.Equals(b) ?? b == null;

        public static bool operator !=(ActiveExpression<TArg1, TArg2, TResult> a, ActiveExpression<TArg1, TArg2, TResult> b) => !(a == b);

        protected ActiveExpression(string expressionString, TArg1 arg1, TArg2 arg2, ActiveExpression expression)
        {
            this.expressionString = expressionString;
            Arg1 = arg1;
            Arg2 = arg2;
            this.expression = expression;
            fault = this.expression.Fault;
            val = (TResult)this.expression.Value;
            this.expression.PropertyChanged += ExpressionPropertyChanged;
        }

        int disposalCount;
        readonly ActiveExpression expression;
        readonly string expressionString;
        Exception fault;
        TResult val;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                expression.PropertyChanged -= ExpressionPropertyChanged;
                expression.Dispose();
                instances.Remove((expressionString, Arg1, Arg2));
                return true;
            }
        }

        public override bool Equals(object obj) => Equals(obj as ActiveExpression<TArg1, TArg2, TResult>);

        public bool Equals(ActiveExpression<TArg1, TArg2, TResult> other) => other != null && other.expression == expression && EqualityComparer<TArg1>.Default.Equals(other.Arg1, Arg1) && EqualityComparer<TArg2>.Default.Equals(other.Arg2, Arg2);

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = expression.Fault;
            else if (e.PropertyName == nameof(Value))
                Value = expression.Value is TResult typedValue ? typedValue : default;
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveExpression<TArg1, TArg2, TResult>), expression, Arg1, Arg2);

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
        /// Gets the result of evaluating the lambda expression
        /// </summary>
        public TResult Value
        {
            get => val;
            private set => SetBackedProperty(ref val, in value);
        }
    }

    /// <summary>
    /// Represents an active evaluation of a strongly-typed lambda expression with two arguments.
    /// <see cref="INotifyPropertyChanged"/>, <see cref="INotifyCollectionChanged"/>, and <see cref="INotifyDictionaryChanged"/> events raised by any value within the lambda expression will cause all dependent portions to be re-evaluated.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument passed to the lambda expression</typeparam>
    /// <typeparam name="TArg2">The type of the second argument passed to the lambda expression</typeparam>
    /// <typeparam name="TArg3">The type of the third argument passed to the lambda expression</typeparam>
    /// <typeparam name="TResult">The type of the value returned by the expression upon which this active expression is based</typeparam>
    public class ActiveExpression<TArg1, TArg2, TArg3, TResult> : OverridableSyncDisposablePropertyChangeNotifier, IEquatable<ActiveExpression<TArg1, TArg2, TArg3, TResult>>
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(string expressionString, TArg1 arg1, TArg2 arg2, TArg3 arg3), ActiveExpression<TArg1, TArg2, TArg3, TResult>> instances = new Dictionary<(string expressionString, TArg1 arg1, TArg2 arg2, TArg3 arg3), ActiveExpression<TArg1, TArg2, TArg3, TResult>>();

        internal static ActiveExpression<TArg1, TArg2, TArg3, TResult> Create(LambdaExpression expression, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            var expressionString = expression.ToString();
            var key = (expressionString, arg1, arg2, arg3);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeExpression))
                {
                    activeExpression = new ActiveExpression<TArg1, TArg2, TArg3, TResult>(expressionString, arg1, arg2, arg3, ActiveExpression.Create(ActiveExpression.ReplaceParameters(expression, arg1, arg2, arg3)));
                    instances.Add(key, activeExpression);
                }
                ++activeExpression.disposalCount;
                return activeExpression;
            }
        }

        public static bool operator ==(ActiveExpression<TArg1, TArg2, TArg3, TResult> a, ActiveExpression<TArg1, TArg2, TArg3, TResult> b) => a?.Equals(b) ?? b == null;

        public static bool operator !=(ActiveExpression<TArg1, TArg2, TArg3, TResult> a, ActiveExpression<TArg1, TArg2, TArg3, TResult> b) => !(a == b);

        protected ActiveExpression(string expressionString, TArg1 arg1, TArg2 arg2, TArg3 arg3, ActiveExpression expression)
        {
            this.expressionString = expressionString;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            this.expression = expression;
            fault = this.expression.Fault;
            val = (TResult)this.expression.Value;
            this.expression.PropertyChanged += ExpressionPropertyChanged;
        }

        int disposalCount;
        readonly ActiveExpression expression;
        readonly string expressionString;
        Exception fault;
        TResult val;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                expression.PropertyChanged -= ExpressionPropertyChanged;
                expression.Dispose();
                instances.Remove((expressionString, Arg1, Arg2, Arg3));
                return true;
            }
        }

        public override bool Equals(object obj) => Equals(obj as ActiveExpression<TArg1, TArg2, TArg3, TResult>);

        public bool Equals(ActiveExpression<TArg1, TArg2, TArg3, TResult> other) => other != null && other.expression == expression && EqualityComparer<TArg1>.Default.Equals(other.Arg1, Arg1) && EqualityComparer<TArg2>.Default.Equals(other.Arg2, Arg2) && EqualityComparer<TArg3>.Default.Equals(other.Arg3, Arg3);

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = expression.Fault;
            else if (e.PropertyName == nameof(Value))
                Value = expression.Value is TResult typedValue ? typedValue : default;
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveExpression<TArg1, TArg2, TArg3, TResult>), expression, Arg1, Arg2, Arg3);

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
        /// Gets the result of evaluating the lambda expression
        /// </summary>
        public TResult Value
        {
            get => val;
            private set => SetBackedProperty(ref val, in value);
        }
    }
}
