using System;

namespace MasterMemory.Validation
{
    public readonly ref struct TableValidator<TKey, TElement>
    {
        private readonly ITable<TKey, TElement> _table;
        private readonly IValidator _validator;

        public TableValidator(ITable<TKey, TElement> table, IValidator validator)
        {
            _table = table;
            _validator = validator;
        }

        public void Validate(Func<TElement, bool> predicate, Func<TElement, string>? message = null)
        {
            _validator.Validate(_table, predicate, message);
        }

        public void ValidateAction(Func<TElement, bool> predicate, Func<TElement, string>? message = null)
        {
            _validator.ValidateAction(_table, predicate, message);
        }

        public void Unique<T>(Func<TElement, T> keySelector)
        {
            _validator.Unique(_table, keySelector);
        }
    }
}