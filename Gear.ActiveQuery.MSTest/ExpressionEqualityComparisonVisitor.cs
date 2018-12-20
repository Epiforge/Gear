using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gear.ActiveQuery.MSTest
{
    [TestClass]
    public class ExpressionEqualityComparisonVisitor
    {
        #region Binary Methods

        public static int AddNumbers(int a, int b) => a + b;

        #endregion Binary Methods

        [TestMethod]
        public void Binary()
        {
            var visitor = new ActiveQuery.ExpressionEqualityComparisonVisitor((Expression<Func<int, int, int>>)((x, y) => x + y));
            visitor.Visit((Expression<Func<int, int, int>>)((a, b) => a + b));
            Assert.IsTrue(visitor.IsLastVisitedEqual);
            visitor.Visit((Expression<Func<int, int, int>>)((a, b) => a - b));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
            visitor.Visit((Expression<Func<int, int, int>>)((a, b) => b + a));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
            visitor.Visit((Expression<Func<int, int, int>>)((a, b) => a));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
            visitor.Visit((Expression<Func<int, int, int>>)((a, b) => a + b + a));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
            visitor = new ActiveQuery.ExpressionEqualityComparisonVisitor((Expression<Func<int, int, int>>)((x, y) => x + y));
            var aParam = Expression.Parameter(typeof(int));
            var bParam = Expression.Parameter(typeof(int));
            visitor.Visit(Expression.Lambda(Expression.Add(aParam, bParam), aParam, bParam));
            Assert.IsTrue(visitor.IsLastVisitedEqual);
            visitor.Visit(Expression.Lambda(Expression.Add(aParam, bParam, typeof(ExpressionEqualityComparisonVisitor).GetMethod(nameof(AddNumbers), BindingFlags.Public | BindingFlags.Static)), aParam, bParam));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
        }

        [TestMethod]
        public void Coalesce()
        {
            var visitor = new ActiveQuery.ExpressionEqualityComparisonVisitor((Expression<Func<int>>)(() => (int?)null ?? 3));
            visitor.Visit((Expression<Func<int>>)(() => (int?)null ?? 3));
            Assert.IsTrue(visitor.IsLastVisitedEqual);
            visitor.Visit((Expression<Func<int>>)(() => (int?)null ?? 4));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
        }

        #region Implicit Conversion Classes

        class A
        {
            public static implicit operator B(A a) => null;

            public static implicit operator C(A a) => null;
        }

        class B
        {
        }

        class C
        {
        }

        #endregion Implicit Conversion Classes

        [TestMethod]
        public void CoalesceImplicitConversion()
        {
            var visitor = new ActiveQuery.ExpressionEqualityComparisonVisitor((Expression<Func<B>>)(() => new A() ?? new B()));
            visitor.Visit((Expression<Func<B>>)(() => new A() ?? new B()));
            Assert.IsTrue(visitor.IsLastVisitedEqual);
            visitor.Visit((Expression<Func<C>>)(() => new A() ?? new C()));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
        }

        [TestMethod]
        public void Conditional()
        {
            var visitor = new ActiveQuery.ExpressionEqualityComparisonVisitor((Expression<Func<int, int, int>>)((x, y) => x > 3 ? x : y));
            visitor.Visit((Expression<Func<int, int, int>>)((a, b) => a > 3 ? a : b));
            Assert.IsTrue(visitor.IsLastVisitedEqual);
            visitor.Visit((Expression<Func<int, int, int>>)((a, b) => a > 3 ? b : a));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
            visitor.Visit((Expression<Func<int, int, int>>)((a, b) => a > 2 ? a : b));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
            visitor.Visit((Expression<Func<int, int, int>>)((a, b) => a < 3 ? a : b));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
        }

        [TestMethod]
        public void Index()
        {
            var stringIndexer = typeof(Dictionary<string, string>).GetProperties().Single(p => p.GetIndexParameters().Any());
            var key = Expression.Constant("key");
            var xParam = Expression.Parameter(typeof(Dictionary<string, string>));
            var visitor = new ActiveQuery.ExpressionEqualityComparisonVisitor(Expression.Lambda(Expression.MakeIndex(xParam, stringIndexer, new Expression[] { key }), xParam));
            var aParam = Expression.Parameter(typeof(Dictionary<string, string>));
            visitor.Visit(Expression.Lambda(Expression.MakeIndex(aParam, stringIndexer, new Expression[] { key }), aParam));
            Assert.IsTrue(visitor.IsLastVisitedEqual);
            var bread = Expression.Constant("bread");
            visitor.Visit(Expression.Lambda(Expression.MakeIndex(aParam, stringIndexer, new Expression[] { bread }), aParam));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
            var intIndexer = typeof(Dictionary<int, string>).GetProperties().Single(p => p.GetIndexParameters().Any());
            var two = Expression.Constant(2);
            var dict = Expression.Constant(new Dictionary<int, string>());
            visitor.Visit(Expression.Lambda(Expression.MakeIndex(dict, intIndexer, new Expression[] { two }), aParam));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
        }

        #region Member Class

        class Members
        {
            public int Member1 = 0;
            public int Member2 = 0;
        }

        #endregion Member Class

        [TestMethod]
        public void Member()
        {
            var visitor = new ActiveQuery.ExpressionEqualityComparisonVisitor((Expression<Func<Members, int>>)(x => x.Member1));
            visitor.Visit((Expression<Func<Members, int>>)(a => a.Member1));
            Assert.IsTrue(visitor.IsLastVisitedEqual);
            visitor.Visit((Expression<Func<Members, int>>)(a => a.Member2));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
        }

        #region Method Call Class

        class Methods
        {
            public int Method1() => 0;
            public int Method2() => 0;
        }

        #endregion Method Call Class

        [TestMethod]
        public void MethodCall()
        {
            var visitor = new ActiveQuery.ExpressionEqualityComparisonVisitor((Expression<Func<Methods, int>>)(x => x.Method1()));
            visitor.Visit((Expression<Func<Methods, int>>)(a => a.Method1()));
            Assert.IsTrue(visitor.IsLastVisitedEqual);
            visitor.Visit((Expression<Func<Methods, int>>)(a => a.Method2()));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
        }

        [TestMethod]
        public void New()
        {
            var visitor = new ActiveQuery.ExpressionEqualityComparisonVisitor((Expression<Func<string>>)(() => new string('a', 4)));
            visitor.Visit((Expression<Func<string>>)(() => new string('a', 4)));
            Assert.IsTrue(visitor.IsLastVisitedEqual);
            visitor.Visit((Expression<Func<string>>)(() => new string(new char[] { 'a', 'a', 'a', 'a' })));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
        }

        [TestMethod]
        public void Nulls()
        {
            var visitor = new ActiveQuery.ExpressionEqualityComparisonVisitor(null);
            visitor.Visit((Expression)null);
            Assert.IsTrue(visitor.IsLastVisitedEqual);
            visitor.Visit((Expression<Func<int>>)(() => 3));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
            visitor = new ActiveQuery.ExpressionEqualityComparisonVisitor((Expression<Func<int>>)(() => 3));
            visitor.Visit((Expression)null);
            Assert.IsFalse(visitor.IsLastVisitedEqual);
        }

        #region Unary Methods

        public static int NegateNumber(int a) => -a;

        #endregion Unary Methods

        [TestMethod]
        public void Unary()
        {
            var visitor = new ActiveQuery.ExpressionEqualityComparisonVisitor((Expression<Func<int, int>>)(x => -x));
            visitor.Visit((Expression<Func<int, int>>)(a => -a));
            Assert.IsTrue(visitor.IsLastVisitedEqual);
            visitor.Visit((Expression<Func<int, int>>)(a => +a));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
            var aParam = Expression.Parameter(typeof(int));
            visitor.Visit(Expression.Lambda(Expression.Negate(aParam), aParam));
            Assert.IsTrue(visitor.IsLastVisitedEqual);
            visitor.Visit(Expression.Lambda(Expression.Negate(aParam, typeof(ExpressionEqualityComparisonVisitor).GetMethod(nameof(NegateNumber), BindingFlags.Public | BindingFlags.Static)), aParam));
            Assert.IsFalse(visitor.IsLastVisitedEqual);
        }
    }
}
