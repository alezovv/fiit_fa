using System;
using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.Sign == 0 || b.Sign == 0)
        {
            return new BetterBigInteger(0, false);
        }

        ReadOnlySpan<uint> el1 = a.GetDigits();
        ReadOnlySpan<uint> el2 = b.GetDigits();

        uint[] result = new uint[el1.Length + el2.Length];

        // умножаем каждую цифру первого на каждую второго
        for (int i = 0; i < el1.Length; i++)
        {
            uint carryLow = 0;
            uint carryHigh = 0;

            for (int j = 0; j < el2.Length; j++)
            {
                MultiplyUint(el1[i], el2[j], out uint productLow, out uint productHigh);

                uint carryFromLow = AddWithCarry(result[i + j], productLow, carryLow, out uint sumLow);
                result[i + j] = sumLow;

                carryHigh = AddWithCarry(productHigh, 0, carryFromLow, out carryLow);
            }

            int nextIdx = i + el2.Length;
            uint carry = carryLow;
            uint overflow = carryHigh;
            // если остался остаток после проходке по j, то тащим его в след разряд
            while ((carry != 0 || overflow != 0) && nextIdx < result.Length)
            {
                overflow = AddWithCarry(result[nextIdx], carry, overflow, out uint sumNext);
                result[nextIdx] = sumNext;
                carry = 0;
                nextIdx++;
            }
        }

        bool isResultNegative = a.IsNegative != b.IsNegative;
        return new BetterBigInteger(result, isResultNegative);
    }

    private static uint AddWithCarry(uint left, uint right, uint carryIn, out uint sum)
    {
        uint temp = left + right;
        uint carry = (temp < left) ? 1u : 0u;
        sum = temp + carryIn;
        carry += (sum < temp) ? 1u : 0u;
        return carry;
    }

    // два 32 умножаем и возвращаем два 32 результат
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
}