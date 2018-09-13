using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveConditionalExpression : ActiveExpression
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ActiveExpression test, ActiveExpression ifTrue, ActiveExpression ifFalse), ActiveConditionalExpression> instances = new Dictionary<(ActiveExpression test, ActiveExpression ifTrue, ActiveExpression ifFalse), ActiveConditionalExpression>();

        public static ActiveConditionalExpression Create(ConditionalExpression conditionalExpression)
        {
            var test = Create(conditionalExpression.Test);
            var ifTrue = Create(conditionalExpression.IfTrue);
            var ifFalse = Create(conditionalExpression.IfFalse);
            var key = (test, ifTrue, ifFalse);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeConditionalExpression))
                {
                    activeConditionalExpression = new ActiveConditionalExpression(conditionalExpression.Type, test, ifTrue, ifFalse);
                    instances.Add(key, activeConditionalExpression);
                }
                ++activeConditionalExpression.disposalCount;
                return activeConditionalExpression;
            }
        }

        ActiveConditionalExpression(Type type, ActiveExpression test, ActiveExpression ifTrue, ActiveExpression ifFalse) : base(type, ExpressionType.Conditional)
        {
            this.test = test;
            this.test.PropertyChanged += TestPropertyChanged;
            this.ifTrue = ifTrue;
            this.ifTrue.PropertyChanged += IfTruePropertyChanged;
            this.ifFalse = ifFalse;
            this.ifFalse.PropertyChanged += IfFalsePropertyChanged;
            var testFault = test.Fault;
            if (testFault != null)
                Fault = testFault;
            else if ((bool)test.Value)
            {
                var ifTrueFault = ifTrue.Fault;
                if (ifTrueFault != null)
                    Fault = ifTrueFault;
                else
                    Value = ifTrue.Value;
            }
            else
            {
                var ifFalseFault = ifFalse.Fault;
                if (ifFalseFault != null)
                    Fault = ifFalseFault;
                else
                    Value = ifFalse.Value;
            }
        }

        int disposalCount;
        readonly ActiveExpression ifFalse;
        readonly ActiveExpression ifTrue;
        readonly ActiveExpression test;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                test.PropertyChanged -= TestPropertyChanged;
                test.Dispose();
                ifTrue.PropertyChanged -= IfTruePropertyChanged;
                ifTrue.Dispose();
                ifFalse.PropertyChanged -= IfFalsePropertyChanged;
                ifFalse.Dispose();
                instances.Remove((test, ifTrue, ifFalse));
                return true;
            }
        }

        void IfFalsePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (test.Fault == null && !(bool)test.Value)
            {
                if (e.PropertyName == nameof(Fault))
                    Fault = ifFalse.Fault;
                else if (e.PropertyName == nameof(Value) && ifFalse.Fault == null)
                    Value = ifFalse.Value;
            }
        }

        void IfTruePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (test.Fault == null && (bool)test.Value)
            {
                if (e.PropertyName == nameof(Fault))
                    Fault = ifTrue.Fault;
                else if (e.PropertyName == nameof(Value) && ifTrue.Fault == null)
                    Value = ifTrue.Value;
            }
        }

        void TestPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = test.Fault;
            else if (e.PropertyName == nameof(Value) && test.Fault == null)
            {
                if ((bool)test.Value)
                {
                    var ifTrueFault = ifTrue.Fault;
                    if (ifTrueFault != null)
                        Fault = ifTrueFault;
                    else
                        Value = ifTrue.Value;
                }
                else
                {
                    var ifFalseFault = ifFalse.Fault;
                    if (ifFalseFault != null)
                        Fault = ifFalseFault;
                    else
                        Value = ifFalse.Value;
                }
            }
        }
    }
}
