namespace MonkePhone.Extensions;

public static class StringEx
{
    public static string ToTitleCase(this string str) => string.Concat(str.ToUpper()[0], str.ToLower()[1..]);
}