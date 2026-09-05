using SharpAvi.Output;
using SharpAvi.Codecs;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using AviFile;

namespace JeremyAnsel.Xwa.Snm
{
    public sealed class SnmFile
    {
        public SnmFile()
        {
            this.Header = new SnmHeader();
            this.VideoHeaders = new List<SnmVideoHeader>();
            this.Frames = new List<SnmFrame>();
            this.CurrentFrameId = -1;
        }

        public string? FileName { get; private set; }

        public string? Name
        {
            get
            {
                return Path.GetFileNameWithoutExtension(this.FileName);
            }
        }

        public SnmHeader Header { get; set; }

        public SnmAudioHeader? AudioHeader { get; set; }

        public IList<SnmVideoHeader> VideoHeaders { get; private set; }

        public string Annotation { get; set; } = string.Empty;

        public IList<SnmFrame> Frames { get; private set; }

        public int CurrentFrameId { get; private set; }

        public Blocky16Context? CurrentFrameContext { get; private set; }

        public SnmSubtitlesFile? Subtitles { get; set; }

        public static SnmFile FromFile(string? fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var snm = new SnmFile
            {
                FileName = fileName
            };

            Stream? filestream = null;

            try
            {
                filestream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

                byte[] magic = new byte[2];
                if (filestream.Read(magic, 0, magic.Length) != magic.Length)
                {
                    throw new InvalidDataException();

                }

                filestream.Seek(0, SeekOrigin.Begin);

                if (magic[0] == 0x1F && magic[1] == 0x8B)
                {
                    filestream = new GZipStream(filestream, CompressionMode.Decompress);
                }

                using (BinaryReader file = new BinaryReader(filestream))
                {
                    filestream = null;

                    if (Encoding.ASCII.GetString(file.ReadBytes(4)) != "SANM")
                    {
                        throw new InvalidDataException();
                    }

                    // movie size
                    file.ReadBigEndianInt32();

                    if (Encoding.ASCII.GetString(file.ReadBytes(4)) != "SHDR")
                    {
                        throw new InvalidDataException();
                    }

                    int headerSize = file.ReadBigEndianInt32();

                    if (headerSize != SnmHeader.Size)
                    {
                        throw new InvalidDataException();
                    }

                    snm.Header = new SnmHeader();
                    snm.Header.Read(file);

                    if (Encoding.ASCII.GetString(file.ReadBytes(4)) != "FLHD")
                    {
                        throw new InvalidDataException();
                    }

                    int flhdSize = file.ReadBigEndianInt32();

                    for (int flhdPosition = 0; flhdPosition < flhdSize;)
                    {
                        flhdPosition += 4;

                        if (flhdPosition == flhdSize)
                        {
                            // unknown value
                            file.ReadInt32();
                        }
                        else
                        {
                            string flhdFourcc = Encoding.ASCII.GetString(file.ReadBytes(4));

                            switch (flhdFourcc)
                            {
                                case "Wave":
                                    {
                                        //if (snm.AudioHeader != null)
                                        //{
                                        //    throw new InvalidDataException();
                                        //}

                                        int size = file.ReadBigEndianInt32();
                                        flhdPosition += size + 4;

                                        snm.AudioHeader = new SnmAudioHeader();
                                        snm.AudioHeader.Read(file, size);
                                        break;
                                    }

                                case "Bl16":
                                    {
                                        int size = file.ReadBigEndianInt32();
                                        flhdPosition += size + 4;

                                        SnmVideoHeader header = new SnmVideoHeader();
                                        header.Read(file);

                                        snm.VideoHeaders.Add(header);
                                        break;
                                    }

                                    //default:
                                    //    throw new InvalidDataException();
                            }
                        }
                    }

                    string fourcc = Encoding.ASCII.GetString(file.ReadBytes(4));

                    if (fourcc == "ANNO")
                    {
                        int size = file.ReadBigEndianInt32();
                        snm.Annotation = Encoding.ASCII.GetString(file.ReadBytes(size)).TrimEnd('\0');

                        fourcc = Encoding.ASCII.GetString(file.ReadBytes(4));
                    }

                    for (int i = 0; i < snm.Header.NumFrames; i++)
                    {
                        if (fourcc != "FRME")
                        {
                            throw new InvalidDataException();
                        }

                        SnmFrame frame = new SnmFrame();

                        int frmeSize = file.ReadBigEndianInt32();

                        for (int frmePosition = 0; frmePosition < frmeSize;)
                        {
                            fourcc = Encoding.ASCII.GetString(file.ReadBytes(4));
                            frmePosition += 4;

                            switch (fourcc)
                            {
                                case "Wave":
                                    {
                                        if (frame.Audio != null)
                                        {
                                            throw new InvalidDataException();
                                        }

                                        int size = file.ReadBigEndianInt32();
                                        frmePosition += size + 4;

                                        frame.Audio = new SnmAudioFrame();
                                        frame.Audio.Read(file, size);
                                        break;
                                    }

                                case "Bl16":
                                    {
                                        if (frame.Video != null)
                                        {
                                            throw new InvalidDataException();
                                        }

                                        int size = file.ReadBigEndianInt32();
                                        frmePosition += size + 4;

                                        frame.Video = new SnmVideoFrame();
                                        frame.Video.Read(file, size);
                                        break;
                                    }

                                default:
                                    {
                                        throw new InvalidDataException();
                                    }
                            }
                        }

                        snm.Frames.Add(frame);

                        fourcc = Encoding.ASCII.GetString(file.ReadBytes(4));
                    }
                }
            }
            finally
            {
                filestream?.Dispose();
            }

            string subtitlesFileName = Path.ChangeExtension(fileName, "sub");

            if (File.Exists(subtitlesFileName))
            {
                snm.Subtitles = SnmSubtitlesFile.FromFile(subtitlesFileName);
            }

            return snm;
        }

