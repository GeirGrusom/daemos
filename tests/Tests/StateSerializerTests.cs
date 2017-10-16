using Daemos.Scripting;
using System;
using System.Collections.Generic;
using Xunit;

namespace Daemos.Tests
{
    public class StateSerializerTests
    {

        public static readonly IEnumerable<object[]> AcceptedValues = new[]
        {
            new object[] { (byte)10},
            new object[] { true },
            new object[] { (short)10},
            new object[] { 'z' },
            new object[] { 10 },
            new object[] { 10L },
            new object[] { 10.0f },
            new object[] { 10.0 },
            new object[] { 10M },
            new object[] { "Hello World!"},
            new object[] { new DateTime(2000, 5, 20, 12, 0, 0) },
            new object[] { new DateTimeOffset(2000, 5, 20, 12, 0, 0, new TimeSpan(2, 0, 0)) }
        };

        [Theory]
        [MemberData(nameof(AcceptedValues))]

        public void Serialize_Value_Ok(object value)
        {
            // Arrange
            var stateSerializer = new StateSerializer();
            stateSerializer.Serialize("foo", value);
            stateSerializer.Dispose();
            var stateDeserializer = new StateDeserializer(stateSerializer.GetState());

            // Act
            var result = stateDeserializer.Deserialize("foo", value.GetType());

            // Assert
            Assert.Equal(value, result);
        }

        public class CustomData
        {
            public string Value { get; }
            public CustomData(string value)
            {
                Value = value;
            }
        }

        private static void ThrowArgumentNullException()
        {
            throw new ArgumentNullException("Foo");
        }

        [Fact]
        public void Serialize_Exception_PreservesValues()
        {
            // Arrange
            ArgumentException exception;
            try
            {
                ThrowArgumentNullException();
                throw new InvalidOperationException();
            }
            catch (ArgumentNullException ex)
            {
                exception = ex;
            }
            var exceptionObject = SerializedException.FromException(exception);
            var stateSerializer = new StateSerializer();
            stateSerializer.Serialize("Foo", exceptionObject);
            stateSerializer.Dispose();
            var stateDeserializer = new StateDeserializer(stateSerializer.GetState());

            // Act
            var result = stateDeserializer.Deserialize<SerializedException>("Foo");

            // Assert
            Assert.Equal(exception.Message, result.Message);
            Assert.Equal(exception.StackTrace, result.StackTrace);
            Assert.Equal(typeof(ArgumentNullException), result.ExceptionType);
            Assert.Equal(exception.ParamName, result.AdditionalProperties["ParamName"]);
        }

        [Fact]
        public void Serialize_Object_PreservesData()
        {
            // Arrange
            var stateSerializer = new StateSerializer();
            var serializedObject = new SerializedObject(new CustomData("Foo"));
            stateSerializer.Serialize("Foo", serializedObject);
            stateSerializer.Dispose();
            var stateDeserializer = new StateDeserializer(stateSerializer.GetState());

            // Act
            var result = stateDeserializer.Deserialize<SerializedObject>("Foo");

            // Assert
            Assert.Equal("Foo", result.Values["Value"]);

        }
    }
}
