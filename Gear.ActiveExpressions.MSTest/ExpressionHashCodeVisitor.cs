using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gear.ActiveExpressions.MSTest
{
    [TestClass]
    public class ExpressionHashCodeVisitor
    {
        #region Binary Methods

        public static int AddNumbers(int a, int b) => a + b;

        #endregion Binary Methods

        [TestMethod]
        public void Binary()
        {
            var basis = new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int, int, int>>)((x, y) => x + y));
            Assert.AreEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int, int, int>>)((a, b) => a + b)).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int, int, int>>)((a, b) => a - b)).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int, int, int>>)((a, b) => b + a)).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int, int, int>>)((a, b) => a)).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int, int, int>>)((a, b) => a + b + a)).LastVisitedHashCode);
            var aParam = Expression.Parameter(typeof(int));
            var bParam = Expression.Parameter(typeof(int));
            Assert.AreEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor(Expression.Lambda(Expression.Add(aParam, bParam), aParam, bParam)).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor(Expression.Lambda(Expression.Add(aParam, bParam, typeof(ExpressionEqualityComparisonVisitor).GetMethod(nameof(AddNumbers), BindingFlags.Public | BindingFlags.Static)), aParam, bParam)).LastVisitedHashCode);
        }

        [TestMethod]
        public void Coalesce()
        {
            var basis = new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int>>)(() => (int?)null ?? 3));
            Assert.AreEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int>>)(() => (int?)null ?? 3)).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int>>)(() => (int?)null ?? 4)).LastVisitedHashCode);
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
            var basis = new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<B>>)(() => new A() ?? new B()));
            Assert.AreEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<B>>)(() => new A() ?? new B())).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<C>>)(() => new A() ?? new C())).LastVisitedHashCode);
        }

        [TestMethod]
        public void Conditional()
        {
            var basis = new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int, int, int>>)((x, y) => x > 3 ? x : y));
            Assert.AreEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int, int, int>>)((a, b) => a > 3 ? a : b)).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int, int, int>>)((a, b) => a > 3 ? b : a)).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int, int, int>>)((a, b) => a > 2 ? a : b)).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int, int, int>>)((a, b) => a < 3 ? a : b)).LastVisitedHashCode);
        }

        [TestMethod]
        public void Index()
        {
            var stringIndexer = typeof(Dictionary<string, string>).GetProperties().Single(p => p.GetIndexParameters().Any());
            var key = Expression.Constant("key");
            var xParam = Expression.Parameter(typeof(Dictionary<string, string>));
            var basis = new ActiveExpressions.ExpressionHashCodeVisitor(Expression.Lambda(Expression.MakeIndex(xParam, stringIndexer, new Expression[] { key }), xParam));
            var aParam = Expression.Parameter(typeof(Dictionary<string, string>));
            Assert.AreEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor(Expression.Lambda(Expression.MakeIndex(aParam, stringIndexer, new Expression[] { key }), aParam)).LastVisitedHashCode);
            var bread = Expression.Constant("bread");
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor(Expression.Lambda(Expression.MakeIndex(aParam, stringIndexer, new Expression[] { bread }), aParam)).LastVisitedHashCode);
            var intIndexer = typeof(Dictionary<int, string>).GetProperties().Single(p => p.GetIndexParameters().Any());
            var two = Expression.Constant(2);
            var dict = Expression.Constant(new Dictionary<int, string>());
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor(Expression.Lambda(Expression.MakeIndex(dict, intIndexer, new Expression[] { two }), aParam)).LastVisitedHashCode);
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
            var basis = new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<Members, int>>)(x => x.Member1));
            Assert.AreEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<Members, int>>)(a => a.Member1)).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<Members, int>>)(a => a.Member2)).LastVisitedHashCode);
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
            var basis = new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<Methods, int>>)(x => x.Method1()));
            Assert.AreEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<Methods, int>>)(a => a.Method1())).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<Methods, int>>)(a => a.Method2())).LastVisitedHashCode);
        }

        [TestMethod]
        public void New()
        {
            var basis = new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<string>>)(() => new string('a', 4)));
            Assert.AreEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<string>>)(() => new string('a', 4))).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<string>>)(() => new string(new char[] { 'a', 'a', 'a', 'a' }))).LastVisitedHashCode);
        }

        [TestMethod]
        public void Nulls()
        {
            var basis = new ActiveExpressions.ExpressionHashCodeVisitor(null);
            Assert.AreEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor(null).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int>>)(() => 3)).LastVisitedHashCode);
            basis = new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int>>)(() => 3));
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor(null).LastVisitedHashCode);
        }

        #region Unary Methods

        public static int NegateNumber(int a) => -a;

        #endregion Unary Methods

        [TestMethod]
        public void Unary()
        {
            var basis = new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int, int>>)(x => -x));
            Assert.AreEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int, int>>)(a => -a)).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor((Expression<Func<int, int>>)(a => +a)).LastVisitedHashCode);
            var aParam = Expression.Parameter(typeof(int));
            Assert.AreEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor(Expression.Lambda(Expression.Negate(aParam), aParam)).LastVisitedHashCode);
            Assert.AreNotEqual(basis.LastVisitedHashCode, new ActiveExpressions.ExpressionHashCodeVisitor(Expression.Lambda(Expression.Negate(aParam, typeof(ExpressionEqualityComparisonVisitor).GetMethod(nameof(NegateNumber), BindingFlags.Public | BindingFlags.Static)), aParam)).LastVisitedHashCode);
        }
    }
}
