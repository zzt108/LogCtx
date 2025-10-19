// ✅ FULL FILE VERSION
// tests/LogCtxShared.Tests/PropsTests.cs

using NUnit.Framework;
using Shouldly;
using LogCtxShared;
using System;
using System.Collections.Generic;

namespace LogCtxShared.Tests
{
    [TestFixture]
    [Category("unit")]
    public class PropsTests
    {
        [Test]
        public void Ctor_WithParams_AssignsSequentialPxxKeys()
        {
            // Act
            var props = new Props("A", 42, true);

            // Assert
            props.Count.ShouldBe(3);
            props["P00"].ShouldBe("A".AsJson(true));
            props["P01"].ShouldBe(42.AsJson(true));
            props["P02"].ShouldBe(true.AsJson(true));
        }

        [Test]
        public void Add_AddsOrReplacesValue_ByKey()
        {
            // Arrange
            var props = new Props();
            props.Add("P00", "Old");

            // Act
            props.Add("P00", "New");
            props.Add("P01", 123);

            // Assert
            props["P00"].ShouldBe("New");
            props["P01"].ShouldBe(123);
            props.Count.ShouldBe(2);
        }

        [Test]
        public void Add_WithNullValue_StoresNull()
        {
            // Arrange
            var props = new Props();

            // Act
            props.Add("P00", null);

            // Assert
            props.ContainsKey("P00").ShouldBeTrue();
            props["P00"].ShouldBe("null value");
        }

        [Test]
        public void Add_SequentialWithoutKey_AppendsPxx()
        {
            // Arrange
            var props = new Props("A");

            // Act
            props.Add("K01", "B");
            props.Add("K02", "C");

            // Assert
            props["P00"].ShouldBe("A".AsJson(true));
            props["K01"].ShouldBe("B");
            props["K02"].ShouldBe("C");
            props.Count.ShouldBe(3);
        }

        [Test]
        public void AddJson_SerializesObject_CompactByDefault()
        {
            // Arrange
            var props = new Props();
            var obj = new { Name = "Test", Value = 7 };

            // Act
            props.AddJson("P00", obj);

            // Assert
            props["P00"].ShouldBe("{\"Name\":\"Test\",\"Value\":7}");
            props["P00"].ToString().ShouldNotContain("\n");
        }

        [Test]
        public void AddJson_WithIndented_SerializesWithNewline()
        {
            // Arrange
            var props = new Props();
            var obj = new { Name = "Test", Value = 7 };

            // Act
            props.AddJson("P00", obj, Newtonsoft.Json.Formatting.Indented);

            // Assert
            var val = props["P00"].ToString();
            val.ShouldContain("\n");
            val.ShouldNotEndWith("\n");
            val.ShouldContain("\"Name\": \"Test\"");
            val.ShouldContain("\"Value\": 7");
        }

        [Test]
        public void Clear_RemovesAllEntries_AndReturnsThis()
        {
            // Arrange
            var props = new Props("A", "B", "C");

            // Act
            var returned = props.Clear();

            // Assert
            props.Count.ShouldBe(0);
            ReferenceEquals(returned, props).ShouldBeTrue();
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes_Safely()
        {
            // Arrange
            var props = new Props("A", "B");

            // Act
            props.Dispose();
            props.Dispose();

            // Assert
            props.Count.ShouldBe(2); // no side effects on dispose per design
        }

        [Test]
        public void Indexer_GetSet_WorksAsDictionary()
        {
            // Arrange
            var props = new Props();

            // Act
            props["P00"] = 10;
            props["P01"] = 20;

            // Assert
            props["P00"].ShouldBe(10);
            props["P01"].ShouldBe(20);
            props.Count.ShouldBe(2);
        }

        [Test]
        public void Enumerator_IteratesAllEntries()
        {
            // Arrange
            var props = new Props("A", "B", "C");
            var collected = new List<object>();

            // Act
            foreach (var kv in props)
                collected.Add(kv.Value);

            // Assert
            collected.Count.ShouldBe(3);
            collected.ShouldContain("A".AsJson(true));
            collected.ShouldContain("B".AsJson(true));
            collected.ShouldContain("C".AsJson(true));
        }
    }
}
