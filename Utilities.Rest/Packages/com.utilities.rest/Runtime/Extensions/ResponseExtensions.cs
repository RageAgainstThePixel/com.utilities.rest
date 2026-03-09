// Licensed under the MIT License. See LICENSE in the project root for license information.

using Unity.Collections;

namespace Utilities.WebRequestRest
{
    /// <summary>
    /// Extension methods for <see cref="Response"/>.
    /// </summary>
    public static class ResponseExtensions
    {
        /// <summary>
        /// Copies the response's <see cref="Response.NativeData"/> into a new <see cref="NativeArray{T}"/> with the specified allocator.
        /// The caller is responsible for disposing the returned array when done.
        /// </summary>
        /// <param name="response">
        /// The response whose native data to copy.
        /// </param>
        /// <param name="allocator">
        /// The allocator to use for the new array (e.g. <see cref="Allocator.Temp"/>, <see cref="Allocator.TempJob"/>, <see cref="Allocator.Persistent"/>).
        /// </param>
        /// <returns>
        /// A new native array containing a copy of the response data, or an empty array if the response has no native data. Must be disposed by the caller.
        /// </returns>
        public static NativeArray<byte> CopyNativeData(this Response response, Allocator allocator)
        {
            if (response == null ||
                !response.NativeData.HasValue ||
                !response.NativeData.Value.IsCreated)
            {
                return new NativeArray<byte>(0, allocator);
            }

            var source = response.NativeData.Value;
            var copy = new NativeArray<byte>(source.Length, allocator);
            copy.CopyFrom(source);
            return copy;
        }
    }
}
