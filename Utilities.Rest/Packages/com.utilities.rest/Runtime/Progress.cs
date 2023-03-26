using System.Diagnostics.CodeAnalysis;

namespace Utilities.WebRequestRest
{
    public readonly struct Progress
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
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

        public Progress(ulong position, ulong length, float progress, float speed, DataUnit unit)
        {
            Position = position;
            Length = length;
            Percentage = progress;
            Speed = speed;
            Unit = unit;
        }

        public ulong Position { get; }

        public ulong Length { get; }

        public float Percentage { get; }

        public float Speed { get; }

        public DataUnit Unit { get; }
    }
}
