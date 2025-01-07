using Microsoft.Extensions.Logging;

namespace McpDotNet.Logging;

/// <summary>
/// Logging methods for the McpDotNet library.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Client {clientId} initializing connection to server {serverId}")]
    internal static partial void ClientConnecting(this ILogger logger, string clientId, string serverId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Server capabilities received: {capabilities}")]
    internal static partial void ServerCapabilitiesReceived(this ILogger logger, string capabilities);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request {requestId} timed out")]
    internal static partial void RequestTimeout(this ILogger logger, int requestId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Command for {ServerId} ({Name}) already contains shell wrapper, skipping argument injection")]
    internal static partial void SkippingShellWrapper(this ILogger logger, string serverId, string name);

    [LoggerMessage(Level = LogLevel.Error, Message = "Server config for Id={serverId} not found")]
    internal static partial void ServerNotFound(this ILogger logger, string serverId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Client for {serverId} ({name}) already created, returning cached client")]
    internal static partial void ClientExists(this ILogger logger, string serverId, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating client for {serverId} ({name})")]
    internal static partial void CreatingClient(this ILogger logger, string serverId, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Client for {serverId} ({name}) created and connected")]
    internal static partial void ClientCreated(this ILogger logger, string serverId, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating transport for {serverId} ({name}) with type {transportType} and options {options}")]
    internal static partial void CreatingTransport(this ILogger logger, string serverId, string name, string transportType, string options);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Promoting command for {serverId} ({name}) to shell argument for stdio transport with arguments {arguments}")]
    internal static partial void PromotingCommandToShellArgumentForStdio(this ILogger logger, string serverId, string name, string command, string arguments);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Initializing stdio commands")]
    internal static partial void InitializingStdioCommands(this ILogger logger);
}