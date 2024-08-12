using System;
using System.Diagnostics.CodeAnalysis;

namespace JeremyAnsel.Xwa.Snm
{
    public static class Blocky16
    {
        private const byte Down = 0;

        private const byte Up = 1;

        private const byte Left = 2;

        private const byte Right = 3;

        private const byte NoEdge = 4;

        private static readonly byte[] _xvector4 = new byte[]
        {
            0, 1, 2, 3, 3, 3, 3, 2, 1, 0, 0, 0, 1, 2, 2, 1,
        };

        private static readonly byte[] _yvector4 = new byte[]
        {
            0, 0, 0, 0, 1, 2, 3, 3, 3, 3, 2, 1, 1, 1, 2, 2,
        };

        private static readonly byte[] _xvector8 = new byte[]
        {
            0, 2, 5, 7, 7, 7, 7, 7, 7, 5, 2, 0, 0, 0, 0, 0,
        };

        private static readonly byte[] _yvector8 = new byte[]
        {
            0, 0, 0, 0, 1, 3, 4, 6, 7, 7, 7, 7, 6, 4, 3, 1,
        };

        private static readonly sbyte[] _motion_vectors = new sbyte[]
        {
              0,   0,  -1, -43,   6, -43,  -9, -42,  13, -41,
            -16, -40,  19, -39, -23, -36,  26, -34,  -2, -33,
              4, -33, -29, -32,  -9, -32,  11, -31, -16, -29,
             32, -29,  18, -28, -34, -26, -22, -25,  -1, -25,
              3, -25,  -7, -24,   8, -24,  24, -23,  36, -23,
            -12, -22,  13, -21, -38, -20,   0, -20, -27, -19,
             -4, -19,   4, -19, -17, -18,  -8, -17,   8, -17,
             18, -17,  28, -17,  39, -17, -12, -15,  12, -15,
            -21, -14,  -1, -14,   1, -14, -41, -13,  -5, -13,
              5, -13,  21, -13, -31, -12, -15, -11,  -8, -11,
              8, -11,  15, -11,  -2, -10,   1, -10,  31, -10,
            -23,  -9, -11,  -9,  -5,  -9,   4,  -9,  11,  -9,
             42,  -9,   6,  -8,  24,  -8, -18,  -7,  -7,  -7,
             -3,  -7,  -1,  -7,   2,  -7,  18,  -7, -43,  -6,
            -13,  -6,  -4,  -6,   4,  -6,   8,  -6, -33,  -5,
             -9,  -5,  -2,  -5,   0,  -5,   2,  -5,   5,  -5,
             13,  -5, -25,  -4,  -6,  -4,  -3,  -4,   3,  -4,
              9,  -4, -19,  -3,  -7,  -3,  -4,  -3,  -2,  -3,
             -1,  -3,   0,  -3,   1,  -3,   2,  -3,   4,  -3,
              6,  -3,  33,  -3, -14,  -2, -10,  -2,  -5,  -2,
             -3,  -2,  -2,  -2,  -1,  -2,   0,  -2,   1,  -2,
              2,  -2,   3,  -2,   5,  -2,   7,  -2,  14,  -2,
             19,  -2,  25,  -2,  43,  -2,  -7,  -1,  -3,  -1,
             -2,  -1,  -1,  -1,   0,  -1,   1,  -1,   2,  -1,
              3,  -1,  10,  -1,  -5,   0,  -3,   0,  -2,   0,
             -1,   0,   1,   0,   2,   0,   3,   0,   5,   0,
              7,   0, -10,   1,  -7,   1,  -3,   1,  -2,   1,
             -1,   1,   0,   1,   1,   1,   2,   1,   3,   1,
            -43,   2, -25,   2, -19,   2, -14,   2,  -5,   2,
             -3,   2,  -2,   2,  -1,   2,   0,   2,   1,   2,
              2,   2,   3,   2,   5,   2,   7,   2,  10,   2,
             14,   2, -33,   3,  -6,   3,  -4,   3,  -2,   3,
             -1,   3,   0,   3,   1,   3,   2,   3,   4,   3,
             19,   3,  -9,   4,  -3,   4,   3,   4,   7,   4,
             25,   4, -13,   5,  -5,   5,  -2,   5,   0,   5,
              2,   5,   5,   5,   9,   5,  33,   5,  -8,   6,
             -4,   6,   4,   6,  13,   6,  43,   6, -18,   7,
             -2,   7,   0,   7,   2,   7,   7,   7,  18,   7,
            -24,   8,  -6,   8, -42,   9, -11,   9,  -4,   9,
              5,   9,  11,   9,  23,   9, -31,  10,  -1,  10,
              2,  10, -15,  11,  -8,  11,   8,  11,  15,  11,
             31,  12, -21,  13,  -5,  13,   5,  13,  41,  13,
             -1,  14,   1,  14,  21,  14, -12,  15,  12,  15,
            -39,  17, -28,  17, -18,  17,  -8,  17,   8,  17,
             17,  18,  -4,  19,   0,  19,   4,  19,  27,  19,
             38,  20, -13,  21,  12,  22, -36,  23, -24,  23,
             -8,  24,   7,  24,  -3,  25,   1,  25,  22,  25,
             34,  26, -18,  28, -32,  29,  16,  29, -11,  31,
              9,  32,  29,  32,  -4,  33,   2,  33, -26,  34,
             23,  36, -19,  39,  16,  40, -13,  41,   9,  42,
             -6,  43,   1,  43,   0,   0,   0,   0,   0,   0
        };

