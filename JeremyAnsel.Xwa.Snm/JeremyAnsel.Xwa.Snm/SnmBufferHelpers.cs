namespace JeremyAnsel.Xwa.Snm
{
    public static class SnmBufferHelpers
    {
        public static byte[] ConvertAudio44100To22050(byte[] audioData)
        {
            byte[] buffer = new byte[audioData.Length / 8 * 4];

            for (int i = 0, j = 0; i <= audioData.Length - 8; i += 8, j += 4)
            {
                short val00 = BitConverter.ToInt16(audioData, i);
                short val01 = BitConverter.ToInt16(audioData, i + 2);
                short val10 = BitConverter.ToInt16(audioData, i + 4);
                short val11 = BitConverter.ToInt16(audioData, i + 6);

                short val0 = (short)((val00 + val10) / 2);
                short val1 = (short)((val01 + val11) / 2);

                buffer[j] = (byte)(val0 & 0xff);
                buffer[j + 1] = (byte)((val0 >> 8) & 0xff);
                buffer[j + 2] = (byte)(val1 & 0xff);
                buffer[j + 3] = (byte)((val1 >> 8) & 0xff);
            }

            return buffer;
        }

        public static byte[] Convert16BppTo32Bpp(byte[] bytes)
        {
            int length = bytes.Length * 2;
            var buffer = new byte[length];

            for (int i = 0, j = 0; i < length; i += 4, j += 2)
            {
                ushort c = BitConverter.ToUInt16(bytes, j);

                byte r = (byte)((c & 0xF800) >> 11);
                byte g = (byte)((c & 0x7E0) >> 5);
                byte b = (byte)(c & 0x1F);

                r = (byte)((r * (0xffU * 2) + 0x1fU) / (0x1fU * 2));
                g = (byte)((g * (0xffU * 2) + 0x3fU) / (0x3fU * 2));
                b = (byte)((b * (0xffU * 2) + 0x1fU) / (0x1fU * 2));

                buffer[i] = b;
                buffer[i + 1] = g;
                buffer[i + 2] = r;
                buffer[i + 3] = 0xff;
            }

            return buffer;
        }

        public static byte[] Convert24BppTo16Bpp(byte[] bytes)
        {
            int length = bytes.Length / 3;
            var buffer = new byte[length * 2];

            for (int i = 0; i < length; i++)
            {
                uint b = bytes[i * 3 + 2];
                uint g = bytes[i * 3 + 1];
                uint r = bytes[i * 3];

                b = (b * (0x1fU * 2) + 0xffU) / (0xffU * 2);
                g = (g * (0x3fU * 2) + 0xffU) / (0xffU * 2);
                r = (r * (0x1fU * 2) + 0xffU) / (0xffU * 2);

                ushort c = (ushort)((b << 11) | (g << 5) | r);
                buffer[i * 2] = (byte)(c & 0xff);
                buffer[i * 2 + 1] = (byte)((c >> 8) & 0xff);
            }

            return buffer;
        }

        public static byte[] Convert32BppTo16Bpp(byte[] bytes)
        {
            int length = bytes.Length / 4;
            var buffer = new byte[length * 2];

            for (int i = 0; i < length; i++)
            {
                uint b = bytes[i * 4 + 2];
                uint g = bytes[i * 4 + 1];
                uint r = bytes[i * 4];

                b = (b * (0x1fU * 2) + 0xffU) / (0xffU * 2);
                g = (g * (0x3fU * 2) + 0xffU) / (0xffU * 2);
                r = (r * (0x1fU * 2) + 0xffU) / (0xffU * 2);

                ushort c = (ushort)((b << 11) | (g << 5) | r);
                buffer[i * 2] = (byte)(c & 0xff);
                buffer[i * 2 + 1] = (byte)((c >> 8) & 0xff);
            }

            return buffer;
        }
    }
}
