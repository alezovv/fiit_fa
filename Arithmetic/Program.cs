// тут можно что-то тестить

// Console.WriteLine("52");

// using Arithmetic.BetterBigInteger
using System.Numerics;
using Arithmetic.BigInt;

namespace Arithmetic;
class Program
{
    private static void Main()
    {
        BetterBigInteger a = new ("123A56A8999A", 12);
        foreach (var digit in a.GetDigits())
        {
            Console.Write(digit + " ");
        }

    }

}

