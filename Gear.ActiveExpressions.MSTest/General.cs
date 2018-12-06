using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gear.ActiveExpressions.MSTest
{
    [TestClass]
    public class General
    {
        [TestMethod]
        public void OperatorExpressionSyntax()
        {
            Assert.AreEqual("(1 + 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Add, typeof(int), 1, 2));
            Assert.AreEqual("checked(1 + 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.AddChecked, typeof(int), 1, 2));
            Assert.AreEqual("(1 & 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.And, typeof(int), 1, 2));
            Assert.AreEqual("((System.Object)1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Convert, typeof(object), 1));
            Assert.AreEqual("checked((System.Int32)1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.ConvertChecked, typeof(int), 1L));
            Assert.AreEqual("(1 - 1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Decrement, typeof(int), 1));
            Assert.AreEqual("(1 / 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Divide, typeof(int), 1, 2));
            Assert.AreEqual("(1 == 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Equal, typeof(int), 1, 2));
            Assert.AreEqual("(1 ^ 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.ExclusiveOr, typeof(int), 1, 2));
            Assert.AreEqual("(1 > 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.GreaterThan, typeof(int), 1, 2));
            Assert.AreEqual("(1 >= 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.GreaterThanOrEqual, typeof(int), 1, 2));
            Assert.AreEqual("(1 + 1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Increment, typeof(int), 1));
            Assert.AreEqual("(1 << 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.LeftShift, typeof(int), 1, 2));
            Assert.AreEqual("(1 < 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.LessThan, typeof(int), 1, 2));
            Assert.AreEqual("(1 <= 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.LessThanOrEqual, typeof(int), 1, 2));
            Assert.AreEqual("(1 % 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Modulo, typeof(int), 1, 2));
            Assert.AreEqual("(1 * 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Multiply, typeof(int), 1, 2));
            Assert.AreEqual("checked(1 * 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.MultiplyChecked, typeof(int), 1, 2));
            Assert.AreEqual("(-1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Negate, typeof(int), 1));
            Assert.AreEqual("checked(-1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.NegateChecked, typeof(int), 1));
            Assert.AreEqual("(!True)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Not, typeof(bool), true));
            Assert.AreEqual("(~1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Not, typeof(int), 1));
            Assert.AreEqual("(1 != 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.NotEqual, typeof(int), 1, 2));
            Assert.AreEqual("(~1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.OnesComplement, typeof(int), 1));
            Assert.AreEqual("(1 | 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Or, typeof(int), 1, 2));
            Assert.AreEqual("Math.Pow(1, 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Power, typeof(int), 1, 2));
            Assert.AreEqual("(1 >> 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.RightShift, typeof(int), 1, 2));
            Assert.AreEqual("(1 - 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.Subtract, typeof(int), 1, 2));
            Assert.AreEqual("checked(1 - 2)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.SubtractChecked, typeof(int), 1, 2));
            Assert.AreEqual("(+1)", ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.UnaryPlus, typeof(int), 1));
            var outOfRangeThrown = false;
            try
            {
                ActiveExpression.GetOperatorExpressionSyntax(ExpressionType.AddAssign, typeof(int), 1, 2);
            }
            catch (ArgumentOutOfRangeException)
            {
                outOfRangeThrown = true;
            }
            Assert.IsTrue(outOfRangeThrown);
        }

        [TestMethod]
        public void UnsupportedExpressionType()
        {
            var expr = Expression.Lambda<Func<int>>(Expression.Block(Expression.Constant(3)));
            Assert.AreEqual(3, expr.Compile()());
            var notSupportedThrown = false;
            try
            {
                using (var ae = ActiveExpression.Create(expr))
                {
                }
            }
            catch (NotSupportedException)
            {
                notSupportedThrown = true;
            }
            Assert.IsTrue(notSupportedThrown);
        }
    }
}
