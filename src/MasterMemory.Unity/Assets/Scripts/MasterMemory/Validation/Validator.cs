using System;
using System.Collections.Generic;
using System.Text;

namespace MasterMemory.Validation
{
    public class Validator : IValidator
    {
        private readonly ValidateResult _resultSet;

        public Validator(ValidateResult resultSet)
        {
            _resultSet = resultSet;
        }

        public void Sequential<TKey, TElement, T>(in RangeView<T, TKey, TElement> values, Func<T, T> getPrevious,
            bool distinct)
        {
            if (values.Count == 0)
                return;

            for (int i = 1; i < values.Count; i++)
            {
                T currentKey = values.GetKeys(i).key;
                T previousKey = values.GetKeys(i - 1).key;
                if (!distinct && EqualityComparer<T>.Default.Equals(currentKey, previousKey))
                    continue;

                if (!EqualityComparer<T>.Default.Equals(getPrevious(currentKey), previousKey))
                {
                    _resultSet.AddFail(typeof(TElement),
                        $"Sequential failed: {values.KeyName} = ({previousKey}, {currentKey}), {BuildPkMessage(values.Table, values[i])}",
                        currentKey);
                }
            }
        }

        public void Exists<TKey, TElement, TElement2, TMainKey1, TMainKey2>(
            in RangeView<TKey, TMainKey1, TElement> values, in RangeView<TKey, TMainKey2, TElement2> existInView,
            Predicate<TKey>? predicate = null)
        {
            predicate ??= _ => true;
            StringBuilder? sb = null;
            for (int i = 0; i < values.Count; i++)
            {
                var key = values.GetKeys(i).key;
                if (!predicate(key))
                    continue;

                if (!existInView.ContainsKey(key))
                {
                    sb ??= new StringBuilder();
                    sb.Clear();
                    sb.Append("Exists failed: ").Append(values.ElementName).Append('.').Append(values.KeyName)
                        .Append(" -> ").Append(existInView.ElementName).Append('.').Append(existInView.KeyName)
                        .Append(", value = ").Append(key).Append(", ").Append(BuildPkMessage(values.Table, values[i]));

                    _resultSet.AddFail(typeof(TElement), sb.ToString(), key);
                    sb.Clear();
                }
            }
        }

        public TableValidator<TKey, TElement> GetTableValidator<TKey, TElement>(ITable<TKey, TElement> table)
        {
            return new TableValidator<TKey, TElement>(table, this);
        }

        public void Validate<TKey, T>(ITable<TKey, T> table, Func<T, bool> predicate,
            Func<T, string>? messageFunc = null)
        {
            var rangeView = table.GetAllSorted();
            foreach (T item in rangeView)
            {
                if (!predicate(item))
                {
                    string? message = null;
                    if (messageFunc != null)
                    {
                        message = messageFunc(item);
                        if (message != null)
                            message += ", ";
                    }

                    _resultSet.AddFail(typeof(T), "Validate failed: " + message + BuildPkMessage(table, item), item);
                }
            }
        }

        public void ValidateAction<TKey, T>(ITable<TKey, T> table, Func<T, bool> predicate,
            Func<T, string>? messageFunc = null)
        {
            foreach (T item in table.GetAllSorted())
            {
                if (!predicate(item))
                {
                    string? message = null;
                    if (messageFunc != null)
                    {
                        message = messageFunc(item);
                        if (message != null)
                            message += ", ";
                    }

                    _resultSet.AddFail(typeof(T),
                        "ValidateAction failed: " + message + BuildPkMessage(table, item),
                        item);
                }
            }
        }

        public void Unique<TKey, TElement, T>(ITable<TKey, TElement> table, Func<TElement, T> keySelector,
            Predicate<T>? predicate = null)
        {
            predicate ??= _ => true;
            var set = new HashSet<T>();
            foreach (TElement item in table.GetAllSorted())
            {
                T key = keySelector(item);
                if (!predicate(key))
                    continue;

                if (!set.Add(key))
                {
                    _resultSet.AddFail(typeof(TElement),
                        "Unique failed: value = " + key + ", " + BuildPkMessage(table, item),
                        item);
                }
            }
        }

        string BuildPkMessage<TKey, T>(ITable<TKey, T> table, in T item)
        {
            var pk = table.KeySelector(item).ToString();
            return $"PK({table.KeyName}) = {pk}";
        }
    }
}