namespace MasterMemory.Validation
{
    public static class ValidatorExtensions
    {
        public static void Sequential<TKey, TElement>(this IValidator validator, RangeView<int, TKey, TElement> values, bool distinct = false)
        {
            validator.Sequential(values, x => x - 1, distinct);
        }

        public static void Sequential<TKey, TElement>(this IValidator validator, RangeView<long, TKey, TElement> values, bool distinct = false)
        {
            validator.Sequential(values, x => x - 1, distinct);
        }

        public static void Sequential<TKey, TElement>(this IValidator validator, RangeView<uint, TKey, TElement> values, bool distinct = false)
        {
            validator.Sequential(values, x => x - 1, distinct);
        }

        public static void Sequential<TKey, TElement>(this IValidator validator,
            RangeView<short, TKey, TElement> values, bool distinct = false)
        {
            validator.Sequential(values, x => (short)(x - 1), distinct);
        }

        public static void Sequential<TKey, TElement>(this IValidator validator,
            RangeView<ushort, TKey, TElement> values, bool distinct = false)
        {
            validator.Sequential(values, x => (ushort)(x - 1), distinct);
        }

        public static void Sequential<TKey, TElement>(this IValidator validator, RangeView<byte, TKey, TElement> values, bool distinct = false)
        {
            validator.Sequential(values, x => (byte)(x - 1), distinct);
        }

        public static void Sequential<TKey, TElement>(this IValidator validator,
            RangeView<sbyte, TKey, TElement> values, bool distinct = false)
        {
            validator.Sequential(values, x => (sbyte)(x - 1), distinct);
        }

        public static void Sequential<TKey, TElement>(this IValidator validator,
            RangeView<decimal, TKey, TElement> values, bool distinct = false)
        {
            validator.Sequential(values, x => x - 1, distinct);
        }
    }
}