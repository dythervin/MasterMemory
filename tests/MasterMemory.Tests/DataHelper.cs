using System;
using System.Runtime.InteropServices;

namespace MasterMemory.Tests;

public static class DataHelper
{
    private static readonly Random random = new Random();

    public static PersonModel[] GetPersonArray()
    {
        return new PersonModel[]
        {
            new PersonModel
            {
                FirstName = "aaa",
                LastName = "bbb",
                RandomId = "1"
            },
            new PersonModel
            {
                FirstName = "ccc",
                LastName = "ddd",
                RandomId = "2"
            },
            new PersonModel
            {
                FirstName = "eee",
                LastName = "fff",
                RandomId = "3"
            },
            new PersonModel
            {
                FirstName = "ggg",
                LastName = "hhh",
                RandomId = "4"
            },
            new PersonModel
            {
                FirstName = "iii",
                LastName = "jjj",
                RandomId = "5"
            }
        };
    }
    
    public static T[] Shuffle<T>(this T[] array)
    {
        int n = array.Length;
        for (int i = 0; i < n; i++)
        {
            int r = i + random.Next(n - i);
            (array[r], array[i]) = (array[i], array[r]);
        }

        return array;
    }
}