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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace AviFile
{
    [SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable")]
    internal sealed class AviManager
    {
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr aviFile = IntPtr.Zero;
        private VideoStream? videoStream = null;
        private AudioStream? audioStream = null;

        /// <summary>Open or create an AVI file</summary>
        /// <param name="fileName">Name of the AVI file</param>
        public AviManager(string fileName)
        {
            NativeMethods.AVIFileInit();

            int result = NativeMethods.AVIFileOpen(ref aviFile, fileName, Avi.OF_READWRITE, 0);

            if (result != 0)
            {
                throw new AviFileException("Exception in AVIFileOpen: " + result.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>Get the first video stream - usually there is only one video stream</summary>
        /// <returns>VideoStream object for the stream</returns>
        public VideoStream? GetVideoStream()
        {
            if (videoStream != null)
            {
                videoStream.Close();
                videoStream = null;
            }

            int result = NativeMethods.AVIFileGetStream(
                aviFile,
                out IntPtr aviStream,
                Avi.streamtypeVIDEO, 0);

            if (result == Avi.AVIERR_NODATA)
            {
                return null;
            }

            if (result != 0)
            {
                throw new AviFileException("Exception in AVIFileGetStream: " + result.ToString(CultureInfo.InvariantCulture));
            }

            VideoStream stream = new VideoStream(aviFile, aviStream);
            videoStream = stream;
            return stream;
        }

        /// <summary>Getthe first wave audio stream</summary>
        /// <returns>AudioStream object for the stream</returns>
        public AudioStream? GetWaveStream()
        {
            if (audioStream != null)
            {
                audioStream.Close();
                audioStream = null;
            }

            int result = NativeMethods.AVIFileGetStream(
                aviFile,
                out IntPtr aviStream,
                Avi.streamtypeAUDIO, 0);

            if (result == Avi.AVIERR_NODATA)
            {
                return null;
            }

            if (result != 0)
            {
                throw new AviFileException("Exception in AVIFileGetStream: " + result.ToString(CultureInfo.InvariantCulture));
            }

            AudioStream stream = new AudioStream(aviFile, aviStream);
            audioStream = stream;
            return stream;
        }

        /// <summary>Release all ressources</summary>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "AviFile.NativeMethods.AVIFileRelease(System.IntPtr)")]
        public void Close()
        {
            if (videoStream != null)
            {
                videoStream.Close();
                videoStream = null;
            }

            if (audioStream != null)
            {
                audioStream.Close();
                audioStream = null;
            }

            NativeMethods.AVIFileRelease(aviFile);
            aviFile = IntPtr.Zero;
            NativeMethods.AVIFileExit();
        }
    }
}
