using Gear.Components;
using System;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveOrElseExpression : ActiveBinaryExpression, IEquatable<ActiveOrElseExpression>
    {
        public static bool operator ==(ActiveOrElseExpression a, ActiveOrElseExpression b) => a?.Equals(b) ?? b is null;

        public static bool operator !=(ActiveOrElseExpression a, ActiveOrElseExpression b) => !(a == b);

        public ActiveOrElseExpression(ActiveExpression left, ActiveExpression right, bool deferEvaluation) : base(typeof(bool), ExpressionType.OrElse, left, right, null, deferEvaluation, false)
        {
        }

        public override bool Equals(object obj) => Equals(obj as ActiveOrElseExpression);

        public bool Equals(ActiveOrElseExpression other) => other?.left == left && other?.right == right;

        protected override void Evaluate()
        {
            var leftFault = left.Fault;
            if (leftFault != null)
                Fault = leftFault;
            else if ((bool)left.Value)
                Value = true;
            else
            {
                var rightFault = right.Fault;
                if (rightFault != null)
                    Fault = rightFault;
                else
                    Value = (bool)right.Value;
            }
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveOrElseExpression), left, right);
    }
}
