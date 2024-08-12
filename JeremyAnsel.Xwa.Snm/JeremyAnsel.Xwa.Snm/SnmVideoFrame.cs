using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JeremyAnsel.Xwa.Snm
{
    public sealed class SnmVideoFrame
    {
        public SnmVideoFrame()
        {
            this.SmallCodebook = new ushort[4];
            this.Codebook = new ushort[256];
        }

        public int Width { get; set; }

        public int Height { get; set; }

        public short Sequence { get; set; }

        public byte SubcodecId { get; set; }

        public byte DiffBufferRotateCode { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Reviewed.")]
        public ushort[] SmallCodebook { get; private set; }

        public uint BackgroundColor { get; set; }

        public int RleOutputSize { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Reviewed.")]
        public ushort[] Codebook { get; private set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Reviewed.")]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        internal int ComputeSize()
        {
            return 8 + 12 + 4 + 8 + 8 + 512 + 8 + this.Data.Length;
        }

        internal void Read(BinaryReader file, int size)
        {
            file.ReadBytes(8);

            this.Width = file.ReadInt32();
            this.Height = file.ReadInt32();
            this.Sequence = file.ReadInt16();
            this.SubcodecId = file.ReadByte();
            this.DiffBufferRotateCode = file.ReadByte();

            file.ReadBytes(4);

            for (int i = 0; i < 4; i++)
            {
                this.SmallCodebook[i] = file.ReadUInt16();
            }

            this.BackgroundColor = file.ReadUInt32();
            this.RleOutputSize = file.ReadInt32();

            for (int i = 0; i < 256; i++)
            {
                this.Codebook[i] = file.ReadUInt16();
            }

            // (int)3, (int)0
            file.ReadBytes(8);

            this.Data = file.ReadBytes(size - 560);
        }

        internal void Write(BinaryWriter file)
        {
            for (int i = 0; i < 8; i++)
            {
                file.Write((byte)0);
            }

            file.Write(this.Width);
            file.Write(this.Height);
            file.Write(this.Sequence);
            file.Write(this.SubcodecId);
            file.Write(this.DiffBufferRotateCode);

            file.Write(0);

            for (int i = 0; i < 4; i++)
            {
                file.Write(this.SmallCodebook[i]);
            }

            file.Write(this.BackgroundColor);
            file.Write(this.RleOutputSize);

            for (int i = 0; i < 256; i++)
            {
                file.Write(this.Codebook[i]);
            }

            file.Write(3);
            file.Write(0);

            file.Write(this.Data);
        }
    }
}
