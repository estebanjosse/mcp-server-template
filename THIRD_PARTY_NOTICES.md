# Third-Party Notices

The template's own source is licensed under MIT (see `LICENSE`). The NuGet template package ships scaffold source and documentation, not third-party runtime assemblies. Generated applications restore their dependencies separately.

## Model Context Protocol C# SDK 2.4.0

The scaffold references `ModelContextProtocol` and, for HTTP, `ModelContextProtocol.AspNetCore`. Both resolve `ModelContextProtocol.Core` transitively. NuGet metadata declares Apache-2.0 for [ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol/2.4.0), [ModelContextProtocol.AspNetCore](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore/2.4.0) and [ModelContextProtocol.Core](https://www.nuget.org/packages/ModelContextProtocol.Core/2.4.0). The [upstream license at v2.4.0](https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.4.0/LICENSE) also documents historical contributions that remain under MIT and separate terms for upstream documentation.

## Redistributing generated applications

This notice is not a complete dependency inventory for a published application. Inspect the resolved dependency graph for the hosts you ship, including transitives, and preserve the applicable license texts, copyright notices and any required attribution when distributing binaries, containers or archives. Check the actual distribution; carrying this scaffold notice alone does not verify its contents.
