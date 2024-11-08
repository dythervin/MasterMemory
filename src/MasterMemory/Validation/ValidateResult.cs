using System;
using System.Collections.Generic;
using System.Text;

namespace MasterMemory.Validation
{
    public class ValidateResult
    {
        private readonly List<FaildItem> _result = new List<FaildItem>();

        public bool IsValidationFailed => _result.Count != 0;

        public IReadOnlyList<FaildItem> FailedResults => _result;

        public string FormatFailedResults()
        {
            var sb = new StringBuilder();
            foreach (var item in _result)
            {
                sb.AppendLine(item.Type.FullName + " - " + item.Message);
            }
            return sb.ToString();
        }

        internal void AddFail(Type type, string message, object data)
        {
            _result.Add(new FaildItem(type, message, data));
        }
        
        public void Validate<T>(bool condition, in T data, string message = "")
        {
            if (!condition)
            {
                AddFail(typeof(T), message, data);
            }
        }
    }
}
