using Gear.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Gear.ActiveQuery.MSTest.Lookup
{
    [TestClass]
    public class ActiveSingleOrDefault
    {
        [TestMethod]
        public void EmptySource()
        {
            var numbers = new ObservableDictionary<int, int>();
            using (var expr = numbers.ActiveSingleOrDefault((key, value) => value % 3 == 0))
            {
                Assert.IsNull(expr.OperationFault);
                Assert.AreEqual(0, expr.Value.Value);
            }
        }

        [TestMethod]
        public void ExpressionlessEmptyNonNotifier()
        {
            var numbers = new Dictionary<int, int>();
            using (var expr = numbers.ActiveSingleOrDefault())
            {
                Assert.IsNull(expr.OperationFault);
                Assert.AreEqual(0, expr.Value.Value);
            }
        }

        [TestMethod]
        public void ExpressionlessEmptySource()
        {
            var numbers = new ObservableDictionary<int, int>();
            using (var expr = numbers.ActiveSingleOrDefault())
            {
                Assert.IsNull(expr.OperationFault);
                Assert.AreEqual(0, expr.Value.Value);
            }
        }

        [TestMethod]
        public void ExpressionlessMultiple()
        {
            var numbers = new ObservableDictionary<int, int>(System.Linq.Enumerable.Range(1, 2).ToDictionary(i => i));
            using (var expr = numbers.ActiveSingleOrDefault())
            {
                Assert.IsNotNull(expr.OperationFault);
                Assert.AreEqual(0, expr.Value.Value);
            }
        }

        [TestMethod]
        public void ExpressionlessNonNotifier()
        {
            var numbers = System.Linq.Enumerable.Range(1, 1).ToDictionary(i => i);
            using (var expr = numbers.ActiveSingleOrDefault())
            {
                Assert.IsNull(expr.OperationFault);
                Assert.AreEqual(1, expr.Value.Value);
            }
        }

        [TestMethod]
        public void ExpressionlessNonNotifierMultiple()
        {
            var numbers = System.Linq.Enumerable.Range(1, 2).ToDictionary(i => i);
            using (var expr = numbers.ActiveSingleOrDefault())
            {
                Assert.IsNotNull(expr.OperationFault);
                Assert.AreEqual(0, expr.Value.Value);
            }
        }

        [TestMethod]
        public void ExpressionlessSourceManipulation()
        {
            var numbers = new ObservableDictionary<int, int>(System.Linq.Enumerable.Range(1, 1).ToDictionary(i => i));
            using (var expr = numbers.ActiveSingleOrDefault())
            {
                Assert.IsNull(expr.OperationFault);
                Assert.AreEqual(1, expr.Value.Value);
                numbers.Add(2, 2);
                Assert.IsNotNull(expr.OperationFault);
                Assert.AreEqual(0, expr.Value.Value);
                numbers.Remove(1);
                Assert.IsNull(expr.OperationFault);
                Assert.AreEqual(2, expr.Value.Value);
                numbers.Clear();
                Assert.IsNull(expr.OperationFault);
                Assert.AreEqual(0, expr.Value.Value);
            }
        }

        [TestMethod]
        public void Multiple()
        {
            var numbers = new ObservableDictionary<int, int>(System.Linq.Enumerable.Range(1, 3).ToDictionary(i => i, i => i * 3));
            using (var expr = numbers.ActiveSingleOrDefault((key, value) => value % 3 == 0))
            {
                Assert.IsNotNull(expr.OperationFault);
                Assert.AreEqual(0, expr.Value.Value);
            }
        }

        [TestMethod]
        public void SourceManipulation()
        {
            var numbers = new ObservableDictionary<int, int>(System.Linq.Enumerable.Range(1, 3).ToDictionary(i => i));
            using (var expr = numbers.ActiveSingleOrDefault((key, value) => value % 3 == 0))
            {
                Assert.IsNull(expr.OperationFault);
                Assert.AreEqual(3, expr.Value.Value);
                numbers.Remove(3);
                Assert.IsNull(expr.OperationFault);
                Assert.AreEqual(0, expr.Value.Value);
                numbers.Add(3, 3);
                Assert.IsNull(expr.OperationFault);
                Assert.AreEqual(3, expr.Value.Value);
                numbers.Add(5, 5);
                Assert.IsNull(expr.OperationFault);
                Assert.AreEqual(3, expr.Value.Value);
                numbers.Add(6, 6);
                Assert.IsNotNull(expr.OperationFault);
                Assert.AreEqual(0, expr.Value.Value);
                numbers.Clear();
                Assert.IsNull(expr.OperationFault);
                Assert.AreEqual(0, expr.Value.Value);
            }
        }
    }
}
