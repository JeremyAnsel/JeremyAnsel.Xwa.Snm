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
using System.Runtime.InteropServices;

namespace AviFile
{
    [SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable")]
    internal sealed class VideoStream : AviStream
    {
        /// <summary>handle for AVIStreamGetFrame</summary>
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr getFrameObject;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int FrameSize { get; }
        
        public double FrameRate { get; }
        
        public int Width { get; }
        
        public int Height { get; }
        
        public short BitsPerPixel { get; }

        /// <summary>count of frames in the stream</summary>
        public int FramesCount { get; } = 0;

        /// <summary>initial frame index</summary>
        /// <remarks>Added by M. Covington</remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int FirstFrame { get; } = 0;

        /// <summary>Initialize a VideoStream for an existing stream</summary>
        /// <param name="aviFile">The file that contains the stream</param>
        /// <param name="aviStream">An IAVISTREAM from [aviFile]</param>
        internal VideoStream(IntPtr aviFile, IntPtr aviStream)
        {
            this.aviFile = aviFile;
            this.aviStream = aviStream;
            Avi.AVISTREAMINFO streamInfo = GetStreamInfo(aviStream);

            Avi.BITMAPINFO bih = new Avi.BITMAPINFO();
            int size = Marshal.SizeOf(bih.bmiHeader);
            int result = NativeMethods.AVIStreamReadFormat(aviStream, 0, ref bih, ref size);
            if (result != 0)
            {
                throw new AviFileException("Exception in VideoStreamReadFormat: " + result.ToString(CultureInfo.InvariantCulture));
            }

            if (bih.bmiHeader.biBitCount < 24)
            {
                throw new NotSupportedException();
            }

            this.FrameRate = (float)streamInfo.dwRate / (float)streamInfo.dwScale;
            this.Width = (int)streamInfo.rcFrame.right;
            this.Height = (int)streamInfo.rcFrame.bottom;
            this.FrameSize = bih.bmiHeader.biSizeImage;
            this.BitsPerPixel = bih.bmiHeader.biBitCount;
            this.FirstFrame = NativeMethods.AVIStreamStart(aviStream);
            this.FramesCount = NativeMethods.AVIStreamLength(aviStream);
        }

        private static Avi.AVISTREAMINFO GetStreamInfo(IntPtr aviStream)
        {
            Avi.AVISTREAMINFO streamInfo = new Avi.AVISTREAMINFO();
            int result = NativeMethods.AVIStreamInfo(aviStream, ref streamInfo, Marshal.SizeOf(streamInfo));
            if (result != 0)
            {
                throw new AviFileException("Exception in VideoStreamInfo: " + result.ToString(CultureInfo.InvariantCulture));
            }
            return streamInfo;
        }


        /// <summary>Prepare for decompressing frames</summary>
        /// <remarks>
        /// This method has to be called before GetBitmap and ExportBitmap.
        /// Release ressources with GetFrameClose.
        /// </remarks>
        [SuppressMessage("Globalization", "CA1303:Ne pas passer de littéraux en paramètres localisés", Justification = "Reviewed.")]
        public void GetFrameOpen()
        {
            //Open frames

            Avi.BITMAPINFOHEADER bih = new Avi.BITMAPINFOHEADER
            {
                biBitCount = BitsPerPixel,
                biClrImportant = 0,
                biClrUsed = 0,
                biCompression = 0,
                biPlanes = 1,
                biXPelsPerMeter = 0,
                biYPelsPerMeter = 0,
                biHeight = 0,
                biWidth = 0
            };

            bih.biSize = Marshal.SizeOf(bih);

            getFrameObject = NativeMethods.AVIStreamGetFrameOpen(StreamPointer, ref bih);

            if (getFrameObject == IntPtr.Zero)
            {
                throw new AviFileException("Exception in VideoStreamGetFrameOpen! Cannot find a decompressor.");
            }
        }


        /// <summary>Returns all data needed to copy the frame</summary>
        /// <param name="position">Position of the frame</param>
        /// <returns>The frame data</returns>
        [SuppressMessage("Globalization", "CA1303:Ne pas passer de littéraux en paramètres localisés", Justification = "Reviewed.")]
        public byte[]? GetFrameData(int position)
        {
            if (position > FramesCount)
            {
                throw new AviFileException("Invalid frame position: " + position);
            }

            //Decompress the frame and return a pointer to the DIB
            IntPtr dib = NativeMethods.AVIStreamGetFrame(getFrameObject, FirstFrame + position);

            if (dib == IntPtr.Zero)
            {
                return null;
            }

            //Copy the bitmap header into a managed struct
            Avi.BITMAPINFOHEADER bih = (Avi.BITMAPINFOHEADER)Marshal.PtrToStructure(dib, typeof(Avi.BITMAPINFOHEADER))!;

            if (bih.biSizeImage < 1)
            {
                throw new AviFileException("Exception in VideoStreamGetFrame");
            }

            //copy the image
            int framePaletteSize = bih.biClrUsed * Avi.RGBQUAD_SIZE;
            byte[] bitmapData = new byte[bih.biSizeImage];
            IntPtr dibPointer = IntPtr.Add(dib, Marshal.SizeOf(bih) + framePaletteSize);
            Marshal.Copy(dibPointer, bitmapData, 0, bih.biSizeImage);

            // flip vertical
            int stride = this.Width * this.BitsPerPixel / 8;
            int length = stride * (this.Height - 1);
            int length2 = stride * this.Height / 2;

            for (int row = 0; row < length2; row += stride)
            {
                for (int i = 0; i < stride; i++)
                {
                    byte b = bitmapData[row + i];
                    bitmapData[row + i] = bitmapData[length - row + i];
                    bitmapData[length - row + i] = b;
                }
            }

            return bitmapData;
        }

        /// <summary>Free ressources that have been used by GetFrameOpen</summary>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "AviFile.NativeMethods.AVIStreamGetFrameClose(System.IntPtr)")]
        public void GetFrameClose()
        {
            if (getFrameObject != IntPtr.Zero)
            {
                NativeMethods.AVIStreamGetFrameClose(getFrameObject);
                getFrameObject = IntPtr.Zero;
            }
        }
    }
}
