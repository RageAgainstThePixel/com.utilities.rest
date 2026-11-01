// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Utilities.Rest.Extensions
{
    /// <summary>
    /// JSON.NET value provider that converts empty or whitespace strings to null during serialization/deserialization.
    /// https://stackoverflow.com/questions/39855694/convert-empty-strings-to-null-with-json-net
    /// </summary>
    public sealed class EmptyToNullStringValueProvider : IValueProvider
    {
        private readonly PropertyInfo memberInfo;

        /// <summary>
        /// Creates a value provider for the given property.
        /// </summary>
        /// <param name="memberInfo">The property to read/write.</param>
        public EmptyToNullStringValueProvider(PropertyInfo memberInfo) => this.memberInfo = memberInfo;

        /// <inheritdoc />
        public object GetValue(object target)
        {
            var result = memberInfo.GetValue(target);

            if (memberInfo.PropertyType == typeof(string) &&
                result is string s &&
                string.IsNullOrWhiteSpace(s))
            {
                result = null;
            }

            return result;
        }

        /// <inheritdoc />
        public void SetValue(object target, object value)
        {
            if (memberInfo.PropertyType == typeof(string) &&
                value is string s &&
                string.IsNullOrWhiteSpace(s))
            {
                value = null;
            }

            memberInfo.SetValue(target, value);
        }
    }
}
