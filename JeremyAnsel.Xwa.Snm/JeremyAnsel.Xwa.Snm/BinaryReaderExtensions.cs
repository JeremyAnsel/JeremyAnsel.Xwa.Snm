using System.IO;

namespace JeremyAnsel.Xwa.Snm
{
    internal static class BinaryReaderExtensions
    {
        public static int ReadBigEndianInt32(this BinaryReader file)
        {
            return (int)((file.ReadByte() << 24) | (file.ReadByte() << 16) | (file.ReadByte() << 8) | file.ReadByte());
        }
    }
}
