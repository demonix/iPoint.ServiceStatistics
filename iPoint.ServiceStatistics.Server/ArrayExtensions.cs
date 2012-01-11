using System;

namespace iPoint.ServiceStatistics.Server
{
    public static class ArrayExtensions
    {
        public static T[] Sort<T>(this T[] array)
        {
            Array.Sort(array);
            return array;
        }
        public static T[] Reverse<T>(this T[] array)
        {
            Array.Reverse(array);
            return array;
        }
    }
}