using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Windows.Forms;

namespace GraphicsEditor
{
    /// <summary>
    /// Code-behind с полными обработчиками событий XAML
    /// </summary>
    public partial class MainWindow : Window
    {
        // Переменные состояния
        private string currentTool = "brush";
        private bool isDrawing = false;
        private bool isSelecting = false;
        private Point lastPoint;
        private Point selectionStart;
        private int brushSize = 5;
        private Color brushColor = Colors.Black;

        // История для Undo
        private Stack<RenderTargetBitmap> history = new Stack<RenderTargetBitmap>();
        private const int MaxHistorySize = 20;

        public MainWindow()
        {
            InitializeComponent();
            SaveState();
        }

        // === ОБРАБОТЧИКИ ИНСТРУМЕНТОВ ===

        private void SetBrush_Click(object sender, RoutedEventArgs e)
        {
            currentTool = "brush";
            BrushBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
            SelectBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            CropBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            SelectionBorder.Visibility = Visibility.Collapsed;
            StatusText.Text = "Инструмент: Кисть";
        }

        private void SetSelect_Click(object sender, RoutedEventArgs e)
        {
            currentTool = "select";
            SelectBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
            BrushBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            CropBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            SelectionBorder.Visibility = Visibility.Collapsed;
            StatusText.Text = "Инструмент: Выделение";
        }

        private void SetCrop_Click(object sender, RoutedEventArgs e)
        {
            currentTool = "crop";
            CropBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
            BrushBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            SelectBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            SelectionBorder.Visibility = Visibility.Collapsed;
            StatusText.Text = "Инструмент: Обрезка";
        }

        // === ОБРАБОТЧИКИ CANVAS ===

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (currentTool == "brush")
            {
                isDrawing = true;
                lastPoint = e.GetPosition(DrawingCanvas);
            }
            else if (currentTool == "select" || currentTool == "crop")
            {
                isSelecting = true;
                selectionStart = e.GetPosition(DrawingCanvas);
                SelectionBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPoint = e.GetPosition(DrawingCanvas);

            if (isDrawing && currentTool == "brush")
            {
                Line line = new Line
                {
                    Stroke = new SolidColorBrush(brushColor),
                    StrokeThickness = brushSize,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    X1 = lastPoint.X,
                    Y1 = lastPoint.Y,
                    X2 = currentPoint.X,
                    Y2 = currentPoint.Y
                };

                DrawingCanvas.Children.Add(line);
                lastPoint = currentPoint;
            }
            else if (isSelecting && (currentTool == "select" || currentTool == "crop"))
            {
                double left = Math.Min(selectionStart.X, currentPoint.X);
                double top = Math.Min(selectionStart.Y, currentPoint.Y);
                double width = Math.Abs(currentPoint.X - selectionStart.X);
                double height = Math.Abs(currentPoint.Y - selectionStart.Y);

                Canvas.SetLeft(SelectionBorder, left);
                Canvas.SetTop(SelectionBorder, top);
                SelectionBorder.Width = width;
                SelectionBorder.Height = height;
                SelectionBorder.Visibility = Visibility.Visible;
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDrawing && currentTool == "brush")
            {
                isDrawing = false;
                SaveState();
            }
            else if (isSelecting && (currentTool == "select" || currentTool == "crop"))
            {
                isSelecting = false;

                if (currentTool == "crop" && SelectionBorder.Visibility == Visibility.Visible)
                {
                    CropImage();
                }
            }
        }

        // === ОБРАБОТЧИКИ КИСТИ ===

        private void BrushSize_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            brushSize = (int)BrushSizeSlider.Value;
            if (BrushSizeLabel != null)
                BrushSizeLabel.Text = brushSize.ToString();
        }

