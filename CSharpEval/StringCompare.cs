using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpEval;

public static class StringCompare
{
    public static int StringDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;

        if (n == 0)
        {
            return m;
        }

        if (m == 0)
        {
            return n;
        }

        int[] p = new int[n + 1];
        int[] d = new int[n + 1];

        int i;
        int j;

        for (i = 0; i <= n; i++)
        {
            p[i] = i;
        }

        for (j = 1; j <= m; j++)
        {
            char tJ = t[j - 1];
            d[0] = j;

            for (i = 1; i <= n; i++)
            {
                int cost = s[i - 1] == tJ ? 0 : 1;

                d[i] = Math.Min(Math.Min(d[i - 1] + 1, p[i] + 1), p[i - 1] + cost);
            }

            (d, p) = (p, d);
        }

        return p[n];
    }
}
