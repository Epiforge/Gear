using Gear.Components;
using System;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveAndAlsoExpression : ActiveBinaryExpression
    {
        public static bool operator ==(ActiveAndAlsoExpression a, ActiveAndAlsoExpression b) => a?.left == b?.left && a?.right == b?.right && a?.options == b?.options;

        public static bool operator !=(ActiveAndAlsoExpression a, ActiveAndAlsoExpression b) => a?.left != b?.left || a?.right != b?.right || a?.options != b?.options;

        public ActiveAndAlsoExpression(ActiveExpression left, ActiveExpression right, bool deferEvaluation) : base(typeof(bool), ExpressionType.AndAlso, left, right, null, deferEvaluation, false)
        {
        }

        public override bool Equals(object obj) => obj is ActiveAndAlsoExpression other && left.Equals(other.left) && right.Equals(other.right) && (options?.Equals(other.options) ?? other.options is null);

        protected override void Evaluate()
        {
            try
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
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveAndAlsoExpression), left, right);

        public override string ToString() => $"({left} && {right}) {ToStringSuffix}";
    }
}
