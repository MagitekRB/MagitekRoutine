namespace Magitek.Converters
{
    public sealed class InvertedBooleanConverter : BooleanConverter<bool>
    {
        public InvertedBooleanConverter() :
            base(false, true)
        { }
    }
}