        private static readonly byte[][] _glyph4_cb = MakeTablesInterpolation(4, _xvector4, _yvector4);

        private static readonly byte[][] _glyph8_cb = MakeTablesInterpolation(8, _xvector8, _yvector8);

        private static byte GetEdge(int x, int y, int param)
        {
            if (y == 0)
            {
                return Down;
            }

            if (y == param - 1)
            {
                return Up;
            }

            if (x == 0)
            {
                return Left;
            }

            if (x == param - 1)
            {
                return Right;
            }

            return NoEdge;
        }

        private static byte GetDirection(byte edge1, byte edge2)
        {
            if ((edge1 == Left && edge2 == Right) || (edge1 == Right && edge2 == Left) || (edge1 == Down && edge2 != Up) || (edge1 != Up && edge2 == Down))
            {
                return Up;
            }

            if ((edge1 != Down && edge2 == Up) || (edge1 == Up && edge2 != Down))
            {
                return Down;
            }

            if ((edge1 == Left && edge2 != Right) || (edge1 != Right && edge2 == Left))
            {
                return Left;
            }

            if ((edge1 == Down && edge2 == Up) || (edge1 == Up && edge2 == Down) || (edge1 == Right && edge2 != Left) || (edge1 != Left && edge2 == Right))
            {
                return Right;
            }

            return NoEdge;
        }

