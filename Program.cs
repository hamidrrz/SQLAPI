using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using System.Windows.Forms;
using System.Drawing;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SQLAPI", Version = "v1" });
    
    // Add API Key Authentication to Swagger
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key needed to access the endpoints",
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                Scheme = "ApiKeyScheme",
                Name = "X-API-Key",
                In = ParameterLocation.Header,
            },
            new string[] {}
        }
    });
});

// Configure strongly typed settings objects
builder.Services.Configure<AppSettings>(builder.Configuration);

// Add custom services
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IQueryService, QueryService>();
builder.Services.AddScoped<IMySqlDataAccess, MySqlDataAccess>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add anti-forgery service for CSRF protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// Add rate limiting services
builder.Services.AddRateLimiter(_ => { });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add redirect from /settings to /settings.html
app.UseWhen(context => context.Request.Path.Equals("/settings", StringComparison.OrdinalIgnoreCase), appBuilder =>
{
    appBuilder.Run(context =>
    {
        context.Response.Redirect("/settings.html");
        return Task.CompletedTask;
    });
});

// Serve static files (including settings.html) without authentication
app.UseDefaultFiles();
app.UseStaticFiles();


// Apply API key authentication only to API endpoints
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseMiddleware<ApiKeyAuthenticationMiddleware>();
});

// Add anti-forgery middleware for CSRF protection (for settings endpoints)
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/settings"), appBuilder =>
{
    appBuilder.UseAntiforgery();
});

// Add security headers middleware
app.Use(async (context, next) =>
{
    // Add security headers
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    
    await next();
});

app.MapControllers();

// Create and configure the system tray icon
NotifyIcon notifyIcon = new NotifyIcon();
try
{
    // Try to load custom icon from wwwroot directory
    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "favicon.ico");
    Console.WriteLine($"Looking for icon at: {iconPath}");
    Console.WriteLine($"Icon file exists: {File.Exists(iconPath)}");
    
    if (File.Exists(iconPath))
    {
        notifyIcon.Icon = new Icon(iconPath);
        Console.WriteLine("Custom icon loaded successfully");
    }
    else
    {
        // Try to load from application directory as fallback
        iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "favicon.ico");
        Console.WriteLine($"Looking for icon at: {iconPath}");
        Console.WriteLine($"Icon file exists: {File.Exists(iconPath)}");
        
        if (File.Exists(iconPath))
        {
            notifyIcon.Icon = new Icon(iconPath);
            Console.WriteLine("Custom icon loaded successfully from application directory");
        }
        else
        {
            // Fallback to default icon if custom icon is not found
            notifyIcon.Icon = SystemIcons.Application;
            Console.WriteLine("Using default icon");
        }
    }
}
catch (Exception ex)
{
    // Fallback to default icon if there's an error loading the custom icon
    notifyIcon.Icon = SystemIcons.Application;
    Console.WriteLine($"Error loading custom icon: {ex.Message}");
}
notifyIcon.Visible = true;
notifyIcon.Text = "SQLAPI";

// Create the context menu
ContextMenuStrip contextMenu = new ContextMenuStrip();

// Settings menu item
ToolStripMenuItem settingsItem = new ToolStripMenuItem("Settings");
settingsItem.Click += (sender, e) => {
    try
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "http://localhost:5000/settings",
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error opening settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
};

// Exit menu item
ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
exitItem.Click += (sender, e) => {
    notifyIcon.Visible = false;
    Environment.Exit(0);
};

// Add menu items to context menu
contextMenu.Items.Add(settingsItem);
contextMenu.Items.Add(exitItem);

// Assign context menu to notify icon
notifyIcon.ContextMenuStrip = contextMenu;

// Run the web application in a separate thread
var appTask = app.RunAsync();

// Run the Windows Forms message loop
Application.Run();

// Wait for the web application to complete (which should be never in a typical web app)
await appTask;
// Models
public class AppSettings
{
    public DatabaseConfig DatabaseConfig { get; set; } = new();
    public SettingsAuth SettingsAuth { get; set; } = new();
    public List<QuerySlot> QuerySlots { get; set; } = new();
    public List<ApiKey> ApiKeys { get; set; } = new(); // API keys for REST API authentication
}

