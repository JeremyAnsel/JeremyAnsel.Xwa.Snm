using AviFile;
using MediaFoundation;
using MediaFoundation.ReadWrite;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace JeremyAnsel.Xwa.Snm
{
    internal static class SnmMp4Helpers
    {
        private const int MF_SOURCE_READER_FIRST_VIDEO_STREAM = unchecked((int)0xFFFFFFFC);

        private const int MF_SOURCE_READER_FIRST_AUDIO_STREAM = unchecked((int)0xFFFFFFFD);

        private const int MF_SOURCE_READER_ANY_STREAM = unchecked((int)0xFFFFFFFE);

        public static void Startup()
        {
            Marshal.ThrowExceptionForHR((int)MFExtern.MFStartup(0x20070, MFStartup.Full));
        }

        public static void Shutdown()
        {
            Marshal.ThrowExceptionForHR((int)MFExtern.MFShutdown());
        }

        public static void ConvertWrite(SnmFile snm, string fileName)
        {
            if (snm == null)
            {
                throw new ArgumentNullException(nameof(snm));
            }

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            int fps = (1000000 + snm.Header.FrameDelay / 2) / snm.Header.FrameDelay;
            long frameDuration = 10 * 1000 * 1000 / fps;

            IMFSinkWriter writer;
            int videoStream;
            int audioStream;
            InitializeSinkWriter(
                fileName,
                snm.Header.Width,
                snm.Header.Height,
                fps,
                out writer,
                out videoStream,
                out audioStream);

            try
            {
                snm.BeginPlay();

                try
                {
                    long rtStartVideo = 0;
                    long rtStartAudio = 0;

                    byte[] audioData;
                    byte[] videoData;
                    while (snm.RetrieveNextFrame(out audioData, out videoData))
                    {
                        if (videoData != null)
                        {
                            WriteVideoFrame(snm.Header.Width, snm.Header.Height, frameDuration, writer, videoStream, rtStartVideo, videoData);
                            rtStartVideo += frameDuration;
                        }

                        if (audioData != null)
                        {
                            byte[] buffer = ConvertAudio22050To44100(audioData);
                            long duration = (long)(10 * 1000 * 1000) * audioData.Length / (22050 * 4);
                            WriteAudioFrame(duration, writer, audioStream, rtStartAudio, buffer);
                            rtStartAudio += duration;
                        }
                    }
                }
                finally
                {
                    snm.EndPlay();
                }

                writer.Finalize_();
            }
            finally
            {
                Marshal.ReleaseComObject(writer);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static SnmFile ConvertRead(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var snm = new SnmFile();

            byte[] aviAudioDataBytes;
            try
            {
                aviAudioDataBytes = GetAviAudioBytes(fileName);
            }
            catch
            {
                aviAudioDataBytes = null;
            }

            IMFSourceReader reader;
            int videoStreamIndex;
            int audioStreamIndex;
            InitializeSourceReader(fileName, out reader, out videoStreamIndex, out audioStreamIndex);

            var audioData = new MemoryStream();

            try
            {
                int width;
                int height;
                int fps;
                GetSourceReaderAttributes(reader, out width, out height, out fps);

                snm.AudioHeader = new SnmAudioHeader();
                snm.AudioHeader.Frequency = 22050;
                snm.AudioHeader.NumChannels = 2;

                snm.Header.FrameDelay = (int)(1000000 / fps + 0.5);
                snm.Header.Width = (short)width;
                snm.Header.Height = (short)height;
                snm.Header.NumFrames = 0;

                while (true)
                {
                    int streamIndex;
                    long timestamp;
                    byte[] bytes = ReadSample(reader, audioStreamIndex, out streamIndex, out timestamp);

                    if (bytes == null)
                    {
                        break;
                    }

                    audioData.Write(bytes, 0, bytes.Length);
                }

                if (audioData.Length == 0 && aviAudioDataBytes != null)
                {
                    audioData.Write(aviAudioDataBytes, 0, aviAudioDataBytes.Length);
                }

                while (true)
                {
                    int streamIndex;
                    long timestamp;
                    byte[] bytes = ReadSample(reader, videoStreamIndex, out streamIndex, out timestamp);

                    if (bytes == null)
                    {
                        break;
                    }

                    snm.VideoHeaders.Add(new SnmVideoHeader
                    {
                        Width = snm.Header.Width,
                        Height = snm.Header.Height
                    });

                    var frame = new SnmFrame();

                    int audioPosition = snm.Header.NumFrames * snm.AudioHeader.Frequency / fps * 4;
                    int audioLength = Math.Min(snm.AudioHeader.Frequency / fps * 4, (int)audioData.Length - audioPosition);

                    if (audioPosition < audioData.Length && audioLength != 0)
                    {
                        frame.Audio = new SnmAudioFrame();
                        frame.Audio.NumSamples = audioLength / 4;

                        byte[] buffer = new byte[audioLength];
                        audioData.Seek(audioPosition, SeekOrigin.Begin);
                        audioData.Read(buffer, 0, buffer.Length);

                        frame.Audio.Data = buffer;
                    }

                    frame.Video = new SnmVideoFrame();
                    frame.Video.Width = snm.Header.Width;
                    frame.Video.Height = snm.Header.Height;
                    frame.Video.RleOutputSize = snm.Header.Width * snm.Header.Height * 2;
                    frame.Video.SubcodecId = 32;
                    frame.Video.Data = bytes;

                    snm.Frames.Add(frame);
                    snm.Header.NumFrames++;
                }
            }
            finally
            {
                audioData.Dispose();
                Marshal.ReleaseComObject(reader);
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
                        byte[] buffer = Convert32BppTo16Bpp(frame.Video.Data);

                        byte subcodecId;
                        frame.Video.Data = Blocky16.Compress(buffer, out subcodecId);
                        frame.Video.SubcodecId = subcodecId;
                    }
                });

            return snm;
        }

        private static byte[] GetAviAudioBytes(string fileName)
        {
            var aviManager = new AviManager(fileName);
            byte[] bytes = null;

            try
            {
                AudioStream audioStream = aviManager.GetWaveStream();

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

                    bytes = audioStream.GetStreamData();

                    if (audioStream.SamplesPerSecond == 44100)
                    {
                        bytes = ConvertAudio44100To22050(bytes);
                    }
                }
            }
            finally
            {
                aviManager.Close();
            }

            return bytes;
        }

        private static void InitializeSinkWriter(string outputUrl, int width, int height, int fps, out IMFSinkWriter writer, out int videoStreamIndex, out int audioStreamIndex)
        {
            IMFAttributes attributes;
            Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateAttributes(out attributes, 0));

            try
            {
                Marshal.ThrowExceptionForHR((int)attributes.SetUINT32(MFAttributesClsid.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1));
                Marshal.ThrowExceptionForHR((int)attributes.SetUINT32(MFAttributesClsid.MF_SINK_WRITER_DISABLE_THROTTLING, 1));

                Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateSinkWriterFromURL(outputUrl, null, attributes, out writer));
            }
            finally
            {
                Marshal.ReleaseComObject(attributes);
            }

            try
            {
                // Set the video output media type.
                IMFMediaType videoMediaTypeOut;
                Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateMediaType(out videoMediaTypeOut));

                try
                {
                    Marshal.ThrowExceptionForHR((int)videoMediaTypeOut.SetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, MFMediaType.Video));
                    Marshal.ThrowExceptionForHR((int)videoMediaTypeOut.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, MFMediaType.H264));
                    Marshal.ThrowExceptionForHR((int)videoMediaTypeOut.SetUINT32(MFAttributesClsid.MF_MT_AVG_BITRATE, 800000));
                    Marshal.ThrowExceptionForHR((int)videoMediaTypeOut.SetUINT32(MFAttributesClsid.MF_MT_INTERLACE_MODE, (int)MFVideoInterlaceMode.Progressive));
                    Marshal.ThrowExceptionForHR((int)MFExtern.MFSetAttributeSize(videoMediaTypeOut, MFAttributesClsid.MF_MT_FRAME_SIZE, width, height));
                    Marshal.ThrowExceptionForHR((int)MFExtern.MFSetAttributeRatio(videoMediaTypeOut, MFAttributesClsid.MF_MT_FRAME_RATE, fps, 1));
                    Marshal.ThrowExceptionForHR((int)MFExtern.MFSetAttributeRatio(videoMediaTypeOut, MFAttributesClsid.MF_MT_PIXEL_ASPECT_RATIO, 1, 1));
                    Marshal.ThrowExceptionForHR((int)writer.AddStream(videoMediaTypeOut, out videoStreamIndex));
                }
                finally
                {
                    Marshal.ReleaseComObject(videoMediaTypeOut);
                }

                // Set the video input media type.
                IMFMediaType videoMediaTypeIn;
                Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateMediaType(out videoMediaTypeIn));

                try
                {
                    Marshal.ThrowExceptionForHR((int)videoMediaTypeIn.SetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, MFMediaType.Video));
                    Marshal.ThrowExceptionForHR((int)videoMediaTypeIn.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, MFMediaType.RGB565));
                    Marshal.ThrowExceptionForHR((int)videoMediaTypeIn.SetUINT32(MFAttributesClsid.MF_MT_INTERLACE_MODE, (int)MFVideoInterlaceMode.Progressive));
                    Marshal.ThrowExceptionForHR((int)MFExtern.MFSetAttributeSize(videoMediaTypeIn, MFAttributesClsid.MF_MT_FRAME_SIZE, width, height));
                    Marshal.ThrowExceptionForHR((int)MFExtern.MFSetAttributeRatio(videoMediaTypeIn, MFAttributesClsid.MF_MT_FRAME_RATE, fps, 1));
                    Marshal.ThrowExceptionForHR((int)MFExtern.MFSetAttributeRatio(videoMediaTypeIn, MFAttributesClsid.MF_MT_PIXEL_ASPECT_RATIO, 1, 1));
                    Marshal.ThrowExceptionForHR((int)writer.SetInputMediaType(videoStreamIndex, videoMediaTypeIn, null));
                }
                finally
                {
                    Marshal.ReleaseComObject(videoMediaTypeIn);
                }

                // Set the audio output media type.
                IMFMediaType audioMediaTypeOut;
                Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateMediaType(out audioMediaTypeOut));

                try
                {
                    Marshal.ThrowExceptionForHR((int)audioMediaTypeOut.SetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, MFMediaType.Audio));
                    Marshal.ThrowExceptionForHR((int)audioMediaTypeOut.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, MFMediaType.AAC));
                    Marshal.ThrowExceptionForHR((int)audioMediaTypeOut.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_BITS_PER_SAMPLE, 16));
                    Marshal.ThrowExceptionForHR((int)audioMediaTypeOut.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_SAMPLES_PER_SECOND, 44100));
                    Marshal.ThrowExceptionForHR((int)audioMediaTypeOut.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_NUM_CHANNELS, 2));
                    Marshal.ThrowExceptionForHR((int)audioMediaTypeOut.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_AVG_BYTES_PER_SECOND, 20000));
                    Marshal.ThrowExceptionForHR((int)writer.AddStream(audioMediaTypeOut, out audioStreamIndex));
                }
                finally
                {
                    Marshal.ReleaseComObject(audioMediaTypeOut);
                }

                // Set the audio input media type.
                IMFMediaType audioMediaTypeIn;
                Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateMediaType(out audioMediaTypeIn));

                try
                {
                    Marshal.ThrowExceptionForHR((int)audioMediaTypeIn.SetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, MFMediaType.Audio));
                    Marshal.ThrowExceptionForHR((int)audioMediaTypeIn.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, MFMediaType.PCM));
                    Marshal.ThrowExceptionForHR((int)audioMediaTypeIn.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_BITS_PER_SAMPLE, 16));
                    Marshal.ThrowExceptionForHR((int)audioMediaTypeIn.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_SAMPLES_PER_SECOND, 44100));
                    Marshal.ThrowExceptionForHR((int)audioMediaTypeIn.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_NUM_CHANNELS, 2));
                    Marshal.ThrowExceptionForHR((int)writer.SetInputMediaType(audioStreamIndex, audioMediaTypeIn, null));
                }
                finally
                {
                    Marshal.ReleaseComObject(audioMediaTypeIn);
                }
            }
            catch
            {
                Marshal.ReleaseComObject(writer);
                throw;
            }

            // Tell the sink writer to start accepting data.
            Marshal.ThrowExceptionForHR((int)writer.BeginWriting());
        }

        private static void WriteVideoFrame(int width, int height, long frameDuration, IMFSinkWriter writer, int videoStreamIndex, long rtStart, byte[] videoFrameBuffer)
        {
            IMFSample sample;
            Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateSample(out sample));

            try
            {
                IMFMediaBuffer buffer;
                Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateMemoryBuffer(videoFrameBuffer.Length, out buffer));

                try
                {
                    IntPtr pData;
                    int maxLength;
                    int currentLength;
                    Marshal.ThrowExceptionForHR((int)buffer.Lock(out pData, out maxLength, out currentLength));

                    try
                    {
                        //Marshal.Copy(videoFrameBuffer, 0, pData, videoFrameBuffer.Length);

                        for (int y = 0; y < height; y++)
                        {
                            Marshal.Copy(videoFrameBuffer, (height - 1 - y) * width * 2, pData + y * width * 2, width * 2);
                        }
                    }
                    finally
                    {
                        Marshal.ThrowExceptionForHR((int)buffer.Unlock());
                    }

                    Marshal.ThrowExceptionForHR((int)buffer.SetCurrentLength(videoFrameBuffer.Length));
                    Marshal.ThrowExceptionForHR((int)sample.AddBuffer(buffer));
                }
                finally
                {
                    Marshal.ReleaseComObject(buffer);
                }

                Marshal.ThrowExceptionForHR((int)sample.SetSampleTime(rtStart));
                Marshal.ThrowExceptionForHR((int)sample.SetSampleDuration(frameDuration));
                Marshal.ThrowExceptionForHR((int)writer.WriteSample(videoStreamIndex, sample));
            }
            finally
            {
                Marshal.ReleaseComObject(sample);
            }
        }

        private static void WriteAudioFrame(long frameDuration, IMFSinkWriter writer, int audioStreamIndex, long rtStart, byte[] audioFrameBuffer)
        {
            IMFSample sample;
            Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateSample(out sample));

            try
            {
                IMFMediaBuffer buffer;
                Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateMemoryBuffer(audioFrameBuffer.Length, out buffer));

                try
                {
                    IntPtr pData;
                    int maxLength;
                    int currentLength;
                    Marshal.ThrowExceptionForHR((int)buffer.Lock(out pData, out maxLength, out currentLength));

                    try
                    {
                        Marshal.Copy(audioFrameBuffer, 0, pData, audioFrameBuffer.Length);
                    }
                    finally
                    {
                        Marshal.ThrowExceptionForHR((int)buffer.Unlock());
                    }

                    Marshal.ThrowExceptionForHR((int)buffer.SetCurrentLength(audioFrameBuffer.Length));
                    Marshal.ThrowExceptionForHR((int)sample.AddBuffer(buffer));
                }
                finally
                {
                    Marshal.ReleaseComObject(buffer);
                }

                Marshal.ThrowExceptionForHR((int)sample.SetSampleTime(rtStart));
                Marshal.ThrowExceptionForHR((int)sample.SetSampleDuration(frameDuration));
                Marshal.ThrowExceptionForHR((int)writer.WriteSample(audioStreamIndex, sample));
            }
            finally
            {
                Marshal.ReleaseComObject(sample);
            }
        }

        private static void InitializeSourceReader(string inputUrl, out IMFSourceReader reader, out int videoStreamIndex, out int audioStreamIndex)
        {
            IMFAttributes attributes;
            Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateAttributes(out attributes, 0));

            try
            {
                Marshal.ThrowExceptionForHR((int)attributes.SetUINT32(MFAttributesClsid.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1));
                Marshal.ThrowExceptionForHR((int)attributes.SetUINT32(MFAttributesClsid.MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING, 1));
                //Marshal.ThrowExceptionForHR((int)attributes.SetUINT32(MFAttributesClsid.MF_SOURCE_READER_ENABLE_ADVANCED_VIDEO_PROCESSING, 1));

                Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateSourceReaderFromURL(inputUrl, attributes, out reader));
            }
            finally
            {
                Marshal.ReleaseComObject(attributes);
            }

            try
            {
                GetSourceReaderStreamIndex(reader, out videoStreamIndex, out audioStreamIndex);

                if (videoStreamIndex != -1)
                {
                    // Set the video input media type.
                    IMFMediaType videoMediaTypeIn;
                    Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateMediaType(out videoMediaTypeIn));

                    try
                    {
                        Marshal.ThrowExceptionForHR((int)videoMediaTypeIn.SetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, MFMediaType.Video));
                        Marshal.ThrowExceptionForHR((int)videoMediaTypeIn.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, MFMediaType.RGB32));
                        Marshal.ThrowExceptionForHR((int)reader.SetCurrentMediaType(videoStreamIndex, null, videoMediaTypeIn));
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(videoMediaTypeIn);
                    }
                }

                if (audioStreamIndex != -1)
                {
                    // Set the audio input media type.
                    IMFMediaType audioMediaTypeIn;
                    Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateMediaType(out audioMediaTypeIn));

                    try
                    {
                        Marshal.ThrowExceptionForHR((int)audioMediaTypeIn.SetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, MFMediaType.Audio));
                        Marshal.ThrowExceptionForHR((int)audioMediaTypeIn.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, MFMediaType.PCM));
                        Marshal.ThrowExceptionForHR((int)audioMediaTypeIn.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_BITS_PER_SAMPLE, 16));
                        Marshal.ThrowExceptionForHR((int)audioMediaTypeIn.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_SAMPLES_PER_SECOND, 22050));
                        Marshal.ThrowExceptionForHR((int)audioMediaTypeIn.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_NUM_CHANNELS, 2));
                        Marshal.ThrowExceptionForHR((int)audioMediaTypeIn.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_BLOCK_ALIGNMENT, 4));
                        Marshal.ThrowExceptionForHR((int)audioMediaTypeIn.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_AVG_BYTES_PER_SECOND, 4 * 22050));
                        Marshal.ThrowExceptionForHR((int)reader.SetCurrentMediaType(audioStreamIndex, audioMediaTypeIn));
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(audioMediaTypeIn);
                    }
                }
            }
            catch
            {
                Marshal.ReleaseComObject(reader);
                throw;
            }
        }

        private static void GetSourceReaderStreamIndex(IMFSourceReader reader, out int videoStreamIndex, out int audioStreamIndex)
        {
            int videoIndex = -1;
            int audioIndex = -1;

            for (int streamIndex = 0; ; streamIndex++)
            {
                IMFMediaType mediaType;
                if (reader.GetNativeMediaType(streamIndex, 0, out mediaType) == HResult.MF_E_NO_MORE_TYPES)
                {
                    break;
                }

                try
                {
                    Guid majorType;
                    Marshal.ThrowExceptionForHR((int)mediaType.GetMajorType(out majorType));

                    if (videoIndex == -1 && majorType == MFMediaType.Video)
                    {
                        videoIndex = streamIndex;
                    }
                    else if (audioIndex == -1 && majorType == MFMediaType.Audio)
                    {
                        audioIndex = streamIndex;
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(mediaType);
                }

                if (videoIndex != -1 && audioIndex != -1)
                {
                    break;
                }
            }

            videoStreamIndex = videoIndex;
            audioStreamIndex = audioIndex;
        }

        private static void GetSourceReaderAttributes(IMFSourceReader reader, out int width, out int height, out int fps)
        {
            IMFMediaType videoMediaType;
            Marshal.ThrowExceptionForHR((int)reader.GetCurrentMediaType(MF_SOURCE_READER_FIRST_VIDEO_STREAM, out videoMediaType));

            try
            {
                uint w;
                uint h;
                Marshal.ThrowExceptionForHR((int)videoMediaType.GetSize(MFAttributesClsid.MF_MT_FRAME_SIZE, out w, out h));

                uint fpsNumerator;
                uint fpsDenominator;
                Marshal.ThrowExceptionForHR((int)videoMediaType.GetRatio(MFAttributesClsid.MF_MT_FRAME_RATE, out fpsNumerator, out fpsDenominator));

                width = (int)w;
                height = (int)h;
                fps = ((int)fpsNumerator + (int)fpsDenominator / 2) / (int)fpsDenominator;
            }
            finally
            {
                Marshal.ReleaseComObject(videoMediaType);
            }
        }

        private static byte[] ReadSample(IMFSourceReader reader, int inputStreamIndex, out int streamIndex, out long timestamp)
        {
            MF_SOURCE_READER_FLAG readFlag;
            IMFSample sample;
            Marshal.ThrowExceptionForHR((int)reader.ReadSample(inputStreamIndex, 0, out streamIndex, out readFlag, out timestamp, out sample));

            try
            {
                if (readFlag == MF_SOURCE_READER_FLAG.EndOfStream)
                {
                    return null;
                }

                IMFMediaBuffer buffer;
                Marshal.ThrowExceptionForHR((int)sample.ConvertToContiguousBuffer(out buffer));

                try
                {
                    IntPtr pData;
                    int maxLength;
                    int currentLength;
                    Marshal.ThrowExceptionForHR((int)buffer.Lock(out pData, out maxLength, out currentLength));

                    try
                    {
                        byte[] bytes = new byte[currentLength];

                        Marshal.Copy(pData, bytes, 0, bytes.Length);

                        return bytes;
                    }
                    finally
                    {
                        Marshal.ThrowExceptionForHR((int)buffer.Unlock());
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(buffer);
                }
            }
            finally
            {
                if (sample != null)
                {
                    Marshal.ReleaseComObject(sample);
                }
            }
        }

        private static byte[] ConvertAudio22050To44100(byte[] audioData)
        {
            byte[] buffer = new byte[audioData.Length * 2];

            for (int i = 0; i < audioData.Length; i += 4)
            {
                byte val0 = audioData[i + 0];
                byte val1 = audioData[i + 1];
                byte val2 = audioData[i + 2];
                byte val3 = audioData[i + 3];

                buffer[i * 2 + 0] = val0;
                buffer[i * 2 + 1] = val1;
                buffer[i * 2 + 2] = val2;
                buffer[i * 2 + 3] = val3;
                buffer[i * 2 + 4] = val0;
                buffer[i * 2 + 5] = val1;
                buffer[i * 2 + 6] = val2;
                buffer[i * 2 + 7] = val3;
            }

            return buffer;
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
