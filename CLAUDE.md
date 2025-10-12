# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

OpenGptChat is a WPF-based chat client application that communicates with OpenAI's API. It's built using .NET 8.0 with WPF and follows the MVVM (Model-View-ViewModel) architecture pattern using CommunityToolkit.Mvvm.

## Common Development Commands

### Build and Run
```bash
# Build the solution
dotnet build OpenGptChat.sln

# Run the application
dotnet run --project OpenGptChat/OpenGptChat.csproj

# Build for release
dotnet build OpenGptChat.sln -c Release
```

### Development Setup
```bash
# Restore dependencies
dotnet restore

# Clean build artifacts
dotnet clean

# Build and run in debug mode
dotnet run --project OpenGptChat/OpenGptChat.csproj -c Debug
```

## Architecture Overview

### Core Architecture Pattern
- **MVVM with CommunityToolkit.Mvvm**: Uses observable properties and relay commands
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection with hosted services
- **Event-driven UI**: WPF with data binding and commands
- **Local Storage**: LiteDB for NoSQL document storage

### Key Architectural Components

#### Application Bootstrap (`App.xaml.cs`)
- Configures dependency injection container
- Sets up configuration from JSON files and environment variables
- Implements singleton application pattern using EventWaitHandle
- Registers all services and view models

#### Service Layer
- **ApplicationHostService**: Main hosted service that initializes storage, markdown renderer, and UI
- **ChatService**: Handles OpenAI API communication with streaming support
- **ChatStorageService**: LiteDB operations for sessions and messages persistence
- **ConfigurationService**: Manages app configuration with hot reloading
- **ChatPageService**: Manages chat page instances and navigation

#### Data Models
- **ChatMessage**: Individual message with role (user/assistant/system), content, and timestamp
- **ChatSession**: Chat session with system messages and context settings
- **ChatDialogue**: Pairs user message with AI response as a conversation unit
- **AppConfig**: Application configuration including API settings and UI preferences

#### ViewModels
- **ChatMessageModel**: Wraps ChatMessage with editing capabilities and UI properties
- **ChatSessionModel**: Wraps ChatSession with editing commands and storage sync
- **ChatPageModel**: Manages individual chat page state and message flow
- **MainPageModel**: Handles main application navigation and session management

### Data Flow Architecture

#### Message Creation and Storage Flow
1. **User Input** → `ChatPageModel.SendMessageCommand`
2. **Message Creation** → `ChatMessage.Create()` factory method
3. **API Communication** → `ChatService.ChatCoreAsync()` with streaming
4. **Real-time Updates** → Message handler callback updates UI progressively
5. **Persistence** → `ChatStorageService.SaveMessage()` with upsert pattern
6. **Auto-sync** → ViewModels automatically sync changes to storage via `OnPropertyChanged`

#### Storage Architecture
- **Database**: LiteDB NoSQL database stored as `AppChatStorage.db`
- **Collections**:
  - `ChatSessions`: Session metadata and configuration
  - `ChatMessages`: Individual messages with session foreign key
- **Relationships**: One-to-many (Session → Messages)
- **Query Patterns**: Time-based queries for message history and pagination

### Key Technical Patterns

#### MVVM Implementation
- Uses `[ObservableProperty]` attribute for automatic property change notification
- `[RelayCommand]` for command generation
- `[NotifyPropertyChangedFor]` for dependent property updates
- Storage synchronization through `OnPropertyChanged` overrides

#### Streaming Chat Implementation
- OpenAI streaming API with real-time content accumulation
- Timeout detection with configurable API timeout
- Cancellation token support for request cancellation
- Progressive UI updates during streaming

#### Configuration Management
- JSON-based configuration with hot reloading
- Environment variable override support
- Automatic configuration file creation on first run
- Service registration with `IOptions<AppConfig>` pattern

## Project Structure

```
OpenGptChat/
├── Models/           # Data models (ChatMessage, ChatSession, AppConfig)
├── Services/         # Business logic and external service integration
├── ViewModels/       # MVVM view models with observable properties
├── Views/           # WPF views (pages, dialogs, windows)
├── Controls/        # Custom WPF controls (ChatBubble, MarkdownViewer)
├── Utilities/       # Helper classes and extensions
├── Behaviors/       # WPF behaviors for UI interactions
├── Converters/      # Value converters for data binding
├── Markdown/        # Markdown rendering and syntax highlighting
└── Assets/          # Images and resources
```

## Important Implementation Notes

### Database Schema
- **ChatSessions**: Id (PK), Name, SystemMessages[], EnableChatContext
- **ChatMessages**: Id (PK), SessionId (FK), Role, Content, Timestamp
- All operations use upsert pattern (update if exists, insert if not)

### Message Synchronization
- ViewModels automatically persist changes to storage when properties change
- Uses record types with `with` expressions for immutable updates
- Storage service handles both create and update operations seamlessly

### API Client Management
- OpenAI client is recreated when configuration changes
- Supports custom API hosts and organization settings
- Implements proper timeout handling and cancellation

### Application Lifecycle
- Singleton enforcement using named EventWaitHandle
- Proper resource disposal through IDisposable implementation
- Hosted service pattern for startup and shutdown coordination

### Configuration Hot Reloading
- Configuration changes automatically trigger client recreation
- UI responds to configuration changes through data binding
- Supports both file-based and environment variable configuration

## Development Guidelines

### When Adding New Features
1. Follow MVVM pattern with proper separation of concerns
2. Use dependency injection for service registration
3. Implement proper storage operations through ChatStorageService
4. Add appropriate error handling and logging
5. Follow the existing naming conventions and code style

### Database Operations
- Always use ChatStorageService for data operations
- Follow the upsert pattern for save operations
- Use proper query patterns for time-based data retrieval
- Handle database initialization through ApplicationHostService

### UI Development
- Use data binding extensively with observable properties
- Implement proper command handling with RelayCommands
- Follow the existing control and styling patterns
- Ensure proper resource cleanup and disposal