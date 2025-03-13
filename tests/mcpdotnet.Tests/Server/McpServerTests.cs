using McpDotNet.Client;
using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Transport;
using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using McpDotNet.Tests.Utils;
using Microsoft.Extensions.Logging;
using Moq;

namespace McpDotNet.Tests.Server;

public class McpServerTests
{
    private readonly Mock<IServerTransport> _serverTransport;
    private readonly Mock<ILoggerFactory> _loggerFactory;
    private readonly Mock<ILogger> _logger;
    private readonly McpServerOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public McpServerTests()
    {
        _serverTransport = new Mock<IServerTransport>();
        _loggerFactory = new Mock<ILoggerFactory>();
        _logger = new Mock<ILogger>();
        _loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
        _options = CreateOptions();
        _serviceProvider = new Mock<IServiceProvider>().Object;
    }

    private static McpServerOptions CreateOptions(ServerCapabilities? capabilities = null)
    {
        return new McpServerOptions
        {
            ServerInfo = new Implementation { Name = "TestServer", Version = "1.0" },
            ProtocolVersion = "2024",
            InitializationTimeout = TimeSpan.FromSeconds(30),
            Capabilities = capabilities
        };
    }

    [Fact]
    public async Task Constructor_Should_Initialize_With_Valid_Parameters()
    {
        // Arrange & Act
        await using var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, _serviceProvider);

