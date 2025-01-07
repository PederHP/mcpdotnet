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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Promoting command for {serverId} ({name}) to shell argument for stdio transport with command {command} and arguments {arguments}")]
    internal static partial void PromotingCommandToShellArgumentForStdio(this ILogger logger, string serverId, string name, string command, string arguments);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Initializing stdio commands")]
    internal static partial void InitializingStdioCommands(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sampling handler not configured for server {serverId} ({serverName}), always set a handler when using this capability")]
    internal static partial void SamplingHandlerNotConfigured(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Client server {serverId} ({serverName}) already initializing")]
    internal static partial void ClientAlreadyInitializing(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Client server {serverId} ({serverName}) already initialized")]
    internal static partial void ClientAlreadyInitialized(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Client server {serverId} ({serverName}) initialization error")]
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Request handler error for server {serverId} ({serverName}) with method {method}")]
    internal static partial void RequestHandlerError(this ILogger logger, string serverId, string serverName, string method, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No request found for message with ID {messageWithId} for server {serverId} ({serverName})")]
    internal static partial void NoRequestFoundForMessageWithId(this ILogger logger, string serverId, string serverName, string messageWithId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Notification handler error for server {serverId} ({serverName}) with method {method}")]
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport for server {serverId} ({serverName}) already connected")]
    internal static partial void TransportAlreadyConnected(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport for server {serverId} ({serverName}) connecting")]
    internal static partial void TransportConnecting(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating process for transport for server {serverId} ({serverName}) with command {command}, arguments {arguments}, environment {environment}, working directory {workingDirectory}, shutdown timeout {shutdownTimeout}")]
    internal static partial void CreateProcessForTransport(this ILogger logger, string serverId, string serverName, string command, string arguments, string environment, string workingDirectory, string shutdownTimeout);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport for server {serverId} ({serverName}) error: {data}")]
    internal static partial void TransportError(this ILogger logger, string serverId, string serverName, string data);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport process start failed for server {serverId} ({serverName})")]
    internal static partial void TransportProcessStartFailed(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport process started for server {serverId} ({serverName}) with PID {processId}")]
    internal static partial void TransportProcessStarted(this ILogger logger, string serverId, string serverName, int processId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport reading messages for server {serverId} ({serverName})")]
    internal static partial void TransportReadingMessages(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport connect failed for server {serverId} ({serverName})")]
    internal static partial void TransportConnectFailed(this ILogger logger, string serverId, string serverName, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport not connected for server {serverId} ({serverName})")]
    internal static partial void TransportNotConnected(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport sending message for server {serverId} ({serverName}) with ID {messageId}, JSON {json}")]
    internal static partial void TransportSendingMessage(this ILogger logger, string serverId, string serverName, string messageId, string json);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport message sent for server {serverId} ({serverName}) with ID {messageId}")]
    internal static partial void TransportSentMessage(this ILogger logger, string serverId, string serverName, string messageId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport send failed for server {serverId} ({serverName}) with ID {messageId}")]
    internal static partial void TransportSendFailed(this ILogger logger, string serverId, string serverName, string messageId, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport entering read messages loop for server {serverId} ({serverName})")]
    internal static partial void TransportEnteringReadMessagesLoop(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport waiting for message for server {serverId} ({serverName})")]
    internal static partial void TransportWaitingForMessage(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport end of stream for server {serverId} ({serverName})")]
    internal static partial void TransportEndOfStream(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport received message for server {serverId} ({serverName}): {line}")]
    internal static partial void TransportReceivedMessage(this ILogger logger, string serverId, string serverName, string line);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport received message parsed for server {serverId} ({serverName}): {messageId}")]
    internal static partial void TransportReceivedMessageParsed(this ILogger logger, string serverId, string serverName, string messageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport message written for server {serverId} ({serverName}) with ID {messageId}")]
    internal static partial void TransportMessageWritten(this ILogger logger, string serverId, string serverName, string messageId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport message parse failed due to unexpected message schema for server {serverId} ({serverName}): {line}")]
    internal static partial void TransportMessageParseUnexpectedType(this ILogger logger, string serverId, string serverName, string line);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport message parse failed for server {serverId} ({serverName}): {line}")]
    internal static partial void TransportMessageParseFailed(this ILogger logger, string serverId, string serverName, string line, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport exiting read messages loop for server {serverId} ({serverName})")]
    internal static partial void TransportExitingReadMessagesLoop(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport read messages cancelled for server {serverId} ({serverName})")]
    internal static partial void TransportReadMessagesCancelled(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport read messages failed for server {serverId} ({serverName})")]
    internal static partial void TransportReadMessagesFailed(this ILogger logger, string serverId, string serverName, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport cleaning up for server {serverId} ({serverName})")]
    internal static partial void TransportCleaningUp(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Transport closing stdin for server {serverId} ({serverName})")]
    internal static partial void TransportClosingStdin(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Transport waiting for shutdown for server {serverId} ({serverName})")]
    internal static partial void TransportWaitingForShutdown(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Transport killing process for server {serverId} ({serverName})")]
    internal static partial void TransportKillingProcess(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport shutdown failed for server {serverId} ({serverName})")]
    internal static partial void TransportShutdownFailed(this ILogger logger, string serverId, string serverName, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Transport waiting for read task for server {serverId} ({serverName})")]
    internal static partial void TransportWaitingForReadTask(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Transport cleanup read task timeout for server {serverId} ({serverName})")]
    internal static partial void TransportCleanupReadTaskTimeout(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport cleanup read task cancelled for server {serverId} ({serverName})")]
    internal static partial void TransportCleanupReadTaskCancelled(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport cleanup read task failed for server {serverId} ({serverName})")]
    internal static partial void TransportCleanupReadTaskFailed(this ILogger logger, string serverId, string serverName, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport read task cleaned up for server {serverId} ({serverName})")]
    internal static partial void TransportReadTaskCleanedUp(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport cleaned up for server {serverId} ({serverName})")]
    internal static partial void TransportCleanedUp(this ILogger logger, string serverId, string serverName);

    [LoggerMessage(Level = LogLevel.Error, Message = "JSON-RPC message start object token expected")]
    internal static partial void JsonRpcMessageConverterExpectedStartObjectToken(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "JSON-RPC message invalid version")]
    internal static partial void JsonRpcMessageConverterInvalidJsonRpcVersion(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Trace, Message = "JSON-RPC message deserializing error response: {rawText}")]
    internal static partial void JsonRpcMessageConverterDeserializingErrorResponse(this ILogger logger, string rawText);

    [LoggerMessage(Level = LogLevel.Trace, Message = "JSON-RPC message deserializing response: {rawText}")]
    internal static partial void JsonRpcMessageConverterDeserializingResponse(this ILogger logger, string rawText);

    [LoggerMessage(Level = LogLevel.Error, Message = "JSON-RPC message response must have result or error: {rawText}")]
    internal static partial void JsonRpcMessageConverterResponseMustHaveResultOrError(this ILogger logger, string rawText);

    [LoggerMessage(Level = LogLevel.Trace, Message = "JSON-RPC message deserializing notification: {rawText}")]
    internal static partial void JsonRpcMessageConverterDeserializingNotification(this ILogger logger, string rawText);

    [LoggerMessage(Level = LogLevel.Trace, Message = "JSON-RPC message deserializing request: {rawText}")]
    internal static partial void JsonRpcMessageConverterDeserializingRequest(this ILogger logger, string rawText);

    [LoggerMessage(Level = LogLevel.Error, Message = "JSON-RPC message invalid format: {rawText}")]
    internal static partial void JsonRpcMessageConverterInvalidMessageFormat(this ILogger logger, string rawText);

    [LoggerMessage(Level = LogLevel.Error, Message = "JSON-RPC message write unknown message type: {type}")]
    internal static partial void JsonRpcMessageConverterWriteUnknownMessageType(this ILogger logger, string type);
}