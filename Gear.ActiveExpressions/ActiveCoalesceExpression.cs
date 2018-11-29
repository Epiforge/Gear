using Gear.Components;
using System;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveCoalesceExpression : ActiveBinaryExpression
    {
        public static bool operator ==(ActiveCoalesceExpression a, ActiveCoalesceExpression b) => a?.left == b?.left && a?.right == b?.right && a?.options == b?.options;

        public static bool operator !=(ActiveCoalesceExpression a, ActiveCoalesceExpression b) => a?.left != b?.left || a?.right != b?.right || a?.options != b?.options;

        public ActiveCoalesceExpression(Type type, ActiveExpression left, ActiveExpression right, LambdaExpression conversion, bool deferEvaluation) : base(type, ExpressionType.Coalesce, left, right, null, deferEvaluation, false)
        {
            if (conversion != null)
                throw new NotSupportedException("Coalesce conversions are not yet supported");
        }

        public override bool Equals(object obj) => obj is ActiveCoalesceExpression other && left.Equals(other.left) && right.Equals(other.right) && (options?.Equals(other.options) ?? other.options is null);

        protected override void Evaluate()
        {
            var leftFault = left.Fault;
            if (leftFault != null)
                Fault = leftFault;
            else
            {
                var leftValue = left.Value;
                if (leftValue != null)
                    Value = leftValue;
                else
                {
                    var rightFault = right.Fault;
                    if (rightFault != null)
                        Fault = rightFault;
                    else
                        Value = right.Value;
                }
            }
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveCoalesceExpression), left, right);

        public override string ToString() => $"({left} ?? {right}) {ToStringSuffix}";
    }
}
