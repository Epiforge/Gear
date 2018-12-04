using Gear.Components;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gear.ActiveExpressions.Tests
{
    [TestFixture]
    class ActiveIndexExpression
    {
        [Test]
        public void ArgumentChanges()
        {
            var reversedNumbersList = Enumerable.Range(1, 10).Reverse().ToImmutableList();
            var john = TestPerson.CreateJohn();
            var values = new BlockingCollection<int>();
            using (var expr = ActiveExpression.Create((p1, p2) => p1[p2.Name.Length], reversedNumbersList, john))
            {
                var disconnect = expr.OnPropertyChanged(ae => ae.Value, value => values.Add(value));
                values.Add(expr.Value);
                john.Name = "J";
                john.Name = "Joh";
                john.Name = string.Empty;
                john.Name = "Johnny";
                john.Name = "John";
                disconnect();
            }
            Assert.IsTrue(new int[] { 6, 9, 7, 10, 4, 6 }.SequenceEqual(values));
        }

        [Test]
        public void Equality()
        {
            var john = TestPerson.CreateJohn();
            var men = new List<TestPerson> { john, null };
            var emily = TestPerson.CreateEmily();
            var women = new List<TestPerson> { emily, null };
            using (var expr1 = ActiveExpression.Create(p1 => p1[0], men))
            using (var expr2 = ActiveExpression.Create(p1 => p1[0], men))
            using (var expr3 = ActiveExpression.Create(p1 => p1[1], men))
            using (var expr4 = ActiveExpression.Create(p1 => p1[0], women))
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
            var men = new List<TestPerson> { john, null };
            var emily = TestPerson.CreateEmily();
            var women = new List<TestPerson> { emily, null };
            using (var expr1 = ActiveExpression.Create(p1 => p1[0], men))
            using (var expr2 = ActiveExpression.Create(p1 => p1[0], men))
            using (var expr3 = ActiveExpression.Create(p1 => p1[1], men))
            using (var expr4 = ActiveExpression.Create(p1 => p1[0], women))
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
            var men = new List<TestPerson> { john, null };
            var emily = TestPerson.CreateEmily();
            var women = new List<TestPerson> { emily, null };
            using (var expr1 = ActiveExpression.Create(p1 => p1[0], men))
            using (var expr2 = ActiveExpression.Create(p1 => p1[0], men))
            using (var expr3 = ActiveExpression.Create(p1 => p1[1], men))
            using (var expr4 = ActiveExpression.Create(p1 => p1[0], women))
            {
                Assert.IsFalse(expr1 != expr2);
                Assert.IsTrue(expr1 != expr3);
                Assert.IsTrue(expr1 != expr4);
            }
        }
    }
}
