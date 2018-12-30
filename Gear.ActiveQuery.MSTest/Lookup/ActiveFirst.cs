using System;
using System.Collections.Generic;
using System.Linq;
using Gear.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gear.ActiveQuery.MSTest.Lookup
{
    [TestClass]
    public class ActiveFirst
    {
        [TestMethod]
        public void ExpressionlessEmptyNonNotifier()
        {
            var numbers = new Dictionary<int, int>();
            using (var query = numbers.ActiveFirst())
            {
                Assert.IsNotNull(query.OperationFault);
                Assert.AreEqual(0, query.Value.Value);
            }
        }

        [TestMethod]
        public void ExpressionlessEmptySource()
        {
            var numbers = new ObservableDictionary<int, int>();
            using (var query = numbers.ActiveFirst())
            {
                Assert.IsNotNull(query.OperationFault);
                Assert.AreEqual(0, query.Value.Value);
            }
        }

        [TestMethod]
        public void ExpressionlessNonNotifier()
        {
            var numbers = System.Linq.Enumerable.Range(0, 10).ToDictionary(i => i);
            using (var query = numbers.ActiveFirst())
            {
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(0, query.Value.Value);
            }
        }

        [TestMethod]
        public void ExpressionlessSourceManipulation()
        {
            var numbers = new ObservableDictionary<int, int>(System.Linq.Enumerable.Range(0, 10).ToDictionary(i => i));
            using (var query = numbers.ActiveFirst())
            {
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(0, query.Value.Value);
                numbers.Remove(0);
                Assert.AreEqual(1, query.Value.Value);
                numbers.Clear();
                Assert.IsNotNull(query.OperationFault);
                Assert.AreEqual(0, query.Value.Value);
                numbers.Add(30, 30);
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(30, query.Value.Value);
            }
        }

        [TestMethod]
        public void SourceManipulation()
        {
            var numbers = new ObservableDictionary<int, int>(System.Linq.Enumerable.Range(0, 10).ToDictionary(i => i));
            using (var query = numbers.ActiveFirst((key, value) => value % 3 == 0))
            {
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(0, query.Value.Value);
                numbers.Remove(0);
                Assert.AreEqual(3, query.Value.Value);
                numbers.RemoveAll((key, value) => value % 3 == 0);
                Assert.IsNotNull(query.OperationFault);
                Assert.AreEqual(0, query.Value.Value);
                numbers.Add(30, 30);
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(30, query.Value.Value);
            }
        }
    }
}
