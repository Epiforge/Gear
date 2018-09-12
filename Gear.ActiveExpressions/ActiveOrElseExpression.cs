using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveOrElseExpression : ActiveBinaryExpression
    {
        public ActiveOrElseExpression(ActiveExpression left, ActiveExpression right) : base(typeof(bool), ExpressionType.OrElse, left, right, false)
        {
        }

        protected override void Evaluate()
        {
            var leftFault = left.Fault;
            var rightFault = right.Fault;
            if (leftFault != null)
                Fault = leftFault;
            else if ((bool)left.Value)
                Value = true;
            else if (rightFault != null)
                Fault = rightFault;
            else
                Value = (bool)right.Value;
        }
    }
}
