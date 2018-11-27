using Gear.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions
{
    class ActiveMemberExpression : ActiveExpression, IEquatable<ActiveMemberExpression>
    {
        static readonly object[] emptyArray = new object[0];
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ActiveExpression expression, MemberInfo member, ActiveExpressionOptions options), ActiveMemberExpression> instances = new Dictionary<(ActiveExpression expression, MemberInfo member, ActiveExpressionOptions options), ActiveMemberExpression>();

        public static ActiveMemberExpression Create(MemberExpression memberExpression, ActiveExpressionOptions options, bool deferEvaluation)
        {
            var expression = Create(memberExpression.Expression, options, deferEvaluation);
            var member = memberExpression.Member;
            var key = (expression, member, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeMemberExpression))
                {
                    activeMemberExpression = new ActiveMemberExpression(memberExpression.Type, expression, member, options, deferEvaluation);
                    instances.Add(key, activeMemberExpression);
                }
                ++activeMemberExpression.disposalCount;
                return activeMemberExpression;
            }
        }

        public static bool operator ==(ActiveMemberExpression a, ActiveMemberExpression b) => a?.Equals(b) ?? b is null;

        public static bool operator !=(ActiveMemberExpression a, ActiveMemberExpression b) => !(a == b);

        ActiveMemberExpression(Type type, ActiveExpression expression, MemberInfo member, ActiveExpressionOptions options, bool deferEvaluation) : base(type, ExpressionType.MemberAccess, options, deferEvaluation)
        {
            this.expression = expression;
            this.expression.PropertyChanged += ExpressionPropertyChanged;
            this.member = member;
            switch (this.member)
            {
                case FieldInfo field:
                    this.field = field;
                    break;
                case PropertyInfo property:
                    this.property = property;
                    getMethod = property.GetMethod;
                    fastGetter = GetFastMethodInfo(getMethod);
                    SubscribeToExpressionValueNotifications();
                    break;
                case null:
                    throw new ArgumentNullException(nameof(member));
                default:
                    throw new NotSupportedException($"Cannot get value using {this.member.GetType().Name} for \"{member.DeclaringType.FullName}.{member.Name}\"");
            }
            EvaluateIfNotDeferred();
        }

        int disposalCount;
        readonly ActiveExpression expression;
        object expressionValue;
        readonly FastMethodInfo fastGetter;
        readonly FieldInfo field;
        readonly MethodInfo getMethod;
        readonly MemberInfo member;
        readonly PropertyInfo property;

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
            {
                if (--disposalCount == 0)
                {
                    if (fastGetter != null)
                        UnsubscribeFromExpressionValueNotifications();
                    expression.PropertyChanged -= ExpressionPropertyChanged;
                    expression.Dispose();
                    instances.Remove((expression, member, options));
                    result = true;
                }
            }
            if (result)
                DisposeValueIfNecessary();
            return result;
        }

        void DisposeValueIfNecessary()
        {
            if (property != null && ApplicableOptions.IsMethodReturnValueDisposed(getMethod) && TryGetUndeferredValue(out var value))
            {
                try
                {
                    if (value is IDisposable disposable)
                        disposable.Dispose();
                    else if (value is IAsyncDisposable asyncDisposable)
                        asyncDisposable.DisposeAsync().Wait();
                }
                catch (Exception ex)
                {
                    throw new Exception("Disposal of property value failed", ex);
                }
            }
        }

        public override bool Equals(object obj) => Equals(obj as ActiveMemberExpression);

        public bool Equals(ActiveMemberExpression other) => other?.expression == expression && other?.member == member && other?.options == options;

        protected override void Evaluate()
        {
            try
            {
                DisposeValueIfNecessary();
                var expressionFault = expression.Fault;
                if (expressionFault != null)
                    Fault = expressionFault;
                else
                {
                    if (fastGetter != null)
                    {
                        var newExpressionValue = expression.Value;
                        if (newExpressionValue != expressionValue)
                        {
                            UnsubscribeFromExpressionValueNotifications();
                            expressionValue = newExpressionValue;
                            SubscribeToExpressionValueNotifications();
                        }
                        Value = fastGetter.Invoke(expressionValue, emptyArray);
                    }
                    else
                        Value = field.GetValue(expressionValue);
                }
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        void ExpressionValuePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == member.Name)
                Evaluate();
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveMemberExpression), expression, member, options);

        void SubscribeToExpressionValueNotifications()
        {
            if (expressionValue is INotifyPropertyChanged propertyChangedNotifier)
                propertyChangedNotifier.PropertyChanged += ExpressionValuePropertyChanged;
        }

        void UnsubscribeFromExpressionValueNotifications()
        {
            if (expressionValue is INotifyPropertyChanged propertyChangedNotifier)
                propertyChangedNotifier.PropertyChanged -= ExpressionValuePropertyChanged;
        }
    }
}
