using CommunityToolkit.Mvvm.ComponentModel;

namespace GraphicsEditor.Models
{
    public class CropRect : ObservableObject
    {
        private double _x;
        private double _y;
        private double _width;
        private double _height;
        private bool _isVisible;

        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public CropRect()
        {
            IsVisible = false;
        }

        public void SetPosition(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            IsVisible = true;
        }

        public void Reset()
        {
            IsVisible = false;
            X = 0;
            Y = 0;
            Width = 0;
            Height = 0;
        }
    }
}
