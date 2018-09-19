using Gear.Components;
using System;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveCoalesceExpression : ActiveBinaryExpression, IEquatable<ActiveCoalesceExpression>
    {
        public static bool operator ==(ActiveCoalesceExpression a, ActiveCoalesceExpression b) => a?.Equals(b) ?? b == null;

        public static bool operator !=(ActiveCoalesceExpression a, ActiveCoalesceExpression b) => !(a == b);

        public ActiveCoalesceExpression(Type type, ActiveExpression left, ActiveExpression right, LambdaExpression conversion) : base(type, ExpressionType.Coalesce, left, right, null, false)
        {
            if (conversion != null)
                throw new NotSupportedException("Coalesce conversions are not yet supported");
        }

        public override bool Equals(object obj) => Equals(obj as ActiveCoalesceExpression);

        public bool Equals(ActiveCoalesceExpression other) => other?.left == left && other?.right == right;

        protected override void Evaluate()
        {
            var leftFault = left.Fault;
            var leftValue = left.Value;
            var rightFault = right.Fault;
            var rightValue = right.Value;
            if (leftFault != null)
                Fault = leftFault;
            else if (leftValue != null)
                Value = leftValue;
            else if (rightFault != null)
                Fault = rightFault;
            else
                Value = right.Value;
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveCoalesceExpression), left, right);
    }
}
