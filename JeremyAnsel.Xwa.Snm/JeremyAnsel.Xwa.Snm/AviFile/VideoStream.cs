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

        /// <summary>size of an imge in bytes, stride*height</summary>
        private int frameSize;
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int FrameSize
        {
            get { return frameSize; }
        }

        private double frameRate;
        public double FrameRate
        {
            get { return frameRate; }
        }

        private int width;
        public int Width
        {
            get { return width; }
        }

        private int height;
        public int Height
        {
            get { return height; }
        }

        private short bitsPerPixel;
        public short BitsPerPixel
        {
            get { return bitsPerPixel; }
        }

        /// <summary>count of frames in the stream</summary>
        private int framesCount = 0;
        public int FramesCount
        {
            get { return framesCount; }
        }

        /// <summary>initial frame index</summary>
        /// <remarks>Added by M. Covington</remarks>
        private int firstFrame = 0;
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int FirstFrame
        {
            get { return firstFrame; }
        }

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

            this.frameRate = (float)streamInfo.dwRate / (float)streamInfo.dwScale;
            this.width = (int)streamInfo.rcFrame.right;
            this.height = (int)streamInfo.rcFrame.bottom;
            this.frameSize = bih.bmiHeader.biSizeImage;
            this.bitsPerPixel = bih.bmiHeader.biBitCount;
            this.firstFrame = NativeMethods.AVIStreamStart(aviStream);
            this.framesCount = NativeMethods.AVIStreamLength(aviStream);
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
        public void GetFrameOpen()
        {
            //Open frames

            Avi.BITMAPINFOHEADER bih = new Avi.BITMAPINFOHEADER();
            bih.biBitCount = bitsPerPixel;
            bih.biClrImportant = 0;
            bih.biClrUsed = 0;
            bih.biCompression = 0;
            bih.biPlanes = 1;
            bih.biSize = Marshal.SizeOf(bih);
            bih.biXPelsPerMeter = 0;
            bih.biYPelsPerMeter = 0;
            bih.biHeight = 0;
            bih.biWidth = 0;

            getFrameObject = NativeMethods.AVIStreamGetFrameOpen(StreamPointer, ref bih);

            if (getFrameObject == IntPtr.Zero)
            {
                throw new AviFileException("Exception in VideoStreamGetFrameOpen! Cannot find a decompressor.");
            }
        }

        /// <summary>Returns all data needed to copy the frame</summary>
        /// <param name="position">Position of the frame</param>
        /// <returns>The frame data</returns>
        public byte[] GetFrameData(int position)
        {
            if (position > framesCount)
            {
                throw new AviFileException("Invalid frame position: " + position);
            }

            //Decompress the frame and return a pointer to the DIB
            IntPtr dib = NativeMethods.AVIStreamGetFrame(getFrameObject, firstFrame + position);

            if (dib == IntPtr.Zero)
            {
                return null;
            }

            //Copy the bitmap header into a managed struct
            Avi.BITMAPINFOHEADER bih = (Avi.BITMAPINFOHEADER)Marshal.PtrToStructure(dib, typeof(Avi.BITMAPINFOHEADER));

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
            int stride = this.width * this.bitsPerPixel / 8;
            int length = stride * (this.height - 1);
            int length2 = stride * this.height / 2;

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
