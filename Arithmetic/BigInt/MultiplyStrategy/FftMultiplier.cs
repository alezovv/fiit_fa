using Arithmetic.BigInt.Interfaces;
using System.Security.Cryptography;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        bool isNeg = a.IsNegative != b.IsNegative;
        uint[] aBytes = SplitToBytes(a.GetDigits());
        uint[] bBytes = SplitToBytes(b.GetDigits());

        int resLen = aBytes.Length + bBytes.Length - 1;
        int n = 1;

        // доводим до ближайшей степени двойки
        while (n < resLen)
        {
            n <<= 1;
        }

        Array.Resize(ref aBytes, n);
        Array.Resize(ref bBytes, n);

        // для NTT нужны модули k * 2^m + 1
        // 5 * 2^25 + 1
        // 7 * 2 ^ 26 + 1
        // 45 * 2 ^24 + 1
        uint[] mods = { 167772161u, 469762049u, 754974721u };
        // примитивные корни для модулей
        uint[] generators = { 3u, 3u, 11u };

        uint[][] results = new uint[3][];

        for (int m = 0; m < 3; m++)
        {
            uint mod = mods[m];
            uint gen = generators[m];

            uint exp = (mod - 1) / (uint)n;
            uint w = BinPow(gen, exp, mod);

            uint[] A = new uint[n];
            uint[] B = new uint[n];
            Array.Copy(aBytes, A, n);
            Array.Copy(bBytes, B, n);

            NTT(A, mod, w);
            NTT(B, mod, w);
            
            // Ck = Ak * Bk
            for (int i = 0; i < n; i++)
                A[i] = MulMod(A[i], B[i], mod);

            InverseNTT(A, mod, w);

            results[m] = A;
        }

        uint[] chunks = CRTReconstruct(results, resLen);

        return ConvertToBigInteger(chunks, resLen, isNeg);
    }
    // разбили uint на 4 отдельных байта
    // меняем базу с 2^32 на 2^8
    private static uint[] SplitToBytes(ReadOnlySpan<uint> digits)
    {
        uint[] bytes = new uint[digits.Length * 4];
        for (int i = 0; i < digits.Length; i++)
        {
            bytes[4 * i] = digits[i] & 0xFF;
            // сдвигаем на ... бит и берем младший байт
            bytes[4 * i + 1] = (digits[i] >> 8) & 0xFF;
            bytes[4 * i + 2] = (digits[i] >> 16) & 0xFF;
            bytes[4 * i + 3] = digits[i] >> 24;
        }
        return bytes;
    }

    private static uint AddMod(uint a, uint b, uint mod)
    {
        uint sum = a + b;
        if (sum >= mod) sum -= mod;
        return sum;
    }

    private static uint SubtractMod(uint a, uint b, uint mod)
    {
        if (a >= b) return a - b;
        return a + (mod - b);
    }

    // русское крестьянское умножение
    private static uint MulMod(uint a, uint b, uint mod)
    {
        uint result = 0;
        // умножаем a на 2, делим b на 2
        // если после деления b нечетное, то добавляем a к результату

        while (b != 0)
        {
            if ((b & 1) != 0)
                result = AddMod(result, a, mod);

            a = AddMod(a, a, mod);
            b >>= 1;
        }

        return result;
    }

    // ищем остаток по модулю для N = high * 2 ^ 32 + low
    // (a + b) mod m = ((a mod m) + (b mod m)) mod m
    private static uint Reduce64(uint high, uint low, uint mod)
    {
        if (mod == 1) return 0;
        
        if (high != 0)
        {   
            // high = q * mod + shifted
            uint shifted = high % mod;
            
            // умножаем остаток на 2 ^ 32
            for (int i = 0; i < 32; i++)
            {
                shifted = AddMod(shifted, shifted, mod);
            }
            low = AddMod(low, shifted, mod);
        }
        
        if (low >= mod)
        {
            return low % mod;
        }
        else
        {
            return low;
        }
    }

    // быстрое возведение в степень
    private static uint BinPow(uint a, uint e, uint mod)
    {
        uint result = 1;
        // степень делим на 2, основание в квадрат возводим
        // если степень нечетная, то результат умножаем на основание
        // по сути русское крестьянское возведение
        while (e != 0)
        {
            if ((e & 1) != 0)
                result = MulMod(result, a, mod);

            a = MulMod(a, a, mod);
            e >>= 1;
        }

        return result;
    }

    // если p - простое, а a - целое, то a ^ (p - 1) - 1 делится на p
    // ну или a ^ (p - 1) сравнимо с 1 по модулю p
    // если умножим на a ^ (-1), то a ^ (p - 2) сравним с a ^ (-1)
    private static uint InverseMod(uint a, uint mod) 
    {
        return BinPow(a, mod - 2, mod);
    }

    // вычисляем значение полинома в точках степеней root
    // сам root примитивный n-й корень единицы по модулю mod
    private static void NTT(uint[] a, uint mod, uint root)
    {
        int n = a.Length;

        // делаем порядок: четные -> нечетные
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
                j ^= bit;

            j ^= bit;

            if (i < j)
            {
                uint tmp = a[i];
                a[i] = a[j];
                a[j] = tmp;
            }
        }

        //
        for (int len = 2; len <= n; len <<= 1)
        {
            uint wlen = BinPow(root, (uint)(n / len), mod);

            for (int i = 0; i < n; i += len)
            {
                uint w = 1;

                for (int j = 0; j < len / 2; j++)
                {
                    uint u = a[i + j];
                    uint v = MulMod(a[i + j + len / 2], w, mod);

                    a[i + j] = AddMod(u, v, mod);
                    a[i + j + len / 2] = SubtractMod(u, v, mod);

                    w = MulMod(w, wlen, mod);
                }
            }
        }
    }
    private static void InverseNTT(uint[] data, uint mod, uint root)
    {
        NTT(data, mod, InverseMod(root, mod));
        // так как в модульной истории не делят, нужно умножить на обратный
        uint invN = InverseMod((uint)data.Length, mod);
        for (int i = 0; i < data.Length; ++i)
            data[i] = MulMod(data[i], invN, mod);
    }

    // алгоритм гарнера для КТО
    // x = r1 + (k * p1) + (t * (p1 * p2))
    private static uint[] CRTReconstruct(uint[][] results, int resultLen)
    {
        uint p1 = 167772161u;
        uint p2 = 469762049u;
        uint p3 = 754974721u;

        uint p1p2High, p1p2Low;
        (p1p2Low, p1p2High) = BetterBigInteger.MultiplyFull(p1, p2);
        
        uint invP1ModP2 = InverseMod(p1 % p2, p2);

        uint p1p2ModP3 = Reduce64(p1p2High, p1p2Low, p3);

        // в модулярной деление - умножение на обратное
        uint invP1P2ModP3 = InverseMod(p1p2ModP3, p3);

        uint[] result = new uint[resultLen * 3];
        
        for (int i = 0; i < resultLen; i++)
        {
            // остатки от деления на p1, p2, p3
            uint r1 = results[0][i];
            uint r2 = results[1][i];
            uint r3 = results[2][i];

            // x12 = r1 + k * p1
            uint diff = SubtractMod(r2, r1 % p2, p2);
            uint k = MulMod(diff, invP1ModP2, p2);

            uint kp1High, kp1Low;
            (kp1Low, kp1High) = BetterBigInteger.MultiplyFull(k, p1);
            uint carry = AddWithCarry(ref kp1Low, r1);
            uint x12High = kp1High + carry;
            uint x12Low = kp1Low;

            uint x12ModP3 = Reduce64(x12High, x12Low, p3);
            diff = SubtractMod(r3, x12ModP3, p3);
            uint t = MulMod(diff, invP1P2ModP3, p3);

            uint tp1p2High, tp1p2Low;
            (tp1p2Low, tp1p2High) = BetterBigInteger.MultiplyFull(t, p1p2Low);

            uint tp1p2Mid, tp1p2High2;
            (tp1p2Mid, tp1p2High2) = BetterBigInteger.MultiplyFull(t, p1p2High);

            carry = AddWithCarry(ref tp1p2Low, 0);
            carry = AddWithCarry(ref tp1p2Mid, tp1p2High + carry);
            uint totalHigh = tp1p2High2 + carry;

            carry = AddWithCarry(ref tp1p2Low, x12Low);
            carry = AddWithCarry(ref tp1p2Mid, x12High + carry);
            totalHigh += carry;

            result[3 * i] = tp1p2Low; // младшие 32
            result[3 * i + 1] = tp1p2Mid; // сркдние 32
            result[3 * i + 2] = totalHigh; // старшие  32
        }
        return result;
    }

    private static uint AddWithCarry(ref uint a, uint b)
    {
        uint sum = a + b;
        uint carry = sum < a ? 1u : 0u;
        a = sum;
        return carry;
    }

    private static BetterBigInteger ConvertToBigInteger(uint[] chunks, int resultLen, bool isNeg)
    {
        List<uint> bytes = new List<uint>();
        uint carry = 0;

        for (int i = 0; i < resultLen; i++)
        {
            uint low = chunks[3 * i];
            uint mid = chunks[3 * i + 1];
            uint high = chunks[3 * i + 2];

            low += carry;
            if (low < carry)
            {
                mid++;
                if (mid == 0)
                    high++;
            }

            bytes.Add(low & 0xFF);

            carry = (low >> 8) | (mid << 24);
            mid = (mid >> 8) | (high << 24);
            high >>= 8;

            while (mid != 0 || high != 0)
            {
                low = carry;

                low += 0;
                bytes.Add(low & 0xFF);

                carry = (low >> 8) | (mid << 24);
                mid = (mid >> 8) | (high << 24);
                high >>= 8;
            }
        }

        while (carry != 0)
        {
            bytes.Add(carry & 0xFF);
            carry >>= 8;
        }

        uint[] digits = new uint[(bytes.Count + 3) / 4];
        for (int i = 0; i < bytes.Count; i++)
        {
            int idx = i / 4;
            int shift = (i % 4) * 8;
            digits[idx] |= bytes[i] << shift;
        }

        int len = digits.Length;
        while (len > 1 && digits[len - 1] == 0)
            len--;

        uint[] result = new uint[len];
        Array.Copy(digits, result, len);

        return new BetterBigInteger(result, isNeg);
    }
}