# SQLAPI - MySQL Query REST API Service

SQLAPI is a web-based application that connects to a remote MySQL server in read-only mode and exposes predefined SQL queries through REST API endpoints. The application also provides a web interface for configuration management.

## Features

- **MySQL Database Connectivity**: Securely connects to remote MySQL databases in read-only mode
- **REST API Endpoints**: Exposes 10 configurable SQL query slots via REST API
- **Parameterized Queries**: Supports parameter passing through POST requests for dynamic queries
- **Web-based Settings Interface**: Secure settings page for configuration management
- **API Key Authentication**: API endpoints secured with API key authentication
- **Configuration Management**: All settings stored in `appsettings.json`
- **Query Management**: 10 configurable SQL query slots with names and SQL statements
- **API Key Management**: Generate and revoke API keys for REST API access

## Prerequisites

Before running the application, ensure you have the following installed:

1. **.NET 9 SDK** - Download from [Microsoft's official website](https://dotnet.microsoft.com/download/dotnet/9.0)
2. **MySQL Database Server** - For testing and development
3. **Web Browser** - For accessing the settings interface

## Installation

### Option 1: Using Pre-built Release (Recommended)

1. Download the latest release from the releases page
2. Extract the archive to your desired location
3. Proceed to the Configuration section

### Option 2: Building from Source

1. Clone or download the repository:
   ```bash
   git clone <repository-url>
   ```

2. Navigate to the project directory:
   ```bash
   cd SQLAPI
   ```

3. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

4. Build the application:
   ```bash
   dotnet build
   ```

## Configuration

All application settings are stored in `appsettings.json`. Before running the application for the first time, you should configure the settings.

### Database Configuration

Open `appsettings.json` and update the DatabaseConfig section:

```json
"DatabaseConfig": {
  "Server": "your-mysql-server-address",
  "Database": "your-database-name",
  "Username": "your-database-username",
  "Password": "your-database-password"
}
```

### Settings Authentication

Update the SettingsAuth section with your preferred credentials for accessing the settings page:

```json
"SettingsAuth": {
  "Username": "admin",
  "Password": "your-secure-password"
}
```

Note: In a production environment, the password should be properly hashed. The application will automatically hash passwords when updated through the settings interface.

### Query Slots

The application provides 10 configurable query slots. You can pre-configure them in `appsettings.json`:

```json
"QuerySlots": [
  {
    "Id": 0,
    "Name": "User List",
    "Sql": "SELECT id, name, email FROM users WHERE active = @active"
  },
  {
    "Id": 1,
    "Name": "Product Catalog",
    "Sql": "SELECT * FROM products WHERE category = @category LIMIT @limit"
  }
  // ... up to 10 slots
]
```

## Running the Application

### Using the Batch File (Windows)

Simply double-click the `run.bat` file or execute it from the command prompt:

```cmd
run.bat
```

### Using the Shell Script (Linux/Mac)

Make the script executable and run it:

```bash
chmod +x run.sh
./run.sh
```

### Using Command Line

1. Navigate to the project directory
2. Run the application:
   ```bash
   dotnet run
   ```

### Using Docker

1. Build the Docker image:
   ```bash
   docker build -t sqlapi .
   ```

2. Run the container:
   ```bash
   docker run -d -p 8080:80 --name sqlapi-container sqlapi
   ```

## Building the Application

### Using the Build Script (Windows)

Simply double-click the `build.bat` file or execute it from the command prompt:

```cmd
build.bat
```

### Using the Build Script (Linux/Mac)

Make the script executable and run it:

```bash
chmod +x build.sh
./build.sh
```

### Using Command Line

1. Navigate to the project directory
2. Publish the application:
   ```bash
   dotnet publish -c Release -o publish
   ```

The published application will be located in the `publish` folder.

## Usage

### Starting the Application

After configuration, start the application using one of the methods mentioned above. The application will start on the following URLs:

- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

### Accessing the Settings Page

1. Open your web browser and navigate to `http://localhost:5000/settings.html`
2. Login with the credentials configured in `appsettings.json` (default: newadmin/newpassword)
3. Configure database connection settings:
   - Enter your MySQL server address
   - Enter your database name
   - Enter your database username and password
   - Click "Test Connection" to verify settings
   - Click "Save Database Settings" to save
4. Update settings authentication credentials if needed
5. Generate API keys for REST API access:
   - Enter a name for your API key
   - Click "Generate API Key"
   - Copy the generated API key for use with REST API endpoints
6. Manage query slots:
   - Select a query slot from the dropdown
   - Enter a name for the query
   - Enter the SQL statement (supports parameters with @ syntax)
   - Click "Save Query"

### Using the REST API

The API provides the following endpoints:

#### Query Execution
```
POST /api/query/{id}
X-API-Key: your-api-key
Content-Type: application/json

{
  "parameters": {
    "param1": "value1",
    "param2": "value2"
  }
}
```

Example with curl:
```bash
curl -X POST "http://localhost:5000/api/query/0" \
  -H "X-API-Key: your-api-key" \
  -H "Content-Type: application/json" \
  -d '{"parameters":{"active":1}}'
```

#### Query List
```
GET /api/queries
X-API-Key: your-api-key
```

Example with curl:
```bash
curl -X GET "http://localhost:5000/api/queries" \
  -H "X-API-Key: your-api-key"
```

### API Response Format

All API responses return data in JSON format:

```json
[
  {
    "id": 1,
    "name": "John Doe",
    "email": "john@example.com"
  },
  {
    "id": 2,
    "name": "Jane Smith",
    "email": "jane@example.com"
  }
]
```

## Security

- Database connections are enforced in read-only mode
- API endpoints are secured with API key authentication
- Settings page requires separate authentication
- Parameterized queries prevent SQL injection attacks
- Sensitive data is not logged
- HTTPS is enabled by default in development mode
- API keys can be revoked at any time

## Project Structure

```
SQLAPI/
├── Controllers/
│   ├── QueryController.cs
│   ├── SettingsController.cs
│   └── AuthController.cs
├── Services/
│   ├── IConfigurationService.cs
│   ├── ConfigurationService.cs
│   ├── IQueryService.cs
│   ├── QueryService.cs
│   ├── IMySqlDataAccess.cs
│   └── MySqlDataAccess.cs
├── Models/
│   ├── AppSettings.cs
│   ├── DatabaseConfig.cs
│   ├── QuerySlot.cs
│   └── DTOs
├── wwwroot/
│   ├── settings.html
│   ├── index.html
│   ├── css/
│   └── js/
├── Program.cs
├── appsettings.json
├── run.bat
├── run.sh
├── build.bat
├── build.sh
├── Dockerfile
├── README.md
└── SQLAPI.csproj
```

## API Documentation

When running in development mode, Swagger documentation is available at:
```
https://localhost:5001/swagger
```

## Troubleshooting

### Common Issues

1. **Database Connection Failed**
   - Verify database server address, name, username, and password
   - Ensure the MySQL server is running and accessible
   - Check firewall settings if connecting to a remote server

2. **Authentication Failed**
   - Verify API key is valid and active
   - Check that the X-API-Key header is correctly formatted

3. **Query Execution Errors**
   - Ensure only SELECT statements are used
   - Verify parameter names match those in the SQL statement
   - Check that the database user has SELECT permissions on the tables

### Logging

The application logs information to the console. For more detailed logging, check the application logs in the system's default logging directory.

## Development

### Adding New Features

1. Create new controllers in the `Controllers` folder
2. Implement services in the `Services` folder
3. Add models to the `Models` folder
4. Register services in `Program.cs`

### Building for Production

1. Publish the application:
   ```bash
   dotnet publish -c Release -o ./publish
   ```
2. Deploy the contents of the `publish` folder to your server

## Deployment

### Windows Server

1. Install .NET 9 Runtime on the server
2. Copy all application files to the server
3. Configure `appsettings.json` for production
4. Run the application using `run.bat` or as a Windows service

### Linux Server

1. Install .NET 9 Runtime on the server
2. Copy all application files to the server
3. Configure `appsettings.json` for production
4. Make the run script executable and run the application:
   ```bash
   chmod +x run.sh
   ./run.sh
   ```

### Docker Deployment

1. Build the Docker image:
   ```bash
   docker build -t sqlapi .
   ```
2. Run the container:
   ```bash
   docker run -d -p 8080:80 --name sqlapi-container sqlapi
   ```

## License

This project is licensed under the MIT License.

## Support

For support, please open an issue on the GitHub repository or contact the development team.
