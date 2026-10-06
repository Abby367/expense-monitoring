using Microsoft.OpenApi.Models;
using SAFC.Expense.Infrastructure;
using SAFC.Expense.Application;
using SAFC.Expense.Api.Middlewares;
using SAFC.Expense.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using SAFC.Expense.Api.Swagger;


var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
    LoadNearestEnvFile(builder.Environment.ContentRootPath);
// Not redundant with CreateBuilder's own provider. LoadNearestEnvFile runs after
// CreateBuilder, so that provider has already snapshotted the environment; only a
// provider added here sees what the .env file just set.
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddInfrastructure(builder.Configuration);
var clientId = builder.Configuration["Microsoft:ClientId"];

if (string.IsNullOrWhiteSpace(clientId))
    throw new InvalidOperationException(
        "Entra application (client) id not found. "
        + "Copy .env.example to .env and set Microsoft__ClientId.");

if (!Guid.TryParse(clientId, out _))
    throw new InvalidOperationException(
        "Microsoft__ClientId is not a GUID. An Entra application id always is, so this "
        + "is most likely the client secret pasted into the wrong key. Rotate it if so.");

clientId = clientId.Trim();


builder.Services.AddApplication();
builder.Services.Configure<UtilitiesOptions>(
builder.Configuration.GetSection(UtilitiesOptions.SectionName));
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();


builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SAFC Expense Monitoring API",
        Version = "v1",
        Description = "Expense request, approval, and liquidation endpoints."
    });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token only — Swagger adds the Bearer prefix."
    });

    options.OperationFilter<BearerSecurityOperationFilter>();


});
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        configureJwtBearerOptions: options =>
        {
            options.MapInboundClaims = false;
        },
        configureMicrosoftIdentityOptions: options =>
        {
            options.ClientId = clientId;
            options.Instance = "https://login.microsoftonline.com/";
            options.TenantId = "common";
        });

builder.Services.AddAuthorization();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SAFC Expense API v1");
        options.DocumentTitle = "SAFC Expense Monitoring API";
    });
}


app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void LoadNearestEnvFile(string contentRootPath)
{
    var directory = new DirectoryInfo(contentRootPath);

    for (var depth = 0; directory is not null && depth < 4; depth++)
    {
        var envPath = Path.Combine(directory.FullName, ".env");

        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath, new DotNetEnv.LoadOptions(clobberExistingVars: false));
            return;
        }

        directory = directory.Parent;
    }
}

