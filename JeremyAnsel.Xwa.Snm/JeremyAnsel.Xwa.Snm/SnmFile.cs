using SharpAvi.Output;
using SharpAvi.Codecs;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

        public string FileName { get; private set; }

        public string Name
        {
            get
            {
                return Path.GetFileNameWithoutExtension(this.FileName);
            }
        }

        public SnmHeader Header { get; set; }

        public SnmAudioHeader AudioHeader { get; set; }

        public IList<SnmVideoHeader> VideoHeaders { get; private set; }

        public string Annotation { get; set; }

        public IList<SnmFrame> Frames { get; private set; }

        public int CurrentFrameId { get; private set; }

        public Blocky16Context CurrentFrameContext { get; private set; }

        public static SnmFile FromFile(string fileName)
        {
            var snm = new SnmFile();

            snm.FileName = fileName;

            Stream filestream = null;

            try
            {
                filestream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

                byte[] magic = new byte[2];
                filestream.Read(magic, 0, 2);
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
                                        if (snm.AudioHeader != null)
                                        {
                                            throw new InvalidDataException();
                                        }

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

                                default:
                                    throw new InvalidDataException();
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
                if (filestream != null)
                {
                    filestream.Dispose();
                }
            }

            return snm;
        }

        public void Save(string fileName)
        {
            FileStream filestream = null;

            try
            {
                filestream = new FileStream(fileName, FileMode.Create, FileAccess.Write);

                GZipStream zip = null;

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
                    if (zip != null)
                    {
                        zip.Dispose();
                    }
                }
            }
            finally
            {
                if (filestream != null)
                {
                    filestream.Dispose();
                }
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
        public bool RetrieveNextFrame(out byte[] audioData, out byte[] videoData)
        {
            int nextId = this.CurrentFrameId + 1;

            if (nextId < 0 || nextId >= this.Frames.Count)
            {
                audioData = null;
                videoData = null;
                return false;
            }

            this.CurrentFrameId = nextId;

            var frame = this.Frames[nextId];

            if (frame.Audio == null)
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

        [SuppressMessage("Microsoft.Reliability", "CA2000:Supprimer les objets avant la mise hors de portée")]
        public void SaveAsAvi(string fileName)
        {
            var writer = new AviWriter(fileName)
            {
                FramesPerSecond = (1000000 + this.Header.FrameDelay / 2) / this.Header.FrameDelay,
                EmitIndex1 = true
            };

            try
            {
                IAviVideoStream videoStream = writer.AddMotionJpegVideoStream(this.Header.Width, this.Header.Height, 70);
                IAviAudioStream audioStream = writer.AddAudioStream(this.AudioHeader.NumChannels, this.AudioHeader.Frequency, 16);

                this.BeginPlay();

                try
                {
                    byte[] audio;
                    byte[] video;

                    while (this.RetrieveNextFrame(out audio, out video))
                    {
                        if (video != null)
                        {
                            byte[] buffer = SnmFile.Convert16BppTo32Bpp(video);
                            videoStream.WriteFrame(true, buffer, 0, buffer.Length);
                        }

                        if (audio != null)
                        {
                            audioStream.WriteBlock(audio, 0, audio.Length);
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

        public static SnmFile FromAviFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException();
            }

            var snm = new SnmFile();
            var aviManager = new AviManager(fileName);

            try
            {
                AudioStream audioStream = aviManager.GetWaveStream();
                byte[] audioData = null;

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

                    snm.AudioHeader = new SnmAudioHeader();
                    snm.AudioHeader.Frequency = audioStream.SamplesPerSecond;
                    snm.AudioHeader.NumChannels = audioStream.ChannelsCount;

                    audioData = audioStream.GetStreamData();

                    if (snm.AudioHeader.Frequency == 44100)
                    {
                        snm.AudioHeader.Frequency = 22050;
                        audioData = SnmFile.ConvertAudio44100To22050(audioData);
                    }
                }

                VideoStream videoStream = aviManager.GetVideoStream();

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
                    int samplesPerFrame = snm.AudioHeader.Frequency / fps;

                    if (samplesPerFrame * fps != snm.AudioHeader.Frequency)
                    {
                        throw new InvalidDataException();
                    }

                    for (int i = 0; i < videoStream.FramesCount; i++)
                    {
                        byte[] videoData = videoStream.GetFrameData(i);

                        var frame = new SnmFrame();

                        if (audioData != null)
                        {
                            int audioPosition = i * samplesPerFrame * 4;
                            int audioLength = Math.Min(samplesPerFrame * 4, audioData.Length - audioPosition);

                            if (audioPosition < audioData.Length && audioLength != 0)
                            {
                                frame.Audio = new SnmAudioFrame();
                                frame.Audio.NumSamples = audioLength / 4;

                                byte[] buffer = new byte[audioLength];
                                Array.Copy(audioData, audioPosition, buffer, 0, audioLength);

                                //frame.Audio.Data = Imc.Vima.Compress(buffer, 2);

                                frame.Audio.Data = buffer;
                            }
                        }

                        if (videoData != null)
                        {
                            frame.Video = new SnmVideoFrame();
                            frame.Video.Width = snm.Header.Width;
                            frame.Video.Height = snm.Header.Height;
                            frame.Video.RleOutputSize = snm.Header.Width * snm.Header.Height * 2;

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

                            frame.Video.SubcodecId = (byte)videoStream.BitsPerPixel;
                            frame.Video.Data = videoData;
                        }

                        snm.Frames.Add(frame);
                    }
                }
                finally
                {
                    videoStream.GetFrameClose();
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
                            buffer = SnmFile.Convert24BppTo16Bpp(frame.Video.Data);
                        }
                        else
                        {
                            buffer = SnmFile.Convert32BppTo16Bpp(frame.Video.Data);
                        }

                        byte subcodecId;
                        frame.Video.Data = Blocky16.Compress(buffer, out subcodecId);
                        frame.Video.SubcodecId = subcodecId;
                    }
                });

            return snm;
        }

        private static byte[] ConvertAudio44100To22050(byte[] audioData)
        {
            byte[] buffer = new byte[audioData.Length / 8 * 4];

            for (int i = 0, j = 0; i <= audioData.Length - 8; i += 8, j += 4)
            {
                short val00 = BitConverter.ToInt16(audioData, i);
                short val01 = BitConverter.ToInt16(audioData, i + 2);
                short val10 = BitConverter.ToInt16(audioData, i + 4);
                short val11 = BitConverter.ToInt16(audioData, i + 6);

                short val0 = (short)((val00 + val10) / 2);
                short val1 = (short)((val01 + val11) / 2);

                buffer[j] = (byte)(val0 & 0xff);
                buffer[j + 1] = (byte)((val0 >> 8) & 0xff);
                buffer[j + 2] = (byte)(val1 & 0xff);
                buffer[j + 3] = (byte)((val1 >> 8) & 0xff);
            }

            return buffer;
        }

        private static byte[] Convert16BppTo32Bpp(byte[] bytes)
        {
            int length = bytes.Length * 2;
            var buffer = new byte[length];

            for (int i = 0, j = 0; i < length; i += 4, j += 2)
            {
                ushort c = BitConverter.ToUInt16(bytes, j);

                byte r = (byte)((c & 0xF800) >> 11);
                byte g = (byte)((c & 0x7E0) >> 5);
                byte b = (byte)(c & 0x1F);

                r = (byte)((r * (0xffU * 2) + 0x1fU) / (0x1fU * 2));
                g = (byte)((g * (0xffU * 2) + 0x3fU) / (0x3fU * 2));
                b = (byte)((b * (0xffU * 2) + 0x1fU) / (0x1fU * 2));

                buffer[i] = b;
                buffer[i + 1] = g;
                buffer[i + 2] = r;
                buffer[i + 3] = 0xff;
            }

            return buffer;
        }

        private static byte[] Convert24BppTo16Bpp(byte[] bytes)
        {
            int length = bytes.Length / 3;
            var buffer = new byte[length * 2];

            for (int i = 0; i < length; i++)
            {
                uint b = bytes[i * 3 + 2];
                uint g = bytes[i * 3 + 1];
                uint r = bytes[i * 3];

                b = (b * (0x1fU * 2) + 0xffU) / (0xffU * 2);
                g = (g * (0x3fU * 2) + 0xffU) / (0xffU * 2);
                r = (r * (0x1fU * 2) + 0xffU) / (0xffU * 2);

                ushort c = (ushort)((b << 11) | (g << 5) | r);
                buffer[i * 2] = (byte)(c & 0xff);
                buffer[i * 2 + 1] = (byte)((c >> 8) & 0xff);
            }

            return buffer;
        }

        private static byte[] Convert32BppTo16Bpp(byte[] bytes)
        {
            int length = bytes.Length / 4;
            var buffer = new byte[length * 2];

            for (int i = 0; i < length; i++)
            {
                uint b = bytes[i * 4 + 2];
                uint g = bytes[i * 4 + 1];
                uint r = bytes[i * 4];

                b = (b * (0x1fU * 2) + 0xffU) / (0xffU * 2);
                g = (g * (0x3fU * 2) + 0xffU) / (0xffU * 2);
                r = (r * (0x1fU * 2) + 0xffU) / (0xffU * 2);

                ushort c = (ushort)((b << 11) | (g << 5) | r);
                buffer[i * 2] = (byte)(c & 0xff);
                buffer[i * 2 + 1] = (byte)((c >> 8) & 0xff);
            }

            return buffer;
        }
    }
}
