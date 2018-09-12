using System;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveCoalesceExpression : ActiveBinaryExpression
    {
        public ActiveCoalesceExpression(Type type, ActiveExpression left, ActiveExpression right, LambdaExpression conversion) : base(type, ExpressionType.Coalesce, left, right, false)
        {
            if (conversion != null)
                throw new NotSupportedException("Coalesce conversions are not yet supported");
        }

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
    }
}
