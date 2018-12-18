using Gear.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Gear.ActiveQuery.MSTest.Enumerable
{
    [TestClass]
    public class ActiveCast
    {
        [TestMethod]
        public void ReferenceSourceManipulation()
        {
            var numbers = new RangeObservableCollection<int>(System.Linq.Enumerable.Range(1, 10));
            using (var query = numbers.ActiveCast<object>())
            {
                Assert.AreEqual(0, query.GetElementFaults().Count);
                Assert.AreEqual(55, query.Cast<int>().Sum());
                Assert.IsInstanceOfType(query[0], typeof(object));
                numbers[0] += 10;
                Assert.AreEqual(0, query.GetElementFaults().Count);
                Assert.AreEqual(65, query.Cast<int>().Sum());
            }
        }

        [TestMethod]
        public void ValueTypeSourceManipulation()
        {
            var numbers = new RangeObservableCollection<int>(System.Linq.Enumerable.Range(1, 10));
            using (var query = numbers.ActiveCast<decimal>())
            {
                Assert.AreEqual(0, query.GetElementFaults().Count);
                Assert.AreEqual(55M, query.Sum());
                Assert.IsInstanceOfType(query[0], typeof(decimal));
                numbers[0] += 10;
                Assert.AreEqual(0, query.GetElementFaults().Count);
                Assert.AreEqual(65M, query.Sum());
            }
        }
    }
}