        // Assert
        Assert.NotNull(server);
    }

    [Fact]
    public void Constructor_Throws_For_Null_Transport()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new McpServer(null!, _options, _loggerFactory.Object, _serviceProvider));
    }

    [Fact]
    public void Constructor_Throws_For_Null_Options()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new McpServer(_serverTransport.Object, null!, _loggerFactory.Object, _serviceProvider));
    }

    [Fact]
    public void Constructor_Throws_For_Null_LoggerFactory()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new McpServer(_serverTransport.Object, _options, null!, _serviceProvider));
    }

    [Fact]
    public async Task Constructor_Does_Not_Throw_For_Null_ServiceProvider()
    {
        // Arrange & Act
        await using var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, null);

        // Assert
        Assert.NotNull(server);
    }

    [Fact]
    public async Task Property_EndpointName_Return_Infos()
    {
        await using var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, _serviceProvider);
        server.ClientInfo = new Implementation { Name = "TestClient", Version = "1.1" };
        Assert.Equal("Server (TestServer 1.0), Client (TestClient 1.1)", server.EndpointName);
    }

    [Fact]
    public async Task StartAsync_Should_Throw_InvalidOperationException_If_Already_Initializing()
    {
        // Arrange
        await using var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, _serviceProvider);
        server.GetType().GetField("_isInitializing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(server, true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => server.StartAsync());
        Assert.Equal("Server is already initializing", exception.Message);
    }

    [Fact]
    public async Task StartAsync_Should_Do_Nothing_If_Already_Initialized()
    {
        // Arrange
        await using var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, _serviceProvider);
        server.IsInitialized = true;

        await server.StartAsync();

        // Assert
        _serverTransport.Verify(t => t.StartListeningAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_ShouldStartListening()
    {
        // Arrange
        await using var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, _serviceProvider);

        // Act
        await server.StartAsync();

        // Assert
        _serverTransport.Verify(t => t.StartListeningAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_Sets_Initialized_After_Transport_Responses_Initialized_Notification()
    {
        await using var transport = new TestServerTransport();
        await using var server = new McpServer(transport, _options, _loggerFactory.Object, _serviceProvider);

        await server.StartAsync();

        // Send initialized notification
        await transport.SendMessageAsync(
            new JsonRpcNotification
            {
                Method = "notifications/initialized"
            }
        );

        await Task.Delay(50);

        Assert.True(server.IsInitialized);
    }

    [Fact]
    public async Task RequestSamplingAsync_Should_Throw_McpServerException_If_Client_Does_Not_Support_Sampling()
    {
        // Arrange
        await using var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, _serviceProvider);
        server.ClientCapabilities = new ClientCapabilities();

        var action = () => server.RequestSamplingAsync(new CreateMessageRequestParams { Messages = [] }, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<McpServerException>(action);
        Assert.Equal("Client does not support sampling", exception.Message);
    }

    [Fact]
    public async Task RequestSamplingAsync_Should_SendRequest()
    {
        // Arrange
        await using var transport = new TestServerTransport();
        await using var server = new McpServer(transport, _options, _loggerFactory.Object, _serviceProvider);
        server.ClientCapabilities = new ClientCapabilities { Sampling = new SamplingCapability() };

        await server.StartAsync();

        // Act
        var result = await server.RequestSamplingAsync(new CreateMessageRequestParams { Messages = [] }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(transport.SentMessages);
        Assert.IsType<JsonRpcRequest>(transport.SentMessages[0]);
        Assert.Equal("sampling/createMessage", ((JsonRpcRequest)transport.SentMessages[0]).Method);
    }

    [Fact]
    public async Task RequestRootsAsync_Should_Throw_McpServerException_If_Client_Does_Not_Support_Roots()
    {
        // Arrange
        await using var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, _serviceProvider);
        server.ClientCapabilities = new ClientCapabilities();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<McpServerException>(() => server.RequestRootsAsync(new ListRootsRequestParams(), CancellationToken.None));
        Assert.Equal("Client does not support roots", exception.Message);
    }

    [Fact]
    public async Task RequestRootsAsync_Should_SendRequest()
    {
        // Arrange
        await using var transport = new TestServerTransport();
        await using var server = new McpServer(transport, _options, _loggerFactory.Object, _serviceProvider);
        server.ClientCapabilities = new ClientCapabilities { Roots = new RootsCapability() };
        await server.StartAsync();

        // Act
        var result = await server.RequestRootsAsync(new ListRootsRequestParams(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(transport.SentMessages);
        Assert.IsType<JsonRpcRequest>(transport.SentMessages[0]);
        Assert.Equal("roots/list", ((JsonRpcRequest)transport.SentMessages[0]).Method);
    }

    [Fact]
    public async Task Throws_Exception_If_Not_Connected()
    {
        await using var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, _serviceProvider);
        server.ClientCapabilities = new ClientCapabilities { Roots = new RootsCapability() };
        _serverTransport.SetupGet(t => t.IsConnected).Returns(false);

        var action = async () => await server.RequestRootsAsync(new ListRootsRequestParams(), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<McpClientException>(action);
        Assert.Equivalent("Transport is not connected", exception.Message);
    }

    [Fact]
    public async Task Can_Handle_Ping_Requests()
    {
        await Can_Handle_Requests(null, "ping",
          configureServer: server => { },
          assertResult: response =>
          {
              Assert.IsType<PingResult>(response);
          });
    }

    [Fact]
    public async Task Can_Handle_Initialize_Requests()
    {
        await Can_Handle_Requests(null, "initialize",
           configureServer: server => { },
           assertResult: response =>
           {
               Assert.IsType<InitializeResult>(response);

               var result = (InitializeResult)response;
               Assert.Equal("TestServer", result.ServerInfo.Name);
               Assert.Equal("1.0", result.ServerInfo.Version);
               Assert.Equal("2024", result.ProtocolVersion);
           });
    }

    [Fact]
    public async Task Can_Handle_Completion_Requests()
    {
        await Can_Handle_Requests(null, "completion/complete",
        configureServer: server => { },
        assertResult: response =>
        {
            Assert.IsType<CompleteResult>(response);

            var result = (CompleteResult)response;
            Assert.NotNull(result.Completion);
            Assert.Empty(result.Completion.Values);
            Assert.Equal(0, result.Completion.Total);
            Assert.False(result.Completion.HasMore);
        });
    }

    [Fact]
    public async Task Can_Handle_Completion_Requests_With_Handler()
    {
        await Can_Handle_Requests(null, "completion/complete",
          configureServer: server =>
          {
              server.SetGetCompletionHandler((request, ct) =>
              {
                  return Task.FromResult(new CompleteResult
                  {
                      Completion = new()
                      {
                          Values = ["test"],
                          Total = 2,
                          HasMore = true
                      }
                  });
              });
          },
         assertResult: response =>
         {
             Assert.IsType<CompleteResult>(response);

             var result = (CompleteResult)response;
             Assert.NotNull(result.Completion);
             Assert.NotEmpty(result.Completion.Values);
             Assert.Equal("test", result.Completion.Values[0]);
             Assert.Equal(2, result.Completion.Total);
             Assert.True(result.Completion.HasMore);
         });
    }

    [Fact]
    public async Task Can_Handle_Resources_List_Requests()
    {
        await Can_Handle_Requests(new ServerCapabilities { Resources = new() }, "resources/list",
          configureServer: server =>
          {
              server.SetListResourcesHandler((request, ct) =>
              {
                  return Task.FromResult(new ListResourcesResult
                  {
                      Resources = [new() { Uri = "test", Name = "Test Resource" }]
                  });
              });

          },
         assertResult: response =>
         {
             Assert.IsType<ListResourcesResult>(response);

             var result = (ListResourcesResult)response;
             Assert.NotNull(result.Resources);
             Assert.NotEmpty(result.Resources);
             Assert.Equal("test", result.Resources[0].Uri);
         });
    }

    [Fact]
    public async Task Can_Handle_Resources_List_Requests_Throws_Exception_If_No_Handler_Assigned()
    {
        await Throws_Exception_If_No_Handler_Assigned(new ServerCapabilities { Resources = new() }, "resources/list", "ListResources handler not configured");
    }

    [Fact]
    public async Task Can_Handle_ResourcesRead_Requests()
    {
        await Can_Handle_Requests(new ServerCapabilities { Resources = new() }, "resources/read",
           configureServer: server =>
           {
               server.SetReadResourceHandler((request, ct) =>
               {
                   return Task.FromResult(new ReadResourceResult
                   {
                       Contents = [new() { Text = "test" }]
                   });
               });
           },
          assertResult: response =>
          {
              Assert.IsType<ReadResourceResult>(response);

              var result = (ReadResourceResult)response;
              Assert.NotNull(result.Contents);
              Assert.NotEmpty(result.Contents);
              Assert.Equal("test", result.Contents[0].Text);
          });
    }

    [Fact]
    public async Task Can_Handle_Resources_Read_Requests_Throws_Exception_If_No_Handler_Assigned()
    {
        await Throws_Exception_If_No_Handler_Assigned(new ServerCapabilities { Resources = new() }, "resources/read", "ReadResource handler not configured");
    }

    [Fact]
    public async Task Can_Handle_List_Prompts_Requests()
    {
        await Can_Handle_Requests(new ServerCapabilities { Prompts = new() }, "prompts/list",
            configureServer: server =>
            {
                server.SetListPromptsHandler((request, ct) =>
                {
                    return Task.FromResult(new ListPromptsResult
                    {
                        Prompts = [new() { Name = "test" }]
                    });
                });
            },
           assertResult: response =>
           {
               Assert.IsType<ListPromptsResult>(response);

               var result = (ListPromptsResult)response;
               Assert.NotNull(result.Prompts);
               Assert.NotEmpty(result.Prompts);
               Assert.Equal("test", result.Prompts[0].Name);
           });
    }

    [Fact]
    public async Task Can_Handle_List_Prompts_Requests_Throws_Exception_If_No_Handler_Assigned()
    {
        await Throws_Exception_If_No_Handler_Assigned(new ServerCapabilities { Prompts = new() }, "prompts/list", "ListPrompts handler not configured");
    }

    [Fact]
    public async Task Can_Handle_Get_Prompts_Requests()
    {
        await Can_Handle_Requests(new ServerCapabilities { Prompts = new() }, "prompts/get",
            configureServer: server =>
            {
                server.SetGetPromptHandler((request, ct) =>
                {
                    return Task.FromResult(new GetPromptResult
                    {
                        Description = "test"
                    });
                });
            },
           assertResult: response =>
           {
               Assert.IsType<GetPromptResult>(response);

               var result = (GetPromptResult)response;
               Assert.Equal("test", result.Description);
           });
    }

    [Fact]
    public async Task Can_Handle_Get_Prompts_Requests_Throws_Exception_If_No_Handler_Assigned()
    {
        await Throws_Exception_If_No_Handler_Assigned(new ServerCapabilities { Prompts = new() }, "prompts/get", "GetPrompt handler not configured");
    }

    [Fact]
    public async Task Can_Handle_List_Tools_Requests()
    {
        await Can_Handle_Requests(new ServerCapabilities { Tools = new() }, "tools/list",
            configureServer: server =>
            {
                server.SetListToolsHandler((request, ct) =>
                {
                    return Task.FromResult(new ListToolsResult
                    {
                        Tools = [new() { Name = "test" }]
                    });
                });
            },
           assertResult: response =>
           {
               Assert.IsType<ListToolsResult>(response);

               var result = (ListToolsResult)response;
               Assert.NotEmpty(result.Tools);
               Assert.Equal("test", result.Tools[0].Name);
           });
    }

    [Fact]
    public async Task Can_Handle_List_Tools_Requests_Throws_Exception_If_No_Handler_Assigned()
    {
        await Throws_Exception_If_No_Handler_Assigned(new ServerCapabilities { Tools = new() }, "tools/list", "ListTools handler not configured");
    }

    [Fact]
    public async Task Can_Handle_Call_Tool_Requests()
    {
        await Can_Handle_Requests(new ServerCapabilities { Tools = new() }, "tools/call",
            configureServer: server =>
            {
                server.SetCallToolHandler((request, ct) =>
                {
                    return Task.FromResult(new CallToolResponse
                    {
                        Content = [new Content { Text = "test" }]
                    });
                });
            },
           assertResult: response =>
           {
               Assert.IsType<CallToolResponse>(response);

               var result = (CallToolResponse)response;
               Assert.NotEmpty(result.Content);
               Assert.Equal("test", result.Content[0].Text);
           });
    }

    [Fact]
    public async Task Can_Handle_Call_Tool_Requests_Throws_Exception_If_No_Handler_Assigned()
    {
        await Throws_Exception_If_No_Handler_Assigned(new ServerCapabilities { Tools = new() }, "tools/call", "CallTool handler not configured");
    }

    private async Task Can_Handle_Requests(ServerCapabilities? serverCapabilities, string method, Action<IMcpServer> configureServer, Action<object> assertResult)
    {
        await using var transport = new TestServerTransport();
        var options = serverCapabilities == null ? _options : CreateOptions(serverCapabilities);

        await using var server = new McpServer(transport, options, _loggerFactory.Object, _serviceProvider);

        await server.StartAsync();

        configureServer(server);

        var receivedMessage = new TaskCompletionSource<JsonRpcResponse>();

        transport.OnMessageSent = (message) =>
        {
            if (message is JsonRpcResponse response && response.Id.AsNumber == 55)
                receivedMessage.SetResult(response);
        };

        await transport.SendMessageAsync(
        new JsonRpcRequest
        {
            Method = method,
            Id = RequestId.FromNumber(55)
        }
        );

        var response = await receivedMessage.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.NotNull(response);
        Assert.NotNull(response.Result);

        assertResult(response.Result);
    }

    private async Task Throws_Exception_If_No_Handler_Assigned(ServerCapabilities serverCapabilities, string method, string expectedError)
    {
        await using var transport = new TestServerTransport();
        var options = CreateOptions(serverCapabilities);

        await using var server = new McpServer(transport, options, _loggerFactory.Object, _serviceProvider);

        await server.StartAsync();

        var receivedMessage = new TaskCompletionSource<IJsonRpcMessage>();

        transport.OnMessageSent = (message) =>
        {
            if (message is JsonRpcError response && response.Id.AsNumber == 55)
                receivedMessage.SetResult(response);
        };

        await transport.SendMessageAsync(
            new JsonRpcRequest
            {
                Method = method,
                Id = RequestId.FromNumber(55)
            }
        );

        var response = await receivedMessage.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.NotNull(response);
        Assert.IsType<JsonRpcError>(response);

        var result = (JsonRpcError)response;
        Assert.NotNull(result.Error);
        Assert.Equal(expectedError, result.Error.Message);
    }
}
