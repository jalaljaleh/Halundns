// Halun DNS - Infrastructure Converters
// Purpose: WPF value converters used in bindings.
// Author: Jalal Jaleh
// License: MIT

using System;
using System.Globalization;
using System.Windows.Data;

namespace Halun.HalunDns.Windows
{
    public class NullToBooleanConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = value != null;
            return Invert ? !result : result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return Binding.DoNothing;
        }
    }
}
