using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraphicsEditor.Models;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;

namespace GraphicsEditor.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private CanvasImage _currentImage;
        public CanvasImage CurrentImage
        {
            get => _currentImage;
            set => SetProperty(ref _currentImage, value);
        }

        private DrawingAttributes _drawingAttributes;
        public DrawingAttributes DrawingAttributes
        {
            get => _drawingAttributes;
            set => SetProperty(ref _drawingAttributes, value);
        }

        private bool _isEraserMode = false;
        public bool IsEraserMode
        {
            get => _isEraserMode;
            set => SetProperty(ref _isEraserMode, value);
        }

        private SolidColorBrush _currentColorBrush;
        public SolidColorBrush CurrentColorBrush
        {
            get => _currentColorBrush;
            set => SetProperty(ref _currentColorBrush, value);
        }

        private double _brushSize = 3;
        public double BrushSize
        {
            get => _brushSize;
            set
            {
                SetProperty(ref _brushSize, value);
                UpdateDrawingAttributes();
            }
        }

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

        public RelayCommand AddImageCommand { get; }
        public RelayCommand RotateImageCommand { get; }
        public RelayCommand ClearImageCommand { get; }
        public RelayCommand<string> SelectColorCommand { get; }
        public RelayCommand BrushCommand { get; }
        public RelayCommand EraserCommand { get; }

        public MainViewModel()
        {
            InitializeDrawingTools();

            AddImageCommand = new RelayCommand(AddImage);
            RotateImageCommand = new RelayCommand(RotateImage, () => HasImage);
            ClearImageCommand = new RelayCommand(ClearImage, () => HasImage);
            SelectColorCommand = new RelayCommand<string>(SelectColor);
            BrushCommand = new RelayCommand(SwitchToBrush);
            EraserCommand = new RelayCommand(SwitchToEraser);
        }

        private void InitializeDrawingTools()
        {
            DrawingAttributes = new DrawingAttributes
            {
                Color = Colors.Blue,
                Width = BrushSize,
                Height = BrushSize,
                FitToCurve = true
            };

            CurrentColorBrush = new SolidColorBrush(Colors.Blue);
        }

        private void SelectColor(string colorName)
        {
            Color color = Colors.Blue;

            switch (colorName?.ToLower())
            {
                case "red":
                    color = Colors.Red;
                    break;
                case "blue":
                    color = Colors.Blue;
                    break;
                case "green":
                    color = Colors.Green;
                    break;
                case "yellow":
                    color = Colors.Yellow;
                    break;
                case "purple":
                    color = Colors.Purple;
                    break;
            }

            DrawingAttributes.Color = color;
            CurrentColorBrush = new SolidColorBrush(color);
            IsEraserMode = false;
        }

        private void SwitchToBrush()
        {
            IsEraserMode = false;
        }

        private void SwitchToEraser()
        {
            IsEraserMode = true;
        }

        private void UpdateDrawingAttributes()
        {
            if (DrawingAttributes != null)
            {
                DrawingAttributes.Width = BrushSize;
                DrawingAttributes.Height = BrushSize;
            }
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
                CurrentImage = new CanvasImage(openFileDialog.FileName);
                CanvasWidth = CurrentImage.Width + 40;
                CanvasHeight = CurrentImage.Height + 40;

                RotateImageCommand.NotifyCanExecuteChanged();
                ClearImageCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(HasImage));
            }
        }

        private void RotateImage()
        {
            if (CurrentImage != null)
            {
                CurrentImage.Angle = (CurrentImage.Angle + 90) % 360;
            }
        }

        private void ClearImage()
        {
            CurrentImage = null;
            CanvasWidth = 1920;
            CanvasHeight = 1080;

            RotateImageCommand.NotifyCanExecuteChanged();
            ClearImageCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(HasImage));
        }
    }
}