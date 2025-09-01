namespace DiscountServer.Services;

public class CodeGenerationService
{
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static readonly Random Random = new();

    public HashSet<string> GenerateUniqueCodes(ushort count, byte length)
    {
        var codes = new HashSet<string>();
        while (codes.Count < count)
        {
            codes.Add(GenerateRandomString(length));
        }
        return codes;
    }

    private string GenerateRandomString(int length)
    {
        return new string(Enumerable.Repeat(Chars, length)
            .Select(s => s[Random.Next(s.Length)]).ToArray());
    }
}