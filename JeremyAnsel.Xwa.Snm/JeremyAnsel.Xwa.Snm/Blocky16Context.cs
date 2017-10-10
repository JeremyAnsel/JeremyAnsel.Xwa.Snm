using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace JeremyAnsel.Xwa.Snm
{
    public sealed class Blocky16Context
    {
        public Blocky16Context(int bufferSize)
        {
            this.BufferSize = bufferSize;
            this.Buffer0 = new byte[bufferSize];
            this.Buffer1 = new byte[bufferSize];
            this.Buffer2 = new byte[bufferSize];
        }

        public int BufferSize { get; private set; }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public byte[] Buffer0 { get; private set; }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public byte[] Buffer1 { get; private set; }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public byte[] Buffer2 { get; private set; }

        public void Clear(uint color)
        {
            byte lowColor = (byte)color;
            byte heighColor = (byte)(color >> 8);

            for (int i = 0; i < this.BufferSize; i += 2)
            {
                this.Buffer0[i] = lowColor;
                this.Buffer0[i + 1] = heighColor;
            }

            Array.Copy(this.Buffer0, 0, this.Buffer1, 0, this.BufferSize);
            Array.Copy(this.Buffer0, 0, this.Buffer2, 0, this.BufferSize);
        }

        public void RotateBuffers(int rotateCode)
        {
            byte[] temp;

            switch (rotateCode)
            {
                case 1:
                    temp = this.Buffer2;
                    this.Buffer2 = this.Buffer0;
                    this.Buffer0 = temp;
                    break;

                case 2:
                    temp = this.Buffer1;
                    this.Buffer1 = this.Buffer2;
                    this.Buffer2 = this.Buffer0;
                    this.Buffer0 = temp;
                    break;
            }
        }
    }
}
