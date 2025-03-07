namespace System.IO;

internal static class TextReaderExtensions
{
    public static Task<string> ReadLineAsync(this TextReader reader, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return reader.ReadLineAsync();
    }
}