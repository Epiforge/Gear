using Gear.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions.MSTest
{
    [TestClass]
    public class General
    {
        [TestMethod]
        public void CharStringConversion()
        {
            var person = new TestPerson("\\");
            using (var expr = ActiveExpression.Create(p1 => p1.Name[0], person))
            {
                Assert.AreEqual("{C} /* {\\} */.Name /* \"\\\\\" */[{C} /* 0 */] /* '\\\\' */", expr.ToString());
                person.Name = "\0";
                Assert.AreEqual("{C} /* {\0} */.Name /* \"\\0\" */[{C} /* 0 */] /* '\\0' */", expr.ToString());
                person.Name = "\a";
                Assert.AreEqual("{C} /* {\a} */.Name /* \"\\a\" */[{C} /* 0 */] /* '\\a' */", expr.ToString());
                person.Name = "\b";
                Assert.AreEqual("{C} /* {\b} */.Name /* \"\\b\" */[{C} /* 0 */] /* '\\b' */", expr.ToString());
                person.Name = "\f";
                Assert.AreEqual("{C} /* {\f} */.Name /* \"\\f\" */[{C} /* 0 */] /* '\\f' */", expr.ToString());
                person.Name = "\n";
                Assert.AreEqual("{C} /* {\n} */.Name /* \"\\n\" */[{C} /* 0 */] /* '\\n' */", expr.ToString());
                person.Name = "\r";
                Assert.AreEqual("{C} /* {\r} */.Name /* \"\\r\" */[{C} /* 0 */] /* '\\r' */", expr.ToString());
                person.Name = "\t";
                Assert.AreEqual("{C} /* {\t} */.Name /* \"\\t\" */[{C} /* 0 */] /* '\\t' */", expr.ToString());
                person.Name = "\v";
                Assert.AreEqual("{C} /* {\v} */.Name /* \"\\v\" */[{C} /* 0 */] /* '\\v' */", expr.ToString());
                person.Name = "x";
                Assert.AreEqual("{C} /* {x} */.Name /* \"x\" */[{C} /* 0 */] /* 'x' */", expr.ToString());
            }
        }

        [TestMethod]
        public void CreateFromLambda()
        {
            using (var expr = ActiveExpression.Create<int>(Expression.Lambda(Expression.Negate(Expression.Constant(3)))))
            {
                Assert.IsNull(expr.Fault);
                Assert.AreEqual(-3, expr.Value);
            }
        }

        [TestMethod]
        public void CreateWithOptions()
        {
            using (var expr = ActiveExpression.CreateWithOptions<int>(Expression.Lambda(Expression.Negate(Expression.Constant(3))), new ActiveExpressionOptions()))
            {
                Assert.IsNull(expr.Fault);
                Assert.AreEqual(-3, expr.Value);
            }
        }

        [TestMethod]
        public void DateTimeStringConversion()
        {
            var now = DateTime.UtcNow;
            using (var expr = ActiveExpression.Create(p1 => p1, now))
                Assert.AreEqual($"{{C}} /* new System.DateTime({now.Ticks}, System.DateTimeKind.Utc) */", expr.ToString());
        }

        [TestMethod]
        public void FaultedStringConversion()
        {
            TestPerson noOne = null;
            using (var expr = ActiveExpression.Create(p1 => p1.Name, noOne))
                Assert.AreEqual($"{{C}} /* null */.Name /* [{typeof(NullReferenceException).Name}: {new NullReferenceException().Message}] */", expr.ToString());
        }

        [TestMethod]
        public void GuidStringConversion()
        {
            var guid = Guid.NewGuid();
            using (var expr = ActiveExpression.Create(p1 => p1, guid))
                Assert.AreEqual($"{{C}} /* new System.Guid(\"{guid}\") */", expr.ToString());
        }

        [TestMethod]
        public void LambdaConsistentHashCode()
        {
            int hashCode1, hashCode2;
            using (var expr = ActiveExpression.Create<int>(Expression.Lambda(Expression.Negate(Expression.Constant(3)))))
                hashCode1 = expr.GetHashCode();
            using (var expr = ActiveExpression.Create<int>(Expression.Lambda(Expression.Negate(Expression.Constant(3)))))
                hashCode2 = expr.GetHashCode();
            Assert.IsTrue(hashCode1 == hashCode2);
        }

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
        public void ThreeArgumentConsistentHashCode()
        {
            int hashCode1, hashCode2;
            using (var expr = ActiveExpression.Create((a, b, c) => a + b + c, 1, 2, 3))
                hashCode1 = expr.GetHashCode();
            using (var expr = ActiveExpression.Create((a, b, c) => a + b + c, 1, 2, 3))
                hashCode2 = expr.GetHashCode();
            Assert.IsTrue(hashCode1 == hashCode2);
        }

        [TestMethod]
        public void ThreeArgumentEquality()
        {
            using (var expr1 = ActiveExpression.Create((a, b, c) => a + b + c, 1, 2, 3))
            using (var expr2 = ActiveExpression.Create((a, b, c) => a + b + c, 1, 2, 3))
            using (var expr3 = ActiveExpression.Create((a, b, c) => a - b + c, 1, 2, 3))
            using (var expr4 = ActiveExpression.Create((a, b, c) => a + b + c, 3, 2, 1))
            {
                Assert.IsTrue(expr1 == expr2);
                Assert.IsFalse(expr1 == expr3);
                Assert.IsFalse(expr1 == expr4);
            }
        }

        [TestMethod]
        public void ThreeArgumentEquals()
        {
            using (var expr1 = ActiveExpression.Create((a, b, c) => a + b + c, 1, 2, 3))
            using (var expr2 = ActiveExpression.Create((a, b, c) => a + b + c, 1, 2, 3))
            using (var expr3 = ActiveExpression.Create((a, b, c) => a - b + c, 1, 2, 3))
            using (var expr4 = ActiveExpression.Create((a, b, c) => a + b + c, 3, 2, 1))
            {
                Assert.IsTrue(expr1.Equals(expr2));
                Assert.IsFalse(expr1.Equals(expr3));
                Assert.IsFalse(expr1.Equals(expr4));
            }
        }

        [TestMethod]
        public void ThreeArgumentInequality()
        {
            using (var expr1 = ActiveExpression.Create((a, b, c) => a + b + c, 1, 2, 3))
            using (var expr2 = ActiveExpression.Create((a, b, c) => a + b + c, 1, 2, 3))
            using (var expr3 = ActiveExpression.Create((a, b, c) => a - b + c, 1, 2, 3))
            using (var expr4 = ActiveExpression.Create((a, b, c) => a + b + c, 3, 2, 1))
            {
                Assert.IsFalse(expr1 != expr2);
                Assert.IsTrue(expr1 != expr3);
                Assert.IsTrue(expr1 != expr4);
            }
        }

        [TestMethod]
        public void ThreeArgumentStringConversion()
        {
            using (var expr = ActiveExpression.Create((a, b, c) => a + b + c, 1, 2, 3))
                Assert.AreEqual($"(({{C}} /* 1 */ + {{C}} /* 2 */) /* 3 */ + {{C}} /* 3 */) /* 6 */", expr.ToString());
        }

        [TestMethod]
        public void ThreeArgumentValueChanges()
        {
            var john = TestPerson.CreateJohn();
            var emily = TestPerson.CreateEmily();
            var charles = new TestPerson("Charles");
            var values = new BlockingCollection<string>();
            using (var expr = ActiveExpression.Create((a, b, c) => $"{a.Name} {b.Name} {c.Name}", john, emily, charles))
            {
                var disconnect = expr.OnPropertyChanged(ae => ae.Value, value => values.Add(value));
                values.Add(expr.Value);
                john.Name = "J";
                emily.Name = "E";
                charles.Name = "C";
                disconnect();
            }
            Assert.IsTrue(new string[]
            {
                "John Emily Charles",
                "J Emily Charles",
                "J E Charles",
                "J E C"
            }.SequenceEqual(values));
        }

        [TestMethod]
        public void TimeSpanStringConversion()
        {
            var threeMinutes = TimeSpan.FromMinutes(3);
            using (var expr = ActiveExpression.Create(p1 => p1, threeMinutes))
                Assert.AreEqual($"{{C}} /* new System.TimeSpan({threeMinutes.Ticks}) */", expr.ToString());
        }

        [TestMethod]
        public void TwoArgumentConsistentHashCode()
        {
            int hashCode1, hashCode2;
            using (var expr = ActiveExpression.Create((a, b) => a + b, 1, 2))
                hashCode1 = expr.GetHashCode();
            using (var expr = ActiveExpression.Create((a, b) => a + b, 1, 2))
                hashCode2 = expr.GetHashCode();
            Assert.IsTrue(hashCode1 == hashCode2);
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
