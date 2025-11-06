using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraphicsEditor.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Controls;
using System.Collections.Generic;

namespace GraphicsEditor.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // переменные
        private CanvasImage _currentImage;
        public CanvasImage CurrentImage
        {
            get => _currentImage;
            set
            {
                SetProperty(ref _currentImage, value);
                UpdateCommands();
                OnPropertyChanged(nameof(HasImage));
            }
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

        private ObservableCollection<ImageFilter> _availableFilters;
        public ObservableCollection<ImageFilter> AvailableFilters
        {
            get => _availableFilters;
            set => SetProperty(ref _availableFilters, value);
        }

        private ImageFilter _selectedFilter;
        public ImageFilter SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                SetProperty(ref _selectedFilter, value);
                ApplyFilterCommand.NotifyCanExecuteChanged();
            }
        }

        private InkCanvas _currentInkCanvas;
        public InkCanvas CurrentInkCanvas
        {
            get => _currentInkCanvas;
            set => SetProperty(ref _currentInkCanvas, value);
        }

        private Stack<Stroke> _undoStack = new Stack<Stroke>();
        private const int MAX_UNDO_STEPS = 50;

        private bool _canUndo = false;
        public bool CanUndo
        {
            get => _canUndo;
            set => SetProperty(ref _canUndo, value);
        }

        public bool HasImage => CurrentImage != null;

        //команды
        public RelayCommand AddImageCommand { get; }
        public RelayCommand RotateImageCommand { get; }
        public RelayCommand ClearImageCommand { get; }
        public RelayCommand<string> SelectColorCommand { get; }
        public RelayCommand BrushCommand { get; }
        public RelayCommand EraserCommand { get; }
        public RelayCommand ApplyFilterCommand { get; }
        public RelayCommand ResetFilterCommand { get; }
        public RelayCommand SaveAsJpgCommand { get; }
        public RelayCommand UndoStrokeCommand { get; }

        public MainViewModel()
        {
            InitializeDrawingTools();
            InitializeFilters();

            AddImageCommand = new RelayCommand(AddImage);
            RotateImageCommand = new RelayCommand(RotateImage, () => HasImage);
            ClearImageCommand = new RelayCommand(ClearImage, () => HasImage);
            SelectColorCommand = new RelayCommand<string>(SelectColor);
            BrushCommand = new RelayCommand(SwitchToBrush);
            EraserCommand = new RelayCommand(SwitchToEraser);
            ApplyFilterCommand = new RelayCommand(ApplyFilter, () => HasImage && SelectedFilter != null);
            ResetFilterCommand = new RelayCommand(ResetFilter, () => HasImage && CurrentImage?.IsFiltered == true);
            SaveAsJpgCommand = new RelayCommand(SaveAsJpg, () => HasImage);
            UndoStrokeCommand = new RelayCommand(UndoLastStroke, () => CanUndo && CurrentInkCanvas != null);
        }

        private void InitializeDrawingTools()
        {
            DrawingAttributes = new DrawingAttributes
            {
                Color = Colors.Black,
                Width = BrushSize,
                Height = BrushSize,
                FitToCurve = true
            };

            CurrentColorBrush = new SolidColorBrush(Colors.Black);
        }

        private void InitializeFilters()
        {
            AvailableFilters = new ObservableCollection<ImageFilter>
            {
                new BlackWhiteFilter(),
                new WarmFilter(),
                new NegativeFilter(),
                new BlurFilter()
            };
        }

        private void SelectColor(string colorName)
        {
            Color color = Colors.Black;

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
                case "black":
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
                CanvasWidth = CurrentImage.Width;
                CanvasHeight = CurrentImage.Height;
                ClearStrokeHistory();
            }
        }

        private void RotateImage()
        {
            if (CurrentImage != null)
            {
                CurrentImage.Angle = (CurrentImage.Angle + 90) % 360;

                OnPropertyChanged(nameof(CanvasWidth));
                OnPropertyChanged(nameof(CanvasHeight));
                OnPropertyChanged(nameof(CurrentImage));
            }
        }

        private void ClearImage()
        {
            CurrentImage = null;
            SelectedFilter = null;
            CanvasWidth = 1920;
            CanvasHeight = 1080;
            ClearStrokeHistory();
        }

        private void ApplyFilter()
        {
            if (CurrentImage != null && SelectedFilter != null)
            {
                CurrentImage.ApplyFilter(SelectedFilter);
                ResetFilterCommand.NotifyCanExecuteChanged();
            }
        }

        private void ResetFilter()
        {
            if (CurrentImage != null)
            {
                CurrentImage.ResetFilter();
                ResetFilterCommand.NotifyCanExecuteChanged();
            }
        }

        public void SaveStrokeToUndo(Stroke stroke)
        {
            if (stroke != null && CurrentInkCanvas != null)
            {
                Stroke strokeCopy = stroke.Clone();
                _undoStack.Push(strokeCopy);

                if (_undoStack.Count > MAX_UNDO_STEPS)
                {
                    var tempStack = new Stack<Stroke>();
                    var array = _undoStack.ToArray();

                    for (int i = array.Length - 1; i >= array.Length - MAX_UNDO_STEPS; i--)
                    {
                        tempStack.Push(array[i]);
                    }
                    _undoStack = tempStack;
                }

                UpdateUndoState();
            }
        }

        private void UndoLastStroke()
        {
            if (_undoStack.Count > 0 && CurrentInkCanvas != null)
            {
                Stroke lastStroke = _undoStack.Pop();

                if (CurrentInkCanvas.Strokes.Count > 0)
                {
                    CurrentInkCanvas.Strokes.RemoveAt(CurrentInkCanvas.Strokes.Count - 1);
                }

                UpdateUndoState();
            }
        }

        private void UpdateUndoState()
        {
            CanUndo = _undoStack.Count > 0;
            UndoStrokeCommand.NotifyCanExecuteChanged();
        }

        public void ClearStrokeHistory()
        {
            _undoStack.Clear();
            UpdateUndoState();
        }

        private void UpdateCommands()
        {
            RotateImageCommand.NotifyCanExecuteChanged();
            ClearImageCommand.NotifyCanExecuteChanged();
            ApplyFilterCommand.NotifyCanExecuteChanged();
            ResetFilterCommand.NotifyCanExecuteChanged();
            SaveAsJpgCommand.NotifyCanExecuteChanged();
            UndoStrokeCommand.NotifyCanExecuteChanged();
        }

        private void SaveAsJpg()
        {
            if (CurrentInkCanvas == null)
            {
                MessageBox.Show("InkCanvas не доступен для сохранения.", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "JPEG Image|*.jpg;*.jpeg",
                DefaultExt = ".jpg",
                Title = "Сохранить как JPG"
            };

            if (saveDialog.ShowDialog() == true)
            {
                SaveInkCanvasToJpeg(CurrentInkCanvas, saveDialog.FileName, 90);
                MessageBox.Show("Изображение успешно сохранено в JPG!", "Сохранение",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SaveInkCanvasToJpeg(InkCanvas inkCanvas, string filePath, int quality)
        {
            Rect bounds = VisualTreeHelper.GetDescendantBounds(inkCanvas);

            if (bounds.IsEmpty)
                bounds = new Rect(0, 0, inkCanvas.ActualWidth, inkCanvas.ActualHeight);

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)bounds.Width,
                (int)bounds.Height,
                96, 96, PixelFormats.Pbgra32);

            rtb.Render(inkCanvas);

            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = quality;
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fs);
            }
        }
    }
}