        public void Save(string? fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            FileStream? filestream = null;

            try
            {
                filestream = new FileStream(fileName, FileMode.Create, FileAccess.Write);

                GZipStream? zip = null;

                try
                {
                    zip = new GZipStream(filestream, CompressionLevel.Optimal);

                    filestream = null;

                    using (BinaryWriter file = new BinaryWriter(zip))
                    {
                        zip = null;

                        file.Write(Encoding.ASCII.GetBytes("SANM"));
                        file.WriteBigEndian(this.ComputeMovieSize());

                        file.Write(Encoding.ASCII.GetBytes("SHDR"));
                        file.WriteBigEndian(SnmHeader.Size);
                        this.Header.Write(file);

                        file.Write(Encoding.ASCII.GetBytes("FLHD"));
                        file.WriteBigEndian(this.ComputeAudioVideoHeadersSize());

                        foreach (SnmVideoHeader video in this.VideoHeaders)
                        {
                            file.Write(Encoding.ASCII.GetBytes("Bl16"));
                            file.WriteBigEndian(SnmVideoHeader.Size);
                            video.Write(file);
                        }

                        if (this.AudioHeader != null)
                        {
                            file.Write(Encoding.ASCII.GetBytes("Wave"));
                            file.WriteBigEndian(SnmAudioHeader.Size);
                            this.AudioHeader.Write(file);
                        }

                        // unknown value
                        // file.Write(0);

                        if (!string.IsNullOrWhiteSpace(this.Annotation))
                        {
                            file.Write(Encoding.ASCII.GetBytes("ANNO"));
                            byte[] bytes = Encoding.ASCII.GetBytes(this.Annotation);
                            file.WriteBigEndian(bytes.Length);
                            file.Write(bytes);
                        }

                        foreach (SnmFrame frame in this.Frames)
                        {
                            file.Write(Encoding.ASCII.GetBytes("FRME"));
                            file.WriteBigEndian(frame.ComputeSize());

                            if (frame.Audio != null)
                            {
                                file.Write(Encoding.ASCII.GetBytes("Wave"));
                                file.WriteBigEndian(frame.Audio.ComputeSize());
                                frame.Audio.Write(file);
                            }

                            if (frame.Video != null)
                            {
                                file.Write(Encoding.ASCII.GetBytes("Bl16"));
                                file.WriteBigEndian(frame.Video.ComputeSize());
                                frame.Video.Write(file);
                            }
                        }

                        this.FileName = fileName;
                    }
                }
                finally
                {
                    zip?.Dispose();
                }
            }
            finally
            {
                filestream?.Dispose();
            }
        }

        private int ComputeMovieSize()
        {
            int size = 0;

            size += 8 + SnmHeader.Size;
            size += 8 + this.ComputeAudioVideoHeadersSize();

            if (!string.IsNullOrWhiteSpace(this.Annotation))
            {
                size += 8 + Encoding.ASCII.GetByteCount(this.Annotation);
            }

            foreach (SnmFrame frame in this.Frames)
            {
                size += 8 + frame.ComputeSize();
            }

            return size;
        }

        private int ComputeAudioVideoHeadersSize()
        {
            int size = 0;

            if (this.AudioHeader != null)
            {
                size += 8 + SnmAudioHeader.Size;
            }

            size += this.VideoHeaders.Count * (8 + SnmVideoHeader.Size);

            // unknown value
            // size += 4;

            return size;
        }

        public void BeginPlay()
        {
            this.CurrentFrameId = -1;

            int bufferSize = this.Header.Width * this.Header.Height * 2;
            this.CurrentFrameContext = new Blocky16Context(bufferSize);
        }

