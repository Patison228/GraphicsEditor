using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using GraphicsEditor.ViewModels;

namespace GraphicsEditor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new MainViewModel();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;

            if (vm != null)
            {
                vm.CurrentInkCanvas = MainInkCanvas;

                MainInkCanvas.DefaultDrawingAttributes = vm.DrawingAttributes;
                MainInkCanvas.EditingMode = vm.IsEraserMode ?
                    InkCanvasEditingMode.EraseByPoint : InkCanvasEditingMode.Ink;

                vm.PropertyChanged += (s, args) =>
                {
                    switch (args.PropertyName)
                    {
                        case nameof(vm.DrawingAttributes):
                            MainInkCanvas.DefaultDrawingAttributes = vm.DrawingAttributes;
                            break;

                        case nameof(vm.IsEraserMode):
                            MainInkCanvas.EditingMode = vm.IsEraserMode ?
                                InkCanvasEditingMode.EraseByPoint : InkCanvasEditingMode.Ink;
                            break;

                        case nameof(vm.CurrentImage):
                            if (vm.CurrentImage == null)
                            {
                                MainInkCanvas.Strokes?.Clear();
                            }
                            break;
                    }
                };
            }
        }

        // Обработчик очистки канваса (опционально)
        private void ClearStrokes()
        {
            MainInkCanvas.Strokes.Clear();
        }
    }
}