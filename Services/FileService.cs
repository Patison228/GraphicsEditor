using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace GraphicsEditor.Services
{
    public class FileService
    {
        public BitmapImage OpenImage(out int width, out int height)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Изображения (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage(new Uri(dialog.FileName));
                width = bitmap.PixelWidth;
                height = bitmap.PixelHeight;
                return bitmap;
            }

            width = 0;
            height = 0;
            return null;
        }

        public bool SaveImage(RenderTargetBitmap bitmap)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "PNG изображение (*.png)|*.png|JPEG изображение (*.jpg)|*.jpg",
                FileName = $"image_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                BitmapEncoder encoder;
                if (dialog.FileName.EndsWith(".jpg"))
                    encoder = new JpegBitmapEncoder();
                else
                    encoder = new PngBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (FileStream fs = new FileStream(dialog.FileName, FileMode.Create))
                {
                    encoder.Save(fs);
                }
                return true;
            }
            return false;
        }
    }
}