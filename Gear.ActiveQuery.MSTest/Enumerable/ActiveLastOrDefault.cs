using System;
using Gear.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gear.ActiveQuery.MSTest.Enumerable
{
    [TestClass]
    public class ActiveLastOrDefault
    {
        [TestMethod]
        public void ExpressionlessEmptySource()
        {
            var numbers = new RangeObservableCollection<int>();
            using (var query = numbers.ActiveLastOrDefault())
            {
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(0, query.Value);
            }
        }

        [TestMethod]
        public void ExpressionlessNonNotifier()
        {
            var numbers = System.Linq.Enumerable.Range(0, 10);
            using (var query = numbers.ActiveLastOrDefault())
            {
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(9, query.Value);
            }
        }

        [TestMethod]
        public void ExpressionlessSourceManipulation()
        {
            var numbers = new RangeObservableCollection<int>(System.Linq.Enumerable.Range(0, 10));
            using (var query = numbers.ActiveLastOrDefault())
            {
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(9, query.Value);
                numbers.Remove(9);
                Assert.AreEqual(8, query.Value);
                numbers.Clear();
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(0, query.Value);
                numbers.Add(30);
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(30, query.Value);
            }
        }

        [TestMethod]
        public void SourceManipulation()
        {
            var numbers = new RangeObservableCollection<int>(System.Linq.Enumerable.Range(0, 10));
            using (var query = numbers.ActiveLastOrDefault(i => i % 3 == 0))
            {
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(9, query.Value);
                numbers.Remove(9);
                Assert.AreEqual(6, query.Value);
                numbers.RemoveAll(i => i % 3 == 0);
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(0, query.Value);
                numbers.Add(30);
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(30, query.Value);
            }
        }
    }
}
