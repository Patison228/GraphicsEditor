using System;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GraphicsEditor.Models
{
    public partial class CanvasImage : ObservableObject 
    {
        [ObservableProperty]
        private double x;

        [ObservableProperty]
        private double y;

        private double _angle;
        public double Angle
        {
            get => _angle;
            set => SetProperty(ref _angle, value);
        }

        public BitmapImage ImageSource { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public CanvasImage(string imagePath)
        {
            ImageSource = new BitmapImage(new Uri(imagePath));
            Width = ImageSource.PixelWidth;
            Height = ImageSource.PixelHeight;
        }
    }
}

