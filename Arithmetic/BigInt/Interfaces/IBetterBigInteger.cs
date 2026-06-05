using System.Dynamic;

namespace Arithmetic.BigInt.Interfaces;


public interface IBigInteger : IComparable<IBigInteger>, IEquatable<IBigInteger>
{
    bool IsNegative { get; }
    int Sign { get; }
    ReadOnlySpan<uint> GetDigits(); // Little-endian представление
    string ToString(int radix);
    bool IsZero(); // проверка на ноль
}