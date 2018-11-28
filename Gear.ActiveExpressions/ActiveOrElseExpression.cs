using Gear.Components;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveOrElseExpression : ActiveBinaryExpression
    {
        public static bool operator ==(ActiveOrElseExpression a, ActiveOrElseExpression b) => a?.left == b?.left && a?.right == b?.right && a?.options == b?.options;

        public static bool operator !=(ActiveOrElseExpression a, ActiveOrElseExpression b) => a?.left != b?.left || a?.right != b?.right || a?.options != b?.options;

        public ActiveOrElseExpression(ActiveExpression left, ActiveExpression right, bool deferEvaluation) : base(typeof(bool), ExpressionType.OrElse, left, right, null, deferEvaluation, false)
        {
        }

        public override bool Equals(object obj) => obj is ActiveOrElseExpression other && left.Equals(other.left) && right.Equals(other.right) && (options?.Equals(other.options) ?? other.options is null);

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