        private static byte[][] MakeTablesInterpolation(int param, byte[] xvector, byte[] yvector)
        {
            var codebook = new byte[256][];

            for (int i = 0; i < 16; i++)
            {
                int vert1_x = xvector[i];
                int vert1_y = yvector[i];

                for (int j = 0; j < 16; j++)
                {
                    int vert2_x = xvector[j];
                    int vert2_y = yvector[j];

                    byte[] glyph = new byte[param * param];

                    for (int y = 0; y < param; y++)
                    {
                        for (int x = 0; x < param; x++)
                        {
                            glyph[y * param + x] = 0;
                        }
                    }

                    byte edge1 = GetEdge(vert1_x, vert1_y, param);
                    byte edge2 = GetEdge(vert2_x, vert2_y, param);
                    byte direction = GetDirection(edge1, edge2);

                    int width = Math.Max(Math.Abs(vert2_x - vert1_x), Math.Abs(vert2_y - vert1_y));

                    for (int w = 0; w <= width; w++)
                    {
                        int x;
                        int y;

                        if (width > 0)
                        {
                            x = (vert1_x * w + vert2_x * (width - w) + width / 2) / width;
                            y = (vert1_y * w + vert2_y * (width - w) + width / 2) / width;
                        }
                        else
                        {
                            x = vert1_x;
                            y = vert1_y;
                        }

                        glyph[y * param + x] = 1;

                        switch (direction)
                        {
                            case Up:
                                for (int row = y; row >= 0; row--)
                                {
                                    glyph[row * param + x] = 1;
                                }

                                break;

                            case Down:
                                for (int row = y; row < param; row++)
                                {
                                    glyph[row * param + x] = 1;
                                }

                                break;

                            case Left:
                                for (int col = x; col >= 0; col--)
                                {
                                    glyph[y * param + col] = 1;
                                }

                                break;

                            case Right:
                                for (int col = x; col < param; col++)
                                {
                                    glyph[y * param + col] = 1;
                                }

                                break;
                        }
                    }

                    codebook[i * 16 + j] = glyph;
                }
            }

            return codebook;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [SuppressMessage("Microsoft.Performance", "CA1809:AvoidExcessiveLocals")]
        private static void Level(int param, byte[] input, ref int inputPos, SnmVideoFrame video, Blocky16Context context, int cx, int cy)
        {
            byte opcode = input[inputPos];
            inputPos++;

            int stride = video.Width * 2;
            int pos = cy * stride + cx * 2;

            if (opcode <= 0xF4)
            {
                int x = _motion_vectors[opcode * 2];
                int y = _motion_vectors[opcode * 2 + 1];

                int offset = y * stride + x * 2;

                for (int i = 0; i < param; i++)
                {
                    if (pos + offset + param * 2 > context.BufferSize)
                    {
                        break;
                    }

                    Array.Copy(context.Buffer2, pos + offset, context.Buffer0, pos, param * 2);
                    pos += stride;
                }
            }
            else if (opcode == 0xF5)
            {
                int motion_vector = BitConverter.ToInt16(input, inputPos);
                inputPos += 2;

                int x = motion_vector % video.Width;
                int y = motion_vector / video.Width;

                int offset = y * stride + x * 2;

                for (int i = 0; i < param; i++)
                {
                    if (pos + offset + param * 2 > context.BufferSize)
                    {
                        break;
                    }

                    Array.Copy(context.Buffer2, pos + offset, context.Buffer0, pos, param * 2);
                    pos += stride;
                }
            }
            else if (opcode == 0xF6)
            {
                for (int i = 0; i < param; i++)
                {
                    if (pos + param * 2 > context.BufferSize)
                    {
                        break;
                    }

                    Array.Copy(context.Buffer1, pos, context.Buffer0, pos, param * 2);
                    pos += stride;
                }
            }
            else if (opcode == 0xF7)
            {
                if (param > 2)
                {
                    byte glyph_index = input[inputPos++];
                    byte fg_index = input[inputPos++];
                    byte bg_index = input[inputPos++];
                    ushort fg_color = video.Codebook[fg_index];
                    ushort bg_color = video.Codebook[bg_index];

                    byte[] glyph;
                    int glyphWidth;

                    if (param == 8)
                    {
                        glyph = _glyph8_cb[glyph_index];
                        glyphWidth = 8;
                    }
                    else
                    {
                        glyph = _glyph4_cb[glyph_index];
                        glyphWidth = 4;
                    }

                    byte bgLow = (byte)bg_color;
                    byte bgHigh = (byte)(bg_color >> 8);

                    byte fgLow = (byte)fg_color;
                    byte fgHigh = (byte)(fg_color >> 8);

                    for (int i = 0; i < param; i++)
                    {
                        for (int j = 0; j < param; j++)
                        {
                            if (glyph[i * glyphWidth + j] == 0)
                            {
                                context.Buffer0[pos + j * 2] = bgLow;
                                context.Buffer0[pos + j * 2 + 1] = bgHigh;
                            }
                            else
                            {
                                context.Buffer0[pos + j * 2] = fgLow;
                                context.Buffer0[pos + j * 2 + 1] = fgHigh;
                            }
                        }

                        pos += stride;
                    }
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                    {
                        for (int j = 0; j < 2 * 2; j += 2)
                        {
                            byte index = input[inputPos++];
                            ushort color = video.Codebook[index];
                            context.Buffer0[pos + j] = (byte)color;
                            context.Buffer0[pos + j + 1] = (byte)(color >> 8);
                        }

                        pos += stride;
                    }
                }
            }
            else if (opcode == 0xF8)
            {
                if (param > 2)
                {
                    byte glyph_index = input[inputPos++];
                    ushort fg_color = BitConverter.ToUInt16(input, inputPos);
                    inputPos += 2;
                    ushort bg_color = BitConverter.ToUInt16(input, inputPos);
                    inputPos += 2;

                    byte[] glyph;
                    int glyphWidth;

                    if (param == 8)
                    {
                        glyph = _glyph8_cb[glyph_index];
                        glyphWidth = 8;
                    }
                    else
                    {
                        glyph = _glyph4_cb[glyph_index];
                        glyphWidth = 4;
                    }

                    byte bgLow = (byte)bg_color;
                    byte bgHigh = (byte)(bg_color >> 8);

                    byte fgLow = (byte)fg_color;
                    byte fgHigh = (byte)(fg_color >> 8);

                    for (int i = 0; i < param; i++)
                    {
                        for (int j = 0; j < param; j++)
                        {
                            if (glyph[i * glyphWidth + j] == 0)
                            {
                                context.Buffer0[pos + j * 2] = bgLow;
                                context.Buffer0[pos + j * 2 + 1] = bgHigh;
                            }
                            else
                            {
                                context.Buffer0[pos + j * 2] = fgLow;
                                context.Buffer0[pos + j * 2 + 1] = fgHigh;
                            }
                        }

                        pos += stride;
                    }
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                    {
                        for (int j = 0; j < 2 * 2; j += 2)
                        {
                            context.Buffer0[pos + j] = input[inputPos++];
                            context.Buffer0[pos + j + 1] = input[inputPos++];
                        }

                        pos += stride;
                    }
                }
            }
            else if (opcode >= 0xF9 && opcode <= 0xFC)
            {
                ushort color = video.SmallCodebook[opcode - 0xF9];

                byte lowColor = (byte)color;
                byte highColor = (byte)(color >> 8);

                for (int i = 0; i < param; i++)
                {
                    for (int j = 0; j < param * 2; j += 2)
                    {
                        context.Buffer0[pos + j] = lowColor;
                        context.Buffer0[pos + j + 1] = highColor;
                    }

                    pos += stride;
                }
            }
            else if (opcode == 0xFD)
            {
                byte index = input[inputPos++];
                ushort color = video.Codebook[index];

                byte lowColor = (byte)color;
                byte highColor = (byte)(color >> 8);

                for (int i = 0; i < param; i++)
                {
                    for (int j = 0; j < param * 2; j += 2)
                    {
                        context.Buffer0[pos + j] = lowColor;
                        context.Buffer0[pos + j + 1] = highColor;
                    }

                    pos += stride;
                }
            }
            else if (opcode == 0xFE)
            {
                ushort color = BitConverter.ToUInt16(input, inputPos);
                inputPos += 2;

                byte lowColor = (byte)color;
                byte highColor = (byte)(color >> 8);

                for (int i = 0; i < param; i++)
                {
                    for (int j = 0; j < param * 2; j += 2)
                    {
                        context.Buffer0[pos + j] = lowColor;
                        context.Buffer0[pos + j + 1] = highColor;
                    }

                    pos += stride;
                }
            }
            else if (opcode == 0xFF)
            {
                if (param > 2)
                {
                    int nextLevel = param / 2;
                    Level(nextLevel, input, ref inputPos, video, context, cx, cy);
                    Level(nextLevel, input, ref inputPos, video, context, cx + nextLevel, cy);
                    Level(nextLevel, input, ref inputPos, video, context, cx, cy + nextLevel);
                    Level(nextLevel, input, ref inputPos, video, context, cx + nextLevel, cy + nextLevel);
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                    {
                        for (int j = 0; j < 2 * 2; j += 2)
                        {
                            context.Buffer0[pos + j] = input[inputPos++];
                            context.Buffer0[pos + j + 1] = input[inputPos++];
                        }

                        pos += stride;
                    }
                }
            }
        }

        [SuppressMessage("Globalization", "CA1303:Ne pas passer de littéraux en paramètres localisés", Justification = "Reviewed.")]
        public static byte[] Decompress(byte[]? input, SnmVideoFrame? video, Blocky16Context? context)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (video == null)
            {
                throw new ArgumentNullException(nameof(video));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (video.Sequence == 0)
            {
                context.Clear(video.BackgroundColor);
            }

            switch (video.SubcodecId)
            {
                case 0:
                    Array.Copy(input, 0, context.Buffer0, 0, context.BufferSize);
                    break;

                case 1:
                    throw new NotImplementedException("SubcodecId 1");

                case 2:
                    {
                        int hblocks = (int)((video.Height + 7) & 0xFFFFFFF8);
                        int wblocks = (int)((video.Width + 7) & 0xFFFFFFF8);

                        int inputPos = 0;

                        for (int cy = 0; cy < hblocks; cy += 8)
                        {
                            for (int cx = 0; cx < wblocks; cx += 8)
                            {
                                Level(8, input, ref inputPos, video, context, cx, cy);
                            }
                        }

                        break;
                    }

                case 3:
                    Array.Copy(context.Buffer2, 0, context.Buffer0, 0, context.BufferSize);
                    break;

                case 4:
                    Array.Copy(context.Buffer1, 0, context.Buffer0, 0, context.BufferSize);
                    break;

                case 5:
                    for (int pos = 0, inputPos = 0; pos < video.RleOutputSize;)
                    {
                        byte code = input[inputPos];
                        inputPos++;

                        int length = (code >> 1) + 1;

                        if ((code & 1) != 0)
                        {
                            byte color = input[inputPos];
                            inputPos++;

                            for (int i = 0; i < length; i++)
                            {
                                context.Buffer0[pos + i] = color;
                            }
                        }
                        else
                        {
                            Array.Copy(input, inputPos, context.Buffer0, pos, length);
                            inputPos += length;
                        }

                        pos += length;
                    }

                    break;

                case 6:
                    for (int pos = 0, inputPos = 0; pos < context.BufferSize; pos += 2)
                    {
                        byte index = input[inputPos];
                        inputPos++;

                        ushort color = video.Codebook[index];
                        context.Buffer0[pos] = (byte)color;
                        context.Buffer0[pos + 1] = (byte)(color >> 8);
                    }

                    break;

                case 7:
                    throw new NotImplementedException("SubcodecId 7");

                case 8:
                    for (int pos = 0, inputPos = 0; pos < video.RleOutputSize;)
                    {
                        byte code = input[inputPos];
                        inputPos++;

                        int length2 = ((code >> 1) + 1) * 2;

                        if ((code & 1) != 0)
                        {
                            byte index = input[inputPos];
                            inputPos++;

                            ushort color = video.Codebook[index];

                            for (int i = 0; i < length2; i += 2)
                            {
                                context.Buffer0[pos + i] = (byte)color;
                                context.Buffer0[pos + i + 1] = (byte)(color >> 8);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < length2; i += 2)
                            {
                                byte index = input[inputPos];
                                inputPos++;

                                ushort color = video.Codebook[index];
                                context.Buffer0[pos + i] = (byte)color;
                                context.Buffer0[pos + i + 1] = (byte)(color >> 8);
                            }
                        }

                        pos += length2;
                    }

                    break;

                default:
                    throw new NotSupportedException();
            }

            byte[] output = context.Buffer0;
            context.RotateBuffers(video.DiffBufferRotateCode);

            return output;
        }

        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public static byte[] Compress(byte[]? input, out byte subcodecId)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            //subcodecId = 0;
            //return input;

            subcodecId = 5;

            byte[] destBuffer = new byte[input.Length * 2];
            int destIndex = 0;

            for (int inputIndex = 0; inputIndex < input.Length;)
            {
                int rleLength = Blocky16.RleLengthEqual(input, inputIndex);

                if (rleLength == 1)
                {
                    rleLength = Blocky16.RleLengthDiff(input, inputIndex);

                    if (rleLength == 1)
                    {
                        byte value = input[inputIndex++];
                        byte code = 0;
                        destBuffer[destIndex++] = code;
                        destBuffer[destIndex++] = value;
                    }
                    else
                    {
                        while (rleLength > 0)
                        {
                            byte length = (byte)Math.Min(rleLength, 0x80);
                            rleLength -= length;
                            byte code = (byte)((length - 1) << 1);
                            destBuffer[destIndex++] = code;
                            Array.Copy(input, inputIndex, destBuffer, destIndex, length);
                            inputIndex += length;
                            destIndex += length;
                        }
                    }
                }
                else
                {
                    byte rleLengthValue = input[inputIndex];
                    inputIndex += rleLength;

                    while (rleLength > 0)
                    {
                        byte length = (byte)Math.Min(rleLength, 0x80);
                        rleLength -= length;
                        byte code = (byte)(((length - 1) << 1) | 1);
                        destBuffer[destIndex++] = code;
                        destBuffer[destIndex++] = rleLengthValue;
                    }
                }
            }

            byte[] buffer = new byte[(destIndex + 3) & (~3)];
            Array.Copy(destBuffer, buffer, destIndex);

            return buffer;
        }

        private static int RleLengthEqual(byte[] buffer, int position)
        {
            if (buffer.Length - position <= 1)
            {
                return 1;
            }

            int length = 1;

            while (position + length < buffer.Length && buffer[position + length] == buffer[position + length - 1])
            {
                length++;
            }

            return length;
        }

        private static int RleLengthDiff(byte[] buffer, int position)
        {
            if (buffer.Length - position <= 1)
            {
                return 1;
            }

            int length = 1;

            while (position + length < buffer.Length && buffer[position + length] != buffer[position + length - 1])
            {
                length++;
            }

            return length;
        }
    }
}
