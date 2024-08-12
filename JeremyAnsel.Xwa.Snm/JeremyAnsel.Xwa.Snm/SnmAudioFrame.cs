using System.IO;

namespace JeremyAnsel.Xwa.Snm
{
    public sealed class SnmAudioFrame
    {
        public int NumSamples { get; set; }

        public int FileId { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Reviewed.")]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        internal int ComputeSize()
        {
            return (this.FileId == 0 ? 4 : 12) + this.Data.Length;
        }

        internal void Read(BinaryReader file, int size)
        {
            this.NumSamples = file.ReadBigEndianInt32();

            if (this.NumSamples < 0)
            {
                this.FileId = file.ReadBigEndianInt32();
                this.NumSamples = file.ReadBigEndianInt32();
                size -= 8;
            }

            this.Data = file.ReadBytes(size - 4);
        }

        internal void Write(BinaryWriter file)
        {
            if (this.FileId == 0)
            {
                file.WriteBigEndian(this.NumSamples);
            }
            else
            {
                file.WriteBigEndian(-1);
                file.WriteBigEndian(this.FileId);
                file.WriteBigEndian(this.NumSamples);
            }

            file.Write(this.Data);
        }
    }
}
