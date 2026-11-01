// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Utilities.WebRequestRest
{
    /// <summary>
    /// Progress information for a request (e.g. download position, length, speed).
    /// </summary>
    public readonly struct Progress
    {
        /// <summary>
        /// Unit for reporting progress (bytes, KB, MB, GB, TB).
        /// </summary>
        public enum DataUnit
        {
            /// <summary>
            /// bytes
            /// </summary>
            b,
            /// <summary>
            /// kilobytes
            /// </summary>
            kB,
            /// <summary>
            /// megabytes
            /// </summary>
            MB,
            /// <summary>
            /// Gigabytes
            /// </summary>
            GB,
            /// <summary>
            /// Terabytes
            /// </summary>
            TB
        }

        /// <summary>
        /// Initializes progress with position, length, percentage, speed, and unit.
        /// </summary>
        public Progress(ulong position, ulong length, float progress, float speed, DataUnit unit)
        {
            Position = position;
            Length = length;
            Percentage = progress;
            Speed = speed;
            Unit = unit;
        }

        /// <summary>Current position (e.g. bytes downloaded).</summary>
        public ulong Position { get; }

        /// <summary>Total length (e.g. total bytes to download).</summary>
        public ulong Length { get; }

        /// <summary>Progress as a percentage (0–100).</summary>
        public float Percentage { get; }

        /// <summary>Current speed in <see cref="Unit"/> per second.</summary>
        public float Speed { get; }

        /// <summary>Unit for <see cref="Position"/>, <see cref="Length"/>, and <see cref="Speed"/>.</summary>
        public DataUnit Unit { get; }
    }
}
