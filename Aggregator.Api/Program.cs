using Aggregator.Core;
using Aggregator.Connectors;
using Aggregator.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;
using Aggregator.Connectors.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// HttpClientFactory + Resilience
builder.Services.AddHttpClient<OpenMeteoConnector>()
    .AddPolicyHandler(Resilience.ForHttp());
builder.Services.AddHttpClient<HackerNewsConnector>()
    .AddPolicyHandler(Resilience.ForHttp());
builder.Services.AddHttpClient<GitHubConnector>()
    .AddPolicyHandler(Resilience.ForHttp());

builder.Services.AddSingleton<IStats, InMemoryStats>();

builder.Services.AddHttpClient<IOpenMeteoConnector, OpenMeteoConnector>(static c =>
{
    c.BaseAddress = new Uri("https://api.open-meteo.com/v1/");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("ApiAggregator/1.0");
});
builder.Services.AddHttpClient<IGitHubConnector, GitHubConnector>(c =>
{
    c.BaseAddress = new Uri("https://api.github.com");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("api-aggregator-demo");
    c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    // προαιρετικά: c.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
}); 
builder.Services.AddHttpClient<IHackerNewsConnector, HackerNewsConnector>();

builder.Services.AddSingleton<IApiConnector, HnApiConnectorAdapter>();
builder.Services.AddSingleton<IApiConnector, GhApiConnectorAdapter>();
builder.Services.AddSingleton<IApiConnector, MeteoApiConnectorAdapter>();
builder.Services.AddMemoryCache();

builder.Services.AddScoped<AggregateService>();

var authEnabled = builder.Configuration.GetValue<bool>("Auth:Enabled");//jwt bearer 

builder.Services.AddEndpointsApiExplorer();

// αρχικο builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Aggregator", Version = "v1" }));
builder.Services.AddSwaggerGen(c =>
{
    if (authEnabled)
    {
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme.",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });
    }
});

if (authEnabled)
{
    // JWT Bearer (dev: user-jwts | prod: real IdP)
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Auth:Authority"]; // π.χ. user-jwts local authority
            options.Audience = builder.Configuration["Auth:Audience"];  // "api-aggregator"
            options.RequireHttpsMetadata = false; // μόνο για dev
            // options.TokenValidationParameters.ValidTypes = new[] { "at+jwt", "JWT" }; // αν χρειαστεί
        });

    builder.Services.AddAuthorization(options =>
    {
        // Ζητάμε scope για το aggregator
        options.AddPolicy("AggregatorReader",
            p => p.RequireClaim("scope", "aggregator.read"));
    });
}

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Endpoints
// Health & Swagger = ανοικτά
app.MapGet("/health", () => Results.Ok(new { ok = true })).AllowAnonymous();

// Προστατευμένο endpoint
app.MapGet("/api/aggregate", async (
    string? query, string? sources, int? limit, double? latitude, double? longitude,
    AggregateService svc, CancellationToken ct) =>
{
    var q = new AggregateQuery
    {
        Query = query,
        Source = sources,
        Take = limit,
        Latitude = latitude,
        Longitude = longitude
    };
    var result = await svc.GetAsync(q, ct);
    return Results.Ok(result);
})
.WithTags("Aggregator")
.WithOpenApi();

app.MapGet("/api/stats", (IStats stats) =>
{
    var snap = stats.Snapshot().Select(s => new {
        api = s.Api,
        total = s.Total,
        errors = s.Errors,
        avgMs = Math.Round(s.AvgMs, 2),
        buckets = new
        {
            lt100 = s.Buckets.GetType().GetProperty("_lt100")!.GetValue(s.Buckets),
            _100_249 = s.Buckets.GetType().GetProperty("_100_249")!.GetValue(s.Buckets),
            ge250 = s.Buckets.GetType().GetProperty("_ge250")!.GetValue(s.Buckets)
        }
    });
    return Results.Ok(snap);
}).WithName("Stats");

app.Run();