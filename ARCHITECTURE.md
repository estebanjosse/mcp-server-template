# Architecture Documentation

## Clean Architecture with SDK Isolation

This MCP server demonstrates clean architecture principles with strict separation of concerns and complete SDK isolation.

## Layer Responsibilities

### 1. McpServer.Abstractions (Public API - Stable)
**Purpose**: Define stable contracts that never change  
**Dependencies**: None (zero external dependencies)  
**Contents**:
- `ITool` - Tool definition and execution contract
- `IPrompt` - Prompt template contract
- `IResource` - Resource access contract
- `IMcpServer` - Server lifecycle contract

**Key Principle**: This layer is the contract between your application and the MCP implementation. It should NEVER reference the SDK.

### 2. McpServer.Implementation.ModelContextProtocol (SDK Adapter - Hidden)
**Purpose**: Wrap the MCP SDK without exposing its types  
**Dependencies**: McpServer.Abstractions, mcpdotnet SDK  
**Contents**:
- `ModelContextProtocolServerAdapter` - Implements `IMcpServer` by wrapping SDK
- `McpServerOptions` - Configuration with validation
- `ServiceCollectionExtensions` - DI registration

**Key Principle**: This is the ONLY layer that knows about the SDK. It translates between our abstractions and the SDK.

**Adapter Pattern**:
```
Application Code → ITool → ModelContextProtocolServerAdapter → mcpdotnet SDK → MCP Protocol
```

### 3. McpServer.Examples (Concrete Implementations)
**Purpose**: Demonstrate how to implement tools, prompts, and resources  
**Dependencies**: McpServer.Abstractions only  
**Contents**:
- Example tools (echo, calculator)
- Example prompts (greeting, code-review)
- Example resources (welcome, server-status)

**Key Principle**: These implementations only know about the abstractions, never the SDK.

### 4. McpServer.Host (Application Entry Point)
**Purpose**: Wire everything together with dependency injection  
**Dependencies**: All other projects  
**Contents**:
- Program.cs with DI configuration
- appsettings.json for configuration

**Key Principle**: This is where the concrete types are resolved. The application code only depends on abstractions.

## Dependency Flow

```
┌─────────────────────────────────────────────────────────┐
│                    McpServer.Host                       │
│                 (Composition Root)                      │
└──────────────────────┬──────────────────────────────────┘
                       │
       ┌───────────────┼───────────────┐
       │               │               │
       ▼               ▼               ▼
┌─────────────┐ ┌─────────────┐ ┌──────────────────────────────────┐
│  Examples   │ │ Abstractions│ │  Implementation.                 │
│             │ │             │ │  ModelContextProtocol            │
│             │ │             │ │                                  │
│ Depends on  │ │ No deps     │ │ Depends on Abstractions + SDK    │
│ Abstractions│ │             │ │                                  │
└─────────────┘ └─────────────┘ └──────────────────────────────────┘
       │                                    │
       └────────────────┬───────────────────┘
                        │
                        ▼
                ┌──────────────┐
                │ mcpdotnet SDK│
                │ (Hidden)     │
                └──────────────┘
```

## SDK Replaceability

To replace the MCP SDK:

1. Create new project: `McpServer.Implementation.AlternativeSDK`
2. Reference `McpServer.Abstractions` (not the old implementation)
3. Implement `IMcpServer` by wrapping the new SDK
4. Update `McpServer.Host` to use the new implementation
5. **Zero changes needed in Examples or Abstractions**

## Key Architecture Principles Applied

### 1. Dependency Inversion Principle (DIP)
- High-level modules (Examples) don't depend on low-level modules (SDK)
- Both depend on abstractions
- SDK is an implementation detail, easily swappable

### 2. Single Responsibility Principle (SRP)
- **Abstractions**: Define contracts
- **Implementation**: Adapt SDK
- **Examples**: Provide concrete implementations
- **Host**: Compose dependencies

### 3. Open/Closed Principle (OCP)
- Open for extension: Add new tools/prompts/resources by implementing interfaces
- Closed for modification: SDK changes don't affect application code

### 4. Interface Segregation Principle (ISP)
- Small, focused interfaces (ITool, IPrompt, IResource)
- Clients only depend on what they use

### 5. Liskov Substitution Principle (LSP)
- Any implementation of ITool can be used wherever ITool is expected
- SDK implementation can be swapped without breaking contracts

## Transport Layer

The implementation layer handles transport via the SDK:

- **STDIO**: JSON-RPC 2.0 over standard input/output
- **HTTP/SSE**: Server-Sent Events for streaming MCP messages

Transport is configured via `McpServerOptions` and handled entirely by the SDK adapter.

## Benefits

1. **Testability**: Mock `ITool`, `IPrompt`, `IResource` in tests
2. **Maintainability**: Clear boundaries, single responsibility
3. **Flexibility**: Swap SDK without touching application code
4. **Type Safety**: Compile-time guarantees via interfaces
5. **Documentation**: Code structure documents architecture
6. **Future-proof**: SDK evolution doesn't affect abstractions

## Anti-Patterns Avoided

❌ **Exposing SDK types in public API**  
✅ SDK types stay in Implementation layer

❌ **Direct SDK dependency in application code**  
✅ Application depends on abstractions

❌ **Mixing concerns (transport + business logic)**  
✅ Clear separation via layers

❌ **Tight coupling to specific SDK**  
✅ Loose coupling via adapter pattern

## Example: Adding a New SDK

```csharp
// New implementation project
namespace McpServer.Implementation.AnotherSDK;

public class AnotherSdkAdapter : IMcpServer
{
    public Task RunAsync(CancellationToken ct)
    {
        // Wrap different SDK here
        var server = AnotherSDK.CreateServer(...);
        // Register tools/prompts/resources
        return server.RunAsync(ct);
    }
}
```

Change in Host:
```csharp
// Only this line changes:
services.AddSingleton<IMcpServer, AnotherSdkAdapter>();
```

**No changes needed**: Abstractions, Examples remain unchanged.
