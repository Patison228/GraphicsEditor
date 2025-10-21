using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraphicsEditor.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;


namespace GraphicEditor.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private CanvasImage _currentImage;
        public CanvasImage CurrentImage
        {
            get => _currentImage;
            set => SetProperty(ref _currentImage, value);
        }


        public RelayCommand AddImageCommand { get; set; }
        public RelayCommand ClearImageCommand { get; set; }


        private double _canvasWidth = 1920;
        public double CanvasWidth
        {
            get => _canvasWidth;
            set => SetProperty(ref _canvasWidth, value);
        }


        private double _canvasHeight = 1080;
        public double CanvasHeight
        {
            get => _canvasHeight;
            set => SetProperty(ref _canvasHeight, value);
        }

        public bool HasImage => CurrentImage != null;

        public MainViewModel()
        {
            AddImageCommand = new RelayCommand(AddImage);
            ClearImageCommand = new RelayCommand(ClearImage, () => HasImage);
        }

        private void AddImage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg;*.bmp;*.gif)|*.png;*.jpeg;*.jpg;*.bmp;*.gif|All files (*.*)|*.*",
                Title = "Выберите изображение"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var image = new CanvasImage(openFileDialog.FileName);
                    CurrentImage = image;
                    ResizeCanvasToImage(image);

                    ClearImageCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(HasImage));
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearImage()
        {
            CurrentImage = null;
            CanvasWidth = 1920;
            CanvasHeight = 1080;

            ClearImageCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(HasImage));
        }

        private void ResizeCanvasToImage(CanvasImage image)
        {
            int padding = 40;

            CanvasWidth = image.Width + padding;
            CanvasHeight = image.Height + padding;
        }
    }
}
