using GritTrack.Api.Configuration;
using GritTrack.Api.Data;
using GritTrack.Api.Repositories;
using GritTrack.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// the SQL password lives in .env (gitignored); load it before the configuration is built
DotEnvLoader.Load(Directory.GetCurrentDirectory());

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    // makes a validation error's key match the camelCase JSON field name
    // instead of the C# property name, so the client can actually match
    // the error to the field it sent
    options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
});

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// appsettings.json "DataSource" picks where courses live. Nothing above the
// repository changes either way: the service only ever sees ICourseRepository.
var dataSource = builder.Configuration["DataSource"] ?? DataSources.Json;
switch (dataSource)
{
    case DataSources.Json:
        // one JSON file backs the whole app, so the repo has to be a singleton.
        // a new instance per request would just mean every request fighting over
        // its own copy of the file
        builder.Services.AddSingleton<ICourseRepository, JsonCourseRepository>();
        break;

    case DataSources.SqlServer:
        // one DbContext per request, so the repo is scoped too
        builder.Services.AddDbContext<GritTrackDbContext>(options =>
            options.UseSqlServer(SqlConnectionStringFactory.Build(builder.Configuration)));
        builder.Services.AddScoped<ICourseRepository, SqlCourseRepository>();
        break;

    default:
        throw new InvalidOperationException(
            $"DataSource '{dataSource}' is not valid. Use '{DataSources.Json}' or '{DataSources.SqlServer}'.");
}

builder.Services.AddScoped<ICourseService, CourseService>();

// Auth:Enabled switches the whole login system on or off. Off means anyone can
// write (handy for the "auth off" demo). On means every create, update and delete
// needs a token from POST /api/auth/login, and GETs stay public either way.
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

if (authOptions.Enabled)
{
    // stop right now, with a clear message, instead of failing on the first login
    ValidateAuthSettings(authOptions);

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = authOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = authOptions.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SigningKey!)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy(AuthPolicies.WriteAccess, policy => policy.RequireAuthenticatedUser());
}
else
{
    // same policy name, but everyone passes, so the controller doesn't need to know which mode we're in
    builder.Services.AddAuthorizationBuilder()
        .AddPolicy(AuthPolicies.WriteAccess, policy => policy.RequireAssertion(_ => true));
}

var app = builder.Build();

// creates or updates the tables before the first request
if (dataSource == DataSources.SqlServer)
    await ApplyMigrationsAsync(app);

// one place that turns an unhandled exception into a proper ProblemDetails
// response, instead of a try/catch in every action
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// no token or a bad one on a protected action = 401
if (authOptions.Enabled)
    app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// the two settings that change how the app behaves, stated once at startup.
// (only the names: never the connection string, password or signing key)
app.Logger.LogInformation("Data source: {DataSource}", dataSource);
app.Logger.LogInformation("Authentication: {AuthState}", authOptions.Enabled ? "ON (writes need a token)" : "OFF (writes are open)");

app.Run();

/// <summary>
/// Refuses to start with auth on but the secrets missing. The secrets live in dotnet user-secrets
/// (only loaded when the environment is Development), never in a file that goes to git.
/// </summary>
static void ValidateAuthSettings(AuthOptions authOptions)
{
    const int minimumSigningKeyLength = 32;
    var problems = new List<string>();

    if (string.IsNullOrWhiteSpace(authOptions.SigningKey) || authOptions.SigningKey.Length < minimumSigningKeyLength)
        problems.Add($"Auth:SigningKey is missing or shorter than {minimumSigningKeyLength} characters");
    if (string.IsNullOrWhiteSpace(authOptions.DemoPassword))
        problems.Add("Auth:DemoPassword is missing");

    if (problems.Count == 0)
        return;

    throw new InvalidOperationException(
        $"Auth:Enabled is true but {string.Join(" and ", problems)}. " +
        "Set them with: dotnet user-secrets set \"Auth:SigningKey\" \"<long random text>\" " +
        "and dotnet user-secrets set \"Auth:DemoPassword\" \"<your demo password>\" " +
        "(run from the GritTrack.Api folder), or set Auth:Enabled to false.");
}

/// <summary>
/// Creates or updates the database tables when the app starts. SQL Server can take a few
/// seconds after the container says "up", so this tries a few times before giving up.
/// </summary>
static async Task ApplyMigrationsAsync(WebApplication app)
{
    const int maximumAttempts = 6;

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GritTrackDbContext>();
            await context.Database.MigrateAsync();
            return;
        }
        catch (Exception exception) when (attempt < maximumAttempts)
        {
            app.Logger.LogWarning("Database not ready yet (attempt {Attempt} of {Maximum}): {Reason}",
                attempt, maximumAttempts, exception.GetType().Name);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