public class DatabaseConfig
{
    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsPasswordProtected { get; set; } = false; // Flag to indicate if password is DPAPI protected
}

public class SettingsAuth
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // Can be plain text or DPAPI protected
    public bool IsPasswordProtected { get; set; } = false; // Flag to indicate if password is DPAPI protected
}

public class ApiKey
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string AllowedIp { get; set; } = string.Empty; // IP binding for the API key
}

// Models and DTOs
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class TestConnectionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; }
}

public class QuerySlot
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sql { get; set; } = string.Empty;
}

public class QueryExecutionRequest
{
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class ApiKeyCreateRequest
{
    public string Name { get; set; } = string.Empty;
}

public class ApiKeyResponse
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public bool IsActive { get; set; }
}

// Interfaces
public interface IConfigurationService
{
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
    Task UpdateDatabaseConfigAsync(DatabaseConfig config);
    Task UpdateSettingsAuthAsync(SettingsAuth auth);
    Task<QuerySlot?> GetQuerySlotAsync(int id);
    Task UpdateQuerySlotAsync(QuerySlot slot);
    Task<List<ApiKey>> GetApiKeysAsync();
    Task<ApiKey> CreateApiKeyAsync(string name);
    Task<bool> RevokeApiKeyAsync(string key);
}

public interface IQueryService
{
    Task<Dictionary<string, object?>[]> ExecuteQueryAsync(int id, Dictionary<string, object> parameters);
    Task<TestConnectionResult> TestConnectionAsync(DatabaseConfig config);
    Task<List<QuerySlot>> GetQuerySlotsAsync();
}

public interface IMySqlDataAccess
{
    Task<Dictionary<string, object?>[]> ExecuteQueryAsync(string sql, Dictionary<string, object>? parameters = null);
    Task<TestConnectionResult> TestConnectionAsync(DatabaseConfig config);
    bool IsConnectionReadOnly { get; }
}

public interface IAuthService
{
    Task<bool> ValidateSettingsCredentialsAsync(string username, string password);
    byte[] Protect(string data);
    string Unprotect(byte[] data);
    bool IsPasswordProtected(string password);
    string GenerateApiKey();
}

// Services
public class ConfigurationService : IConfigurationService
{
    private readonly IOptions<AppSettings> _appSettings;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly string _appSettingsPath;

    public ConfigurationService(IOptions<AppSettings> appSettings, ILogger<ConfigurationService> logger)
    {
        _appSettings = appSettings;
        _logger = logger;
        
        // Use the appsettings.json file in the current directory (source directory)
        _appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        _logger.LogInformation("AppSettings path: {Path}", _appSettingsPath);
    }

    public Task<AppSettings> GetSettingsAsync()
    {
        return Task.FromResult(_appSettings.Value);
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            _logger.LogInformation("Attempting to save settings to {Path}", _appSettingsPath);
            
            // Check if file exists
            if (!File.Exists(_appSettingsPath))
            {
                _logger.LogWarning("AppSettings file does not exist at {Path}", _appSettingsPath);
                throw new FileNotFoundException($"AppSettings file not found at {_appSettingsPath}");
            }
            
            // Read the current appsettings.json content
            var jsonContent = await File.ReadAllTextAsync(_appSettingsPath);
            _logger.LogInformation("Read existing JSON content: {Content}", jsonContent);
            
            // Parse the JSON
            var jsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent) ?? new Dictionary<string, object>();
            _logger.LogInformation("Parsed JSON object with {Count} properties", jsonObject.Count);
            
            // Update the settings
            jsonObject["DatabaseConfig"] = settings.DatabaseConfig;
            jsonObject["SettingsAuth"] = settings.SettingsAuth;
            jsonObject["QuerySlots"] = settings.QuerySlots;
            jsonObject["ApiKeys"] = settings.ApiKeys;
            
            _logger.LogInformation("Updated JSON object with new settings");

            // Serialize and save back to file
            var options = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = JsonSerializer.Serialize(jsonObject, options);
            _logger.LogInformation("Serialized updated JSON: {Json}", updatedJson);
            
