using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private const uint Mask32 = 0xFFFFFFFF;

    private int _signBit; // 0 - полож, 1 - отриц

    private uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data;

    public bool IsNegative => _signBit == 1;

    public int Sign
    {
        get
        {
            if (IsZero()) return 0;
            return IsNegative ? -1 : 1;
        }
    }

    // ОТ числа
    public BetterBigInteger(uint value, bool Negative = false)
    {
        _smallValue = value;
        _data = null;
        _signBit = (value == 0) ? 0 : (Negative ? 1 : 0);
    }

    /// От массива цифр (little endian)
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        if (digits == null) throw new ArgumentNullException("Передан null");

        int fact_len = digits.Length;

        // пока старший элемент нулевой
        while (fact_len > 0 && digits[fact_len - 1] == 0)
        {
            fact_len--;
        }

        _data = null;

        if (fact_len == 0)
        {
            _signBit = 0;
            _smallValue = 0;
        }
        else
        {
            // если число не ноль, сохраняем переданный знак
            _signBit = isNegative ? 1 : 0;

            if (fact_len == 1)
            {
                _smallValue = digits[0];
                if (_smallValue == 0) _signBit = 0; // по факту не может случиться
            }
            else
            {
                // для ситуации, когда digits = [5, 2, 0, 0] => _data = [5, 2]
                _data = new uint[fact_len];
                Array.Copy(digits, _data, fact_len);
            }
        }
    }

    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
        : this(digits.ToArray(), isNegative)
    {
    }

    public BetterBigInteger(string value, int radix)
    {

        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Пустая строка");

        if (radix < 2 || radix > 36)
            throw new ArgumentOutOfRangeException(nameof(radix), "Основание должно быть в пределах [2..36]");

        int start = 0;
        _data = null;
        int len = value.Length;

        if (value[start] == '+' || value[start] == '-')
        {
            _signBit = value[start] == '-' ? 1 : 0;
            start++;
        }

        if (start == len) throw new ArgumentNullException("Число не передано");

        while (start < len && value[start] == '0') start++;

        if (start == len)
        {
            _signBit = 0; // какой бы знак не был передан
            _smallValue = 0;
            return;
        }

        ParseString(value, start, radix);

    }

    private void ParseString(string value, int start, int radix)
    {
        for (int i = start; i < value.Length; i++)
        {
            uint digitValue = CharToValue(value[i]);
            if (digitValue >= radix) throw new ArgumentException("Символ не соответствует radix");

            MultiplyByAndAdd((uint)radix, digitValue);
        }
    }

    private void MultiplyByAndAdd(uint multiplier, uint adder)
    {
        // Число пока помещается в один разряд
        if (_data == null)
        {
            MultiplyUint(_smallValue, multiplier, out uint low, out uint high);
            uint carry = AddWithCarry(low, adder, 0, out uint sumLow);

            if (high == 0 && carry == 0)
            {
                _smallValue = sumLow;
                return;
            }

            _data = new uint[2];
            _data[0] = sumLow;
            _data[1] = high + carry;
            _smallValue = 0;
            return;
        }

        uint carryValue = adder;

        for (int i = 0; i < _data.Length; i++)
        {
            MultiplyUint(_data[i], multiplier, out uint low, out uint high);
            uint carryLow = AddWithCarry(low, carryValue, 0, out uint sumLow);
            _data[i] = sumLow;
            carryValue = high + carryLow;
        }

        if (carryValue > 0)
        {
            int oldLen = _data.Length;
            Array.Resize(ref _data, oldLen + 1);
            _data[oldLen] = carryValue;
        }
    }
    private uint CharToValue(char c)
    {
        if (c >= '0' && c <= '9') return (uint)(c - '0');
        if (c >= 'a' && c <= 'z') return (uint)(c - 'a' + 10);
        if (c >= 'A' && c <= 'Z') return (uint)(c - 'A' + 10);

        throw new ArgumentException($"Недопустимый символ в строке: {c}");
    }

    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? new uint[] { _smallValue };
    }

    public bool IsZero()
    {
        if (_data == null) return _smallValue == 0;

        for (int i = 0; i < _data.Length; i++)
        {
            if (_data[i] != 0) return false;
        }

        return true;
    }

    private static uint[] Trim(uint[] data)
    {
        int last = data.Length - 1;
        while (last > 0 && data[last] == 0)
        {
            last--;
        }

        if (last == data.Length - 1) return data;

        if (last == 0 && data[0] == 0) return new uint[] { 0 };

        uint[] trimmed = new uint[last + 1];
        Array.Copy(data, trimmed, last + 1);
        return trimmed;
    }

    private static uint AddWithCarry(uint left, uint right, uint carryIn, out uint sum)
    {
        uint temp = left + right;
        uint carry = temp < left ? 1u : 0u;
        sum = temp + carryIn;
        carry += (sum < temp) ? 1u : 0u;
        return carry;
    }

    private static uint SubtractWithBorrow(uint left, uint right, uint borrowIn, out uint difference)
    {
        uint temp = left - right;
        uint borrow = left < right ? 1u : 0u;
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

    private static uint Divide64By32(uint high, uint low, uint divisor, out uint remainder)
    {
        uint quotient = 0;

        for (int i = 0; i < 32; i++)
        {
            uint carryBit = low >> 31;
            low <<= 1;
            high = (high << 1) | carryBit;

            quotient <<= 1;
            if (high >= divisor)
            {
                high -= divisor;
                quotient |= 1u;
            }
        }

        remainder = high;
        return quotient;
    }

    public int CompareTo(IBigInteger? other)
    {
        if (other is null) return 1;
        if (ReferenceEquals(this, other)) return 0;
        if (this.IsZero() && other.IsZero()) return 0;

        // Сравнение знаков
        if (this.IsNegative != other.IsNegative)
        {
            return this.IsNegative ? -1 : 1;
        }

        // Сравнение модулей
        ReadOnlySpan<uint> thisDigits = this.GetDigits();
        ReadOnlySpan<uint> otherDigits = other.GetDigits();

        int result = 0;
        if (thisDigits.Length != otherDigits.Length)
        {
            result = thisDigits.Length.CompareTo(otherDigits.Length);
        }
        else
        {
            // Поразрядное сравнение
            for (int i = thisDigits.Length - 1; i >= 0; i--)
            {
                if (thisDigits[i] != otherDigits[i])
                {
                    result = thisDigits[i].CompareTo(otherDigits[i]);
                    break;
                }
            }
        }

        // Если оба отрицательные, то больше то что меньше по модулю
        return this.IsNegative ? -result : result;
    }
    public bool Equals(IBigInteger? other)
    {
        if (ReferenceEquals(this, other)) return true;

        if (other == null) return false;

        if (this.IsNegative != other.IsNegative) return false;

        ReadOnlySpan<uint> otherdigits = other.GetDigits();
        ReadOnlySpan<uint> thisdigits = this.GetDigits();

        if (thisdigits.Length != otherdigits.Length) return false;

        return thisdigits.SequenceEqual(otherdigits);
    }
    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_signBit);

        if (_data == null)
            hash.Add(_smallValue);
        else
            hash.Add(_data.Length);

        return hash.ToHashCode();
    }

    // умножает два uint и возвращает результат в виде двух uint вместо long
    public static (uint low, uint high) MultiplyFull(uint a, uint b)
    {
        uint a0 = a & 0xFFFF;
        uint a1 = a >> 16;

        uint b0 = b & 0xFFFF;
        uint b1 = b >> 16;

        uint p00 = a0 * b0;
        uint p01 = a0 * b1;
        uint p10 = a1 * b0;
        uint p11 = a1 * b1;

        uint low = p00;

        uint carry = 0;

        uint add = (p01 & 0xFFFF) << 16;
        uint tmp = low + add;
        if (tmp < low) carry++;
        low = tmp;

        add = (p10 & 0xFFFF) << 16;
        tmp = low + add;
        if (tmp < low) carry++;
        low = tmp;

        uint high = p11;
        high += (p01 >> 16);
        high += (p10 >> 16);
        high += carry;

        return (low, high);
    }

    private static BetterBigInteger AddModules(BetterBigInteger a, BetterBigInteger b, bool isnegative)
    {
        BetterBigInteger result;
        // оба маленькие
        if (a._data == null && b._data == null)
        {
            uint carry = AddWithCarry(a._smallValue, b._smallValue, 0, out uint sum);
            if (carry > 0)
            {
                uint[] newData = new uint[2];
                newData[0] = sum;
                newData[1] = carry;
                result = new BetterBigInteger(newData, isnegative);
            }
            else
            {
                result = new BetterBigInteger(sum, isnegative);
            }
        }
        // одно маленькое второе большое
        else if (a._data != null && b._data == null)
        {
            uint[] newData = new uint[a._data.Length + 1];

            uint carry = AddWithCarry(a._data[0], b._smallValue, 0, out newData[0]);
            int i = 1;

            while (i < a._data.Length && carry > 0)
            {
                carry = AddWithCarry(a._data[i], 0, carry, out newData[i]);
                i++;
            }

            if (i < a._data.Length)
            {
                Array.Copy(a._data, i, newData, i, a._data.Length - i);
                i = a._data.Length;
            }

            newData[i] = carry;
            result = new BetterBigInteger(Trim(newData), isnegative);
        }
        else if (a._data == null && b._data != null)
        {
            return AddModules(b, a, b.IsNegative);
        }
        // оба массивы
        else
        {
            int len = Math.Max(a._data.Length, b._data.Length);
            uint[] newData = new uint[len + 1];
            uint carry = 0;

            for (int i = 0; i < len; i++)
            {
                uint valA = i < a._data.Length ? a._data[i] : 0;
                uint valB = i < b._data.Length ? b._data[i] : 0;
                carry = AddWithCarry(valA, valB, carry, out newData[i]);
            }

            if (carry > 0) newData[len] = carry;
            result = new BetterBigInteger(Trim(newData), isnegative);
        }

        return result;
    }

    private static BetterBigInteger SubtractModules(BetterBigInteger a, BetterBigInteger b, bool isnegative)
    {
        if (a._data == null && b._data == null)
        {
            uint borrow = SubtractWithBorrow(a._smallValue, b._smallValue, 0, out uint diff);
            if (borrow != 0) return new BetterBigInteger(0, false);
            return new BetterBigInteger(diff, isnegative);
        }

        if (a._data == null && b._data != null)
        {
            return SubtractModules(b, a, !isnegative);
        }

        if (a._data != null && b._data == null)
        {
            uint[] newData = new uint[a._data.Length];
            uint borrow = SubtractWithBorrow(a._data[0], b._smallValue, 0, out newData[0]);

            for (int i = 1; i < a._data.Length; i++)
            {
                borrow = SubtractWithBorrow(a._data[i], 0, borrow, out newData[i]);
            }

            return new BetterBigInteger(Trim(newData), isnegative);
        }

        int len = Math.Max(a._data.Length, b._data.Length);
        uint[] result = new uint[len];
        uint borrowAll = 0;

        for (int i = 0; i < len; i++)
        {
            uint valA = i < a._data.Length ? a._data[i] : 0;
            uint valB = i < b._data.Length ? b._data[i] : 0;
            borrowAll = SubtractWithBorrow(valA, valB, borrowAll, out result[i]);
        }

        return new BetterBigInteger(Trim(result), isnegative);
    }

    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        // если знаки одинаковые
        if (a.IsNegative == b.IsNegative)
        {
            return AddModules(a, b, a.IsNegative);
        }
        // если знаки разные то вычитание по модулю -сравниваем абсолютные значения
        int compare = CompareAbs(a, b);

        if (compare == 0) return new BetterBigInteger(0, false);
        else if (compare > 0)
        {
            return SubtractModules(a, b, a.IsNegative);
        }
        else
        {
            return SubtractModules(b, a, b.IsNegative);
        }
    }

    private static int CompareAbs(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> ad = a.GetDigits();
        ReadOnlySpan<uint> bd = b.GetDigits();

        if (ad.Length != bd.Length) return ad.Length.CompareTo(bd.Length);

        for (int i = ad.Length - 1; i >= 0; i--)
        {
            if (ad[i] != bd[i]) return ad[i].CompareTo(bd[i]);
        }

        return 0;
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
    {
        return a + (-b);
    }

    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        if (a.IsZero()) return a;

        uint[] currentDigits = a.GetDigits().ToArray();

        bool newIsNegative = (a._signBit == 0);

        return new BetterBigInteger(currentDigits, newIsNegative);
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        BetterBigInteger quotient = DivMod(a, b, out BetterBigInteger remainder);
        return quotient;
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        DivMod(a, b, out BetterBigInteger remainder);
        return remainder;
    }

    private static BetterBigInteger DivMod(BetterBigInteger a, BetterBigInteger b, out BetterBigInteger remainder)
    {
        if (b == null) throw new ArgumentNullException(nameof(b));
        if (b.IsZero()) throw new DivideByZeroException();

        if (a.IsZero())
        {
            remainder = new BetterBigInteger(0, false);
            return new BetterBigInteger(0, false);
        }

        bool resultNegative = a.IsNegative ^ b.IsNegative;

        BetterBigInteger absA = a.IsNegative ? -a : a;
        BetterBigInteger absB = b.IsNegative ? -b : b;

        if (absA < absB)
        {
            remainder = a;
            return new BetterBigInteger(0, false);
        }

        if (absB._data == null)
        {
            uint divisor = absB._smallValue;
            uint[] qdigits = absA.GetDigits().ToArray();
            uint rem = DivRem(qdigits, divisor);
            BetterBigInteger q = new BetterBigInteger(Trim(qdigits), resultNegative);
            BetterBigInteger r = new BetterBigInteger(rem, false);
            if (a.IsNegative && !r.IsZero()) r = -r;
            remainder = r;
            return q;
        }

        BetterBigInteger quotient = new BetterBigInteger(0u, false);
        BetterBigInteger current = absB;
        int shift = 0;

        while (current <= absA)
        {
            current = current << 1;
            shift++;
        }

        if (shift > 0)
        {
            current = current >> 1;
            shift--;
        }

        for (int k = shift; k >= 0; k--)
        {
            if (current <= absA)
            {
                absA = absA - current;
                BetterBigInteger add = (new BetterBigInteger(1u, false) << k);
                quotient = quotient + add;
            }
            current = current >> 1;
        }

        remainder = absA;
        if (a.IsNegative && !remainder.IsZero()) remainder = -remainder;

        if (resultNegative) quotient = -quotient;
        return quotient;
    }


    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.Sign == 0 || b.Sign == 0)
            return new BetterBigInteger(0, false);

        ReadOnlySpan<uint> x = a.GetDigits();
        ReadOnlySpan<uint> y = b.GetDigits();

        int minLength = Math.Min(x.Length, y.Length);
        int maxLength = Math.Max(x.Length, y.Length);
        
        if (minLength < 16) 
            return new SimpleMultiplier().Multiply(a, b);
        
        if (maxLength < 512)
            return new KaratsubaMultiplier().Multiply(a, b);
        
        return new FftMultiplier().Multiply(a, b);
    }

    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        return -(a + new BetterBigInteger(1, false));
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
    {
        return BitwiseOp(a, b, (x, y) => x & y);
    }

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
    {
        return BitwiseOp(a, b, (x, y) => x | y);
    }

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
    {
        return BitwiseOp(a, b, (x, y) => x ^ y);
    }

    private static BetterBigInteger BitwiseOp(BetterBigInteger a, BetterBigInteger b, Func<uint, uint, uint> op)
    {
        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();

        int len = Math.Max(aDigits.Length, bDigits.Length) + 1;

        uint[] aTwos = new uint[len];
        uint[] bTwos = new uint[len];

        for (int i = 0; i < aDigits.Length && i < len; i++) aTwos[i] = aDigits[i];
        for (int i = 0; i < bDigits.Length && i < len; i++) bTwos[i] = bDigits[i];

        if (a.IsNegative)
        {
            for (int i = 0; i < len; i++) aTwos[i] = ~aTwos[i];

            uint carry = 1u;
            for (int i = 0; i < len && carry != 0; i++)
            {
                carry = AddWithCarry(aTwos[i], 0, carry, out uint sum);
                aTwos[i] = sum;
            }
        }

        if (b.IsNegative)
        {
            for (int i = 0; i < len; i++) bTwos[i] = ~bTwos[i];
            uint carry = 1u;
            for (int i = 0; i < len && carry != 0; i++)
            {
                carry = AddWithCarry(bTwos[i], 0, carry, out uint sum);
                bTwos[i] = sum;
            }
        }

        uint[] resTwos = new uint[len];
        for (int i = 0; i < len; i++)
        {
            resTwos[i] = op(aTwos[i], bTwos[i]);
        }

        bool resNegative = (resTwos[len - 1] >> 31) == 1;

        uint[] magnitude = new uint[len];

        if (resNegative)
        {
            for (int i = 0; i < len; i++) magnitude[i] = ~resTwos[i];
            uint carry = 1u;
            for (int i = 0; i < len && carry != 0; i++)
            {
                carry = AddWithCarry(magnitude[i], 0, carry, out uint sum);
                magnitude[i] = sum;
            }
        }
        else
        {
            Array.Copy(resTwos, magnitude, len);
        }

        return new BetterBigInteger(Trim(magnitude), resNegative);
    }

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        if (shift == 0 || a.IsZero()) return a;
        if (shift < 0) return a >> -shift;

        int fullWords = shift / 32;
        int bits = shift % 32;

        ReadOnlySpan<uint> aDigits = a.GetDigits();
        uint[] newData = new uint[aDigits.Length + fullWords + 1];

        uint carry = 0;
        for (int i = 0; i < aDigits.Length; i++)
        {
            newData[i + fullWords] = (aDigits[i] << bits) | carry;
            carry = bits > 0 ? aDigits[i] >> (32 - bits) : 0;
        }

        if (carry > 0)
            newData[aDigits.Length + fullWords] = carry;

        return new BetterBigInteger(Trim(newData), a.IsNegative);
    }

    public static BetterBigInteger operator >>(BetterBigInteger a, int shift)
    {
        if (shift == 0 || a.IsZero()) return a;
        if (shift < 0) return a << -shift;

        int fullWords = shift / 32;
        int bits = shift % 32;

        ReadOnlySpan<uint> digits = a.GetDigits();
        int totalLen = digits.Length + 1;

        uint[] twos = new uint[totalLen];
        for (int i = 0; i < digits.Length; i++) twos[i] = digits[i];

        if (a.IsNegative)
        {
            for (int i = 0; i < totalLen; i++) twos[i] = ~twos[i];
            uint carry = 1u;
            for (int i = 0; i < totalLen && carry != 0; i++)
            {
                carry = AddWithCarry(twos[i], 0, carry, out uint sum);
                twos[i] = sum;
            }
        }

        if (fullWords >= totalLen)
        {
            return a.IsNegative ? new BetterBigInteger(1, true) : new BetterBigInteger(0, false);
        }

        int resLen = Math.Max(1, totalLen - fullWords);
        uint[] resTwos = new uint[resLen];

        for (int i = 0; i < resLen; i++)
        {
            int src = i + fullWords;
            uint lowPart = twos[src] >> bits;
            uint highPart = 0u;
            if (bits != 0)
            {
                if (src + 1 < totalLen) highPart = twos[src + 1] << (32 - bits);
                else highPart = (a.IsNegative ? Mask32 : 0u) << (32 - bits);
            }
            resTwos[i] = lowPart | highPart;
        }

        bool resNegative = (resTwos[resLen - 1] >> 31) == 1;

        uint[] magnitude = new uint[resLen];
        if (resNegative)
        {
            for (int i = 0; i < resLen; i++) magnitude[i] = ~resTwos[i];
            uint carry = 1u;
            for (int i = 0; i < resLen && carry != 0; i++)
            {
                carry = AddWithCarry(magnitude[i], 0, carry, out uint sum);
                magnitude[i] = sum;
            }
        }
        else
        {
            Array.Copy(resTwos, magnitude, resLen);
        }

        return new BetterBigInteger(Trim(magnitude), resNegative);
    }

    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;

    private static uint DivRem(uint[] digits, uint divisor)
    {
        uint remainder = 0;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            digits[i] = Divide64By32(remainder, digits[i], divisor, out remainder);
        }

        return remainder;
    }

    private static char DigitToChar(uint digit)
    {
        return digit < 10 ? (char)('0' + digit) : (char)('A' + (digit - 10));
    }

    public override string ToString() => ToString(10);

    public string ToString(int radix)
    {
        if (radix < 2 || radix > 36)
            throw new ArgumentOutOfRangeException(nameof(radix), "Основание должно быть в пределах [2..36].");

        if (IsZero())
            return "0";

        uint[] digits = GetDigits().ToArray();
        bool negative = IsNegative;
        var chars = new List<char>();
        uint baseValue = (uint)radix;

        while (!(digits.Length == 1 && digits[0] == 0))
        {
            uint remainder = DivRem(digits, baseValue);
            chars.Add(DigitToChar(remainder));
            digits = Trim(digits);
        }

        if (negative)
            chars.Add('-');

        chars.Reverse();
        return new string(chars.ToArray());
    }

}