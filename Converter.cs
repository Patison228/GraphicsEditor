using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace GraphicsEditor.Converters
{
    public class BoolToInkCanvasEditingModeConverter : IValueConverter
    {
        public static BoolToInkCanvasEditingModeConverter Instance { get; } = new BoolToInkCanvasEditingModeConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEraserMode)
            {
                return isEraserMode ? InkCanvasEditingMode.EraseByPoint : InkCanvasEditingMode.Ink;
            }

            return InkCanvasEditingMode.Ink;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InkCanvasEditingMode mode)
            {
                return mode == InkCanvasEditingMode.EraseByPoint;
            }

            return false;
        }
    }
}
