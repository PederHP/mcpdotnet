using McpDotNet.Configuration;
using McpDotNet.Protocol.Messages;
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Command for {endpointName} already contains shell wrapper, skipping argument injection")]
    internal static partial void SkippingShellWrapper(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Server config for Id={serverId} not found")]
    internal static partial void ServerNotFound(this ILogger logger, string serverId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Client for {endpointName} already created, returning cached client")]
    internal static partial void ClientExists(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating client for {endpointName}")]
    internal static partial void CreatingClient(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Client for {endpointName} created and connected")]
    internal static partial void ClientCreated(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating transport for {endpointName} with type {transportType} and options {options}")]
    internal static partial void CreatingTransport(this ILogger logger, string endpointName, string transportType, string options);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Promoting command for {endpointName} to shell argument for stdio transport with command {command} and arguments {arguments}")]
    internal static partial void PromotingCommandToShellArgumentForStdio(this ILogger logger, string endpointName, string command, string arguments);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Initializing stdio commands")]
    internal static partial void InitializingStdioCommands(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sampling handler not configured for server {endpointName}, always set a handler when using this capability")]
    internal static partial void SamplingHandlerNotConfigured(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Roots handler not configured for server {endpointName}, always set a handler when using this capability")]
    internal static partial void RootsHandlerNotConfigured(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Error, Message = "List tools handler not configured for server {endpointName}, always set a handler when using this capability")]
    internal static partial void ListToolsHandlerNotConfigured(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Call tool handler not configured for server {endpointName}, always set a handler when using this capability")]
    internal static partial void CallToolHandlerNotConfigured(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Error, Message = "List prompts handler not configured for server {endpointName}, always set a handler when using this capability")]
    internal static partial void ListPromptsHandlerNotConfigured(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Get prompt handler not configured for server {endpointName}, always set a handler when using this capability")]
    internal static partial void GetPromptHandlerNotConfigured(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Error, Message = "List resources handler not configured for server {endpointName}, always set a handler when using this capability")]
    internal static partial void ListResourcesHandlerNotConfigured(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Read resource handler not configured for server {endpointName}, always set a handler when using this capability")]
    internal static partial void ReadResourceHandlerNotConfigured(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Client server {endpointName} already initializing")]
    internal static partial void ClientAlreadyInitializing(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Client server {endpointName} already initialized")]
    internal static partial void ClientAlreadyInitialized(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Client server {endpointName} initialization error")]
    internal static partial void ClientInitializationError(this ILogger logger, string endpointName, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Server {endpointName} capabilities received: {capabilities}, server info: {serverInfo}")]
    internal static partial void ServerCapabilitiesReceived(this ILogger logger, string endpointName, string capabilities, string serverInfo);

    [LoggerMessage(Level = LogLevel.Error, Message = "Server {endpointName} protocol version mismatch, expected {expected}, received {received}")]
    internal static partial void ServerProtocolVersionMismatch(this ILogger logger, string endpointName, string expected, string received);

    [LoggerMessage(Level = LogLevel.Error, Message = "Client server {endpointName} initialization timeout")]
    internal static partial void ClientInitializationTimeout(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Pinging server {endpointName}")]
    internal static partial void PingingServer(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Listing tools for server {endpointName} with cursor {cursor}")]
    internal static partial void ListingTools(this ILogger logger, string endpointName, string cursor);

    [LoggerMessage(Level = LogLevel.Information, Message = "Listing prompts for server {endpointName} with cursor {cursor}")]
    internal static partial void ListingPrompts(this ILogger logger, string endpointName, string cursor);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting prompt {name} for server {endpointName} with arguments {arguments}")]
    internal static partial void GettingPrompt(this ILogger logger, string endpointName, string name, string arguments);

    [LoggerMessage(Level = LogLevel.Information, Message = "Listing resources for server {endpointName} with cursor {cursor}")]
    internal static partial void ListingResources(this ILogger logger, string endpointName, string cursor);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reading resource {uri} for server {endpointName}")]
    internal static partial void ReadingResource(this ILogger logger, string endpointName, string uri);

    [LoggerMessage(Level = LogLevel.Information, Message = "Subscribing to resource {uri} for server {endpointName}")]
    internal static partial void SubscribingToResource(this ILogger logger, string endpointName, string uri);

    [LoggerMessage(Level = LogLevel.Information, Message = "Unsubscribing from resource {uri} for server {endpointName}")]
    internal static partial void UnsubscribingFromResource(this ILogger logger, string endpointName, string uri);

    [LoggerMessage(Level = LogLevel.Information, Message = "Calling tool {toolName} for server {endpointName} with arguments {arguments}")]
    internal static partial void CallingTool(this ILogger logger, string endpointName, string toolName, string arguments);

    [LoggerMessage(Level = LogLevel.Information, Message = "Client message processing cancelled for server {endpointName}")]
    internal static partial void EndpointMessageProcessingCancelled(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request handler called for server {endpointName} with method {method}")]
    internal static partial void RequestHandlerCalled(this ILogger logger, string endpointName, string method);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request handler completed for server {endpointName} with method {method}")]
    internal static partial void RequestHandlerCompleted(this ILogger logger, string endpointName, string method);

    [LoggerMessage(Level = LogLevel.Error, Message = "Request handler error for server {endpointName} with method {method}")]
    internal static partial void RequestHandlerError(this ILogger logger, string endpointName, string method, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No request found for message with ID {messageWithId} for server {endpointName}")]
    internal static partial void NoRequestFoundForMessageWithId(this ILogger logger, string endpointName, string messageWithId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Notification handler error for server {endpointName} with method {method}")]
    internal static partial void NotificationHandlerError(this ILogger logger, string endpointName, string method, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Client not connected for server {endpointName}")]
    internal static partial void ClientNotConnected(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Endpoint not connected for {endpointName}")]
    internal static partial void EndpointNotConnected(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sending request payload for server {endpointName}: {payload}")]
    internal static partial void SendingRequestPayload(this ILogger logger, string endpointName, string payload);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sending request for server {endpointName} with method {method}")]
    internal static partial void SendingRequest(this ILogger logger, string endpointName, string method);

    [LoggerMessage(Level = LogLevel.Error, Message = "Request failed for server {endpointName} with method {method}: {message} ({code})")]
    internal static partial void RequestFailed(this ILogger logger, string endpointName, string method, string message, int code);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request response received payload for server {endpointName}: {payload}")]
    internal static partial void RequestResponseReceivedPayload(this ILogger logger, string endpointName, string payload);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request response received for server {endpointName} with method {method}")]
    internal static partial void RequestResponseReceived(this ILogger logger, string endpointName, string method);

    [LoggerMessage(Level = LogLevel.Error, Message = "Request response type conversion error for server {endpointName} with method {method}: expected {expectedType}")]
    internal static partial void RequestResponseTypeConversionError(this ILogger logger, string endpointName, string method, Type expectedType);

    [LoggerMessage(Level = LogLevel.Error, Message = "Request invalid response type for server {endpointName} with method {method}")]
    internal static partial void RequestInvalidResponseType(this ILogger logger, string endpointName, string method);

    [LoggerMessage(Level = LogLevel.Error, Message = "Request params type conversion error for server {endpointName} with method {method}: expected {expectedType}")]
    internal static partial void RequestParamsTypeConversionError(this ILogger logger, string endpointName, string method, Type expectedType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cleaning up client for server {endpointName}")]
    internal static partial void CleaningUpClient(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Client cleaned up for server {endpointName}")]
    internal static partial void ClientCleanedUp(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport for server {endpointName} already connected")]
    internal static partial void TransportAlreadyConnected(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport for server {endpointName} connecting")]
    internal static partial void TransportConnecting(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating process for transport for server {endpointName} with command {command}, arguments {arguments}, environment {environment}, working directory {workingDirectory}, shutdown timeout {shutdownTimeout}")]
    internal static partial void CreateProcessForTransport(this ILogger logger, string endpointName, string command, string arguments, string environment, string workingDirectory, string shutdownTimeout);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport for server {endpointName} error: {data}")]
    internal static partial void TransportError(this ILogger logger, string endpointName, string data);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport process start failed for server {endpointName}")]
    internal static partial void TransportProcessStartFailed(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport process started for server {endpointName} with PID {processId}")]
    internal static partial void TransportProcessStarted(this ILogger logger, string endpointName, int processId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport reading messages for server {endpointName}")]
    internal static partial void TransportReadingMessages(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport connect failed for server {endpointName}")]
    internal static partial void TransportConnectFailed(this ILogger logger, string endpointName, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport not connected for server {endpointName}")]
    internal static partial void TransportNotConnected(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport sending message for server {endpointName} with ID {messageId}, JSON {json}")]
    internal static partial void TransportSendingMessage(this ILogger logger, string endpointName, string messageId, string json);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport message sent for server {endpointName} with ID {messageId}")]
    internal static partial void TransportSentMessage(this ILogger logger, string endpointName, string messageId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport send failed for server {endpointName} with ID {messageId}")]
    internal static partial void TransportSendFailed(this ILogger logger, string endpointName, string messageId, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport entering read messages loop for server {endpointName}")]
    internal static partial void TransportEnteringReadMessagesLoop(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport waiting for message for server {endpointName}")]
    internal static partial void TransportWaitingForMessage(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport end of stream for server {endpointName}")]
    internal static partial void TransportEndOfStream(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport received message for server {endpointName}: {line}")]
    internal static partial void TransportReceivedMessage(this ILogger logger, string endpointName, string line);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport received message parsed for server {endpointName}: {messageId}")]
    internal static partial void TransportReceivedMessageParsed(this ILogger logger, string endpointName, string messageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport message written for server {endpointName} with ID {messageId}")]
    internal static partial void TransportMessageWritten(this ILogger logger, string endpointName, string messageId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport message parse failed due to unexpected message schema for server {endpointName}: {line}")]
    internal static partial void TransportMessageParseUnexpectedType(this ILogger logger, string endpointName, string line);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport message parse failed for server {endpointName}: {line}")]
    internal static partial void TransportMessageParseFailed(this ILogger logger, string endpointName, string line, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport exiting read messages loop for server {endpointName}")]
    internal static partial void TransportExitingReadMessagesLoop(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport read messages cancelled for server {endpointName}")]
    internal static partial void TransportReadMessagesCancelled(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport read messages failed for server {endpointName}")]
    internal static partial void TransportReadMessagesFailed(this ILogger logger, string endpointName, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport cleaning up for server {endpointName}")]
    internal static partial void TransportCleaningUp(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Transport closing stdin for server {endpointName}")]
    internal static partial void TransportClosingStdin(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Transport waiting for shutdown for server {endpointName}")]
    internal static partial void TransportWaitingForShutdown(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Transport killing process for server {endpointName}")]
    internal static partial void TransportKillingProcess(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport shutdown failed for server {endpointName}")]
    internal static partial void TransportShutdownFailed(this ILogger logger, string endpointName, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Transport waiting for read task for server {endpointName}")]
    internal static partial void TransportWaitingForReadTask(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Transport cleanup read task timeout for server {endpointName}")]
    internal static partial void TransportCleanupReadTaskTimeout(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport cleanup read task cancelled for server {endpointName}")]
    internal static partial void TransportCleanupReadTaskCancelled(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Transport cleanup read task failed for server {endpointName}")]
    internal static partial void TransportCleanupReadTaskFailed(this ILogger logger, string endpointName, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport read task cleaned up for server {endpointName}")]
    internal static partial void TransportReadTaskCleanedUp(this ILogger logger, string endpointName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transport cleaned up for server {endpointName}")]
    internal static partial void TransportCleanedUp(this ILogger logger, string endpointName);

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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending notification payload for {endpointName}: {payload}")]
    internal static partial void SendingNotificationPayload(this ILogger logger, string endpointName, string payload);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sending notification for {endpointName}: {method}")]
    internal static partial void SendingNotification(this ILogger logger, string endpointName, string method);

    [LoggerMessage(
        EventId = 7000,
        Level = LogLevel.Error,
        Message = "Transport connection error for server {endpointName}"
    )]
    public static partial void TransportConnectionError(
        this ILogger logger,
        string endpointName,
        Exception exception);

    [LoggerMessage(
        EventId = 7001,
        Level = LogLevel.Warning,
        Message = "Transport message received before connected for server {endpointName}: {data}"
    )]
    public static partial void TransportMessageReceivedBeforeConnected(
        this ILogger logger,
        string endpointName,
        string data);

    [LoggerMessage(
        EventId = 7002,
        Level = LogLevel.Error,
        Message = "Transport endpoint event received out of order for server {endpointName}: {data}"
    )]
    public static partial void TransportEndpointEventInvalid(
        this ILogger logger,
        string endpointName,
        string data);

    [LoggerMessage(
        EventId = 7003,
        Level = LogLevel.Error,
        Message = "Transport event parse failed for server {endpointName}: {data}"
    )]
    public static partial void TransportEndpointEventParseFailed(
        this ILogger logger,
        string endpointName,
        string data,
        Exception exception);

    [LoggerMessage(
        EventId = 7004,
        Level = LogLevel.Trace,
        Message = "Invalid completion reference {reference} for server {endpointName}: {validationMessage}"
    )]
    public static partial void InvalidCompletionReference(
        this ILogger logger,
        string endpointName,
        string reference,
        string validationMessage);

    [LoggerMessage(
        EventId = 7005,
        Level = LogLevel.Trace,
        Message = "Invalid completion argument name {argumentName} for server {endpointName}"
    )]
    public static partial void InvalidCompletionArgumentName(
        this ILogger logger,
        string endpointName,
        string argumentName);

    [LoggerMessage(
        EventId = 7006,
        Level = LogLevel.Trace,
        Message = "Invalid completion argument value {argumentValue} for server {endpointName}"
    )]
    public static partial void InvalidCompletionArgumentValue(
        this ILogger logger,
        string endpointName,
        string argumentValue);

    [LoggerMessage(
        EventId = 7007,
        Level = LogLevel.Debug,
        Message = "Getting completion for {endpointName} with reference {reference}, argument name {argumentName}, argument value {argumentValue}"
    )]
    public static partial void GettingCompletion(
        this ILogger logger,
        string endpointName,
        string reference,
        string argumentName,
        string argumentValue);

    [LoggerMessage(
        EventId = 7008,
        Level = LogLevel.Error,
        Message = "Message handler error for {endpointName} with message type {messageType}, payload {payload}"
    )]
    public static partial void MessageHandlerError(
        this ILogger logger,
        string endpointName,
        string messageType,
        string payload,
        Exception exception);

    [LoggerMessage(
        EventId = 7009,
        Level = LogLevel.Trace,
        Message = "Writing message to channel: {message}"
    )]
    public static partial void TransportWritingMessageToChannel(
        this ILogger logger,
        IJsonRpcMessage message);

    [LoggerMessage(
        EventId = 7010,
        Level = LogLevel.Trace,
        Message = "Message written to channel"
    )]
    public static partial void TransportMessageWrittenToChannel(this ILogger logger);

    [LoggerMessage(
        EventId = 7011,
        Level = LogLevel.Trace,
        Message = "Message read from channel for {endpointName} with type {messageType}"
    )]
    public static partial void TransportMessageRead(
        this ILogger logger,
        string endpointName,
        string messageType);

    [LoggerMessage(
        EventId = 7012,
        Level = LogLevel.Warning,
        Message = "No handler found for request {method} for server {endpointName}"
    )]
    public static partial void NoHandlerFoundForRequest(
        this ILogger logger,
        string endpointName,
        string method);

    [LoggerMessage(
        EventId = 7013,
        Level = LogLevel.Trace,
        Message = "Response matched pending request for {endpointName} with ID {messageId}"
    )]
    public static partial void ResponseMatchedPendingRequest(
        this ILogger logger,
        string endpointName,
        string messageId);

    [LoggerMessage(
        EventId = 7014,
        Level = LogLevel.Warning,
        Message = "Endpoint handler received unexpected message type for {endpointName}: {messageType}"
    )]
    public static partial void EndpointHandlerUnexpectedMessageType(
        this ILogger logger,
        string endpointName,
        string messageType);

    [LoggerMessage(
        EventId = 7015,
        Level = LogLevel.Debug,
        Message = "Request sent for {endpointName} with method {method}, ID {id}. Waiting for response."
    )]
    public static partial void RequestSentAwaitingResponse(
        this ILogger logger,
        string endpointName,
        string method,
        string id);
}