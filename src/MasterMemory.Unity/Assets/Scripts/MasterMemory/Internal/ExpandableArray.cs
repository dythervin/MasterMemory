using System;
using System.Collections.Generic;

namespace MasterMemory
{
    public struct ExpandableArray<TElement>
    {
        public TElement[] items;
        public int count { get; private set; }

        public ExpandableArray(object dummy)
        {
            items = Array.Empty<TElement>();
            count = 0;
        }

        internal void Add(TElement item)
        {
            if (items.Length == 0)
            {
                items = new TElement[4];
            }
            else if (items.Length == (count + 1))
            {
                Array.Resize(ref items, checked(count * 2));
            }
            items[count++] = item;
        }
        
        internal void AddRange(IEnumerable<TElement> source)
        {
            if (source is ICollection<TElement> collection)
            {
                if (items.Length == 0)
                {
                    items = new TElement[collection.Count];
                }
                else if (items.Length < (count + collection.Count))
                {
                    Array.Resize(ref items, checked(count + collection.Count));
                }
                collection.CopyTo(items, count);
                count += collection.Count;
                return;
            }
            
            foreach (var item in source)
            {
                Add(item);
            }
        }
    }
}