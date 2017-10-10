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
using System.Runtime.InteropServices;
using System.Security;

namespace AviFile
{
    [SecurityCritical, SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        //Initialize the AVI library
        [DllImport("avifil32.dll")]
        public static extern void AVIFileInit();

        //Open an AVI file
        [DllImport("avifil32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        public static extern int AVIFileOpen(
            ref IntPtr ppfile,
            string szFile,
            int uMode,
            int pclsidHandler);

        //Get a stream from an open AVI file
        [DllImport("avifil32.dll")]
        public static extern int AVIFileGetStream(
            IntPtr pfile,
            out IntPtr ppavi,
            int fccType,
            int lParam);

        //Get the start position of a stream
        [DllImport("avifil32.dll", PreserveSig = true)]
        public static extern int AVIStreamStart(IntPtr pavi);

        //Get the length of a stream in frames
        [DllImport("avifil32.dll", PreserveSig = true)]
        public static extern int AVIStreamLength(IntPtr pavi);

        //Get information about an open stream
        [DllImport("avifil32.dll")]
        public static extern int AVIStreamInfo(
            IntPtr pAVIStream,
            ref Avi.AVISTREAMINFO psi,
            int lSize);

        //Get a pointer to a GETFRAME object (returns 0 on error)
        [DllImport("avifil32.dll")]
        public static extern IntPtr AVIStreamGetFrameOpen(
            IntPtr pAVIStream,
            ref Avi.BITMAPINFOHEADER bih);

        //Get a pointer to a packed DIB (returns 0 on error)
        [DllImport("avifil32.dll")]
        public static extern IntPtr AVIStreamGetFrame(
            IntPtr pGetFrameObj,
            int lPos);

        //Read the format for a stream
        [DllImport("avifil32.dll")]
        public static extern int AVIStreamReadFormat(
            IntPtr aviStream, int lPos,
            ref Avi.BITMAPINFO lpFormat, ref int cbFormat
            );

        //Read the format for a stream
        [DllImport("avifil32.dll")]
        public static extern int AVIStreamReadFormat(
            IntPtr aviStream, int lPos,
            ref Avi.PCMWAVEFORMAT lpFormat, ref int cbFormat
            );

        //Release the GETFRAME object
        [DllImport("avifil32.dll")]
        public static extern int AVIStreamGetFrameClose(
            IntPtr pGetFrameObj);

        //Release an open AVI stream
        [DllImport("avifil32.dll")]
        public static extern int AVIStreamRelease(IntPtr aviStream);

        //Release an open AVI file
        [DllImport("avifil32.dll")]
        public static extern int AVIFileRelease(IntPtr pfile);

        //Close the AVI library
        [DllImport("avifil32.dll")]
        public static extern void AVIFileExit();

        [DllImport("avifil32.dll")]
        public static extern int AVIStreamRead(
            IntPtr pavi,
            int lStart,
            int lSamples,
            IntPtr lpBuffer,
            int cbBuffer,
            IntPtr plBytes,
            IntPtr plSamples
            );
    }
}
