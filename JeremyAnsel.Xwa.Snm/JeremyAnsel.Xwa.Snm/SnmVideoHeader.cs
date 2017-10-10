using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JeremyAnsel.Xwa.Snm
{
    public sealed class SnmVideoHeader
    {
        internal const int Size = 8;

        public short Width { get; set; }

        public short Height { get; set; }

        internal void Read(BinaryReader file)
        {
            short padding1 = file.ReadInt16();

            if (padding1 != 0)
            {
                throw new InvalidDataException();
            }

            this.Width = file.ReadInt16();
            this.Height = file.ReadInt16();

            short padding2 = file.ReadInt16();

            if (padding2 != 3)
            {
                throw new InvalidDataException();
            }
        }

        internal void Write(BinaryWriter file)
        {
            file.Write((short)0);
            file.Write(this.Width);
            file.Write(this.Height);
            file.Write((short)3);
        }
    }
}
