using JeremyAnsel.DirectX.D2D1;
using JeremyAnsel.DirectX.DWrite;
using JeremyAnsel.DirectX.Dxgi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace JeremyAnsel.Xwa.Snm
{
    internal static class SnmSubtitlesHelpers
    {
        private static DWriteFactory _dwriteFactory;
        private static D2D1Factory _d2dFactory;
        private static D2D1DCRenderTarget _d2dRenderTarget;
        private static D2D1SolidColorBrush _brush;
        private static DWriteTextFormat _textFormat10;
        private static DWriteTextFormat _textFormat12;
        private static DWriteTextFormat _textFormat15;
        private static DWriteTextFormat _textFormat20;

        public static void Begin()
        {
            _dwriteFactory = DWriteFactory.Create(DWriteFactoryType.Shared);

            _d2dFactory = D2D1Factory.Create(D2D1FactoryType.SingleThreaded);

            _d2dRenderTarget = _d2dFactory.CreateDCRenderTarget(new D2D1RenderTargetProperties(
                D2D1RenderTargetType.Default,
                new D2D1PixelFormat(DxgiFormat.B8G8R8A8UNorm, D2D1AlphaMode.Premultiplied),
                96.0f,
                96.0f,
                D2D1RenderTargetUsages.None,
                D2D1FeatureLevel.FeatureLevel100));

            _brush = _d2dRenderTarget.CreateSolidColorBrush(new D2D1ColorF(0U));

            _textFormat10 = _dwriteFactory.CreateTextFormat(
                "Verdana",
                null,
                DWriteFontWeight.Bold,
                DWriteFontStyle.Normal,
                DWriteFontStretch.Expanded,
                10,
                "en-US");

            _textFormat12 = _dwriteFactory.CreateTextFormat(
                "Verdana",
                null,
                DWriteFontWeight.Bold,
                DWriteFontStyle.Normal,
                DWriteFontStretch.Expanded,
                12,
                "en-US");

            _textFormat15 = _dwriteFactory.CreateTextFormat(
                "Verdana",
                null,
                DWriteFontWeight.Bold,
                DWriteFontStyle.Normal,
                DWriteFontStretch.Expanded,
                15,
                "en-US");

            _textFormat20 = _dwriteFactory.CreateTextFormat(
                "Verdana",
                null,
                DWriteFontWeight.Bold,
                DWriteFontStyle.Normal,
                DWriteFontStretch.Expanded,
                20,
                "en-US");
        }

        public static void End()
        {
            DWriteUtils.DisposeAndNull(ref _textFormat10);
            DWriteUtils.DisposeAndNull(ref _textFormat12);
            DWriteUtils.DisposeAndNull(ref _textFormat15);
            DWriteUtils.DisposeAndNull(ref _textFormat20);
            D2D1Utils.DisposeAndNull(ref _brush);
            D2D1Utils.DisposeAndNull(ref _d2dRenderTarget);
            D2D1Utils.DisposeAndNull(ref _d2dFactory);
            DWriteUtils.DisposeAndNull(ref _dwriteFactory);
        }

        public static void DrawSubtitle(SnmSubtitlesFile subtitles, byte[] videoData, int width, int height, long frame)
        {
            using Bitmap baseImage = new(
                width,
                height,
                width * 4,
                PixelFormat.Format32bppRgb,
                Marshal.UnsafeAddrOfPinnedArrayElement(videoData, 0));

            using Graphics baseContext = Graphics.FromImage(baseImage);

            IntPtr hdc = baseContext.GetHdc();

            try
            {
                _d2dRenderTarget.BindDC(hdc, new D2D1RectL(0, 0, width, height));
                _d2dRenderTarget.BeginDraw();

                foreach (var subtitle in subtitles.Subtitles)
                {
                    long start = subtitle.StartFrame * (long)(10000000 / 15);
                    long end = subtitle.EndFrame * (long)(10000000 / 15);
                    long startFade = start + 10 * (long)(10000000 / 15);
                    long endFade = end + 10 * (long)(10000000 / 15);

                    if (start <= frame && frame <= endFade)
                    {
                        float a;

                        if (frame < startFade)
                        {
                            a = (float)(frame - start) / (float)(startFade - start);
                        }
                        else if (frame > end)
                        {
                            a = (float)(endFade - frame) / (float)(endFade - end);
                        }
                        else
                        {
                            a = 1.0f;
                        }

                        _brush.Color = new D2D1ColorF(subtitle.ColorR / 255.0f, subtitle.ColorG / 255.0f, subtitle.ColorB / 255.0f, a);

                        string text = subtitle.Text;

                        if (text.StartsWith("!"))
                        {
                            text = text.Substring(text.IndexOf("!", 1) + 1);
                        }

                        int x = subtitle.PositionX;
                        int y = subtitle.PositionY - (480 - height) / 2;

                        DWriteTextFormat textFormat = subtitle.FontSize switch
                        {
                            10 => _textFormat10,
                            12 => _textFormat12,
                            15 => _textFormat15,
                            20 => _textFormat20,
                            _ => _textFormat15
                        };

                        D2D1RectF layoutRect = new(x, y, x + width, y + height);

                        if (x < 0)
                        {
                            textFormat.TextAlignment = DWriteTextAlignment.Center;
                            layoutRect.Left = 0;
                            layoutRect.Right = width;
                        }
                        else
                        {
                            textFormat.TextAlignment = DWriteTextAlignment.Leading;
                        }

                        if (y < 0)
                        {
                            textFormat.ParagraphAlignment = DWriteParagraphAlignment.Center;
                            layoutRect.Top = 0;
                            layoutRect.Bottom = height;
                        }
                        else
                        {
                            textFormat.ParagraphAlignment = DWriteParagraphAlignment.Near;
                        }

                        _d2dRenderTarget.DrawText(
                            text,
                            textFormat,
                            layoutRect,
                            _brush);
                    }
                }

                _d2dRenderTarget.EndDraw();
            }
            finally
            {
                baseContext.ReleaseHdc();
            }
        }
    }
}
