using Gear.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions
{
    class ActiveMemberExpression : ActiveExpression
    {
        static readonly object[] emptyArray = new object[0];
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ActiveExpression expression, MemberInfo member, ActiveExpressionOptions options), ActiveMemberExpression> instanceInstances = new Dictionary<(ActiveExpression expression, MemberInfo member, ActiveExpressionOptions options), ActiveMemberExpression>();
        static readonly Dictionary<(MemberInfo member, ActiveExpressionOptions options), ActiveMemberExpression> staticInstances = new Dictionary<(MemberInfo member, ActiveExpressionOptions options), ActiveMemberExpression>();

        public static ActiveMemberExpression Create(MemberExpression memberExpression, ActiveExpressionOptions options, bool deferEvaluation)
        {
            if (memberExpression.Expression == null)
            {
                var member = memberExpression.Member;
                var key = (member, options);
                lock (instanceManagementLock)
                {
                    if (!staticInstances.TryGetValue(key, out var activeMemberExpression))
                    {
                        activeMemberExpression = new ActiveMemberExpression(memberExpression.Type, member, options, deferEvaluation);
                        staticInstances.Add(key, activeMemberExpression);
                    }
                    ++activeMemberExpression.disposalCount;
                    return activeMemberExpression;
                }
            }
            else
            {
                var expression = Create(memberExpression.Expression, options, deferEvaluation);
                var member = memberExpression.Member;
                var key = (expression, member, options);
                lock (instanceManagementLock)
                {
                    if (!instanceInstances.TryGetValue(key, out var activeMemberExpression))
                    {
                        activeMemberExpression = new ActiveMemberExpression(memberExpression.Type, expression, member, options, deferEvaluation);
                        instanceInstances.Add(key, activeMemberExpression);
                    }
                    ++activeMemberExpression.disposalCount;
                    return activeMemberExpression;
                }
            }
        }

        public static bool operator ==(ActiveMemberExpression a, ActiveMemberExpression b) => a?.expression == b?.expression && a?.member == b?.member && a?.options == b?.options;

        public static bool operator !=(ActiveMemberExpression a, ActiveMemberExpression b) => a?.expression != b?.expression || a?.member != b?.member || a?.options != b?.options;

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

        ActiveMemberExpression(Type type, MemberInfo member, ActiveExpressionOptions options, bool deferEvaluation) : base(type, ExpressionType.MemberAccess, options, deferEvaluation)
        {
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
                    if (expression != null)
                    {
                        expression.PropertyChanged -= ExpressionPropertyChanged;
                        expression.Dispose();
                        instanceInstances.Remove((expression, member, options));
                    }
                    else
                        staticInstances.Remove((member, options));
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

        public override bool Equals(object obj) => obj is ActiveMemberExpression other && (expression?.Equals(other.expression) ?? other.expression is null) && member.Equals(other.member) && (options?.Equals(other.options) ?? other.options is null);

        protected override void Evaluate()
        {
            try
            {
                DisposeValueIfNecessary();
                var expressionFault = expression?.Fault;
                if (expressionFault != null)
                    Fault = expressionFault;
                else
                {
                    if (fastGetter != null)
                    {
                        var newExpressionValue = expression?.Value;
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

        public override string ToString() => $"{expression?.ToString() ?? member.DeclaringType.FullName}.{member.Name} {ToStringSuffix}";

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