            await File.WriteAllTextAsync(_appSettingsPath, updatedJson);
            _logger.LogInformation("Successfully saved settings to appsettings.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings to appsettings.json");
            throw;
        }
    }

    public async Task UpdateDatabaseConfigAsync(DatabaseConfig config)
    {
        try
        {
            _logger.LogInformation("Updating database configuration");
            // Get current settings
            var currentSettings = await GetSettingsAsync();
            
            // Update database config
            currentSettings.DatabaseConfig = config;
            
            // Save updated settings
            await SaveSettingsAsync(currentSettings);
            
            _logger.LogInformation("Database configuration updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating database configuration");
            throw;
        }
    }

    public async Task UpdateSettingsAuthAsync(SettingsAuth auth)
    {
        try
        {
            _logger.LogInformation("Updating settings authentication");
            // Get current settings
            var currentSettings = await GetSettingsAsync();
            
            // Update settings auth
            currentSettings.SettingsAuth = auth;
            
            // Save updated settings
            await SaveSettingsAsync(currentSettings);
            
            _logger.LogInformation("Settings authentication updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings authentication");
            throw;
        }
    }

    public Task<QuerySlot?> GetQuerySlotAsync(int id)
    {
        var settings = _appSettings.Value;
        var slot = settings.QuerySlots.FirstOrDefault(q => q.Id == id);
        return Task.FromResult(slot);
    }

    public async Task UpdateQuerySlotAsync(QuerySlot slot)
    {
        try
        {
            _logger.LogInformation("Updating query slot {Id}", slot.Id);
            // Get current settings
            var currentSettings = await GetSettingsAsync();
            
            // Find and update the query slot
            var existingSlot = currentSettings.QuerySlots.FirstOrDefault(q => q.Id == slot.Id);
            if (existingSlot != null)
            {
                existingSlot.Name = slot.Name;
                existingSlot.Sql = slot.Sql;
            }
            else
            {
                currentSettings.QuerySlots.Add(slot);
            }
            
            // Save updated settings
            await SaveSettingsAsync(currentSettings);
            
            _logger.LogInformation("Query slot {Id} updated", slot.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating query slot {Id}", slot.Id);
            throw;
        }
    }

    public Task<List<ApiKey>> GetApiKeysAsync()
    {
        var settings = _appSettings.Value;
        return Task.FromResult(settings.ApiKeys);
    }

    public async Task<ApiKey> CreateApiKeyAsync(string name)
    {
        try
        {
            _logger.LogInformation("Creating new API key");
            Console.WriteLine($"CreateApiKeyAsync called with name: {name}");
            
            // Get current settings
            var currentSettings = await GetSettingsAsync();
            
            // Create new API key
            var apiKey = new ApiKey
            {
                Key = Guid.NewGuid().ToString("N"),
                Name = name,
                Created = DateTime.UtcNow,
                IsActive = true
            };
            
            // Add to settings
            currentSettings.ApiKeys.Add(apiKey);
            
            // Save updated settings
            await SaveSettingsAsync(currentSettings);
            
            _logger.LogInformation("API key created with name {Name}", name);
            Console.WriteLine($"API key created with key: {apiKey.Key}, name: {apiKey.Name}");
            return apiKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key");
            Console.WriteLine($"Error creating API key: {ex}");
            throw;
        }
    }

    public async Task<bool> RevokeApiKeyAsync(string key)
    {
        try
        {
            _logger.LogInformation("Revoking API key");
            // Get current settings
            var currentSettings = await GetSettingsAsync();
            
            // Find and update the API key
            var apiKey = currentSettings.ApiKeys.FirstOrDefault(k => k.Key == key);
            if (apiKey != null)
            {
                apiKey.IsActive = false;
                
                // Save updated settings
                await SaveSettingsAsync(currentSettings);
                
                _logger.LogInformation("API key revoked");
                return true;
            }
            
            _logger.LogWarning("API key not found for revocation");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key");
            throw;
        }
    }
}

