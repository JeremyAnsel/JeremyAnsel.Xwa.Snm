using System.IO;

namespace JeremyAnsel.Xwa.Snm
{
    internal static class BinaryWriterExtensions
    {
        public static void WriteBigEndian(this BinaryWriter file, int value)
        {
            file.Write((byte)((uint)value >> 24));
            file.Write((byte)((uint)value >> 16));
            file.Write((byte)((uint)value >> 8));
            file.Write((byte)((uint)value));
        }
    }
}
