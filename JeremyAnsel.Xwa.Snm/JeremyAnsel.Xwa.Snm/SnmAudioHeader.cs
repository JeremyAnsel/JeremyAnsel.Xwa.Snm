using System.IO;

namespace JeremyAnsel.Xwa.Snm
{
    public sealed class SnmAudioHeader
    {
        internal const int Size = 8;

        public int Frequency { get; set; }

        public int NumChannels { get; set; }

        internal void Read(BinaryReader file, int size)
        {
            this.Frequency = file.ReadInt32();
            this.NumChannels = file.ReadInt32();

            if (size > 8)
            {
                file.ReadBytes(size - 8);
            }
        }

        internal void Write(BinaryWriter file)
        {
            file.Write(this.Frequency);
            file.Write(this.NumChannels);
        }
    }
}
