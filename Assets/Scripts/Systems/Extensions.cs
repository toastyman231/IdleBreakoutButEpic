using System.Numerics;
using UnityEngine;

public static class Extensions
{
    public static string NumberFormat(this BigInteger num, bool countK = false)
    {
        // Ensure number has max 3 significant digits (no rounding up can happen)
        if (num == BigInteger.Zero) return "0";
        
        BigInteger i = BigInteger.Pow(10, (int)BigInteger.Max(0, BigInteger.Subtract((BigInteger)BigInteger.Log10(num), 3)));
        num = BigInteger.Multiply(BigInteger.Divide(num, i), i);

        if (num >= BigInteger.Parse("1000000000000000000000000000"))
            return (num / BigInteger.Parse("1000000000000000000000000000")).ToString("##0.##") + "OCT";
        else if (num >= BigInteger.Parse("1000000000000000000000000"))
            return (num / BigInteger.Parse("1000000000000000000000000")).ToString("##0.##") + "SEPT";
        else if (num >= BigInteger.Parse("1000000000000000000000"))
            return (num / BigInteger.Parse("1000000000000000000000")).ToString("##0.##") + "SEXT";
        else if (num >= BigInteger.Parse("1000000000000000000"))
            return (num / BigInteger.Parse("1000000000000000000")).ToString("##0.##") + "QUINT";
        else if (num >= BigInteger.Parse("1000000000000000"))
            return (num / BigInteger.Parse("1000000000000000")).ToString("##0.##") + "QUAD";
        else if (num >= 1000000000000)
            return (num / 1000000000000).ToString("##0.##") + "T";
        else if (num >= 1000000000)
            return (num / 1000000000).ToString("##0.##") + "B";
        else if (num >= 1000000)
            return (num / 1000000).ToString("##0.##") + "M";
        else if (num >= 100000)
            return (num / 1000).ToString("##0.##") + "K";
        else if (num >= 1000 && countK)
            return (num / 1000).ToString("0#.##") + "K";

        return num.ToString("#,0");
    } 
}