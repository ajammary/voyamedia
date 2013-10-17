using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SortingAlgorithms
{
    public sealed class NaturalSort : IComparer<string>
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string stringA, string stringB);

        public int Compare(string a, string b)
        {
            // Ignore leading checkmarks for completed media files.
            if (a.Contains("\u2713"))
            {
                a = a.Substring(2);
            }
            if (b.Contains("\u2713"))
            {
                b = b.Substring(2);
            }

            return StrCmpLogicalW(a, b);
        }
    }
    
    public sealed class NaturalSortAscending : IComparer<string>
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string stringA, string stringB);

        public int Compare(string a, string b)
        {
            // Ignore leading checkmarks for completed media files.
            if (a.Contains("\u2713"))
            {
                a = a.Substring(2);
            }
            if (b.Contains("\u2713"))
            {
                b = b.Substring(2);
            }

            return StrCmpLogicalW(a, b);
        }
    }

    public sealed class NaturalSortDescending : IComparer<string>
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string stringA, string stringB);

        public int Compare(string a, string b)
        {
            // Ignore leading checkmarks for completed media files.
            if (a.Contains("\u2713"))
            {
                a = a.Substring(2);
            }
            if (b.Contains("\u2713"))
            {
                b = b.Substring(2);
            }

            return StrCmpLogicalW(b, a);
        }
    }
}
