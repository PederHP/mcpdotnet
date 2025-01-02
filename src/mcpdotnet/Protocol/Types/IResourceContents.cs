namespace McpDotNet.Protocol.Types;

/// <summary>
/// Interface for resource contents.
/// </summary>
public interface IResourceContents
{
    string Uri { get; }
    string? MimeType { get; }
}
