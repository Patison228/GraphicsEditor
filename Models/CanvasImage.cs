using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GraphicsEditor.Models
{
    public class CanvasImage : ObservableObject
    {
        private ImageSource _imageSource;
        private double _width;
        private double _height;
        private double _angle;
        private BitmapSource _originalImage;
        private bool _isFiltered = false;

        public ImageSource ImageSource
        {
            get => _imageSource;
            set => SetProperty(ref _imageSource, value);
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

        public double Angle
        {
            get => _angle;
            set => SetProperty(ref _angle, value);
        }

        public bool IsFiltered => _isFiltered;

        public CanvasImage(string imagePath)
        {
            LoadImage(imagePath);
        }

        private void LoadImage(string imagePath)
        {
            try
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                ImageSource = bitmapImage;
                _originalImage = bitmapImage;
                Width = bitmapImage.PixelWidth;
                Height = bitmapImage.PixelHeight;
                Angle = 0;
                _isFiltered = false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void ApplyFilter(ImageFilter filter)
        {
            if (_originalImage == null || filter == null)
                return;

            try
            {
                var filteredImage = filter.ApplyFilter(_originalImage);
                ImageSource = filteredImage;
                _isFiltered = true;
                OnPropertyChanged(nameof(IsFiltered));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка применения фильтра: {ex.Message}", "Ошибка",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void ResetFilter()
        {
            if (_originalImage != null)
            {
                ImageSource = _originalImage;
                _isFiltered = false;
                OnPropertyChanged(nameof(IsFiltered));
            }
        }
    }
}