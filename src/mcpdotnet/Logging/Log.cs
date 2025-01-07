using McpDotNet.Configuration;
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Sampling handler not configured for server {serverId} ({serverName}), always set a handler when using this capability")]
    internal static partial void SamplingHandlerNotConfigured(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Client server {serverId} ({serverName}) already initializing")]
    internal static partial void ClientAlreadyInitializing(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Client server {serverId} ({serverName}) already initialized")]
    internal static partial void ClientAlreadyInitialized(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Client server {serverId} ({serverName}) initialization error: {exception}")]
    internal static partial void ClientInitializationError(this ILogger logger, string serverId, string serverName, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Server {serverId} ({serverName}) capabilities received: {capabilities}, server info: {serverInfo}")]
    internal static partial void ServerCapabilitiesReceived(this ILogger logger, string serverId, string serverName, string capabilities, string serverInfo);

    [LoggerMessage(Level = LogLevel.Error, Message = "Server {serverId} ({serverName}) protocol version mismatch, expected {expected}, received {received}")]
    internal static partial void ServerProtocolVersionMismatch(this ILogger logger, string serverId, string serverName, string expected, string received);

    [LoggerMessage(Level = LogLevel.Error, Message = "Client server {serverId} ({serverName}) initialization timeout")]
    internal static partial void ClientInitializationTimeout(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Pinging server {serverId} ({serverName})")]
    internal static partial void PingingServer(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Listing tools for server {serverId} ({serverName}) with cursor {cursor}")]
    internal static partial void ListingTools(this ILogger logger, string serverId, string serverName, string cursor);

    [LoggerMessage(Level = LogLevel.Information, Message = "Listing prompts for server {serverId} ({serverName}) with cursor {cursor}")]
    internal static partial void ListingPrompts(this ILogger logger, string serverId, string serverName, string cursor);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting prompt {name} for server {serverId} ({serverName}) with arguments {arguments}")]
    internal static partial void GettingPrompt(this ILogger logger, string serverId, string serverName, string name, string arguments);

    [LoggerMessage(Level = LogLevel.Information, Message = "Listing resources for server {serverId} ({serverName}) with cursor {cursor}")]
    internal static partial void ListingResources(this ILogger logger, string serverId, string serverName, string cursor);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reading resource {uri} for server {serverId} ({serverName})")]
    internal static partial void ReadingResource(this ILogger logger, string serverId, string serverName, string uri);

    [LoggerMessage(Level = LogLevel.Information, Message = "Subscribing to resource {uri} for server {serverId} ({serverName})")]
    internal static partial void SubscribingToResource(this ILogger logger, string serverId, string serverName, string uri);

    [LoggerMessage(Level = LogLevel.Information, Message = "Unsubscribing from resource {uri} for server {serverId} ({serverName})")]
    internal static partial void UnsubscribingFromResource(this ILogger logger, string serverId, string serverName, string uri);

    [LoggerMessage(Level = LogLevel.Information, Message = "Calling tool {toolName} for server {serverId} ({serverName}) with arguments {arguments}")]
    internal static partial void CallingTool(this ILogger logger, string serverId, string serverName, string toolName, string arguments);

    [LoggerMessage(Level = LogLevel.Information, Message = "Client message processing cancelled for server {serverId} ({serverName})")]
    internal static partial void ClientMessageProcessingCancelled(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request handler called for server {serverId} ({serverName}) with method {method}")]
    internal static partial void RequestHandlerCalled(this ILogger logger, string serverId, string serverName, string method);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request handler completed for server {serverId} ({serverName}) with method {method}")]
    internal static partial void RequestHandlerCompleted(this ILogger logger, string serverId, string serverName, string method);

    [LoggerMessage(Level = LogLevel.Error, Message = "Request handler error for server {serverId} ({serverName}) with method {method}: {exception}")]
    internal static partial void RequestHandlerError(this ILogger logger, string serverId, string serverName, string method, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No request found for message with ID {messageWithId} for server {serverId} ({serverName})")]
    internal static partial void NoRequestFoundForMessageWithId(this ILogger logger, string serverId, string serverName, string messageWithId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Notification handler error for server {serverId} ({serverName}) with method {method}: {exception}")]
    internal static partial void NotificationHandlerError(this ILogger logger, string serverId, string serverName, string method, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Client not connected for server {serverId} ({serverName})")]
    internal static partial void ClientNotConnected(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sending request payload for server {serverId} ({serverName}): {payload}")]
    internal static partial void SendingRequestPayload(this ILogger logger, string serverId, string serverName, string payload);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sending request for server {serverId} ({serverName}) with method {method}")]
    internal static partial void SendingRequest(this ILogger logger, string serverId, string serverName, string method);

    [LoggerMessage(Level = LogLevel.Error, Message = "Request failed for server {serverId} ({serverName}) with method {method}: {message} ({code})")]
    internal static partial void RequestFailed(this ILogger logger, string serverId, string serverName, string method, string message, int code);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request response received payload for server {serverId} ({serverName}): {payload}")]
    internal static partial void RequestResponseReceivedPayload(this ILogger logger, string serverId, string serverName, string payload);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request response received for server {serverId} ({serverName}) with method {method}")]
    internal static partial void RequestResponseReceived(this ILogger logger, string serverId, string serverName, string method);

    [LoggerMessage(Level = LogLevel.Error, Message = "Request response type conversion error for server {serverId} ({serverName}) with method {method}: expected {expectedType}")]
    internal static partial void RequestResponseTypeConversionError(this ILogger logger, string serverId, string serverName, string method, Type expectedType);

    [LoggerMessage(Level = LogLevel.Error, Message = "Request invalid response type for server {serverId} ({serverName}) with method {method}")]
    internal static partial void RequestInvalidResponseType(this ILogger logger, string serverId, string serverName, string method);

    [LoggerMessage(Level = LogLevel.Error, Message = "Request params type conversion error for server {serverId} ({serverName}) with method {method}: expected {expectedType}")]
    internal static partial void RequestParamsTypeConversionError(this ILogger logger, string serverId, string serverName, string method, Type expectedType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cleaning up client for server {serverId} ({serverName})")]
    internal static partial void CleaningUpClient(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Client cleaned up for server {serverId} ({serverName})")]
    internal static partial void ClientCleanedUp(this ILogger logger, string serverId, string serverName);
}