using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JeremyAnsel.Xwa.Snm
{
    public sealed class SnmSubtitle
    {
        public int PositionX { get; set; }

        public int PositionY { get; set; }

        public byte ColorR { get; set; }

        public byte ColorG { get; set; }

        public byte ColorB { get; set; }

        public int StartFrame { get; set; }

        public int EndFrame { get; set; }

        public int FontSize { get; set; }

        public string Text { get; set; } = string.Empty;
    }
}
