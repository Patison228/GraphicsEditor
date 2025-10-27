using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;

namespace GraphicsEditor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                var vm = DataContext as ViewModels.MainViewModel;

                if (vm != null)
                {
                    MainInkCanvas.DefaultDrawingAttributes = vm.DrawingAttributes;

                    vm.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(vm.DrawingAttributes))
                        {
                            MainInkCanvas.DefaultDrawingAttributes = vm.DrawingAttributes;
                        }
                        else if (args.PropertyName == nameof(vm.IsEraserMode))
                        {
                            MainInkCanvas.EditingMode = vm.IsEraserMode ?
                                InkCanvasEditingMode.EraseByPoint : InkCanvasEditingMode.Ink;
                        }
                    };
                }
            };
        }
    }
}