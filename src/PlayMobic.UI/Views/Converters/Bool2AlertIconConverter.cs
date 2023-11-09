namespace PlayMobic.UI.Views.Converters;
using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using FluentAvalonia.UI.Controls;

internal class Bool2AlertIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isValid) {
            return isValid ? Symbol.Checkmark : Symbol.ImportantFilled;
        }

        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
