using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.IO;

namespace GraphicsEditor
{
    /// <summary>
    /// Главное окно редактора изображений
    /// </summary>
    public partial class MainWindow : Window
    {
        // Основные переменные
        private WriteableBitmap currentBitmap;
        private Stack<WriteableBitmap> undoStack = new Stack<WriteableBitmap>();
        private Stack<WriteableBitmap> redoStack = new Stack<WriteableBitmap>();

        // Режимы работы
        private bool isDrawing = false;
        private bool isSelecting = false;
        private Point lastPoint;
        private Point selectionStart;

        // Настройки кисти
        private Color brushColor = Colors.Black;
        private int originalBrightness = 0;

        public MainWindow()
        {
            InitializeComponent();
            CreateDefaultImage();
        }

        // ==================== ФАЙЛОВЫЕ ОПЕРАЦИИ ====================

        /// <summary>
        /// Создание пустого холста
        /// </summary>
        private void CreateDefaultImage()
        {
            currentBitmap = new WriteableBitmap(800, 600, 96, 96, PixelFormats.Bgra32, null);
            FillBitmap(currentBitmap, Colors.White);
            MainImage.Source = currentBitmap;
            UpdateCanvasSize();
        }

        /// <summary>
        /// Открыть изображение
        /// </summary>
        private void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(openDialog.FileName));
                    currentBitmap = new WriteableBitmap(bitmap);
                    MainImage.Source = currentBitmap;
                    undoStack.Clear();
                    redoStack.Clear();
                    BrightnessSlider.Value = 0;
                    originalBrightness = 0;
                    UpdateCanvasSize();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка");
                }
            }
        }

        /// <summary>
        /// Сохранить изображение
        /// </summary>
        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmap == null) return;

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|BMP Image|*.bmp",
                DefaultExt = ".png"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    BitmapEncoder encoder = null;
                    string ext = Path.GetExtension(saveDialog.FileName).ToLower();

                    switch (ext)
                    {
                        case ".png":
                            encoder = new PngBitmapEncoder();
                            break;
                        case ".jpg":
                        case ".jpeg":
                            encoder = new JpegBitmapEncoder();
                            break;
                        case ".bmp":
                            encoder = new BmpBitmapEncoder();
                            break;
                        default:
                            encoder = new PngBitmapEncoder();
                            break;
                    }

                    encoder.Frames.Add(BitmapFrame.Create(currentBitmap));
                    using (var stream = File.Create(saveDialog.FileName))
                    {
                        encoder.Save(stream);
                    }
                    MessageBox.Show("Изображение сохранено!", "Успех");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
                }
            }
        }

        /// <summary>
        /// Создать новый холст
        /// </summary>
        private void NewCanvas_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Создать новый холст? Несохраненные изменения будут потеряны.",
                "Подтверждение", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                CreateDefaultImage();
                undoStack.Clear();
                redoStack.Clear();
                BrightnessSlider.Value = 0;
            }
        }

        // ==================== ИСТОРИЯ (UNDO/REDO) ====================

        /// <summary>
        /// Сохранить текущее состояние для отмены
        /// </summary>
        private void SaveToHistory()
        {
            if (currentBitmap != null)
            {
                undoStack.Push(CloneBitmap(currentBitmap));
                redoStack.Clear();
            }
        }

        /// <summary>
        /// Отмена действия (Ctrl+Z)
        /// </summary>
        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push(CloneBitmap(currentBitmap));
                currentBitmap = undoStack.Pop();
                MainImage.Source = currentBitmap;
            }
        }

        /// <summary>
        /// Повтор действия (Ctrl+Y)
        /// </summary>
        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push(CloneBitmap(currentBitmap));
                currentBitmap = redoStack.Pop();
                MainImage.Source = currentBitmap;
            }
        }

        // ==================== ФИЛЬТРЫ ====================

        /// <summary>
        /// Изменение яркости
        /// </summary>
        private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentBitmap == null) return;

            int newValue = (int)BrightnessSlider.Value;
            int change = newValue - originalBrightness;

            if (change != 0)
            {
                ApplyBrightness(change);
                originalBrightness = newValue;
            }
        }

        /// <summary>
        /// Применить изменение яркости
        /// </summary>
        private void ApplyBrightness(int amount)
        {
            SaveToHistory();
            ProcessPixels((r, g, b, a) =>
            {
                r = Clamp(r + amount);
                g = Clamp(g + amount);
                b = Clamp(b + amount);
                return (r, g, b, a);
            });
        }

        /// <summary>
        /// Размытие Гаусса
        /// </summary>
        private void Blur_Click(object sender, RoutedEventArgs e)
        {
            SaveToHistory();
            ApplyGaussianBlur(currentBitmap, 5);
            MainImage.Source = currentBitmap;
        }

        /// <summary>
        /// Черно-белый фильтр
        /// </summary>
        private void Grayscale_Click(object sender, RoutedEventArgs e)
        {
            SaveToHistory();
            ProcessPixels((r, g, b, a) =>
            {
                byte gray = (byte)(0.299 * r + 0.587 * g + 0.114 * b);
                return (gray, gray, gray, a);
            });
        }

        /// <summary>
        /// Негатив
        /// </summary>
        private void Invert_Click(object sender, RoutedEventArgs e)
        {
            SaveToHistory();
            ProcessPixels((r, g, b, a) =>
            {
                return ((byte)(255 - r), (byte)(255 - g), (byte)(255 - b), a);
            });
        }

        /// <summary>
        /// Сепия
        /// </summary>
        private void Sepia_Click(object sender, RoutedEventArgs e)
        {
            SaveToHistory();
            ProcessPixels((r, g, b, a) =>
            {
                int tr = (int)(0.393 * r + 0.769 * g + 0.189 * b);
                int tg = (int)(0.349 * r + 0.686 * g + 0.168 * b);
                int tb = (int)(0.272 * r + 0.534 * g + 0.131 * b);
                return ((byte)Clamp(tr), (byte)Clamp(tg), (byte)Clamp(tb), a);
            });
        }

        /// <summary>
        /// Повышение резкости
        /// </summary>
        private void Sharpen_Click(object sender, RoutedEventArgs e)
        {
            SaveToHistory();
            ApplyConvolution(currentBitmap, new double[,] {
                { 0, -1, 0 },
                { -1, 5, -1 },
                { 0, -1, 0 }
            });
            MainImage.Source = currentBitmap;
        }

        // ==================== ПОВОРОТ ====================

        /// <summary>
        /// Поворот влево на 90 градусов
        /// </summary>
        private void RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            SaveToHistory();
            currentBitmap = RotateBitmap(currentBitmap, -90);
            MainImage.Source = currentBitmap;
            UpdateCanvasSize();
        }

        /// <summary>
        /// Поворот вправо на 90 градусов
        /// </summary>
        private void RotateRight_Click(object sender, RoutedEventArgs e)
        {
            SaveToHistory();
            currentBitmap = RotateBitmap(currentBitmap, 90);
            MainImage.Source = currentBitmap;
            UpdateCanvasSize();
        }

        // ==================== ВЫДЕЛЕНИЕ И КРОП ====================

        /// <summary>
        /// Переключение режима выделения
        /// </summary>
        private void SelectionMode_Changed(object sender, RoutedEventArgs e)
        {
            if (SelectionModeCheck.IsChecked == true)
            {
                DrawModeCheck.IsChecked = false;
            }
        }

        /// <summary>
        /// Обрезка по выделенной области
        /// </summary>
        private void Crop_Click(object sender, RoutedEventArgs e)
        {
            if (SelectionRect.Visibility != Visibility.Visible)
            {
                MessageBox.Show("Сначала выделите область!", "Внимание");
                return;
            }

            SaveToHistory();

            int x = (int)Canvas.GetLeft(SelectionRect);
            int y = (int)Canvas.GetTop(SelectionRect);
            int width = (int)SelectionRect.Width;
            int height = (int)SelectionRect.Height;

            currentBitmap = CropBitmap(currentBitmap, x, y, width, height);
            MainImage.Source = currentBitmap;
            ClearSelection_Click(null, null);
            UpdateCanvasSize();
        }

        /// <summary>
        /// Очистить выделение
        /// </summary>
        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            SelectionRect.Visibility = Visibility.Collapsed;
        }

        // ==================== РИСОВАНИЕ ====================

        /// <summary>
        /// Переключение режима рисования
        /// </summary>
        private void DrawMode_Changed(object sender, RoutedEventArgs e)
        {
            if (DrawModeCheck.IsChecked == true)
            {
                SelectionModeCheck.IsChecked = false;
            }
        }

        /// <summary>
        /// Выбор цвета кисти
        /// </summary>
        private void BrushColor_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null)
            {
                var colorName = button.Tag.ToString();
                brushColor = (Color)ColorConverter.ConvertFromString(colorName);
            }
        }

        // ==================== МЫШЬ ====================

        /// <summary>
        /// Обработка нажатия мыши
        /// </summary>
        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (currentBitmap == null) return;

            Point pos = e.GetPosition(MainImage);

            if (DrawModeCheck.IsChecked == true)
            {
                SaveToHistory();
                isDrawing = true;
                lastPoint = pos;
                DrawPoint(pos);
            }
            else if (SelectionModeCheck.IsChecked == true)
            {
                isSelecting = true;
                selectionStart = pos;
                SelectionRect.Visibility = Visibility.Visible;
                Canvas.SetLeft(SelectionRect, pos.X);
                Canvas.SetTop(SelectionRect, pos.Y);
                SelectionRect.Width = 0;
                SelectionRect.Height = 0;
            }
        }

        /// <summary>
        /// Обработка движения мыши
        /// </summary>
        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (currentBitmap == null) return;

            Point pos = e.GetPosition(MainImage);

            if (isDrawing)
            {
                DrawLine(lastPoint, pos);
                lastPoint = pos;
            }
            else if (isSelecting)
            {
                double x = Math.Min(pos.X, selectionStart.X);
                double y = Math.Min(pos.Y, selectionStart.Y);
                double width = Math.Abs(pos.X - selectionStart.X);
                double height = Math.Abs(pos.Y - selectionStart.Y);

                Canvas.SetLeft(SelectionRect, x);
                Canvas.SetTop(SelectionRect, y);
                SelectionRect.Width = width;
                SelectionRect.Height = height;
            }
        }

        /// <summary>
        /// Обработка отпускания мыши
        /// </summary>
        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDrawing = false;
            isSelecting = false;
        }

        // ==================== ГОРЯЧИЕ КЛАВИШИ ====================

        /// <summary>
        /// Обработка горячих клавиш
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            bool ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            if (ctrl)
            {
                switch (e.Key)
                {
                    case Key.Z:
                        Undo_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.Y:
                        Redo_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.S:
                        SaveImage_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.O:
                        OpenImage_Click(null, null);
                        e.Handled = true;
                        break;
                }
            }
        }

        // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ====================

        /// <summary>
        /// Обработка пикселей с функцией преобразования
        /// </summary>
        private void ProcessPixels(Func<byte, byte, byte, byte, (byte, byte, byte, byte)> transform)
        {
            int width = currentBitmap.PixelWidth;
            int height = currentBitmap.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            currentBitmap.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                byte a = pixels[i + 3];

                var result = transform(r, g, b, a);
                pixels[i + 2] = result.Item1;
                pixels[i + 1] = result.Item2;
                pixels[i] = result.Item3;
                pixels[i + 3] = result.Item4;
            }

            currentBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        }

        /// <summary>
        /// Размытие Гаусса
        /// </summary>
        private void ApplyGaussianBlur(WriteableBitmap bitmap, int radius)
        {
            double[,] kernel = CreateGaussianKernel(radius);
            ApplyConvolution(bitmap, kernel);
        }

        /// <summary>
        /// Создание ядра Гаусса
        /// </summary>
        private double[,] CreateGaussianKernel(int radius)
        {
            int size = radius * 2 + 1;
            double[,] kernel = new double[size, size];
            double sigma = radius / 3.0;
            double sum = 0;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    double value = Math.Exp(-(x * x + y * y) / (2 * sigma * sigma));
                    kernel[y + radius, x + radius] = value;
                    sum += value;
                }
            }

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    kernel[y, x] /= sum;

            return kernel;
        }

        /// <summary>
        /// Применение свертки (convolution)
        /// </summary>
        private void ApplyConvolution(WriteableBitmap bitmap, double[,] kernel)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            byte[] result = new byte[height * stride];

            bitmap.CopyPixels(pixels, stride, 0);

            int kSize = kernel.GetLength(0);
            int kRadius = kSize / 2;

            for (int y = kRadius; y < height - kRadius; y++)
            {
                for (int x = kRadius; x < width - kRadius; x++)
                {
                    double sumR = 0, sumG = 0, sumB = 0;

                    for (int ky = 0; ky < kSize; ky++)
                    {
                        for (int kx = 0; kx < kSize; kx++)
                        {
                            int py = y + ky - kRadius;
                            int px = x + kx - kRadius;
                            int idx = py * stride + px * 4;

                            double weight = kernel[ky, kx];
                            sumB += pixels[idx] * weight;
                            sumG += pixels[idx + 1] * weight;
                            sumR += pixels[idx + 2] * weight;
                        }
                    }

                    int index = y * stride + x * 4;
                    result[index] = (byte)Clamp((int)sumB);
                    result[index + 1] = (byte)Clamp((int)sumG);
                    result[index + 2] = (byte)Clamp((int)sumR);
                    result[index + 3] = pixels[index + 3];
                }
            }

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), result, stride, 0);
        }

        /// <summary>
        /// Рисование точки
        /// </summary>
        private void DrawPoint(Point point)
        {
            int brushSize = (int)BrushSizeSlider.Value;
            int x = (int)point.X;
            int y = (int)point.Y;

            currentBitmap.Lock();
            for (int dy = -brushSize / 2; dy <= brushSize / 2; dy++)
            {
                for (int dx = -brushSize / 2; dx <= brushSize / 2; dx++)
                {
                    if (dx * dx + dy * dy <= (brushSize / 2) * (brushSize / 2))
                    {
                        int px = x + dx;
                        int py = y + dy;
                        if (px >= 0 && px < currentBitmap.PixelWidth &&
                            py >= 0 && py < currentBitmap.PixelHeight)
                        {
                            SetPixel(px, py, brushColor);
                        }
                    }
                }
            }
            currentBitmap.Unlock();
            currentBitmap.AddDirtyRect(new Int32Rect(0, 0, currentBitmap.PixelWidth, currentBitmap.PixelHeight));
        }

        /// <summary>
        /// Рисование линии
        /// </summary>
        private void DrawLine(Point start, Point end)
        {
            int x0 = (int)start.X;
            int y0 = (int)start.Y;
            int x1 = (int)end.X;
            int y1 = (int)end.Y;

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                DrawPoint(new Point(x0, y0));

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        /// <summary>
        /// Установка цвета пикселя
        /// </summary>
        private unsafe void SetPixel(int x, int y, Color color)
        {
            if (x < 0 || x >= currentBitmap.PixelWidth ||
                y < 0 || y >= currentBitmap.PixelHeight) return;

            IntPtr buffer = currentBitmap.BackBuffer;
            int stride = currentBitmap.BackBufferStride;

            buffer += y * stride + x * 4;
            int colorData = color.B | (color.G << 8) | (color.R << 16) | (color.A << 24);
            *(int*)buffer = colorData;
        }

        /// <summary>
        /// Обрезка изображения
        /// </summary>
        private WriteableBitmap CropBitmap(WriteableBitmap source, int x, int y, int width, int height)
        {
            var cropped = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            source.CopyPixels(new Int32Rect(x, y, width, height), pixels, stride, 0);
            cropped.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);

            return cropped;
        }

        /// <summary>
        /// Поворот изображения
        /// </summary>
        private WriteableBitmap RotateBitmap(WriteableBitmap source, double angle)
        {
            var transform = new RotateTransform(angle);
            var transformed = new TransformedBitmap(source, transform);
            return new WriteableBitmap(transformed);
        }

        /// <summary>
        /// Клонирование битмапа
        /// </summary>
        private WriteableBitmap CloneBitmap(WriteableBitmap source)
        {
            var clone = new WriteableBitmap(source.PixelWidth, source.PixelHeight,
                96, 96, PixelFormats.Bgra32, null);
            int stride = source.PixelWidth * 4;
            byte[] pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(pixels, stride, 0);
            clone.WritePixels(new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight),
                pixels, stride, 0);
            return clone;
        }

        /// <summary>
        /// Заполнение битмапа цветом
        /// </summary>
        private void FillBitmap(WriteableBitmap bitmap, Color color)
        {
            int stride = bitmap.PixelWidth * 4;
            byte[] pixels = new byte[bitmap.PixelHeight * stride];

            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = color.B;
                pixels[i + 1] = color.G;
                pixels[i + 2] = color.R;
                pixels[i + 3] = color.A;
            }

            bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                pixels, stride, 0);
        }

        /// <summary>
        /// Обновление размера канваса выделения
        /// </summary>
        private void UpdateCanvasSize()
        {
            if (currentBitmap != null)
            {
                SelectionCanvas.Width = currentBitmap.PixelWidth;
                SelectionCanvas.Height = currentBitmap.PixelHeight;
            }
        }

        /// <summary>
        /// Ограничение значения в диапазоне 0-255
        /// </summary>
        private int Clamp(int value)
        {
            return Math.Max(0, Math.Min(255, value));
        }
    }
}