using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveAndAlsoExpression : ActiveBinaryExpression
    {
        public ActiveAndAlsoExpression(ActiveExpression left, ActiveExpression right) : base(typeof(bool), ExpressionType.AndAlso, left, right, false)
        {
        }

        protected override void Evaluate()
        {
            var leftFault = left.Fault;
            var rightFault = right.Fault;
            if (leftFault != null)
                Fault = leftFault;
            else if (!(bool)left.Value)
                Value = false;
            else if (rightFault != null)
                Fault = rightFault;
            else
                Value = (bool)right.Value;
        }
    }
}
