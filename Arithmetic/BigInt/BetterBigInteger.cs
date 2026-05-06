using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit; // 0 - полож, 1 - отриц
    
    private uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data;
    
    public bool IsNegative => _signBit == 1;
    
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
            ulong res = (ulong)_smallValue * multiplier + adder;
            
            if (res <= uint.MaxValue)
            {
                _smallValue = (uint)res;
            }
            else
            {
                // Число выросло: создаем массив из двух элементов
                _data = new uint[2];
                _data[0] = (uint)(res & 0xFFFFFFFF); // Младшие 32 бита
                _data[1] = (uint)(res >> 32);         // Старшие 32 бита
                _smallValue = 0; // Сбрасываем, так как теперь работаем с _data
            }
        }
        // Число уже хранится в массиве
        else
        {
            ulong carry = (ulong)adder;
            
            for (int i = 0; i < _data.Length; i++)
            {
                ulong current = (ulong)_data[i] * multiplier + carry;
                _data[i] = (uint)(current & 0xFFFFFFFF);
                carry = current >> 32; // Перенос на следующий разряд
            }

            // Если после прохода по всему массиву остался перенос — расширяем массив
            if (carry > 0)
            {
                int oldLen = _data.Length;
                Array.Resize(ref _data, oldLen + 1);
                _data[oldLen] = (uint)carry;
            }
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
        return _data ?? [_smallValue];
    }
    
    public int CompareTo(IBigInteger? other)
    {
        throw new NotImplementedException();
        /* if (other is null) return 1; // Любое число больше null
        if (ReferenceEquals(this, other)) return 0;

        // Сразу обрабатываем случай двух нулей, чтобы не возиться со знаками
        if (this.IsZero() && other.IsZero()) return 0;

        // 2. Сравнение по знакам
        // Предполагаем: Sign == 1 (отрицательное), Sign == 0 (положительное)
        if (this.Sign != other.Sign)
        {
            // Положительное всегда больше отрицательного
            // Если this.Sign == 1 (минус), значит он меньше (вернуть -1)
            return this.Sign == 1 ? -1 : 1;
        }

        // 3. Сравнение по количеству разрядов (длине массива)
        ReadOnlySpan<uint> thisDigits = this.GetDigits();
        ReadOnlySpan<uint> otherDigits = other.GetDigits();

        if (thisDigits.Length != otherDigits.Length)
        {
            int result = thisDigits.Length.CompareTo(otherDigits.Length);
            // Если числа отрицательные, то более длинное число на самом деле меньше 
            // (например, -100 < -10)
            return this.Sign == 1 ? -result : result;
        }

        // 4. Поразрядное сравнение (самый важный этап)
        // Идем от старших разрядов к младшим (с конца массива)
        for (int i = thisDigits.Length - 1; i >= 0; i--)
        {
            if (thisDigits[i] != otherDigits[i])
            {
                int result = thisDigits[i].CompareTo(otherDigits[i]);
                // Опять же, для отрицательных инвертируем результат
                return this.Sign == 1 ? -result : result;
            }
        }

        // Если всё совпало
        return 0; */
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
    
    public bool IsZero()
    {
        if (_data == null || _data.Length == 0) return true;

        // Если в массиве только один элемент и он 0
        if (_data.Length == 1 && _data[0] == 0) return true;

        return true;
    }
    
    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        throw new NotImplementedException();
        /* uint carry = 0;
        int i = 0;
        while (i < Math.Max(a, b) || carry)
        {
            
        } */

    }
    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    
    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        if (a.IsZero()) return a;
        
        uint[] currentDigits = a.GetDigits().ToArray();

        bool newIsNegative = (a._signBit == 0);

        return new BetterBigInteger(currentDigits, newIsNegative);
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    
    
    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
       => throw new NotImplementedException("Умножение делегируется стратегии, выбирать необходимо в зависимости от размеров чисел");
    
    public static BetterBigInteger operator ~(BetterBigInteger a) => throw new NotImplementedException();
    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator <<(BetterBigInteger a, int shift) => throw new NotImplementedException();
    public static BetterBigInteger operator >> (BetterBigInteger a, int shift) => throw new NotImplementedException();
    
    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;
    
    public override string ToString() => ToString(10);
    public string ToString(int radix) => throw new NotImplementedException();
    
}