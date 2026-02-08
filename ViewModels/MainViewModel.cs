using GraphicsEditor.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GraphicsEditor.ViewModels
{

    public class MainViewModel : INotifyPropertyChanged
    {

        private readonly HistoryService _historyService;
        private readonly FilterService _filterService;
        private readonly FileService _fileService;
        private readonly DrawingService _drawingService;



        private Canvas _canvas;
        private Border _selectionBorder;

        private string _currentTool = "brush";
        private bool _isDrawing = false;
        private Point _lastPoint;
        private int _brushSize = 5;
        private Color _brushColor = Colors.Black;
        private string _statusText = "Готово";

        private Point _selectionStart;
        private bool _isSelecting = false;

        // Параметры активной области выделения
        private bool _hasActiveSelection = false;
        private double _selectionLeft = 0;
        private double _selectionTop = 0;
        private double _selectionWidth = 0;
        private double _selectionHeight = 0;



        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public int BrushSize
        {
            get => _brushSize;
            set { _brushSize = value; OnPropertyChanged(); }
        }

        public Color BrushColor
        {
            get => _brushColor;
            set { _brushColor = value; OnPropertyChanged(); }
        }

        public string CurrentTool
        {
            get => _currentTool;
            set { _currentTool = value; OnPropertyChanged(); }
        }



        public ICommand NewFileCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand SaveFileCommand { get; }
        public ICommand SetBrushCommand { get; }
        public ICommand SetSelectCommand { get; }
        public ICommand SetCropCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand RotateRightCommand { get; }
        public ICommand RotateLeftCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand ClearCommand { get; }


        public MainViewModel()
        {
            // Инициализация сервисов
            _historyService = new HistoryService();
            _filterService = new FilterService();
            _fileService = new FileService();
            _drawingService = new DrawingService();

            // Инициализация команд
            NewFileCommand = new RelayCommand(_ => NewFile());
            OpenFileCommand = new RelayCommand(_ => OpenFile());
            SaveFileCommand = new RelayCommand(_ => SaveFile());
            SetBrushCommand = new RelayCommand(_ => SetTool("brush"));
            SetSelectCommand = new RelayCommand(_ => SetTool("select"));
            SetCropCommand = new RelayCommand(_ => SetTool("crop"));
            ApplyFilterCommand = new RelayCommand(param => ApplyFilter(param?.ToString()));
            RotateRightCommand = new RelayCommand(_ => Rotate(90));
            RotateLeftCommand = new RelayCommand(_ => Rotate(-90));
            UndoCommand = new RelayCommand(_ => Undo(), _ => _historyService.CanUndo);
            ClearCommand = new RelayCommand(_ => Clear());
        }


        public void Initialize(Canvas canvas, Border selectionBorder)
        {
            _canvas = canvas;
            _selectionBorder = selectionBorder;
            SaveState();
            StatusText = "Графический редактор готов к работе";
        }



        private void SetTool(string tool)
        {
            CurrentTool = tool;
            if (tool == "brush")
            {
                // При переключении на кисть снимаем выделение
                HideSelection();
            }
            StatusText = $"Инструмент: {(tool == "brush" ? "Кисть" : tool == "select" ? "Выделение" : "Обрезка")}";
        }



        public void OnMouseDown(Point position)
        {
            if (CurrentTool == "brush")
            {
                _isDrawing = true;
                _lastPoint = position;
            }
            else if (CurrentTool == "select" || CurrentTool == "crop")
            {
                _isSelecting = true;
                _selectionStart = position;
                HideSelection();
            }
        }
        public void OnMouseMove(Point position)
        {
            if (_isDrawing && CurrentTool == "brush")
            {
                // Проверка, находится ли точка в активной области выделения
                if (_hasActiveSelection)
                {
                    if (!IsPointInSelection(position) && !IsPointInSelection(_lastPoint))
                    {
                        _lastPoint = position;
                        return;
                    }
                }

                Line line = new Line
                {
                    Stroke = new SolidColorBrush(BrushColor),
                    StrokeThickness = BrushSize,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    X1 = _lastPoint.X,
                    Y1 = _lastPoint.Y,
                    X2 = position.X,
                    Y2 = position.Y
                };

                // Обрезка линии по границам выделения
                if (_hasActiveSelection)
                {
                    ClipLineToSelection(line);
                }

                _canvas.Children.Add(line);
                _lastPoint = position;
            }
            else if (_isSelecting && (CurrentTool == "select" || CurrentTool == "crop"))
            {
                UpdateSelectionRectangle(_selectionStart, position);
            }
        }

        public void OnMouseUp(Point position)
        {
            if (_isDrawing && CurrentTool == "brush")
            {
                _isDrawing = false;
                SaveState();
            }
            else if (_isSelecting && (CurrentTool == "select" || CurrentTool == "crop"))
            {
                _isSelecting = false;

                double width = Math.Abs(position.X - _selectionStart.X);
                double height = Math.Abs(position.Y - _selectionStart.Y);

                if (width > 5 && height > 5)
                {
                    UpdateSelectionRectangle(_selectionStart, position);

                    if (CurrentTool == "crop")
                    {
                        CropImage();
                    }
                    else
                    {
                        // Сохраняем параметры выделения
                        _hasActiveSelection = true;
                        _selectionLeft = Canvas.GetLeft(_selectionBorder);
                        _selectionTop = Canvas.GetTop(_selectionBorder);
                        _selectionWidth = _selectionBorder.Width;
                        _selectionHeight = _selectionBorder.Height;
                        StatusText = "Область выделена. Рисование ограничено выделением. Кнопка 'Кисть' снимет выделение.";
                    }
                }
            }
        }



        private void UpdateSelectionRectangle(Point start, Point end)
        {
            // Ограничиваем координаты границами canvas
            double left = Math.Max(0, Math.Min(start.X, end.X));
            double top = Math.Max(0, Math.Min(start.Y, end.Y));
            double right = Math.Min(_canvas.Width, Math.Max(start.X, end.X));
            double bottom = Math.Min(_canvas.Height, Math.Max(start.Y, end.Y));

            double width = right - left;
            double height = bottom - top;

            // Устанавливаем позицию относительно Canvas
            Canvas.SetLeft(_selectionBorder, left);
            Canvas.SetTop(_selectionBorder, top);
            _selectionBorder.Width = width;
            _selectionBorder.Height = height;
            _selectionBorder.Visibility = Visibility.Visible;
        }

        private void HideSelection()
        {
            _selectionBorder.Visibility = Visibility.Collapsed;
            _hasActiveSelection = false;
        }

        private bool IsPointInSelection(Point point)
        {
            return point.X >= _selectionLeft &&
                   point.X <= _selectionLeft + _selectionWidth &&
                   point.Y >= _selectionTop &&
                   point.Y <= _selectionTop + _selectionHeight;
        }

        private void ClipLineToSelection(Line line)
        {
            // Ограничиваем координаты линии границами выделения
            double left = _selectionLeft;
            double top = _selectionTop;
            double right = _selectionLeft + _selectionWidth;
            double bottom = _selectionTop + _selectionHeight;

            line.X1 = Math.Max(left, Math.Min(right, line.X1));
            line.Y1 = Math.Max(top, Math.Min(bottom, line.Y1));
            line.X2 = Math.Max(left, Math.Min(right, line.X2));
            line.Y2 = Math.Max(top, Math.Min(bottom, line.Y2));
        }

        private void CropImage()
        {
            if (_selectionBorder.Visibility != Visibility.Visible) return;

            SaveState();

            double left = Canvas.GetLeft(_selectionBorder);
            double top = Canvas.GetTop(_selectionBorder);
            double width = _selectionBorder.Width;
            double height = _selectionBorder.Height;

            var rtb = _drawingService.RenderCanvas(_canvas);
            var cropped = _drawingService.CropImage(rtb,
                new Int32Rect((int)left, (int)top, (int)width, (int)height));

            _canvas.Children.Clear();
            _canvas.Width = width;
            _canvas.Height = height;

            Image img = new Image { Source = cropped };
            _canvas.Children.Add(img);

            HideSelection();
            StatusText = "Изображение обрезано";
        }



        private void ApplyFilter(string filterType)
        {
            if (string.IsNullOrEmpty(filterType)) return;

            SaveState();

            var rtb = _drawingService.RenderCanvas(_canvas);
            var filtered = _filterService.ApplyFilter(rtb, filterType);

            _canvas.Children.Clear();
            Image img = new Image { Source = filtered };
            _canvas.Children.Add(img);

            StatusText = $"Фильтр применен: {filterType}";
        }



        private void Rotate(double angle)
        {
            SaveState();

            var rtb = _drawingService.RenderCanvas(_canvas);
            var rotated = _drawingService.RotateImage(rtb, angle);

            _canvas.Children.Clear();
            _canvas.Width = rotated.PixelWidth;
            _canvas.Height = rotated.PixelHeight;

            Image img = new Image { Source = rotated };
            _canvas.Children.Add(img);

            StatusText = $"Повернуто на {angle}°";
        }



        private void NewFile()
        {
            if (MessageBox.Show("Создать новый холст?", "Новый файл",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _canvas.Children.Clear();
                _canvas.Width = 800;
                _canvas.Height = 600;
                _canvas.Background = Brushes.White;
                _historyService.Clear();
                SaveState();
                StatusText = "Создан новый холст";
            }
        }

        private void OpenFile()
        {
            var bitmap = _fileService.OpenImage(out int width, out int height);
            if (bitmap != null)
            {
                _canvas.Width = width;
                _canvas.Height = height;
                _canvas.Children.Clear();

                Image img = new Image { Source = bitmap };
                _canvas.Children.Add(img);

                _historyService.Clear();
                SaveState();
                StatusText = "Изображение загружено";
            }
        }

        private void SaveFile()
        {
            var rtb = _drawingService.RenderCanvas(_canvas);
            if (_fileService.SaveImage(rtb))
            {
                StatusText = "Изображение сохранено";
            }
        }

        private void Clear()
        {
            if (MessageBox.Show("Очистить холст?", "Очистка",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SaveState();
                _canvas.Children.Clear();
                _canvas.Background = Brushes.White;
                StatusText = "Холст очищен";
            }
        }



        private void SaveState()
        {
            var rtb = _drawingService.RenderCanvas(_canvas);
            _historyService.SaveState(rtb);
        }

        private void Undo()
        {
            var previousState = _historyService.Undo();
            if (previousState != null)
            {
                _canvas.Children.Clear();
                Image img = new Image { Source = previousState };
                _canvas.Children.Add(img);
                StatusText = "Отменено действие";
            }
            else
            {
                StatusText = "Нет действий для отмены";
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}