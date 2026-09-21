using Microsoft.OpenApi.Models;
using SAFC.Expense.Infrastructure;
using SAFC.Expense.Application;
using SAFC.Expense.Api.Middlewares;
using SAFC.Expense.Api.Options;
var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddInfrastructure(builder.Configuration);
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
});

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

app.UseAuthorization();

app.MapControllers();

app.Run();
