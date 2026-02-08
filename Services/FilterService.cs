using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GraphicsEditor.Services
{

    public class FilterService
    {
        public BitmapSource ApplyFilter(RenderTargetBitmap source, string filterType)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            switch (filterType)
            {
                case "brightness":
                    ApplyBrightness(pixels, 1.3);
                    break;
                case "darken":
                    ApplyBrightness(pixels, 0.7);
                    break;
                case "grayscale":
                    ApplyGrayscale(pixels);
                    break;
                case "negative":
                    ApplyNegative(pixels);
                    break;
                case "warm":
                    ApplyWarm(pixels);
                    break;
                case "blur":
                    ApplyBlur(pixels, width, height);
                    break;
                case "contrast":
                    ApplyContrast(pixels);
                    break;
            }

            return BitmapSource.Create(width, height, 96, 96,
                PixelFormats.Bgra32, null, pixels, stride);
        }

        private void ApplyBrightness(byte[] pixels, double factor)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = (byte)Math.Min(255, pixels[i] * factor);
                pixels[i + 1] = (byte)Math.Min(255, pixels[i + 1] * factor);
                pixels[i + 2] = (byte)Math.Min(255, pixels[i + 2] * factor);
            }
        }

        private void ApplyGrayscale(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte gray = (byte)(pixels[i + 2] * 0.299 + pixels[i + 1] * 0.587 + pixels[i] * 0.114);
                pixels[i] = pixels[i + 1] = pixels[i + 2] = gray;
            }
        }

        private void ApplyNegative(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = (byte)(255 - pixels[i]);
                pixels[i + 1] = (byte)(255 - pixels[i + 1]);
                pixels[i + 2] = (byte)(255 - pixels[i + 2]);
            }
        }

        private void ApplyWarm(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i + 2] = (byte)Math.Min(255, pixels[i + 2] * 1.15); 
                pixels[i + 1] = (byte)Math.Min(255, pixels[i + 1] * 1.05); 
                pixels[i] = (byte)Math.Max(0, pixels[i] * 0.9); 
            }
        }

        private void ApplyContrast(byte[] pixels)
        {
            double factor = 1.5; 
            double intercept = 128 * (1 - factor);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                for (int c = 0; c < 3; c++)
                {
                    int value = (int)(pixels[i + c] * factor + intercept);
                    pixels[i + c] = (byte)Math.Max(0, Math.Min(255, value));
                }
            }
        }

        private void ApplyBlur(byte[] pixels, int width, int height)
        {
            byte[] temp = (byte[])pixels.Clone();
            int stride = width * 4;

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        int sum = 0;
                        for (int ky = -1; ky <= 1; ky++)
                        {
                            for (int kx = -1; kx <= 1; kx++)
                            {
                                int idx = ((y + ky) * width + (x + kx)) * 4 + c;
                                sum += temp[idx];
                            }
                        }
                        pixels[(y * width + x) * 4 + c] = (byte)(sum / 9);
                    }
                }
            }
        }

    }
}