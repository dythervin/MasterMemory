using System;

namespace MasterMemory.Validation
{
    public interface IValidator
    {
        void Validate<TKey, T>(ITable<TKey, T> table, Func<T, bool> predicate, Func<T, string>? message = null);

        void ValidateAction<TKey, T>(ITable<TKey, T> table, Func<T, bool> predicate, Func<T, string>? message = null);

        void Unique<TKey, TElement, T>(ITable<TKey, TElement> values, Func<TElement, T> keySelector,
            Predicate<T>? predicate = null);

        void Sequential<TKey, TElement, T>(in RangeView<T, TKey, TElement> values, Func<T, T> getPrevious,
            bool distinct);

        TableValidator<TKey, TElement> GetTableValidator<TKey, TElement>(ITable<TKey, TElement> table);

        void Exists<TKey, TElement1, TElement2, TMainKey1, TMainKey2>(in RangeView<TKey, TMainKey1, TElement1> values,
            in RangeView<TKey, TMainKey2, TElement2> existInView, Predicate<TKey>? predicate = null);
    }
}