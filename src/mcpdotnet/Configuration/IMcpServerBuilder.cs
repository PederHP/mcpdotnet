﻿using McpDotNet.Server;
using Microsoft.Extensions.DependencyInjection;

namespace McpDotNet.Configuration;

/// <summary>
/// Builder for configuring <see cref="IMcpServer"/> instances.
/// </summary>
public interface IMcpServerBuilder
{
    /// <summary>
    /// Gets the service collection.
    /// </summary>
    IServiceCollection Services { get; }
}