public class QueryService : IQueryService
{
    private readonly IConfigurationService _configurationService;
    private readonly IMySqlDataAccess _dataAccess;
    private readonly ILogger<QueryService> _logger;

    public QueryService(IConfigurationService configurationService, IMySqlDataAccess dataAccess, ILogger<QueryService> logger)
    {
        _configurationService = configurationService;
        _dataAccess = dataAccess;
        _logger = logger;
    }

    public async Task<Dictionary<string, object?>[]> ExecuteQueryAsync(int id, Dictionary<string, object> parameters)
    {
        var slot = await _configurationService.GetQuerySlotAsync(id);
        if (slot == null)
        {
            throw new ArgumentException($"Query slot {id} not found");
        }

        // Validate that the query is a SELECT statement
        if (!IsSelectStatement(slot.Sql))
        {
            throw new InvalidOperationException("Only SELECT statements are allowed");
        }

        return await _dataAccess.ExecuteQueryAsync(slot.Sql, parameters);
    }

    public async Task<TestConnectionResult> TestConnectionAsync(DatabaseConfig config)
    {
        return await _dataAccess.TestConnectionAsync(config);
    }

    public async Task<List<QuerySlot>> GetQuerySlotsAsync()
    {
        var settings = await _configurationService.GetSettingsAsync();
        return settings.QuerySlots;
    }

    private bool IsSelectStatement(string sql)
    {
        var trimmedSql = sql.TrimStart();
        return trimmedSql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
               trimmedSql.StartsWith("WITH", StringComparison.OrdinalIgnoreCase);
    }
}

public class MySqlDataAccess : IMySqlDataAccess
{
    private readonly DatabaseConfig _config;
    private readonly ILogger<MySqlDataAccess> _logger;
    private readonly IOptions<AppSettings> _appSettings;

    public bool IsConnectionReadOnly => true;

    public MySqlDataAccess(IOptions<AppSettings> appSettings, ILogger<MySqlDataAccess> logger)
    {
        _appSettings = appSettings;
        _config = appSettings.Value.DatabaseConfig;
        _logger = logger;
    }

