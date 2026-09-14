using System.Security.Cryptography;
using SAFC.Expense.Application.Common.Interfaces;

namespace SAFC.Expense.Infrastructure.Authentication;

internal sealed class TemporaryPasswordGenerator : ITemporaryPasswordGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
    private const int Length = 16;

    public string Generate() => RandomNumberGenerator.GetString(Alphabet, Length);
}
