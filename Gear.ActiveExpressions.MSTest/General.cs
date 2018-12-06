using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gear.ActiveExpressions.MSTest
{
    [TestClass]
    public class General
    {
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
