using NUnit.Framework;
using Shouldly;
using LogCtxShared;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace LogCtx.Tests
{
    [TestFixture]
    public class PropsTests
    {
        private ILogger<BasicTests> _logger;

        [SetUp]
        public void Setup()
        {
            _logger = Logging.Factory.CreateLogger<BasicTests>();
        }

        #region Constructor Tests

        [Test]
        [Obsolete("Use Props(logger) instead")]
        public void Constructor_Default_ShouldCreateEmptyDictionary()
        {
            // Act
            var props = new Props(_logger);

            // Assert
            props.Count.ShouldBe(0);
        }

        [Test]
        [Obsolete("Use Props(...).Add() instead")]
        public void Constructor_WithParams_ShouldSerializeObjects()
        {
            // Arrange
            //var obj1 = new { Name = "Test1" };
            //var obj2 = new { Name = "Test2" };

            //// Act
            //var props = new Props(obj1, obj2);

            //// Assert
            //props.Count.ShouldBe(2);
            //var p0 = props["P00"] as string;
            //p0.ShouldNotBeNullOrWhiteSpace();
            //p0.ShouldContain("Test1"); // JSON serialized

            //var p1 = props["P01"] as string;
            //p1.ShouldNotBeNullOrWhiteSpace();
            //p1.ShouldContain("Test2");
        }

        #endregion Constructor Tests

        #region Add Tests

        [Test]
        public void Add_ShouldAddKeyValuePair()
        {
            // Arrange
            var props = new Props(_logger);

            // Act
            props.Add("Key1", "Value1");

            // Assert
            props.Count.ShouldBe(1);
            props["Key1"].ShouldBe("Value1");
        }

        [Test]
        public void Add_ShouldReturnPropsForChaining()
        {
            // Arrange
            var props = new Props(_logger);

            // Act
            var result = props.Add("Key1", "Value1");

            // Assert
            result.ShouldBeSameAs(props); // Fluent API
        }

        [Test]
        public void Add_FluentChaining_ShouldWork()
        {
            // Act
            var props = new Props(_logger)
                .Add("Key1", "Value1")
                .Add("Key2", 42)
                .Add("Key3", true);

            // Assert
            props.Count.ShouldBe(3);
            props["Key1"].ShouldBe("Value1");
            props["Key2"].ShouldBe(42);
            props["Key3"].ShouldBe(true);
        }

        [Test]
        public void Add_DuplicateKey_ShouldReplaceValue()
        {
            // Arrange
            var props = new Props(_logger)
                .Add("Key1", "OriginalValue");

            // Act
            props.Add("Key1", "NewValue");

            // Assert
            props.Count.ShouldBe(1);
            props["Key1"].ShouldBe("NewValue");
        }

        [Test]
        public void Add_NullValue_ShouldStoreNull()
        {
            // Arrange
            var props = new Props(_logger);

            // Act
            props.Add("NullKey", null);

            // Assert
            props.ContainsKey("NullKey").ShouldBeTrue();
            props["NullKey"].ShouldBe("null value");
        }

        [Test]
        public void Add_MixedTypes_ShouldStoreAllTypes()
        {
            // Act
            var props = new Props(_logger)
                .Add("StringKey", "text")
                .Add("IntKey", 123)
                .Add("BoolKey", true)
                .Add("DoubleKey", 45.67);

            // Assert
            props.Count.ShouldBe(4);
            props["StringKey"].ShouldBe("text");
            props["IntKey"].ShouldBe(123);
            props["BoolKey"].ShouldBe(true);
            props["DoubleKey"].ShouldBe(45.67);
        }

        #endregion Add Tests

        #region AddJson Tests

        [Test]
        public void AddJson_ShouldSerializeObjectToJson()
        {
            // Arrange
            var props = new Props(_logger);
            var obj = new { Name = "Test", Value = 123 };

            // Act
            props.AddJson("JsonKey", obj);

            // Assert
            props.ContainsKey("JsonKey").ShouldBeTrue();
            var json = props["JsonKey"] as string;
            json.ShouldNotBeNullOrWhiteSpace();
            json.ShouldContain("Name");
            json.ShouldContain("Test");
            json.ShouldContain("Value");
            json.ShouldContain("123");
        }

        [Test]
        public void AddJson_WithFormatting_ShouldUseSpecifiedFormat()
        {
            // Arrange
            var props = new Props(_logger);
            var obj = new { Name = "Test" };

            // Act
            props.AddJson("IndentedKey", obj, Formatting.Indented);

            // Assert
            var json = props["IndentedKey"] as string;
            json!.ShouldContain("\n"); // Indented JSON has newlines
        }

        [Test]
        public void AddJson_WithoutFormatting_ShouldUseCompactFormat()
        {
            // Arrange
            var props = new Props(_logger);
            var obj = new { Name = "Test" };

            // Act
            props.AddJson("CompactKey", obj, Formatting.None);

            // Assert
            var json = props["CompactKey"] as string;
            json!.ShouldNotContain("\n"); // Compact JSON has no newlines
        }

        [Test]
        public void AddJson_ShouldReturnPropsForChaining()
        {
            // Arrange
            var props = new Props(_logger);
            var obj = new { Name = "Test" };

            // Act
            var result = props.AddJson("JsonKey", obj);

            // Assert
            result.ShouldBeSameAs(props);
        }

        [Test]
        public void AddJson_ComplexObject_ShouldSerialize()
        {
            // Arrange
            var props = new Props(_logger);
            var obj = new
            {
                OrderId = 123,
                Customer = new { Id = 456, Name = "John" },
                Items = new[] { "Item1", "Item2" }
            };

            // Act
            props.AddJson("OrderKey", obj);

            // Assert
            var json = props["OrderKey"] as string;
            json!.ShouldContain("OrderId");
            json!.ShouldContain("Customer");
            json!.ShouldContain("Items");
        }

        #endregion AddJson Tests

        #region Clear Tests

        [Test]
        public void Clear_ShouldRemoveAllEntries()
        {
            // Arrange
            var props = new Props(_logger)
                .Add("Key1", "Value1")
                .Add("Key2", "Value2");

            // Act
            props.Clear();

            // Assert
            props.Count.ShouldBe(0);
        }

        [Test]
        public void Clear_ShouldReturnPropsForChaining()
        {
            // Arrange
            var props = new Props(_logger)
                .Add("Key1", "Value1");

            // Act
            var result = props.Clear();

            // Assert
            result.ShouldBeSameAs(props);
        }

        [Test]
        public void Clear_OnEmptyProps_ShouldNotThrow()
        {
            // Arrange
            var props = new Props(_logger);

            // Act & Assert
            Should.NotThrow(() => props.Clear());
        }

        #endregion Clear Tests

        #region IDisposable Tests

        [Test]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var props = new Props(_logger).Add("Key", "Value");

            // Act & Assert
            Should.NotThrow(() => props.Dispose());
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var props = new Props(_logger).Add("Key", "Value");

            // Act & Assert
            Should.NotThrow(() =>
            {
                props.Dispose();
                props.Dispose();
            });
        }

        [Test]
        public void Using_ShouldDisposeAutomatically()
        {
            // Act & Assert
            Should.NotThrow(() =>
            {
                using (var props = new Props(_logger).Add("Key", "Value"))
                {
                    props.Count.ShouldBe(1);
                }
            });
        }

        #endregion IDisposable Tests

        #region Dictionary Behavior Tests

        [Test]
        public void ContainsKey_ExistingKey_ShouldReturnTrue()
        {
            // Arrange
            var props = new Props(_logger).Add("ExistingKey", "Value");

            // Act & Assert
            props.ContainsKey("ExistingKey").ShouldBeTrue();
        }

        [Test]
        public void ContainsKey_NonExistingKey_ShouldReturnFalse()
        {
            // Arrange
            var props = new Props(_logger).Add("Key1", "Value");

            // Act & Assert
            props.ContainsKey("NonExistingKey").ShouldBeFalse();
        }

        [Test]
        public void Indexer_ShouldRetrieveValue()
        {
            // Arrange
            var props = new Props(_logger).Add("Key1", "Value1");

            // Act
            var value = props["Key1"];

            // Assert
            value.ShouldBe("Value1");
        }

        [Test]
        public void Indexer_NonExistingKey_ShouldThrow()
        {
            // Arrange
            var props = new Props(_logger);

            // Act & Assert
            Should.Throw<KeyNotFoundException>(() =>
            {
                var value = props["NonExistingKey"];
            });
        }

        #endregion Dictionary Behavior Tests

        #region Complex Scenarios

        [Test]
        public void Props_ComplexWorkflow_ShouldWork()
        {
            // Act
            var props = new Props(_logger)
                .Add("UserId", 123)
                .AddJson("User", new { Name = "John", Age = 30 })
                .Add("SessionId", "abc-123")
                .Clear()
                .Add("NewKey", "NewValue");

            // Assert
            props.Count.ShouldBe(1);
            props["NewKey"].ShouldBe("NewValue");
        }

        [Test]
        public void Props_AsBeginScopeParameter_ShouldBeDictionary()
        {
            // Arrange
            var props = new Props(_logger)
                .Add("Key1", "Value1")
                .Add("Key2", 42);

            // Act - cast to IDictionary to simulate BeginScope usage
            var dict = props as IDictionary<string, object>;

            // Assert
            dict.ShouldNotBeNull();
            dict.Count.ShouldBe(2);
            dict["Key1"].ShouldBe("Value1");
            dict["Key2"].ShouldBe(42);
        }

        #endregion Complex Scenarios
    }
}