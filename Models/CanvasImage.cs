using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GraphicsEditor.Models
{
    public class CanvasImage : INotifyPropertyChanged
    {
        private ImageSource _imageSource;
        private double _width;
        private double _height;
        private double _angle;

        public ImageSource ImageSource
        {
            get => _imageSource;
            set
            {
                _imageSource = value;
                OnPropertyChanged();
            }
        }

        public double Width
        {
            get => _width;
            set
            {
                _width = value;
                OnPropertyChanged();
            }
        }

        public double Height
        {
            get => _height;
            set
            {
                _height = value;
                OnPropertyChanged();
            }
        }

        public double Angle
        {
            get => _angle;
            set
            {
                _angle = value;
                OnPropertyChanged();
            }
        }

        public CanvasImage(string imagePath)
        {
            var bitmap = new BitmapImage(new System.Uri(imagePath));
            ImageSource = bitmap;
            Width = bitmap.PixelWidth;
            Height = bitmap.PixelHeight;
            Angle = 0;
        }

        public CanvasImage()
        {
            Angle = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}