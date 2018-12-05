using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gear.ActiveExpressions.Tests
{
    [TestFixture]
    class ActiveNewExpression
    {
        [Test]
        public void ArgumentFaultPropagation()
        {
            var john = TestPerson.CreateJohn();
            using (var expr = ActiveExpression.Create(() => new TestPerson(john.Name.Length.ToString())))
            {
                Assert.IsNull(expr.Fault);
                john.Name = null;
                Assert.IsNotNull(expr.Fault);
                john.Name = "John";
                Assert.IsNull(expr.Fault);
            }
        }

        [Test]
        public void Equality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr1 = ActiveExpression.Create(() => new TestPerson("Charles")))
            using (var expr2 = ActiveExpression.Create(() => new TestPerson("Charles")))
            using (var expr3 = ActiveExpression.Create(() => new TestPerson()))
            using (var expr4 = ActiveExpression.Create(() => new TestPerson("Erin")))
            {
                Assert.IsTrue(expr1 == expr2);
                Assert.IsFalse(expr1 == expr3);
                Assert.IsFalse(expr1 == expr4);
            }
        }

        [Test]
        public void Equals()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr1 = ActiveExpression.Create(() => new TestPerson("Charles")))
            using (var expr2 = ActiveExpression.Create(() => new TestPerson("Charles")))
            using (var expr3 = ActiveExpression.Create(() => new TestPerson()))
            using (var expr4 = ActiveExpression.Create(() => new TestPerson("Erin")))
            {
                Assert.IsTrue(expr1.Equals(expr2));
                Assert.IsFalse(expr1.Equals(expr3));
                Assert.IsFalse(expr1.Equals(expr4));
            }
        }

        [Test]
        public void Inequality()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            using (var expr1 = ActiveExpression.Create(() => new TestPerson("Charles")))
            using (var expr2 = ActiveExpression.Create(() => new TestPerson("Charles")))
            using (var expr3 = ActiveExpression.Create(() => new TestPerson()))
            using (var expr4 = ActiveExpression.Create(() => new TestPerson("Erin")))
            {
                Assert.IsFalse(expr1 != expr2);
                Assert.IsTrue(expr1 != expr3);
                Assert.IsTrue(expr1 != expr4);
            }
        }

        [Test]
        public void StringConversion()
        {
            using (var expr = ActiveExpression.Create(() => new TestPerson("Charles")))
                Assert.AreEqual($"new {typeof(TestPerson)}({{C}} /* \"Charles\" */) /* {expr.Value} */", expr.ToString());
        }

        [Test]
        public void ValueAsyncDisposal()
        {
            var john = AsyncDisposableTestPerson.CreateJohn();
            var options = new ActiveExpressionOptions();
            options.AddConstructedTypeDisposal(typeof(AsyncDisposableTestPerson));
            AsyncDisposableTestPerson first, second;
            using (var expr = ActiveExpression.Create(() => new AsyncDisposableTestPerson(john.Name.Length.ToString()), options))
            {
                Assert.IsNull(expr.Fault);
                first = expr.Value;
                Assert.IsFalse(first.IsDisposed);
                john.Name = string.Empty;
                Assert.IsNull(expr.Fault);
                second = expr.Value;
                Assert.IsFalse(second.IsDisposed);
                Assert.IsTrue(first.IsDisposed);
            }
            Assert.IsTrue(second.IsDisposed);
        }

        [Test]
        public void ValueDisposal()
        {
            var john = SyncDisposableTestPerson.CreateJohn();
            var options = new ActiveExpressionOptions();
            options.AddConstructedTypeDisposal(typeof(SyncDisposableTestPerson));
            SyncDisposableTestPerson first, second;
            using (var expr = ActiveExpression.Create(() => new SyncDisposableTestPerson(john.Name.Length.ToString()), options))
            {
                Assert.IsNull(expr.Fault);
                first = expr.Value;
                Assert.IsFalse(first.IsDisposed);
                john.Name = string.Empty;
                Assert.IsNull(expr.Fault);
                second = expr.Value;
                Assert.IsFalse(second.IsDisposed);
                Assert.IsTrue(first.IsDisposed);
            }
            Assert.IsTrue(second.IsDisposed);
        }

        [Test]
        public void ValueDisposalFault()
        {
            var john = SyncDisposableTestPerson.CreateJohn();
            john.Name = string.Empty;
            var options = new ActiveExpressionOptions();
            options.AddConstructedTypeDisposal(typeof(SyncDisposableTestPerson));
            using (var expr = ActiveExpression.Create(() => new SyncDisposableTestPerson(null, john.Name.Length == 0), options))
            {
                Assert.IsNull(expr.Fault);
                john.Name = "John";
                Assert.IsNotNull(expr.Fault);
                john.Name = string.Empty;
                Assert.IsNull(expr.Fault);
                john.Name = "John";
                Assert.IsNotNull(expr.Fault);
            }
        }
    }
}
