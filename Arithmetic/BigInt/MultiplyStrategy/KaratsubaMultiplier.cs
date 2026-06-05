using System;
using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.Sign == 0 || b.Sign == 0)
            return new BetterBigInteger(0, false);

        ReadOnlySpan<uint> x = a.GetDigits();
        ReadOnlySpan<uint> y = b.GetDigits();

        uint[] resultDigits = MultiplyKaratsuba(x, y);
        bool isNegative = a.IsNegative != b.IsNegative;

        return new BetterBigInteger(Trim(resultDigits), isNegative);
    }

    private static uint[] MultiplyKaratsuba(ReadOnlySpan<uint> x, ReadOnlySpan<uint> y)
    {
        if (x.IsEmpty || y.IsEmpty)
            return Array.Empty<uint>();
        
        if (x.Length == 1 && y.Length == 1)
        {
            MultiplyUint(x[0], y[0], out uint low, out uint high);
            
            return high == 0 
                ? new uint[] { low } 
                : new uint[] { low, high };
        }

        int n = Math.Max(x.Length, y.Length);
        int half = n / 2;

        ReadOnlySpan<uint> xLow = x.Slice(0, Math.Min(half, x.Length));
        ReadOnlySpan<uint> xHigh = x.Length <= half ? ReadOnlySpan<uint>.Empty : x.Slice(half);
        ReadOnlySpan<uint> yLow = y.Slice(0, Math.Min(half, y.Length));
        ReadOnlySpan<uint> yHigh = y.Length <= half ? ReadOnlySpan<uint>.Empty : y.Slice(half);

        // считаем крайние случаи 
        uint[] z0 = MultiplyKaratsuba(xLow, yLow);
        uint[] z2 = MultiplyKaratsuba(xHigh, yHigh);
        
        uint[] sumX = AddSpan(xLow, xHigh);
        uint[] sumY = AddSpan(yLow, yHigh);
        // z1 = z0 + z2 + среднее
        uint[] z1 = MultiplyKaratsuba(sumX, sumY);

        uint[] temp = AddSpan(z0, z2);
        // среднее = z1 - (z0 + z2)
        z1 = SubtractSpan(z1, temp);

        uint[] result = new uint[x.Length + y.Length + 1];
        AddTo(result, z0, 0); 
        AddTo(result, z1, half);
        AddTo(result, z2, half * 2);

        return result;
    }
    
    private static uint[] AddSpan(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int maxLen = Math.Max(a.Length, b.Length);
        uint[] result = new uint[maxLen + 1];
        uint carry = 0;

        for (int i = 0; i < maxLen; i++)
        {
            uint aval = i < a.Length ? a[i] : 0;
            uint bval = i < b.Length ? b[i] : 0;
            carry = AddWithCarry(aval, bval, carry, out result[i]);
        }

        if (carry != 0)
            result[maxLen] = carry;

        return Trim(result);
    }

    private static uint[] SubtractSpan(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int maxLen = Math.Max(a.Length, b.Length);
        uint[] result = new uint[maxLen];
        uint borrow = 0;

        for (int i = 0; i < maxLen; i++)
        {
            uint aval = i < a.Length ? a[i] : 0;
            uint bval = i < b.Length ? b[i] : 0;
            borrow = SubtractWithBorrow(aval, bval, borrow, out result[i]);
        }

        return Trim(result);
    }

    private static void AddTo(uint[] target, ReadOnlySpan<uint> source, int offset)
    {
        uint carry = 0;
        int index = offset;
        // идем пока есть carry
        for (int i = 0; i < source.Length || carry != 0; i++, index++)
        {
            uint addend = i < source.Length ? source[i] : 0;
            carry = AddWithCarry(target[index], addend, carry, out target[index]);
        }
    }

    private static uint AddWithCarry(uint left, uint right, uint carryIn, out uint sum)
    {
        uint temp = left + right;
        uint carry = (temp < left) ? 1u : 0u;
        sum = temp + carryIn;
        carry += (sum < temp) ? 1u : 0u;
        return carry;
    }

    private static uint SubtractWithBorrow(uint left, uint right, uint borrowIn, out uint difference)
    {
        uint temp = left - right;
        uint borrow = (left < right) ? 1u : 0u;
        difference = temp - borrowIn;
        borrow += (temp < borrowIn) ? 1u : 0u;
        return borrow;
    }

    private static void MultiplyUint(uint a, uint b, out uint low, out uint high)
    {
        uint aLo = a & 0xFFFF;
        uint aHi = a >> 16;
        uint bLo = b & 0xFFFF;
        uint bHi = b >> 16;

        uint p0 = aLo * bLo;
        uint p1 = aLo * bHi;
        uint p2 = aHi * bLo;
        uint p3 = aHi * bHi;

        uint middle = (p0 >> 16) + (p1 & 0xFFFF) + (p2 & 0xFFFF);
        low = (p0 & 0xFFFF) | (middle << 16);
        high = p3 + (p1 >> 16) + (p2 >> 16) + (middle >> 16);
    }

    private static uint[] Trim(uint[] data)
    {
        int last = data.Length - 1;
        while (last > 0 && data[last] == 0)
            last--;

        if (last == data.Length - 1)
            return data;

        uint[] trimmed = new uint[last + 1];
        Array.Copy(data, trimmed, last + 1);
        return trimmed;
    }
}