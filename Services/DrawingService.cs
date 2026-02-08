using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GraphicsEditor.Services
{
    public class DrawingService
    {
        public TransformedBitmap RotateImage(RenderTargetBitmap source, double angle)
        {
            TransformedBitmap transformed = new TransformedBitmap();
            transformed.BeginInit();
            transformed.Source = source;
            transformed.Transform = new RotateTransform(angle);
            transformed.EndInit();
            return transformed;
        }

        public CroppedBitmap CropImage(RenderTargetBitmap source, Int32Rect cropArea)
        {
            return new CroppedBitmap(source, cropArea);
        }

        public RenderTargetBitmap RenderCanvas(System.Windows.Controls.Canvas canvas)
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)canvas.Width,
                (int)canvas.Height,
                96, 96, PixelFormats.Pbgra32);
            rtb.Render(canvas);
            return rtb;
        }
    }
}