        public void EndPlay()
        {
            this.CurrentFrameId = -1;
            this.CurrentFrameContext = null;
        }

        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#")]
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public bool RetrieveNextFrame(out byte[]? audioData, out byte[]? videoData)
        {
            int nextId = this.CurrentFrameId + 1;

            if (nextId < 0 || nextId >= this.Frames.Count || this.CurrentFrameContext is null)
            {
                audioData = null;
                videoData = null;
                return false;
            }

            this.CurrentFrameId = nextId;

            var frame = this.Frames[nextId];

            if (frame.Audio == null || this.AudioHeader == null)
            {
                audioData = null;
            }
            else
            {
                int decompressedSize = frame.Audio.NumSamples * this.AudioHeader.NumChannels * 2;
                audioData = Imc.Vima.Decompress(frame.Audio.Data, decompressedSize);
            }

            if (frame.Video == null)
            {
                videoData = null;
            }
            else
            {
                videoData = Blocky16.Decompress(frame.Video.Data, frame.Video, this.CurrentFrameContext);
            }

            return true;
        }

#if NET48
        [SuppressMessage("Microsoft.Reliability", "CA2000:Supprimer les objets avant la mise hors de portée")]
        public void SaveAsAviMotionJpeg(string? fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var writer = new AviWriter(fileName)
            {
                FramesPerSecond = (1000000 + this.Header.FrameDelay / 2) / this.Header.FrameDelay,
                EmitIndex1 = true
            };

            try
            {
                IAviVideoStream videoStream = writer.AddMJpegWpfVideoStream(this.Header.Width, this.Header.Height, 100);
                IAviAudioStream? audioStream = this.AudioHeader is null ? null : writer.AddAudioStream(this.AudioHeader.NumChannels, this.AudioHeader.Frequency, 16);

                this.BeginPlay();

                try
                {
                    while (this.RetrieveNextFrame(out byte[]? audio, out byte[]? video))
                    {
                        if (video != null)
                        {
                            byte[] buffer = SnmBufferHelpers.Convert16BppTo32Bpp(video);
                            videoStream.WriteFrame(true, buffer, 0, buffer.Length);
                        }

                        if (audio != null)
                        {
                            audioStream?.WriteBlock(audio, 0, audio.Length);
                        }
                    }
                }
                finally
                {
                    this.EndPlay();
                }
            }
            finally
            {
                writer.Close();
            }
        }
#endif

        [SuppressMessage("Microsoft.Reliability", "CA2000:Supprimer les objets avant la mise hors de portée")]
        public void SaveAsAviMpeg4(string? fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var writer = new AviWriter(fileName)
            {
                FramesPerSecond = (1000000 + this.Header.FrameDelay / 2) / this.Header.FrameDelay,
                EmitIndex1 = true
            };

            try
            {
                IAviVideoStream videoStream = writer.AddMpeg4VcmVideoStream(this.Header.Width, this.Header.Height, (int)writer.FramesPerSecond, 0, 100);
                IAviAudioStream? audioStream = this.AudioHeader is null ? null : writer.AddAudioStream(this.AudioHeader.NumChannels, this.AudioHeader.Frequency, 16);

                this.BeginPlay();

                try
                {
                    while (this.RetrieveNextFrame(out byte[]? audio, out byte[]? video))
                    {
                        if (video != null)
                        {
                            byte[] buffer = SnmBufferHelpers.Convert16BppTo32Bpp(video);
                            videoStream.WriteFrame(true, buffer, 0, buffer.Length);
                        }

                        if (audio != null)
                        {
                            audioStream?.WriteBlock(audio, 0, audio.Length);
                        }
                    }
                }
                finally
                {
                    this.EndPlay();
                }
            }
            finally
            {
                writer.Close();
            }
        }

        public void SaveAsMp4(string? fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            this.SaveAsMp4(fileName, false);
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Mp")]
        public void SaveAsMp4(string? fileName, bool addSubtitles)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            SnmMp4Helpers.Startup();

            try
            {
                SnmMp4Helpers.ConvertWrite(this, fileName, addSubtitles);
            }
            finally
            {
                SnmMp4Helpers.Shutdown();
            }
        }

        public static SnmFile FromAviFile(string? fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException();
            }

            var snm = new SnmFile();
            var aviManager = new AviManager(fileName);

