using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Gear.ActiveQuery.MSTest.Lookup
{
    [TestClass]
    public class ToActiveEnumerable
    {
        [TestMethod]
        public void SourceManipulation()
        {
            var people = TestPerson.CreatePeopleDictionary();
            using (var query = people.ToActiveEnumerable())
            {
                void checkSum(int against) => Assert.AreEqual(against, query.Sum(person => person.Name.Length));
                Assert.AreEqual(0, query.GetElementFaults().Count);
                checkSum(74);
                people.Add(people.Count, people[0]);
                checkSum(78);
                people[0].Name = "Johnny";
                checkSum(82);
            }
        }
    }
}