    public async Task<Dictionary<string, object?>[]> ExecuteQueryAsync(string sql, Dictionary<string, object>? parameters = null)
    {
        var results = new List<Dictionary<string, object?>>();

        try
        {
            var connectionString = BuildConnectionString();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);
            command.CommandTimeout = 30; // 30 seconds timeout

            // Add parameters if provided
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    // Handle JsonElement values by converting them to their actual types
                    var value = param.Value;
                    if (value is System.Text.Json.JsonElement jsonElement)
                    {
                        switch (jsonElement.ValueKind)
                        {
                            case System.Text.Json.JsonValueKind.String:
                                value = jsonElement.GetString();
                                break;
                            case System.Text.Json.JsonValueKind.Number:
                                // Try to parse as int first, then long, then double
                                if (jsonElement.TryGetInt32(out int intValue))
                                    value = intValue;
                                else if (jsonElement.TryGetInt64(out long longValue))
                                    value = longValue;
                                else if (jsonElement.TryGetDouble(out double doubleValue))
                                    value = doubleValue;
                                break;
                            case System.Text.Json.JsonValueKind.True:
                                value = true;
                                break;
                            case System.Text.Json.JsonValueKind.False:
                                value = false;
                                break;
                            case System.Text.Json.JsonValueKind.Null:
                                value = null;
                                break;
                            default:
                                value = jsonElement.GetString();
                                break;
                        }
                    }
                    command.Parameters.AddWithValue($"@{param.Key}", value);
                }
            }

            using var reader = await command.ExecuteReaderAsync();
            var columnNames = Enumerable.Range(0, reader.FieldCount)
                .Select(i => reader.GetName(i))
                .ToArray();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                foreach (var columnName in columnNames)
                {
                    var ordinal = reader.GetOrdinal(columnName);
                    row[columnName] = await reader.IsDBNullAsync(ordinal) ? null : reader[columnName];
                }
                results.Add(row);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query: {Sql}", sql);
            throw new InvalidOperationException($"Error executing query: {ex.Message}");
        }

        return results.ToArray();
    }

    public async Task<TestConnectionResult> TestConnectionAsync(DatabaseConfig config)
    {
        var result = new TestConnectionResult();

        try
        {
            var testConfig = new DatabaseConfig
            {
                Server = config.Server,
                Database = config.Database,
                Username = config.Username,
                Password = config.Password,
                IsPasswordProtected = config.IsPasswordProtected
            };

            // Unprotect the password if it's protected
            if (testConfig.IsPasswordProtected && !string.IsNullOrEmpty(testConfig.Password))
            {
                try
                {
                    var protectedPasswordBytes = Convert.FromBase64String(testConfig.Password);
                    testConfig.Password = UnprotectPassword(protectedPasswordBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error unprotecting database password");
                    throw new InvalidOperationException("Error unprotecting database password");
                }
            }

            var connectionString = BuildTestConnectionString(testConfig);
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // Test if connection is read-only
            using var command = new MySqlCommand("SELECT @@read_only", connection);
            var readOnlyResult = await command.ExecuteScalarAsync();
            result.IsReadOnly = Convert.ToBoolean(readOnlyResult);

            result.Success = true;
            result.Message = "Connection successful";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
            _logger.LogError(ex, "Connection test failed");
        }

        return result;
    }

    private string BuildConnectionString()
    {
        // Get the actual password by unprotecting it if it's protected
        var password = _config.Password;
        if (_config.IsPasswordProtected && !string.IsNullOrEmpty(password))
        {
            try
            {
                var protectedPasswordBytes = Convert.FromBase64String(password);
                password = UnprotectPassword(protectedPasswordBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unprotecting database password");
                throw new InvalidOperationException("Error unprotecting database password");
            }
        }

        var builder = new MySqlConnectionStringBuilder
        {
            Server = _config.Server,
            Database = _config.Database,
            UserID = _config.Username,
            Password = password,
            ConnectionTimeout = 30,
            DefaultCommandTimeout = 30,
            Pooling = true,
            MinimumPoolSize = 0,
            MaximumPoolSize = 20,
            AllowLoadLocalInfile = false,
            AllowPublicKeyRetrieval = false,
            SslMode = MySqlSslMode.Preferred
        };

        return builder.ConnectionString;
    }

    private string BuildTestConnectionString(DatabaseConfig config)
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = config.Server,
            Database = config.Database,
            UserID = config.Username,
            Password = config.Password,
            ConnectionTimeout = 10,
            DefaultCommandTimeout = 10,
            Pooling = false,
            AllowLoadLocalInfile = false,
            AllowPublicKeyRetrieval = false,
            SslMode = MySqlSslMode.Preferred
        };

        return builder.ConnectionString;
    }

    private string UnprotectPassword(byte[] data)
    {
        try
        {
            // Use DPAPI to unprotect the data
            var unprotectedBytes = ProtectedData.Unprotect(
                data,
                null, // Optional entropy
                DataProtectionScope.CurrentUser);
            return System.Text.Encoding.UTF8.GetString(unprotectedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unprotecting data with DPAPI");
            throw;
        }
    }
}

