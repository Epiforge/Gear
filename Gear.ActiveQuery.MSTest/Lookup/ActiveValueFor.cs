using Gear.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Gear.ActiveQuery.MSTest.Lookup
{
    [TestClass]
    public class ActiveValueFor
    {
        [TestMethod]
        public void NonNotifier()
        {
            var numbers = System.Linq.Enumerable.Range(0, 10).ToDictionary(i => i);
            using (var query = numbers.ActiveValueFor(9))
            {
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(9, query.Value);
            }
        }

        [TestMethod]
        public void NonNotifierOutOfRange()
        {
            var numbers = System.Linq.Enumerable.Range(0, 5).ToDictionary(i => i);
            using (var query = numbers.ActiveValueFor(9))
            {
                Assert.IsNotNull(query.OperationFault);
                Assert.AreEqual(0, query.Value);
            }
        }

        [TestMethod]
        public void SourceManipulation()
        {
            var numbers = new ObservableDictionary<int, int>(System.Linq.Enumerable.Range(0, 10).ToDictionary(i => i));
            using (var query = numbers.ActiveValueFor(9))
            {
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(9, query.Value);
                numbers.Remove(9);
                Assert.IsNotNull(query.OperationFault);
                Assert.AreEqual(0, query.Value);
                numbers.Add(9, 30);
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(30, query.Value);
                numbers[9] = 15;
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(15, query.Value);
            }
        }

        [TestMethod]
        public void SourceManipulationSorted()
        {
            var numbers = new ObservableSortedDictionary<int, int>(System.Linq.Enumerable.Range(0, 10).ToDictionary(i => i));
            using (var query = numbers.ActiveValueFor(9))
            {
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(9, query.Value);
                numbers.Remove(9);
                Assert.IsNotNull(query.OperationFault);
                Assert.AreEqual(0, query.Value);
                numbers.Add(9, 30);
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(30, query.Value);
                numbers[9] = 15;
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(15, query.Value);
            }
        }
    }
}
