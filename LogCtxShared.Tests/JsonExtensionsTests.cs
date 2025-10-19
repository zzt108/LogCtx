// tests/LogCtxShared.Tests/JsonExtensionsTests.cs

using NUnit.Framework;
using Shouldly;
using LogCtxShared;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LogCtxShared.Tests
{
    [TestFixture]
    [Category("unit")]
    public class JsonExtensionsTests
    {
        [Test]
        public void AsJson_WithCompactFormat_ReturnsMinifiedJson()
        {
            // Arrange
            var obj = new { Name = "Test", Value = 42 };

            // Act
            var result = obj.AsJson(false);

            // Assert
            result.ShouldNotContain("\n");
            result.ShouldContain("\"Name\":\"Test\"");
            result.ShouldContain("\"Value\":42");
        }

        [Test]
        public void AsJson_WithIndentedFormat_ReturnsFormattedJsonWithNewline()
        {
            // Arrange
            var obj = new { Name = "Test", Value = 42 };

            // Act
            var result = obj.AsJson(true);

            // Assert
            result.ShouldContain("\n");
            result.ShouldEndWith("\n");
            result.ShouldContain("\"Name\": \"Test\"");
            result.ShouldContain("\"Value\": 42");
        }

        [Test]
        public void AsJson_WithNullObject_ReturnsNullString()
        {
            // Arrange
            object? obj = null;

            // Act
            var result = obj?.AsJson(false);

            // Assert
            result.ShouldBe("null");
        }

        [Test]
        public void AsJson_WithComplexObject_SerializesCorrectly()
        {
            // Arrange
            var obj = new
            {
                Id = 1,
                Items = new List<string> { "A", "B", "C" },
                Nested = new { Key = "Value" }
            };

            // Act
            var result = obj.AsJson(false);

            // Assert
            result.ShouldContain("\"Id\":1");
            result.ShouldContain("\"Items\":[\"A\",\"B\",\"C\"]");
            result.ShouldContain("\"Key\":\"Value\"");
        }

        [Test]
        public void AsJsonDiagram_WithObject_WrapsInPlantUmlMarkers()
        {
            // Arrange
            var obj = new { Status = "Active" };

            // Act
            var result = obj.AsJsonDiagram();

            // Assert
            result.ShouldStartWith("@startjson");
            result.ShouldEndWith("@endjson\n");
            result.ShouldContain("\"Status\": \"Active\"");
        }

        [Test]
        public void AsJsonDiagram_WithTypeName_IncludesTypeInOutput()
        {
            // Arrange
            var obj = new { Status = "Active" };

            // Act
            var result = obj.AsJsonDiagram();

            // Assert
            result.ShouldContain("<>"); // Type name placeholder pattern
        }

        [Test]
        public void AsJsonEmbedded_WithObject_WrapsInPlantUmlMarkersWithIndentation()
        {
            // Arrange
            var obj = new { Config = "Value" };

            // Act
            var result = obj.AsJsonEmbedded();

            // Assert
            result.ShouldStartWith("json \"");
            result.ShouldContain("\" as J{");
            result.ShouldContain("  \"Config\": \"Value\""); // Check indented content
            result.ShouldEndWith("}\n");
            // Check indentation exists
            // Verify structure has nested braces (outer for PlantUML, inner for JSON)
            var lines = result.Split('\n');
            lines.Length.ShouldBeGreaterThan(3); // Should have multiple lines with indentation
        }

        [Test]
        public void FromJson_WithValidJson_DeserializesCorrectly()
        {
            // Arrange
            var json = "{\"Name\":\"Test\",\"Value\":42}";

            // Act
            var result = JsonExtensions.FromJson<TestDto>(json);

            // Assert
            result.ShouldNotBeNull();
            result.Name.ShouldBe("Test");
            result.Value.ShouldBe(42);
        }

        [Test]
        public void FromJson_WithInvalidJson_ThrowsException()
        {
            // Arrange
            var json = "{invalid json}";

            // Act & Assert
            Should.Throw<Exception>(() => JsonExtensions.FromJson<TestDto>(json));
        }

        [Test]
        public void FromJson_WithCollection_DeserializesCorrectly()
        {
            // Arrange
            var json = "[{\"Name\":\"A\",\"Value\":1},{\"Name\":\"B\",\"Value\":2}]";

            // Act
            var result = JsonExtensions.FromJson<List<TestDto>>(json);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);
            result[0].Name.ShouldBe("A");
            result[1].Value.ShouldBe(2);
        }

        [Test]
        public void FromJson_WithNullJson_ThrowsException()
        {
            // Arrange
            string? json = null;

            // Act & Assert
            Should.Throw<Exception>(() => JsonExtensions.FromJson<TestDto>(json));
        }

        [Test]
        public void Link_ReturnsFormattedCallerInfo()
        {
            // Act
            var result = JsonExtensions.Link();

            // Assert
            result.ShouldNotBeNullOrEmpty();
            // Pattern: path(line):WT@F or FileName.cs(line):WT@F
            var pattern = @".+\(\d+\):WT@F$";
            Regex.IsMatch(result, pattern).ShouldBeTrue($"Expected pattern '{pattern}' but got '{result}'");
        }

        [Test]
        public void Link_ContainsCallerMethodInfo()
        {
            // Act
            var result = JsonExtensions.Link();

            // Assert
            result.ShouldContain(":");
            result.ShouldContain("WT@F");
            result.ShouldContain("(");
            result.ShouldContain(")");
        }

        [Test]
        public void Link_CalledFromDifferentMethods_ReturnsDifferentInfo()
        {
            // Act
            var result1 = HelperMethod1();
            var result2 = HelperMethod2();

            // Assert
            result1.ShouldNotBe(result2);
        }

        private string HelperMethod1() => JsonExtensions.Link();
        private string HelperMethod2() => JsonExtensions.Link();

        // Test DTO
        private class TestDto
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }
    }
}
