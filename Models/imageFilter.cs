using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows;
using System;

namespace GraphicsEditor.Models
{
    public class ImageFilter
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public virtual BitmapSource ApplyFilter(BitmapSource source)
        {
            return source; 
        }
    }

    public class BlurFilter : ImageFilter
    {
        public double BlurRadius { get; set; } = 5;

        public BlurFilter()
        {
            Name = "Размытие";
            Description = "Apply Gaussian blur effect";
        }

        public override BitmapSource ApplyFilter(BitmapSource source)
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {

                visual.Effect = new BlurEffect()
                {
                    Radius = BlurRadius,
                    RenderingBias = RenderingBias.Performance
                };

                context.DrawImage(source, new System.Windows.Rect(0, 0, source.PixelWidth, source.PixelHeight));
            }

            var target = new RenderTargetBitmap(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY,
                PixelFormats.Pbgra32);

            target.Render(visual);

            return target;
        }
    }

    public class BlackWhiteFilter : ImageFilter
    {
        public BlackWhiteFilter()
        {
            Name = "Черно-белый";
            Description = "Convert image to black and white";
        }

        public override BitmapSource ApplyFilter(BitmapSource source)
        {
            var format = source.Format;
            if (format != PixelFormats.Bgra32)
                source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            int stride = source.PixelWidth * 4;
            byte[] pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte blue = pixels[i];
                byte green = pixels[i + 1];
                byte red = pixels[i + 2];

                byte gray = (byte)(0.299 * red + 0.587 * green + 0.114 * blue);

                pixels[i] = gray;     
                pixels[i + 1] = gray; 
                pixels[i + 2] = gray; 
            }

            return BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY,
                PixelFormats.Bgra32, null, pixels, stride);
        }
    }

    public class WarmFilter : ImageFilter
    {
        public double Warmth { get; set; } = 0.3;

        public WarmFilter()
        {
            Name = "Теплый";
            Description = "Apply warm temperature effect";
        }

        public override BitmapSource ApplyFilter(BitmapSource source)
        {
            var bitmap = new WriteableBitmap(source);
            int pixels = bitmap.PixelWidth * bitmap.PixelHeight;
            int[] pixelData = new int[pixels];
            int stride = bitmap.PixelWidth * 4;

            bitmap.CopyPixels(pixelData, stride, 0);

            for (int i = 0; i < pixels; i++)
            {
                int pixel = pixelData[i];

                byte a = (byte)(pixel >> 24);
                byte r = (byte)(pixel >> 16);
                byte g = (byte)(pixel >> 8);
                byte b = (byte)(pixel);

                int newR = Math.Min(255, (int)(r + (255 - r) * Warmth * 0.5));
                int newG = Math.Min(255, (int)(g + (255 - g) * Warmth * 0.3));
                int newB = Math.Max(0, (int)(b - b * Warmth * 0.2));

                pixelData[i] = (a << 24) | (newR << 16) | (newG << 8) | newB;
            }

            bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                              pixelData, stride, 0);
            return bitmap;
        }
    }

    public class NegativeFilter : ImageFilter
    {
        public NegativeFilter()
        {
            Name = "Негатив";
            Description = "Convert in negative";
        }

        public override BitmapSource ApplyFilter(BitmapSource source)
        {
            var format = source.Format;
            if (format != PixelFormats.Bgra32)
                source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            int stride = source.PixelWidth * 4;
            byte[] pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = (byte)(255 - pixels[i]);   
                pixels[i + 1] = (byte)(255 - pixels[i + 1]);
                pixels[i + 2] = (byte)(255 - pixels[i + 2]); 
            }

            return BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY,
                PixelFormats.Bgra32, null, pixels, stride);
        }
    }
}