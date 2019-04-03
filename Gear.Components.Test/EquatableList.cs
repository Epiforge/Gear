using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gear.Components.Test
{
    [TestClass]
    public class EquatableList
    {
        [TestMethod]
        public void ConsistentHashCode()
        {
            var strings = new string[] { "ABC", null, "XYZ" };
            Assert.IsTrue(new EquatableList<string>(strings).GetHashCode() == new EquatableList<string>(strings).GetHashCode());
        }
    }
}
