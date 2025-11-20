using System.Windows;

namespace Magitek.Converters
{
    public sealed class InvertedBooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        public InvertedBooleanToVisibilityConverter() :
            base(Visibility.Collapsed, Visibility.Visible)
        { }
    }
}

