using System.IO;

namespace JeremyAnsel.Xwa.Snm
{
    public sealed class SnmHeader
    {
        internal const int Size = 1062;

        public SnmHeader()
        {
            this.ImageType = 3;
            this.FrameDelay = 66667;
            this.ColorPalette = new uint[256];
        }

        public short NumFrames { get; set; }

        public short VideoCoordinateX { get; set; }

        public short VideoCoordinateY { get; set; }

        public short Width { get; set; }

        public short Height { get; set; }

        public short ImageType { get; set; }

        public int FrameDelay { get; set; }

        public int MaximumFrameBufferSize { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Reviewed.")]
        public uint[] ColorPalette { get; private set; }

        internal void Read(BinaryReader file)
        {
            byte versionValue1 = file.ReadByte();

            if (versionValue1 != 1)
            {
                throw new InvalidDataException();
            }

            byte versionValue2 = file.ReadByte();

            if (versionValue2 != 0)
            {
                throw new InvalidDataException();
            }

            this.NumFrames = file.ReadInt16();
            this.VideoCoordinateX = file.ReadInt16();
            this.VideoCoordinateY = file.ReadInt16();
            this.Width = file.ReadInt16();
            this.Height = file.ReadInt16();
            this.ImageType = file.ReadInt16();
            this.FrameDelay = file.ReadInt32();
            this.MaximumFrameBufferSize = file.ReadInt32();

            for (int i = 0; i < 256; i++)
            {
                this.ColorPalette[i] = file.ReadUInt32();
            }

            file.ReadBytes(16);
        }

        internal void Write(BinaryWriter file)
        {
            file.Write((byte)1);
            file.Write((byte)0);
            file.Write(this.NumFrames);
            file.Write(this.VideoCoordinateX);
            file.Write(this.VideoCoordinateY);
            file.Write(this.Width);
            file.Write(this.Height);
            file.Write(this.ImageType);
            file.Write(this.FrameDelay);
            file.Write(this.MaximumFrameBufferSize);

            for (int i = 0; i < 256; i++)
            {
                file.Write(this.ColorPalette[i]);
            }

            for (int i = 0; i < 16; i++)
            {
                file.Write((byte)0);
            }
        }
    }
}
