using System.Reflection;
using System.Text.Json.Serialization;
using SimQ.DAL;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using SimQ.Core;
using SimQ.Domain.Models.DBSettings;
using Swashbuckle.AspNetCore.SwaggerGen;

var assembly = Assembly.GetEntryAssembly();
var assemblyName = assembly?.GetName();
var projectName = assemblyName?.Name;
var version = assemblyName?.Version?.ToString();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.SetBasePath(AppContext.BaseDirectory);

ConfigureServices(builder.Services);

var webApplication = builder.Build();
webApplication.Logger.LogInformation("Версия приложения {Version}", version);
await ConfigureApplication(webApplication);
return;

void ConfigureServices(IServiceCollection services)
{
    services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DatabaseSettings"));
    
    services.AddEndpointsApiExplorer();
    services.AddMvcCore()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        })
        .AddApiExplorer();

    services
        .AddFactories()
        .AddConverters()
        .AddRepositories()
        .AddServices();

    AddSwaggerGen(services);
    AddCors(services);

    services.AddHealthChecks();
}

void AddCors(IServiceCollection services)
{
    services.AddCors(options => options.AddDefaultPolicy(b => ConfigureCorsPolicy(b, builder.Configuration)));
}

void ConfigureCorsPolicy(CorsPolicyBuilder corsPolicyBuilder, IConfiguration configuration)
{
    var origins = configuration.GetSection("AllowedOrigins").Get<string[]>();
    if (origins?.Any() != true)
        return;

    corsPolicyBuilder.WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod();
}

void AddSwaggerGen(IServiceCollection services)
{
    services.AddSwaggerGen(c =>
    {
        c.CustomSchemaIds(type => type.ToString());
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = projectName,
            Version = "v1",
            Description = $"SimQ {version}",
        });

        c.SupportNonNullableReferenceTypes();
        IncludeXmlComments(c);
    });
}

void IncludeXmlComments(SwaggerGenOptions options)
{
    var baseDirectory = AppContext.BaseDirectory;
    var xmlPaths = new List<string>
    {
        Path.Combine(baseDirectory, $"{projectName}.xml"),
    };

    xmlPaths.ForEach(s =>
    {
        if (File.Exists(s))
            options.IncludeXmlComments(s, true);
    });
}

async Task ConfigureApplication(WebApplication app)
{
    // Configure the HTTP request pipeline.
    if (app.Environment.EnvironmentName == "Development")
    {
        app.UseDeveloperExceptionPage();

        app.UseSwagger(c => c.RouteTemplate = "swagger/{documentName}/swagger.json");
        app.UseSwaggerUI(c => c.SwaggerEndpoint("v1/swagger.json", $"{projectName} v1"));
    }

    app.UseExceptionHandler("/error");

    app.UseRouting();
    app.UseCors();

    app.MapControllers();
    app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = x => x.Tags.Contains("live") })
        .AllowAnonymous();
    
    app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = x => x.Tags.Contains("ready") })
        .AllowAnonymous();

    SetThreads();

    await app.RunAsync();
}

void SetThreads()
{
    const int threads = 2500;

    ThreadPool.GetMaxThreads(out var workerThreads, out var ioCompletionThreads);
    ThreadPool.SetMaxThreads(Math.Max(threads, workerThreads), Math.Max(threads, ioCompletionThreads));
    ThreadPool.SetMinThreads(threads, threads);
}



public partial class Program
{
}