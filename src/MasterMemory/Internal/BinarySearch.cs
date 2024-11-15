﻿using System.Collections.Generic;

namespace MasterMemory
{
    public static class BinarySearch
    {
        public static int FindFirst<TKey>(TKey[] array, TKey key, IComparer<TKey> comparer)
        {
            var lo = 0;
            var hi = array.Length - 1;

            while (lo <= hi)
            {
                var mid = (int)(((uint)hi + (uint)lo) >> 1);
                var found = comparer.Compare(array[mid], key);

                if (found == 0) return mid;
                if (found < 0)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return -1;
        }

        // lo = 0, hi = Count.
        public static int FindClosest<TKey>(TKey[] array, int lo, int hi, TKey key, IComparer<TKey> comparer, bool selectLower)
        {
            if (array.Length == 0) return -1;

            lo = lo - 1;

            while (hi - lo > 1)
            {
                var mid = lo + ((hi - lo) >> 1);
                var found = comparer.Compare(array[mid], key);

                if (found == 0)
                {
                    lo = hi = mid;
                    break;
                }
                if (found >= 1)
                {
                    hi = mid;
                }
                else
                {
                    lo = mid;
                }
            }

            return selectLower ? lo : hi;
        }

        // default lo = 0, hi = array.Count
        public static int LowerBound<TKey>(TKey[] array, int lo, int hi, TKey key, IComparer<TKey> comparer)
        {
            while (lo < hi)
            {
                var mid = lo + ((hi - lo) >> 1);
                var found = comparer.Compare(key, array[mid]);

                if (found <= 0)
                {
                    hi = mid;
                }
                else
                {
                    lo = mid + 1;
                }
            }

            var index = lo;
            if (index == -1 || array.Length <= index)
            {
                return -1;
            }

            // check final
            return comparer.Compare(key, array[index]) == 0
                ? index
                : -1;
        }

        public static int UpperBound<TKey>(TKey[] array, int lo, int hi, TKey key, IComparer<TKey> comparer)
        {
            while (lo < hi)
            {
                var mid = lo + ((hi - lo) >> 1);
                var found = comparer.Compare(key, array[mid]);

                if (found >= 0)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            var index = lo == 0 ? 0 : lo - 1;
            if (index == -1 || array.Length <= index)
            {
                return -1;
            }

            // check final
            return comparer.Compare(key, array[index]) == 0
                ? index
                : -1;
        }


        //... want the lowest index of  Key <= Value
        //... returns 0 if key is <= all values in array
        //... returns array.Length if key is > all values in array

        public static int LowerBoundClosest<TKey>(TKey[] array, int lo, int hi, TKey key, IComparer<TKey> comparer)
        {
            while (lo < hi)
            {
                var mid = lo + ((hi - lo) >> 1);
                var found = comparer.Compare(key, array[mid]);

                if (found <= 0)     //... Key is <= value at mid
                {
                    hi = mid;
                }
                else
                {
                    lo = mid + 1;   //... Notice that lo starts at zero and can only increase
                }
            }

            var index = lo;         //... index will always be zero or greater

            if ( array.Length <= index)
            {
               return array.Length;
            }

            // check final
            return comparer.Compare(key, array[index]) <= 0
                ? index
                : -1;
        }

 
        //... want the highest index of  Key >= Value
        //... returns -1 if key is < than all values in array
        //... returns array.Length - 1 if key is >= than all values in array

        public static int UpperBoundClosest<TKey>(TKey[] array, int lo, int hi, TKey key, IComparer<TKey> comparer)
        {
            while (lo < hi)
            {
                var mid = lo + ((hi - lo) >> 1);
                var found = comparer.Compare(key, array[mid]);

                if (found >= 0)     //... Key >= value at mid
                {
                    lo = mid + 1;   //... Note lo starts at zero and can only increase
                }
                else
                {
                    hi = mid;
                }
            }

            var index = lo == 0 ? 0 : lo - 1;   //... index will always be zero or greater

            if ( index >= array.Length )
            {
               return array.Length;
            }

            // check final
            return comparer.Compare(key, array[index]) >= 0
                ? index
                : -1;
        }



    }
}