            try
            {
                AudioStream? audioStream = aviManager.GetWaveStream();
                byte[]? audioData = null;

                if (audioStream != null)
                {
                    if (audioStream.ChannelsCount != 2 || audioStream.BitsPerSample != 16)
                    {
                        throw new InvalidDataException();
                    }

                    if (audioStream.SamplesPerSecond != 22050 && audioStream.SamplesPerSecond != 44100)
                    {
                        throw new NotSupportedException();
                    }

                    snm.AudioHeader = new SnmAudioHeader
                    {
                        Frequency = audioStream.SamplesPerSecond,
                        NumChannels = audioStream.ChannelsCount
                    };

                    audioData = audioStream.GetStreamData();

                    if (snm.AudioHeader.Frequency == 44100)
                    {
                        snm.AudioHeader.Frequency = 22050;
                        audioData = SnmBufferHelpers.ConvertAudio44100To22050(audioData);
                    }
                }

                VideoStream? videoStream = aviManager.GetVideoStream();

                if (videoStream != null)
                {
                    if (videoStream.BitsPerPixel != 24 && videoStream.BitsPerPixel != 32)
                    {
                        throw new NotSupportedException();
                    }

                    snm.Header.FrameDelay = (int)(1000000 / videoStream.FrameRate + 0.5);
                    snm.Header.Width = (short)videoStream.Width;
                    snm.Header.Height = (short)videoStream.Height;
                    snm.Header.NumFrames = (short)videoStream.FramesCount;

                    for (int i = 0; i < videoStream.FramesCount; i++)
                    {
                        snm.VideoHeaders.Add(new SnmVideoHeader
                        {
                            Width = snm.Header.Width,
                            Height = snm.Header.Height
                        });
                    }

                    videoStream.GetFrameOpen();

                    try
                    {
                        int fps = (1000000 + snm.Header.FrameDelay / 2) / snm.Header.FrameDelay;
                        int samplesPerFrame = snm.AudioHeader is null ? 0 : snm.AudioHeader.Frequency / fps;

                        if (snm.AudioHeader != null && samplesPerFrame * fps != snm.AudioHeader.Frequency)
                        {
                            throw new InvalidDataException();
                        }

                        for (int i = 0; i < videoStream.FramesCount; i++)
                        {
                            byte[]? videoData = videoStream.GetFrameData(i);

                            var frame = new SnmFrame();

                            if (audioData != null)
                            {
                                int audioPosition = i * samplesPerFrame * 4;
                                int audioLength = Math.Min(samplesPerFrame * 4, audioData.Length - audioPosition);

                                if (audioPosition < audioData.Length && audioLength != 0)
                                {
                                    frame.Audio = new SnmAudioFrame
                                    {
                                        NumSamples = audioLength / 4
                                    };

                                    byte[] buffer = new byte[audioLength];
                                    Array.Copy(audioData, audioPosition, buffer, 0, audioLength);

                                    //frame.Audio.Data = Imc.Vima.Compress(buffer, 2);

                                    frame.Audio.Data = buffer;
                                }
                            }

                            if (videoData != null)
                            {
                                frame.Video = new SnmVideoFrame
                                {
                                    Width = snm.Header.Width,
                                    Height = snm.Header.Height,
                                    RleOutputSize = snm.Header.Width * snm.Header.Height * 2,

                                    SubcodecId = (byte)videoStream.BitsPerPixel,
                                    Data = videoData
                                };

                                //byte[] buffer;

                                //if (videoStream.BitsPerPixel == 24)
                                //{
                                //    buffer = SnmFile.Convert24BppTo16Bpp(videoData);
                                //}
                                //else
                                //{
                                //    buffer = SnmFile.Convert32BppTo16Bpp(videoData);
                                //}

                                //byte subcodecId;
                                //frame.Video.Data = Blocky16.Compress(buffer, out subcodecId);
                                //frame.Video.SubcodecId = subcodecId;
                            }

                            snm.Frames.Add(frame);
                        }
                    }
                    finally
                    {
                        videoStream.GetFrameClose();
                    }
                }
            }
            finally
            {
                aviManager.Close();
            }

            snm.Frames
                .AsParallel()
                .ForAll(frame =>
                {
                    if (frame.Audio != null)
                    {
                        frame.Audio.Data = Imc.Vima.Compress(frame.Audio.Data, 2);
                    }

                    if (frame.Video != null)
                    {
                        byte[] buffer;

                        if (frame.Video.SubcodecId == 24)
                        {
                            buffer = SnmBufferHelpers.Convert24BppTo16Bpp(frame.Video.Data);
                        }
                        else
                        {
                            buffer = SnmBufferHelpers.Convert32BppTo16Bpp(frame.Video.Data);
                        }

                        frame.Video.Data = Blocky16.Compress(buffer, out byte subcodecId);
                        frame.Video.SubcodecId = subcodecId;
                    }
                });

            return snm;
        }

        public static SnmFile FromMFFile(string? fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException();
            }

            SnmMp4Helpers.Startup();

            try
            {
                return SnmMp4Helpers.ConvertRead(fileName);
            }
            finally
            {
                SnmMp4Helpers.Shutdown();
            }
        }
    }
}
