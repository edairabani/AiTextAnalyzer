using AiTextAnalyzer.Api.Middleware;
using AiTextAnalyzer.Application.AI;
using AiTextAnalyzer.Application.Chunks;
using AiTextAnalyzer.Application.Ingest;
using AiTextAnalyzer.Application.Rag;
using AiTextAnalyzer.Application.Search;
using AiTextAnalyzer.Application.Vector;
using AiTextAnalyzer.Infrastruction.AI;
using AiTextAnalyzer.Infrastruction.Caching;
using AiTextAnalyzer.Infrastruction.Chunks;
using AiTextAnalyzer.Infrastruction.Data;
using AiTextAnalyzer.Infrastruction.Rag;
using AiTextAnalyzer.Infrastruction.Vector;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Events;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration);
      //.Enrich.FromLogContext();
});



builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();




builder.Services.AddScoped<IRagService, AiTextAnalyzer.Application.Rag.RagService>();

//builder.Services.AddScoped<IEmbeddingProvider, OpenAIEmbeddingProvider>();
//builder.Services.AddScoped<IChatProvider, OpenAIChatProvider>();
builder.Services.AddScoped<IVectorStore, PgVectorStore>();
builder.Services.AddScoped<IRagLogRepository, EfRagLogRepository>();

builder.Services.AddScoped<IngestDocument>();
builder.Services.AddScoped<SearchChunks>();

builder.Services.AddScoped<StoreChunk>();
builder.Services.AddScoped<IngestDocument>();
builder.Services.AddScoped<IChunkQuery, EfChunkQuery>();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();

builder.Services.AddDbContext<VectorDbContext>(opt =>
    opt.UseNpgsql(
        builder.Configuration.GetConnectionString("VectorDb"),
        o => o.UseVector()
    ));
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.Timeout = TimeSpan.FromSeconds(30);

    var apiKey = builder.Configuration["OpenAI:ApiKey"]!;
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
})
.AddPolicyHandler(GetRetryPolicy());
builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", p =>
        p.WithOrigins("http://localhost:5173")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

var jwtKey = builder.Configuration["Jwt:Key"];
var key = Encoding.UTF8.GetBytes(jwtKey!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Gib dein JWT Token ein: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    // Wenn Limit erreicht: 429
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Policy: pro User (wenn eingeloggt) sonst pro IP
    options.AddPolicy("rag", httpContext =>
    {
        var user = httpContext.User?.Identity?.IsAuthenticated == true
            ? httpContext.User.Identity!.Name ?? "user"
            : httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: user,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,                 // 20 Requests
                Window = TimeSpan.FromMinutes(1), // pro Minute
                QueueLimit = 2,                   // kleine Warteschlange
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });
});

builder.Services.AddAuthorization();


# region AI Provider





 var provider = builder.Configuration["AI:Provider"] ?? "OpenAI";

// HttpClients
builder.Services.AddHttpClient("OpenAI", c => { c.BaseAddress = new Uri("https://api.openai.com/"); });

builder.Services.AddHttpClient("Ollama", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["AI:Ollama:BaseUrl"] ?? "http://localhost:11434");
});

builder.Services.AddHttpClient("AzureOpenAI", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["AI:AzureOpenAI:Endpoint"]!);
    c.DefaultRequestHeaders.Add("api-key", builder.Configuration["AI:AzureOpenAI:ApiKey"]!);
});

// Provider switch
if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IChatProvider>(sp =>
        new OllamaChatProvider(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Ollama"),
                               sp.GetRequiredService<IConfiguration>()));
    builder.Services.AddScoped<IEmbeddingProvider>(sp =>
        new OllamaEmbeddingProvider(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Ollama"),
                                    sp.GetRequiredService<IConfiguration>()));
}
else if (provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IChatProvider>(sp =>
        new AzureOpenAIChatProvider(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AzureOpenAI"),
                                    sp.GetRequiredService<IConfiguration>()));
    builder.Services.AddScoped<IEmbeddingProvider>(sp =>
        new AzureOpenAIEmbeddingProvider(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AzureOpenAI"),
                                         sp.GetRequiredService<IConfiguration>(),
                                         sp.GetRequiredService<ILogger<AzureOpenAIEmbeddingProvider>>()));
}
else
{
    builder.Services.AddScoped<OpenAIEmbeddingProvider>();
    builder.Services.AddScoped<IEmbeddingProvider>(sp =>
    new CachedEmbeddingProvider(
        sp.GetRequiredService<OpenAIEmbeddingProvider>(),
        sp.GetRequiredService<ICacheService>()
    ));

    builder.Services.AddScoped<IChatProvider, OpenAIChatProvider>();
}
#endregion
var app = builder.Build();
// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.UseCors("dev");
app.UseMiddleware<ExceptionMiddleware>();

app.Run();
app.UseSerilogRequestLogging(options =>
{
    // Optional: mehr Details im Request Log
    //options.EnrichDiagnosticContext = (diag, http) =>
    //{
    //    diag.Set("TraceId", http.TraceIdentifier);
    //    diag.Set("RemoteIP", http.Connection.RemoteIpAddress?.ToString());
    //    diag.Set("User", http.User?.Identity?.Name);
    //};
});

Log.Information("Starting up");



static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // 5xx, 408, network
        .OrResult(r => r.StatusCode == (HttpStatusCode)429) // rate limit
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))
        );
}