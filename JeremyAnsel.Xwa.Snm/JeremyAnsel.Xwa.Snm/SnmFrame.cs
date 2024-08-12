namespace JeremyAnsel.Xwa.Snm
{
    public sealed class SnmFrame
    {
        public SnmAudioFrame? Audio { get; set; }

        public SnmVideoFrame? Video { get; set; }

        internal int ComputeSize()
        {
            int size = 0;

            if (this.Audio != null)
            {
                size += 8 + this.Audio.ComputeSize();
            }

            if (this.Video != null)
            {
                size += 8 + this.Video.ComputeSize();
            }

            return size;
        }
    }
}
