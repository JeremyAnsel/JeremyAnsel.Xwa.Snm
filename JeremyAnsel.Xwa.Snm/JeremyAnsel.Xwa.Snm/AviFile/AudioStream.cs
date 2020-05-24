/* This class has been written by
* Corinna John (Hannover, Germany)
* cj@binary-universe.net
*
* Modified by Jérémy Ansel (@JeremyAnsel)
*
* You may do with this code whatever you like,
* except selling it or claiming any rights/ownership.
*
* Please send me a little feedback about what you're
* using this code for and what changes you'd like to
* see in later versions. (And please excuse my bad english.)
*
* WARNING: This is experimental code.
* Please do not expect Release Quality.
* */

using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace AviFile
{
    internal sealed class AudioStream : AviStream
    {
        /// <summary>the stream's format</summary>
        private Avi.PCMWAVEFORMAT waveFormat = new Avi.PCMWAVEFORMAT();

        /// <summary>Initialize an AudioStream for an existing stream</summary>
        /// <param name="aviFile">The file that contains the stream</param>
        /// <param name="aviStream">An IAVISTREAM from [aviFile]</param>
        internal AudioStream(IntPtr aviFile, IntPtr aviStream)
        {
            this.aviFile = aviFile;
            this.aviStream = aviStream;

            int size = Marshal.SizeOf(waveFormat);
            int result = NativeMethods.AVIStreamReadFormat(aviStream, 0, ref waveFormat, ref size);
            if (result != 0)
            {
                throw new AviFileException("Exception in AVIStreamReadFormat: " + result.ToString(CultureInfo.InvariantCulture));
            }
        }

        public int BitsPerSample
        {
            get { return waveFormat.wBitsPerSample; }
        }

        public int SamplesPerSecond
        {
            get { return waveFormat.nSamplesPerSec; }
        }

        public int ChannelsCount
        {
            get { return waveFormat.nChannels; }
        }

        /// <summary>Returns all data needed to copy the stream</summary>
        /// <returns>The wave data</returns>
        public byte[] GetStreamData()
        {
            int dataLength = NativeMethods.AVIStreamLength(aviStream);
            int streamLength = dataLength / (this.ChannelsCount * this.BitsPerSample / 8);
            byte[] waveData = new byte[dataLength];

            GCHandle waveDataHandle = GCHandle.Alloc(waveData, GCHandleType.Pinned);

            try
            {
                int result = NativeMethods.AVIStreamRead(aviStream, 0, streamLength, waveDataHandle.AddrOfPinnedObject(), dataLength, IntPtr.Zero, IntPtr.Zero);
                if (result != 0)
                {
                    throw new AviFileException("Exception in AVIStreamRead: " + result.ToString(CultureInfo.InvariantCulture));
                }
            }
            finally
            {
                waveDataHandle.Free();
            }

            return waveData;
        }
    }
}
