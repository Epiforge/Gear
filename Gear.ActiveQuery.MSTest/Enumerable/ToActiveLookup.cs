using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gear.ActiveQuery.MSTest.Enumerable
{
    [TestClass]
    public class ToActiveLookup
    {
        [TestMethod]
        public void ElementResultChanges()
        {
            var people = TestPerson.GetPeople();
            using (var query = people.ToActiveLookup(p => new KeyValuePair<string, string>(p.Name.Substring(0, 3), p.Name.Substring(3))))
            {
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual(string.Empty, query["Ben"]);
                people[6].Name = "Benjamin";
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual("jamin", query["Ben"]);
                people[6].Name = "Ben";
                var benny = new TestPerson("!!!TROUBLE");
                people.Add(benny);
                Assert.IsNull(query.OperationFault);
                benny.Name = "Benny";
                Assert.IsNotNull(query.OperationFault);
                var benjamin = new TestPerson("@@@TROUBLE");
                people.Add(benjamin);
                benjamin.Name = "Benjamin";
                Assert.IsNotNull(query.OperationFault);
                benny.Name = "!!!TROUBLE";
                Assert.IsNotNull(query.OperationFault);
                Assert.AreEqual("TROUBLE", query["!!!"]);
                benjamin.Name = "@@@TROUBLE";
                Assert.IsNull(query.OperationFault);
                Assert.AreEqual("TROUBLE", query["@@@"]);
            }
        }
    }
}
