using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JeremyAnsel.Xwa.Snm
{
    public sealed class SnmSubtitlesFile
    {
        private static readonly Encoding _encoding = Encoding.GetEncoding("iso-8859-1");

        public List<SnmSubtitle> Subtitles { get; } = new List<SnmSubtitle>();

        public static SnmSubtitlesFile FromFile(string fileName)
        {
            var subtitles = new SnmSubtitlesFile();

            using (var filestream = new StreamReader(fileName, _encoding))
            {
                string line;
                SnmSubtitle subtitle = new();
                int step = -1;
                string[] parts;

                while ((line = filestream.ReadLine()) != null)
                {
                    if (line.StartsWith("//"))
                    {
                        continue;
                    }

                    step++;

                    switch (step)
                    {
                        case 0:
                            // position
                            parts = line.Split(',');
                            subtitle.PositionX = int.Parse(parts[0], CultureInfo.InvariantCulture);
                            subtitle.PositionY = int.Parse(parts[1], CultureInfo.InvariantCulture);
                            break;

                        case 1:
                            // color
                            parts = line.Split(',');
                            subtitle.ColorR = byte.Parse(parts[0], CultureInfo.InvariantCulture);
                            subtitle.ColorG = byte.Parse(parts[1], CultureInfo.InvariantCulture);
                            subtitle.ColorB = byte.Parse(parts[2], CultureInfo.InvariantCulture);
                            break;

                        case 2:
                            // start/end frame
                            parts = line.Split(',');
                            subtitle.StartFrame = int.Parse(parts[0], CultureInfo.InvariantCulture);
                            subtitle.EndFrame = int.Parse(parts[1], CultureInfo.InvariantCulture);
                            break;

                        case 3:
                            // font size
                            subtitle.FontSize = int.Parse(line, CultureInfo.InvariantCulture);
                            break;

                        case 4:
                            // text
                            subtitle.Text = line;
                            break;

                        case 5:
                            // separator
                            subtitles.Subtitles.Add(subtitle);
                            subtitle = new SnmSubtitle();
                            step = -1;
                            break;
                    }
                }
            }

            return subtitles;
        }
    }
}
