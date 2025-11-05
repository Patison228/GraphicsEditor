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

                MainInkCanvas.StrokeCollected += InkCanvas_StrokeCollected;

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
                                vm.ClearStrokeHistory();
                            }
                            break;

                        case nameof(vm.BrushSize):
                            if (MainInkCanvas.DefaultDrawingAttributes != null)
                            {
                                MainInkCanvas.DefaultDrawingAttributes.Width = vm.BrushSize;
                                MainInkCanvas.DefaultDrawingAttributes.Height = vm.BrushSize;
                            }
                            break;
                    }
                };
            }
        }

        private void InkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SaveStrokeToUndo(e.Stroke);
            }
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Z &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                if (DataContext is MainViewModel viewModel && viewModel.UndoStrokeCommand.CanExecute(null))
                {
                    viewModel.UndoStrokeCommand.Execute(null);
                    e.Handled = true;
                }
            }
            base.OnKeyDown(e);
        }

        protected override void OnClosed(System.EventArgs e)
        {
            if (MainInkCanvas != null)
            {
                MainInkCanvas.StrokeCollected -= InkCanvas_StrokeCollected;
            }
            base.OnClosed(e);
        }
    }
}