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
        static readonly Dictionary<(ActiveExpression expression, MemberInfo member), ActiveMemberExpression> instances = new Dictionary<(ActiveExpression expression, MemberInfo member), ActiveMemberExpression>();

        public static ActiveMemberExpression Create(MemberExpression memberExpression)
        {
            var expression = Create(memberExpression.Expression);
            var member = memberExpression.Member;
            var key = (expression, member);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeMemberExpression))
                {
                    activeMemberExpression = new ActiveMemberExpression(memberExpression.Type, expression, member);
                    instances.Add(key, activeMemberExpression);
                }
                ++activeMemberExpression.disposalCount;
                return activeMemberExpression;
            }
        }

        ActiveMemberExpression(Type type, ActiveExpression expression, MemberInfo member) : base(type, ExpressionType.MemberAccess)
        {
            this.expression = expression;
            expressionValue = this.expression.Value;
            this.expression.PropertyChanged += ExpressionPropertyChanged;
            this.member = member;
            switch (this.member)
            {
                case FieldInfo field:
                    this.field = field;
                    break;
                case PropertyInfo property:
                    fastGetter = GetFastMethodInfo(property.GetMethod);
                    SubscribeToExpressionValueNotifications();
                    break;
                case null:
                    throw new ArgumentNullException(nameof(member));
                default:
                    throw new NotSupportedException($"Cannot get value using {this.member.GetType().Name} for \"{member.DeclaringType.FullName}.{member.Name}\"");
            }
            Evaluate();
        }

        int disposalCount;
        readonly ActiveExpression expression;
        object expressionValue;
        readonly FastMethodInfo fastGetter;
        readonly FieldInfo field;
        readonly MemberInfo member;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                if (fastGetter != null)
                    UnsubscribeFromExpressionValueNotifications();
                expression.PropertyChanged -= ExpressionPropertyChanged;
                expression.Dispose();
                instances.Remove((expression, member));
                return true;
            }
        }

        void Evaluate()
        {
            try
            {
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
