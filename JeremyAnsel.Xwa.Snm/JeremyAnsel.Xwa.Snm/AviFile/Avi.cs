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

namespace AviFile
{
    internal static class Avi
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RGBQUAD
        {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }

        public static int RGBQUAD_SIZE = 4;

        public static readonly int streamtypeVIDEO = MmioFOURCC('v', 'i', 'd', 's');
        public static readonly int streamtypeAUDIO = MmioFOURCC('a', 'u', 'd', 's');

        public const int OF_READWRITE = 2;

        public const int AVIERR_NODATA = -2147205005;

        //macro mmioFOURCC
        public static int MmioFOURCC(char ch0, char ch1, char ch2, char ch3)
        {
            return ((int)(byte)(ch0) | ((byte)(ch1) << 8) |
            ((byte)(ch2) << 16) | ((byte)(ch3) << 24));
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RECT
        {
            public uint left;
            public uint top;
            public uint right;
            public uint bottom;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BITMAPINFOHEADER
        {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public RGBQUAD[] bmiColors;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PCMWAVEFORMAT
        {
            public short wFormatTag;
            public short nChannels;
            public int nSamplesPerSec;
            public int nAvgBytesPerSec;
            public short nBlockAlign;
            public short wBitsPerSample;
            public short cbSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct AVISTREAMINFO
        {
            public int fccType;
            public int fccHandler;
            public int dwFlags;
            public int dwCaps;
            public short wPriority;
            public short wLanguage;
            public int dwScale;
            public int dwRate;
            public int dwStart;
            public int dwLength;
            public int dwInitialFrames;
            public int dwSuggestedBufferSize;
            public int dwQuality;
            public int dwSampleSize;
            public RECT rcFrame;
            public int dwEditCount;
            public int dwFormatChangeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public char[] szName;
        }
    }
}
