using Gear.Components;
using System;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveAndAlsoExpression : ActiveBinaryExpression, IEquatable<ActiveAndAlsoExpression>
    {
        public static bool operator ==(ActiveAndAlsoExpression a, ActiveAndAlsoExpression b) => a?.Equals(b) ?? b is null;

        public static bool operator !=(ActiveAndAlsoExpression a, ActiveAndAlsoExpression b) => !(a == b);

        public ActiveAndAlsoExpression(ActiveExpression left, ActiveExpression right, bool deferEvaluation) : base(typeof(bool), ExpressionType.AndAlso, left, right, null, deferEvaluation, false)
        {
        }

        public override bool Equals(object obj) => Equals(obj as ActiveAndAlsoExpression);

        public bool Equals(ActiveAndAlsoExpression other) => other?.left == left && other?.right == right;

        protected override void Evaluate()
        {
            var leftFault = left.Fault;
            if (leftFault != null)
                Fault = leftFault;
            else if (!(bool)left.Value)
                Value = false;
            else
            {
                var rightFault = right.Fault;
                if (rightFault != null)
                    Fault = rightFault;
                else
                    Value = (bool)right.Value;
            }
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveAndAlsoExpression), left, right);
    }
}