public class AuthService : IAuthService
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IConfigurationService configurationService, ILogger<AuthService> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    public async Task<bool> ValidateSettingsCredentialsAsync(string username, string password)
    {
        var settings = await _configurationService.GetSettingsAsync();
        var storedUsername = settings.SettingsAuth.Username;
        var storedPassword = settings.SettingsAuth.Password;
        var isPasswordProtected = settings.SettingsAuth.IsPasswordProtected;

        if (string.IsNullOrEmpty(storedUsername) || string.IsNullOrEmpty(storedPassword))
        {
            return false;
        }

        // Check username first
        if (username != storedUsername)
        {
            return false;
        }

        // Check password based on whether it's protected or not
        if (isPasswordProtected)
        {
            try
            {
                var protectedPasswordBytes = Convert.FromBase64String(storedPassword);
                var unprotectedPassword = Unprotect(protectedPasswordBytes);
                return password == unprotectedPassword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unprotecting settings password");
                return false;
            }
        }
        else
        {
            // Plain text comparison
            return password == storedPassword;
        }
    }

    public byte[] Protect(string data)
    {
        try
        {
            // Use DPAPI to protect the data
            return ProtectedData.Protect(
                System.Text.Encoding.UTF8.GetBytes(data),
                null, // Optional entropy
                DataProtectionScope.CurrentUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error protecting data with DPAPI");
            throw;
        }
    }

    public string Unprotect(byte[] data)
    {
        try
        {
            // Use DPAPI to unprotect the data
            var unprotectedBytes = ProtectedData.Unprotect(
                data,
                null, // Optional entropy
                DataProtectionScope.CurrentUser);
            return System.Text.Encoding.UTF8.GetString(unprotectedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unprotecting data with DPAPI");
            throw;
        }
    }

    public bool IsPasswordProtected(string password)
    {
        // Try to decode from base64 and then try to unprotect it
        try
        {
            var decoded = Convert.FromBase64String(password);
            Unprotect(decoded);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GenerateApiKey()
    {
        return Guid.NewGuid().ToString("N");
    }
}

// Controllers
[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly IQueryService _queryService;

    public QueryController(IQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpPost("{id}")]
    public async Task<ActionResult<Dictionary<string, object?>[]>> ExecuteQuery(int id, [FromBody] QueryExecutionRequest request)
    {
        try
        {
            var results = await _queryService.ExecuteQueryAsync(id, request.Parameters ?? new Dictionary<string, object>());
            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<QuerySlot>>> GetQuerySlots()
    {
        try
        {
            var slots = await _queryService.GetQuerySlotsAsync();
            return Ok(slots);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IConfigurationService _configurationService;
    private readonly IQueryService _queryService;
    private readonly IAuthService _authService;

    public SettingsController(IConfigurationService configurationService, IQueryService queryService, IAuthService authService)
    {
        _configurationService = configurationService;
        _queryService = queryService;
        _authService = authService;
    }

    [HttpGet("database")]
    public async Task<ActionResult<DatabaseConfig>> GetDatabaseConfig()
    {
        var settings = await _configurationService.GetSettingsAsync();
        return Ok(settings.DatabaseConfig);
    }

    [HttpPost("database")]
    public async Task<IActionResult> UpdateDatabaseConfig([FromBody] DatabaseConfig config)
    {
        // Validate anti-forgery token
        try
        {
            await HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest("Invalid anti-forgery token");
        }
        
        // Protect the database password if it's not already protected
        if (!string.IsNullOrEmpty(config.Password) && !config.IsPasswordProtected)
        {
            var protectedPassword = _authService.Protect(config.Password);
            config.Password = Convert.ToBase64String(protectedPassword);
            config.IsPasswordProtected = true;
        }
        
        // In a real implementation, you would verify authentication before allowing updates
        await _configurationService.UpdateDatabaseConfigAsync(config);
        return Ok();
    }

    [HttpPost("test-connection")]
    public async Task<ActionResult<TestConnectionResult>> TestConnection([FromBody] DatabaseConfig config)
    {
        // Validate anti-forgery token
        try
        {
            await HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest("Invalid anti-forgery token");
        }
        
        var result = await _queryService.TestConnectionAsync(config);
        return Ok(result);
    }

    [HttpGet("auth")]
    public async Task<ActionResult<SettingsAuth>> GetSettingsAuth()
    {
        var settings = await _configurationService.GetSettingsAsync();
        return Ok(settings.SettingsAuth);
    }
    
    [HttpGet("antiforgery-token")]
    public IActionResult GetAntiForgeryToken()
    {
        var tokens = HttpContext.RequestServices.GetRequiredService<IAntiforgery>().GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpGet("queries")]
    public async Task<ActionResult<List<QuerySlot>>> GetQuerySlots()
    {
        var slots = await _queryService.GetQuerySlotsAsync();
        return Ok(slots);
    }

    [HttpPost("queries/{id}")]
    public async Task<IActionResult> UpdateQuerySlot(int id, [FromBody] QuerySlot slot)
    {
        // Validate anti-forgery token
        try
        {
            await HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest("Invalid anti-forgery token");
        }
        
        if (id != slot.Id)
        {
            return BadRequest("ID mismatch");
        }

        // In a real implementation, you would verify authentication before allowing updates
        await _configurationService.UpdateQuerySlotAsync(slot);
        return Ok();
    }

    [HttpPost("auth")]
    public async Task<IActionResult> UpdateSettingsAuth([FromBody] SettingsAuth auth)
    {
        // Validate anti-forgery token
        try
        {
            await HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest("Invalid anti-forgery token");
        }
        
        // Protect the settings auth password if it's not already protected
        if (!string.IsNullOrEmpty(auth.Password) && !auth.IsPasswordProtected)
        {
            var protectedPassword = _authService.Protect(auth.Password);
            auth.Password = Convert.ToBase64String(protectedPassword);
            auth.IsPasswordProtected = true;
        }
        
        // In a real implementation, you would verify authentication before allowing updates
        await _configurationService.UpdateSettingsAuthAsync(auth);
        return Ok();
    }

    [HttpGet("apikeys")]
    public async Task<ActionResult<List<ApiKeyResponse>>> GetApiKeys()
    {
        var apiKeys = await _configurationService.GetApiKeysAsync();
        var response = apiKeys.Select(k => new ApiKeyResponse
        {
            Key = k.Key,
            Name = k.Name,
            Created = k.Created,
            IsActive = k.IsActive
        }).ToList();
        
        return Ok(response);
    }

    [HttpPost("apikeys")]
    public async Task<ActionResult<ApiKeyResponse>> CreateApiKey([FromBody] ApiKeyCreateRequest request)
    {
        // Validate anti-forgery token
        try
        {
            await HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest("Invalid anti-forgery token");
        }
        
        // Debugging: Log the request
        Console.WriteLine($"CreateApiKey called with name: {request.Name}");
        
        var apiKey = await _configurationService.CreateApiKeyAsync(request.Name);
        
        // Debugging: Log the created API key
        Console.WriteLine($"API key created with key: {apiKey.Key}, name: {apiKey.Name}");
        
        var response = new ApiKeyResponse
        {
            Key = apiKey.Key,
            Name = apiKey.Name,
            Created = apiKey.Created,
            IsActive = apiKey.IsActive
        };
        
        return Ok(response);
    }

    [HttpPost("apikeys/{key}/revoke")]
    public async Task<IActionResult> RevokeApiKey(string key)
    {
        // Validate anti-forgery token
        try
        {
            await HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest("Invalid anti-forgery token");
        }
        
        var result = await _configurationService.RevokeApiKeyAsync(key);
        if (result)
        {
            return Ok(new { Message = "API key revoked successfully" });
        }
        else
        {
            return NotFound(new { Message = "API key not found" });
        }
    }
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var isValid = await _authService.ValidateSettingsCredentialsAsync(request.Username, request.Password);
        if (isValid)
        {
            // In a real implementation, you would create a proper authentication token or session
            return Ok(new { Message = "Login successful" });
        }

        return Unauthorized(new { Message = "Invalid credentials" });
    }
}

// Middleware
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfigurationService configurationService)
    {
        // Skip authentication for non-protected endpoints
        if (context.Request.Path.StartsWithSegments("/api/auth") ||
            context.Request.Path.StartsWithSegments("/api/settings"))
        {
            await _next(context);
            return;
        }

        // Check for API key in header
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API key is missing");
            return;
        }

        var apiKey = apiKeyHeader.FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API key is missing");
            return;
        }

        // Validate API key
        try
        {
            var settings = await configurationService.GetSettingsAsync();
            var validApiKey = settings.ApiKeys.FirstOrDefault(k => k.Key == apiKey && k.IsActive);

            if (validApiKey == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid or inactive API key");
                return;
            }

            // Check IP binding if configured
            if (!string.IsNullOrEmpty(validApiKey.AllowedIp) &&
                context.Connection.RemoteIpAddress?.ToString() != validApiKey.AllowedIp)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Access denied from this IP address");
                return;
            }

            // Add API key info to context for use in controllers if needed
            context.Items["ApiKey"] = validApiKey;

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal server error");
        }
    }
}