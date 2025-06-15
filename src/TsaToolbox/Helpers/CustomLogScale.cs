using System;
using System.Linq;

namespace TsaToolbox.Helpers;

internal class CustomLogScale
{
    public static double[] Calculate(double[] values) =>
        values.Select(v => Calculate(v)).ToArray();

    public static double Calculate(double x)
    {
        // Handle edge cases
        if (x == 0) return 0;
        if (x < 0) return -Calculate(-x);

        // Use Math.Log10 for positive numbers
        return Math.Log10(Math.Abs(x));
    }
}
