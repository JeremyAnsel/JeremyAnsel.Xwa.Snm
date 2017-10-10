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

namespace AviFile
{
    internal abstract class AviStream
    {
        protected IntPtr aviFile;
        protected IntPtr aviStream;

        internal AviStream()
        {
        }

        /// <summary>Pointer to the unmanaged AVI file</summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IntPtr FilePointer
        {
            get { return aviFile; }
        }

        /// <summary>Pointer to the unmanaged AVI Stream</summary>
        internal virtual IntPtr StreamPointer
        {
            get { return aviStream; }
        }

        /// <summary>Close the stream</summary>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "AviFile.NativeMethods.AVIStreamRelease(System.IntPtr)")]
        public virtual void Close()
        {
            NativeMethods.AVIStreamRelease(StreamPointer);
        }
    }
}
