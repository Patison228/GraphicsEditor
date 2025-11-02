using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Ink;

namespace GraphicsEditor.Models
{
    public class CanvasSaverService
    {
        public static void SaveInkCanvasWithImage(InkCanvas inkCanvas, FrameworkElement imageContainer, string filePath)
        {
            Rect bounds = VisualTreeHelper.GetDescendantBounds(inkCanvas);

            if (bounds.IsEmpty)
                bounds = new Rect(0, 0, inkCanvas.ActualWidth, inkCanvas.ActualHeight);

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)bounds.Width,
                (int)bounds.Height,
                96, 96, PixelFormats.Pbgra32);

            rtb.Render(inkCanvas);

            SaveBitmapToFile(rtb, filePath);
        }

        private static void SaveBitmapToFile(RenderTargetBitmap bitmap, string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLower();
            BitmapEncoder encoder = null;

            switch (extension)
            {
                case ".png":
                    encoder = new PngBitmapEncoder();
                    break;
                case ".jpg":
                case ".jpeg":
                    encoder = new JpegBitmapEncoder();
                    break;
                case ".bmp":
                    encoder = new BmpBitmapEncoder();
                    break;
                default:
                    encoder = new PngBitmapEncoder();
                    break;
            }

            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fs);
            }
        }
    }
}