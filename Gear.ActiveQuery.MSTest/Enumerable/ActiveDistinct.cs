using Gear.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Gear.ActiveQuery.MSTest.Enumerable
{
    [TestClass]
    public class ActiveDistinct
    {
        [TestMethod]
        public void SourceManipulation()
        {
            var numbers = new SynchronizedRangeObservableCollection<int>(null, System.Linq.Enumerable.Range(0, 10).SelectMany(i => i.Repeat(5)));
            using (var query = numbers.ActiveDistinct())
            {
                Assert.AreEqual(0, query.GetElementFaults().Count);
                Assert.AreEqual(10, query.Count);
                numbers.RemoveAt(0);
                Assert.AreEqual(10, query.Count);
                numbers.RemoveRange(0, 4);
                Assert.AreEqual(9, query.Count);
                numbers.Add(10);
                Assert.AreEqual(10, query.Count);
                numbers.AddRange(10.Repeat(4));
                Assert.AreEqual(10, query.Count);
                numbers.Reset(System.Linq.Enumerable.Range(0, 5).SelectMany(i => i.Repeat(2)));
                Assert.AreEqual(5, query.Count);
                Assert.IsTrue(numbers.IsSynchronized);
                Assert.IsTrue(query.IsSynchronized);
                numbers.IsSynchronized = false;
                Assert.IsFalse(query.IsSynchronized);
                numbers.IsSynchronized = true;
                Assert.IsTrue(query.IsSynchronized);
            }
        }
    }
}
