# Aggregator - Product Price Parser

A .NET 8 console application that parses product prices from multiple websites and stores them in a PostgreSQL database. This project is fully containerized with Docker for easy deployment and development.

## 🚀 Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://docs.docker.com/get-docker/) and [Docker Compose](https://docs.docker.com/compose/install/)
- [Entity Framework CLI tools](https://docs.microsoft.com/en-us/ef/core/cli/dotnet)


## 🌍 Environment Configuration

The application supports multiple environments with different configuration settings. The environment is determined by the `ASPNETCORE_ENVIRONMENT` variable.

### Available Environments

| Environment | Description | Configuration File |
|-------------|-------------|-------------------|
| **Development** | Local development with debug logs | `appsettings.Development.json` |
| **Docker** | Containerized environment | `appsettings.Docker.json` |
| **Production** | Production environment (default) | `appsettings.json` |

### Setting Environment Variables

#### 🖥️ Windows

**Command Prompt:**
```cmd
# Set environment variable
set ASPNETCORE_ENVIRONMENT=Development

# Run application
dotnet run

# Or in one command
set ASPNETCORE_ENVIRONMENT=Development && dotnet run
```

**PowerShell:**
```powershell
# Set environment variable
$env:ASPNETCORE_ENVIRONMENT="Development"

# Run application
dotnet run

# Or in one command
$env:ASPNETCORE_ENVIRONMENT="Development"; dotnet run
```

**Permanent setting (Windows):**
```cmd
# Set permanently for current user
setx ASPNETCORE_ENVIRONMENT Development
```

#### 🐧 Linux/macOS (Unix)

**Bash/Zsh:**
```bash
# Set environment variable for current session
export ASPNETCORE_ENVIRONMENT=Development

# Run application
dotnet run

# Or in one command
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

**Permanent setting (Unix):**
```bash
# Add to ~/.bashrc or ~/.zshrc
echo 'export ASPNETCORE_ENVIRONMENT=Development' >> ~/.bashrc
source ~/.bashrc
```

### Configuration Loading Order

The application loads configuration files in this order (later files override earlier ones):

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific overrides)
3. Environment variables (highest priority)

**Example:**
```bash
# This will load: appsettings.json → appsettings.Development.json
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

## 💻 Development Setup

### Option 1: Local Development

```bash
# 1. Set development environment
export ASPNETCORE_ENVIRONMENT=Development  # Unix
# OR
set ASPNETCORE_ENVIRONMENT=Development     # Windows

# 2. Start PostgreSQL database
docker-compose up -d postgres

# 3. Install EF CLI tools (one time setup)
dotnet tool install --global dotnet-ef

# 4. Apply database migrations
dotnet ef database update

# 5. Run the application
dotnet run
```

### Option 2: Full Docker Environment

```bash
# Set Docker environment (optional, handled by docker-compose)
export ASPNETCORE_ENVIRONMENT=Docker

# Start database and run application with Docker
docker-compose up --build
```

## ⚙️ Configuration Details

### Development Environment Settings

```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Port=5432;...",
    "CommandTimeout": 60,
    "EnableSqlLogging": true
  },
  "HttpClient": {
    "TimeoutSeconds": 60,
    "RetryDelayMs": 2000
  },
  "Parsing": {
    "DelayBetweenRequestsMs": 2000,
    "EnableDetailedLogging": true
  }
}
```

### Docker Environment Settings

```json
{
  "Database": {
    "ConnectionString": "Host=postgres;Port=5432;...",
    "CommandTimeout": 30,
    "EnableSqlLogging": false
  },
  "HttpClient": {
    "TimeoutSeconds": 30,
    "RetryDelayMs": 1000
  },
  "Parsing": {
    "DelayBetweenRequestsMs": 1000,
    "EnableDetailedLogging": false
  }
}
```

## 🗄️ Database Management

### Migration Commands

```bash
# Create a new migration
dotnet ef migrations add <MigrationName>

# Apply migrations to database
dotnet ef database update

# List all migrations
dotnet ef migrations list

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script for migrations
dotnet ef migrations script
```

### Environment-Specific Database Connections

| Environment | Host | Port | Database | Notes |
|------------|------|------|----------|-------|
| Development | localhost | 5432 | aggregator | Local PostgreSQL |
| Docker | postgres | 5432 | aggregator | Container name as host |
| Production | [configured] | 5432 | aggregator | Production server |

## 🧪 Testing

The project includes comprehensive unit and integration tests using xUnit, Moq, FluentAssertions, and TestContainers.

### Test Project Structure

```
tests/Aggregator.Tests/
├── Unit/                              # Unit tests
│   └── ParserServices/
│       └── AskStudioParserTests.cs   # Parser unit tests
├── Integration/                       # Integration tests (future)
├── Fixtures/                         # Test fixtures and helpers
│   ├── DatabaseFixture.cs           # In-memory database setup
│   └── TestDataHelper.cs            # Test data utilities
├── Helpers/                          # Test helper classes
│   ├── ProductBuilder.cs            # Builder pattern for test objects
│   └── MockHttpClientFactory.cs     # HTTP client mocking
└── TestData/                        # Real HTML data for testing
    └── HtmlPages/askstudio/          # Downloaded HTML files
        ├── main_shop_page.html
        ├── shop_page_2.html
        └── category_aksessuary.html
```

### Running Tests

#### 📋 All Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test -v normal

# Run tests with coverage (if coverlet installed)
dotnet test --collect:"XPlat Code Coverage"
```

#### 🎯 Specific Test Categories

```bash
# Run only unit tests
dotnet test --filter "Category=Unit"

# Run tests for specific class
dotnet test --filter "AskStudioParserTests"

# Run specific test method
dotnet test --filter "ShopName_ShouldReturnCorrectValue"

# Run tests with pattern matching
dotnet test --filter "FullyQualifiedName~AskStudioParser"
```

#### 🏷️ Test Filtering Examples

```bash
# Run tests by namespace
dotnet test --filter "FullyQualifiedName~Unit.ParserServices"

# Run tests by multiple criteria (AND)
dotnet test --filter "TestCategory=Unit&FullyQualifiedName~Parser"

# Run tests by multiple criteria (OR)
dotnet test --filter "TestCategory=Unit|TestCategory=Integration"

# Exclude specific tests
dotnet test --filter "TestCategory!=Slow"
```

### Test Dependencies

The test project includes the following NuGet packages:

| Package | Purpose | Usage |
|---------|---------|-------|
| **xUnit** | Test framework | `[Fact]`, `[Theory]` attributes |
| **Moq** | Mocking framework | `Mock<T>` for dependencies |
| **FluentAssertions** | Better assertions | `.Should().Be()`, `.Should().NotBeNull()` |
| **EntityFrameworkCore.InMemory** | In-memory database | Unit testing with EF Core |
| **Testcontainers.PostgreSql** | Real database containers | Integration testing |
| **Microsoft.AspNetCore.Mvc.Testing** | ASP.NET integration tests | `WebApplicationFactory<T>` |

### Test Data Management

#### 📥 Downloading Test Data

Real HTML files are used for parser testing. To refresh test data:

```bash
# Run the download script
./download_test_data.sh

# Or download manually with curl
curl -o tests/Aggregator.Tests/TestData/HtmlPages/askstudio/main_shop_page.html \
     "https://askstudio.ru/shop/"
```

#### 📁 Test Data Usage

```csharp
// Reading test HTML files in tests
var htmlContent = TestDataHelper.ReadTestFile("HtmlPages/askstudio/main_shop_page.html");

// Using ProductBuilder for test objects
var product = new ProductBuilder()
    .WithName("Test Product")
    .WithPrice("1500")
    .WithShop("Ask Studio")
    .Build();
```

### Writing Tests

#### 🔧 Unit Test Example

```csharp
[Fact]
public void ShopName_ShouldReturnCorrectValue()
{
    // Arrange
    var parser = new AskStudioParser(context, httpFactory, logger, imageService);
    
    // Act
    var shopName = parser.ShopName;
    
    // Assert
    shopName.Should().Be("Ask Studio");
}
```

#### 🌐 Integration Test Example (Future)

```csharp
[Fact]
public async Task ParseAsync_ShouldSaveProductsToDatabase()
{
    // Arrange
    using var container = new PostgreSqlBuilder().Build();
    await container.StartAsync();
    
    // Act & Assert
    // Test full parsing workflow with real database
}
```

### Test Configuration

Tests automatically:
- ✅ **Copy HTML files** to output directory
- ✅ **Setup in-memory database** for unit tests  
- ✅ **Mock HTTP clients** to avoid real web requests
- ✅ **Use test-specific configuration** 
- ✅ **Clean up resources** after each test

### Test Best Practices

1. **📝 Follow AAA Pattern**: Arrange, Act, Assert
2. **🏷️ Use descriptive test names**: `Method_Scenario_ExpectedResult`
3. **🧹 Keep tests isolated**: Each test should be independent
4. **📊 Use builders for complex objects**: `ProductBuilder`, `ParserBuilder`
5. **🎭 Mock external dependencies**: HTTP clients, file system, etc.
6. **📐 Test edge cases**: null values, empty strings, malformed HTML
7. **⚡ Prefer unit over integration tests**: Faster execution, easier debugging

## 🏗️ Project Structure

```
Aggregator/
├── src/
│   └── Aggregator/                    # Main application
│       ├── Configuration/             # Strongly-typed configuration models
│       │   ├── DatabaseOptions.cs    # Database settings
│       │   ├── HttpClientOptions.cs  # HTTP client settings
│       │   └── ParsingOptions.cs     # Parsing settings
│       ├── Data/                     # Database context and configurations
│       ├── Extensions/               # Extension methods for DI and configuration
│       │   ├── ServiceCollectionExtensions.cs
│       │   └── ConfigurationExtensions.cs
│       ├── Interfaces/               # Service interfaces
│       ├── Migrations/               # Entity Framework migrations
│       ├── Models/                   # Data models
│       ├── ParserServices/           # Website parser implementations
│       ├── Services/                 # Business logic services
│       │   └── Application/
│       │       └── ParsingApplicationService.cs
│       ├── appsettings.json          # Base configuration
│       ├── appsettings.Development.json # Development overrides
│       ├── appsettings.Docker.json   # Docker overrides
│       └── Program.cs               # Application entry point
├── tests/
│   └── Aggregator.Tests/            # Test project
│       ├── Unit/                    # Unit tests
│       ├── Integration/             # Integration tests
│       ├── Fixtures/                # Test fixtures
│       ├── Helpers/                 # Test helpers
│       └── TestData/                # Test data files
├── docker-compose.yml               # Multi-container orchestration
├── download_test_data.sh            # Script to download test HTML
└── README.md                        # This file
```

## 🔧 Environment-Specific Features

### Development Environment
- ✅ **Debug logging** enabled
- ✅ **SQL query logging** enabled
- ✅ **Detailed parsing logs** enabled
- ✅ **Increased timeouts** for debugging
- ✅ **Slower request intervals** to prevent rate limiting

### Docker Environment  
- ✅ **Optimized for containers** 
- ✅ **Production-like settings**
- ✅ **Minimal logging** for performance
- ✅ **Faster request intervals**
- ✅ **Container networking** (postgres hostname)

## 🐛 Troubleshooting

### Environment Detection Issues

**Check current environment:**
```bash
# In application logs, look for:
[INFO] Среда выполнения: Development

# Or check environment variable directly:
echo $ASPNETCORE_ENVIRONMENT          # Unix
echo %ASPNETCORE_ENVIRONMENT%         # Windows CMD
echo $env:ASPNETCORE_ENVIRONMENT      # PowerShell
```

**If environment is not detected:**
1. Verify environment variable is set correctly
2. Restart terminal/IDE after setting permanent variables
3. Check for typos in environment name (case-sensitive)

### Common Configuration Issues

1. **Wrong database host in different environments:**
   - Development: `localhost`
   - Docker: `postgres` (container name)

2. **File not found errors:**
   - Ensure `appsettings.{Environment}.json` exists
   - Check file naming (case-sensitive on Unix)

3. **Configuration not loading:**
   - Verify JSON syntax is valid
   - Check configuration section names match exactly

### Test Troubleshooting

**Test data files not found:**
```bash
# Ensure test data exists
ls tests/Aggregator.Tests/TestData/HtmlPages/askstudio/

# Re-download if missing
./download_test_data.sh
```

**Tests fail with database errors:**
```bash
# Clear test databases
dotnet test --logger console --verbosity detailed

# Check test configuration
dotnet test --collect:"XPlat Code Coverage" --logger:trx
```

## 🚀 Production Deployment

### Environment Variables for Production

```bash
# Required environment variables
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Host=prod-server;Port=5432;Database=aggregator;Username=user;Password=pass"

# Optional overrides
export Database__CommandTimeout=30
export HttpClient__TimeoutSeconds=30
export Parsing__DelayBetweenRequestsMs=500
```

### Docker Production

```bash
# Create production environment file
cat > .env.production << EOF
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=prod-db;Port=5432;Database=aggregator;Username=prod_user;Password=secure_password
EOF

# Run with production settings
docker-compose --env-file .env.production up -d
```

## 📚 Additional Resources

- [.NET Configuration Documentation](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Docker Documentation](https://docs.docker.com/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [xUnit Testing Documentation](https://xunit.net/docs/getting-started/netcore/cmdline)
- [Moq Framework Documentation](https://github.com/moq/moq4)
- [FluentAssertions Documentation](https://fluentassertions.com/introduction)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Set up your development environment:
   ```bash
   export ASPNETCORE_ENVIRONMENT=Development
   docker-compose up -d postgres
   dotnet ef database update
   ```
4. Make your changes
5. **Run tests to ensure everything works**:
   ```bash
   dotnet test
   ```
6. Submit a pull request

---

## Quick Reference

### Environment Commands

| Action | Windows CMD | Windows PowerShell | Unix |
|--------|-------------|-------------------|------|
| **Set Development** | `set ASPNETCORE_ENVIRONMENT=Development` | `$env:ASPNETCORE_ENVIRONMENT="Development"` | `export ASPNETCORE_ENVIRONMENT=Development` |
| **Set Docker** | `set ASPNETCORE_ENVIRONMENT=Docker` | `$env:ASPNETCORE_ENVIRONMENT="Docker"` | `export ASPNETCORE_ENVIRONMENT=Docker` |
| **Check Current** | `echo %ASPNETCORE_ENVIRONMENT%` | `echo $env:ASPNETCORE_ENVIRONMENT` | `echo $ASPNETCORE_ENVIRONMENT` |
| **Run App** | `dotnet run` | `dotnet run` | `dotnet run` |
| **Run Tests** | `dotnet test` | `dotnet test` | `dotnet test` |

### Development Workflow

```bash
# Start development session
export ASPNETCORE_ENVIRONMENT=Development    # Set environment
docker-compose up -d postgres               # Start database
dotnet ef database update                   # Apply migrations
dotnet test                                 # Run tests
dotnet run                                  # Run application
```

### Testing Workflow

```bash
# Download fresh test data
./download_test_data.sh

# Run all tests
dotnet test -v normal

# Run specific parser tests
dotnet test --filter "AskStudioParserTests"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
``` 