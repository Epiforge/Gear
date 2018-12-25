using System;
using System.Collections;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gear.ActiveQuery.MSTest.Enumerable
{
    [TestClass]
    public class ActiveSelect
    {
        [TestMethod]
        public void EnumerableSorted()
        {
            var argumentOutOfRangeThrown = false;
            try
            {
                ((IEnumerable)new object[0]).ActiveSelect(obj => obj.GetHashCode(), indexingStrategy: IndexingStrategy.SelfBalancingBinarySearchTree);
            }
            catch (ArgumentOutOfRangeException)
            {
                argumentOutOfRangeThrown = true;
            }
            Assert.IsTrue(argumentOutOfRangeThrown);
        }

        [TestMethod]
        public void EnumerableSourceManipulation()
        {
            var people = TestPerson.CreatePeople();
            using (var expr = ((IEnumerable)people).ActiveSelect(person => (person as TestPerson).Name.Length))
            {
                void checkValues(params int[] values) => Assert.IsTrue(values.SequenceEqual(expr));
                Assert.IsTrue(expr.IsSynchronized);
                checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.Add(people.First());
                checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 4);
                people[0].Name = "Johnny";
                checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 6);
                people.RemoveAt(people.Count - 1);
                checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.Move(0, 1);
                checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.IsSynchronized = false;
                Assert.IsFalse(expr.IsSynchronized);
                people.IsSynchronized = true;
                Assert.IsTrue(expr.IsSynchronized);
                people.Insert(0, people[0]);
                checkValues(5, 5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.RemoveAt(1);
                checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.Move(1, 0);
                checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.RemoveAt(0);
                checkValues(5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
            }
        }

        [TestMethod]
        public void EnumerableSourceManipulationUnindexed()
        {
            var people = TestPerson.CreatePeople();
            using (var expr = ((IEnumerable)people).ActiveSelect(person => (person as TestPerson).Name.Length, indexingStrategy: IndexingStrategy.NoneOrInherit))
            {
                void checkValues(params int[] values) => Assert.IsTrue(values.SequenceEqual(expr));
                Assert.IsTrue(expr.IsSynchronized);
                checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.Add(people.First());
                checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 4);
                people[0].Name = "Johnny";
                checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 6);
                people.RemoveAt(people.Count - 1);
                checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.Move(0, 1);
                checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.IsSynchronized = false;
                Assert.IsFalse(expr.IsSynchronized);
                people.IsSynchronized = true;
                Assert.IsTrue(expr.IsSynchronized);
            }
        }

        [TestMethod]
        public void SourceManipulation()
        {
            var people = TestPerson.CreatePeople();
            using (var expr = people.ActiveSelect(person => person.Name.Length))
            {
                void checkValues(params int[] values) => Assert.IsTrue(values.SequenceEqual(expr));
                Assert.IsTrue(expr.IsSynchronized);
                checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.Add(people.First());
                checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 4);
                people[0].Name = "Johnny";
                checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 6);
                people.RemoveAt(people.Count - 1);
                checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.Move(0, 1);
                checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.IsSynchronized = false;
                Assert.IsFalse(expr.IsSynchronized);
                people.IsSynchronized = true;
                Assert.IsTrue(expr.IsSynchronized);
                people.Insert(0, people[0]);
                checkValues(5, 5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.RemoveAt(1);
                checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.Move(1, 0);
                checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.RemoveAt(0);
                checkValues(5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
            }
        }

        [TestMethod]
        public void SourceManipulationSorted()
        {
            var people = TestPerson.CreatePeople();
            using (var expr = people.ActiveSelect(person => person.Name.Length, indexingStrategy: IndexingStrategy.SelfBalancingBinarySearchTree))
            {
                void checkValues(params int[] values) => Assert.IsTrue(values.SequenceEqual(expr));
                Assert.IsTrue(expr.IsSynchronized);
                checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.Add(people.First());
                checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 4);
                people[0].Name = "Johnny";
                checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 6);
                people.RemoveAt(people.Count - 1);
                checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.Move(0, 1);
                checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.IsSynchronized = false;
                Assert.IsFalse(expr.IsSynchronized);
                people.IsSynchronized = true;
                Assert.IsTrue(expr.IsSynchronized);
                people.Insert(0, people[0]);
                checkValues(5, 5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.RemoveAt(1);
                checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.Move(1, 0);
                checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.RemoveAt(0);
                checkValues(5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
            }
        }

        [TestMethod]
        public void SourceManipulationUnindexed()
        {
            var people = TestPerson.CreatePeople();
            using (var expr = people.ActiveSelect(person => person.Name.Length, indexingStrategy: IndexingStrategy.NoneOrInherit))
            {
                void checkValues(params int[] values) => Assert.IsTrue(values.SequenceEqual(expr));
                Assert.IsTrue(expr.IsSynchronized);
                checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.Add(people.First());
                checkValues(4, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 4);
                people[0].Name = "Johnny";
                checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5, 6);
                people.RemoveAt(people.Count - 1);
                checkValues(6, 5, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.Move(0, 1);
                checkValues(5, 6, 7, 4, 5, 6, 3, 5, 7, 7, 6, 5, 5, 5);
                people.IsSynchronized = false;
                Assert.IsFalse(expr.IsSynchronized);
                people.IsSynchronized = true;
                Assert.IsTrue(expr.IsSynchronized);
            }
        }
    }
}