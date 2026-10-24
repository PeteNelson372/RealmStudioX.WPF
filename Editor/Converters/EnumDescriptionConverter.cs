using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;

namespace RealmStudioX.WPF.Editor.Converters;

public sealed class EnumDescriptionConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is not Enum enumValue)
            return value?.ToString() ?? string.Empty;

        FieldInfo? field = enumValue.GetType()
            .GetField(enumValue.ToString());

        DescriptionAttribute? attribute =
            field?.GetCustomAttribute<DescriptionAttribute>();

        return attribute?.Description ?? enumValue.ToString();
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}