        private void ColorPicker_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                brushColor = Color.FromArgb(
                    colorDialog.Color.A,
                    colorDialog.Color.R,
                    colorDialog.Color.G,
                    colorDialog.Color.B);
                ColorBtn.Background = new SolidColorBrush(brushColor);
            }
        }

        private void PresetColor_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string colorHex = button.Tag.ToString();
            brushColor = (Color)ColorConverter.ConvertFromString(colorHex);
            ColorBtn.Background = new SolidColorBrush(brushColor);
        }

        // === ОБРЕЗКА ===

        private void CropImage()
        {
            if (SelectionBorder.Visibility != Visibility.Visible) return;

            SaveState();

            double left = Canvas.GetLeft(SelectionBorder);
            double top = Canvas.GetTop(SelectionBorder);
            double width = SelectionBorder.Width;
            double height = SelectionBorder.Height;

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)DrawingCanvas.Width,
                (int)DrawingCanvas.Height,
                96, 96, PixelFormats.Pbgra32);
            rtb.Render(DrawingCanvas);

            CroppedBitmap croppedBitmap = new CroppedBitmap(rtb,
                new Int32Rect((int)left, (int)top, (int)width, (int)height));

            DrawingCanvas.Children.Clear();
            DrawingCanvas.Width = width;
            DrawingCanvas.Height = height;

            Image img = new Image { Source = croppedBitmap };
            DrawingCanvas.Children.Add(img);

            SelectionBorder.Visibility = Visibility.Collapsed;
            StatusText.Text = "Изображение обрезано";
        }

        // === ФИЛЬТРЫ ===

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (FilterCombo.SelectedIndex == 0) return;

            var item = FilterCombo.SelectedItem as ComboBoxItem;
            string filterType = item.Tag?.ToString();

            if (!string.IsNullOrEmpty(filterType))
            {
                SaveState();
                ApplyFilter(filterType);
                FilterCombo.SelectedIndex = 0;
            }
        }

        private void ApplyFilter(string filterType)
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)DrawingCanvas.Width,
                (int)DrawingCanvas.Height,
                96, 96, PixelFormats.Pbgra32);
            rtb.Render(DrawingCanvas);

            int stride = (int)DrawingCanvas.Width * 4;
            byte[] pixels = new byte[(int)DrawingCanvas.Height * stride];
            rtb.CopyPixels(pixels, stride, 0);

            switch (filterType)
            {
                case "brightness":
                    for (int i = 0; i < pixels.Length; i += 4)
                    {
                        pixels[i] = (byte)Math.Min(255, pixels[i] * 1.3);
                        pixels[i + 1] = (byte)Math.Min(255, pixels[i + 1] * 1.3);
                        pixels[i + 2] = (byte)Math.Min(255, pixels[i + 2] * 1.3);
                    }
                    break;

                case "grayscale":
                    for (int i = 0; i < pixels.Length; i += 4)
                    {
                        byte gray = (byte)(pixels[i + 2] * 0.299 + pixels[i + 1] * 0.587 + pixels[i] * 0.114);
                        pixels[i] = pixels[i + 1] = pixels[i + 2] = gray;
                    }
                    break;

                case "negative":
                    for (int i = 0; i < pixels.Length; i += 4)
                    {
                        pixels[i] = (byte)(255 - pixels[i]);
                        pixels[i + 1] = (byte)(255 - pixels[i + 1]);
                        pixels[i + 2] = (byte)(255 - pixels[i + 2]);
                    }
                    break;

                case "warm":
                    for (int i = 0; i < pixels.Length; i += 4)
                    {
                        pixels[i + 2] = (byte)Math.Min(255, pixels[i + 2] * 1.15);
                        pixels[i + 1] = (byte)Math.Min(255, pixels[i + 1] * 1.05);
                        pixels[i] = (byte)Math.Max(0, pixels[i] * 0.9);
                    }
                    break;

                case "blur":
                    ApplyBlurFilter(pixels, (int)DrawingCanvas.Width, (int)DrawingCanvas.Height);
                    break;

                case "contrast":
                    for (int i = 0; i < pixels.Length; i += 4)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int value = (int)((pixels[i + c] - 128) * 1.5 + 128);
                            pixels[i + c] = (byte)Math.Max(0, Math.Min(255, value));
                        }
                    }
                    break;
            }

            BitmapSource bitmap = BitmapSource.Create(
                (int)DrawingCanvas.Width,
                (int)DrawingCanvas.Height,
                96, 96, PixelFormats.Bgra32, null, pixels, stride);

            DrawingCanvas.Children.Clear();
            Image img = new Image { Source = bitmap };
            DrawingCanvas.Children.Add(img);

            StatusText.Text = $"Фильтр применен: {filterType}";
        }

        private void ApplyBlurFilter(byte[] pixels, int width, int height)
        {
            byte[] temp = (byte[])pixels.Clone();
            int stride = width * 4;

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        int sum = 0;
                        for (int ky = -1; ky <= 1; ky++)
                        {
                            for (int kx = -1; kx <= 1; kx++)
                            {
                                int idx = ((y + ky) * width + (x + kx)) * 4 + c;
                                sum += temp[idx];
                            }
                        }
                        pixels[(y * width + x) * 4 + c] = (byte)(sum / 9);
                    }
                }
            }
        }

        // === ПОВОРОТ ===

        private void RotateRight_Click(object sender, RoutedEventArgs e)
        {
            RotateCanvas(90);
        }

        private void RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            RotateCanvas(-90);
        }

        private void RotateCanvas(double angle)
        {
            SaveState();

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)DrawingCanvas.Width,
                (int)DrawingCanvas.Height,
                96, 96, PixelFormats.Pbgra32);
            rtb.Render(DrawingCanvas);

            TransformedBitmap transformedBitmap = new TransformedBitmap();
            transformedBitmap.BeginInit();
            transformedBitmap.Source = rtb;
            transformedBitmap.Transform = new RotateTransform(angle);
            transformedBitmap.EndInit();

            DrawingCanvas.Children.Clear();
            DrawingCanvas.Width = transformedBitmap.PixelWidth;
            DrawingCanvas.Height = transformedBitmap.PixelHeight;

            Image img = new Image { Source = transformedBitmap };
            DrawingCanvas.Children.Add(img);

            StatusText.Text = $"Повернуто на {angle}°";
        }

        // === ИСТОРИЯ (UNDO) ===

        private void SaveState()
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)DrawingCanvas.Width,
                (int)DrawingCanvas.Height,
                96, 96, PixelFormats.Pbgra32);
            rtb.Render(DrawingCanvas);

            history.Push(rtb);

            if (history.Count > MaxHistorySize)
            {
                var temp = new Stack<RenderTargetBitmap>();
                for (int i = 0; i < MaxHistorySize; i++)
                {
                    temp.Push(history.Pop());
                }
                history = new Stack<RenderTargetBitmap>(temp);
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (history.Count > 1)
            {
                history.Pop();
                var previousState = history.Peek();

                DrawingCanvas.Children.Clear();
                Image img = new Image { Source = previousState };
                DrawingCanvas.Children.Add(img);

                StatusText.Text = "Отменено действие";
            }
            else
            {
                StatusText.Text = "Нет действий для отмены";
            }
        }

        // === ФАЙЛОВЫЕ ОПЕРАЦИИ ===

        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Создать новый холст?", "Новый файл",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                DrawingCanvas.Children.Clear();
                DrawingCanvas.Width = 800;
                DrawingCanvas.Height = 600;
                DrawingCanvas.Background = Brushes.White;
                history.Clear();
                SaveState();
                StatusText.Text = "Создан новый холст";
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));

                DrawingCanvas.Width = bitmap.PixelWidth;
                DrawingCanvas.Height = bitmap.PixelHeight;
                DrawingCanvas.Children.Clear();

                Image img = new Image { Source = bitmap };
                DrawingCanvas.Children.Add(img);

                history.Clear();
                SaveState();
                StatusText.Text = $"Изображение загружено";
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG изображение (*.png)|*.png|JPEG изображение (*.jpg)|*.jpg",
                FileName = $"image_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                RenderTargetBitmap rtb = new RenderTargetBitmap(
                    (int)DrawingCanvas.Width,
                    (int)DrawingCanvas.Height,
                    96, 96, PixelFormats.Pbgra32);
                rtb.Render(DrawingCanvas);

                BitmapEncoder encoder;
                if (saveFileDialog.FileName.EndsWith(".jpg"))
                    encoder = new JpegBitmapEncoder();
                else
                    encoder = new PngBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(rtb));

                using (FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                StatusText.Text = "Изображение сохранено";
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Очистить холст?", "Очистка",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SaveState();
                DrawingCanvas.Children.Clear();
                DrawingCanvas.Background = Brushes.White;
                StatusText.Text = "Холст очищен";
            }
        }

        // === ГОРЯЧИЕ КЛАВИШИ ===

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.Z:
                        Undo_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.S:
                        SaveFile_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.D:
                        Clear_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.N:
                        NewFile_Click(null, null);
                        e.Handled = true;
                        break;
                }
            }
        }
    }
}