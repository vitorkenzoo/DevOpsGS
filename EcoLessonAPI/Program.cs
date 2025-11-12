using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using EcoLessonAPI.Data;
using EcoLessonAPI.Services;

// (Importação necessária para o UseSqlServer)
using Microsoft.EntityFrameworkCore; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger (Seu código original, 100% correto)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EcoLesson API",
        Version = "v1",
        Description = "API RESTful para Plataforma de Requalificação Profissional - Versão 1..."
        // (O resto da sua descrição do Swagger)
    });
    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "EcoLesson API",
        Version = "v2",
        Description = "API RESTful para Plataforma de Requalificação Profissional - Versão 2..."
        // (O resto da sua descrição do Swagger)
    });

    // Configure JWT in Swagger (Seu código original, 100% correto)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header...",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

//
// -----------------------------------------------------------------
// MUDANÇA 1: USANDO SQL SERVER (AZURE SQL)
// -----------------------------------------------------------------
//
// Configure Entity Framework Core com SQL Server (Azure SQL)
builder.Services.AddDbContext<EcoLessonDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//
// -----------------------------------------------------------------
// MUDANÇA 2: ADICIONANDO OS SERVIÇOS DE AUTENTICAÇÃO QUE FALTAVAM
// -----------------------------------------------------------------
//

// Configure JWT Authentication (Seu código original, que estava faltando)
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLongForSecurity!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "EcoLessonAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "EcoLessonAPI";

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
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(5) 
    };
});

// ****** A LINHA QUE CAUSOU O ERRO *******
// Esta linha precisa existir ANTES de 'var app = builder.Build()'
builder.Services.AddAuthorization();
// ****** FIM DA LINHA DO ERRO *******


// Register custom services (Seu código original, que estava faltando)
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRecomendacaoService, RecomendacaoService>();

//
// -----------------------------------------------------------------
// MUDANÇA 3: TAG DO HEALTH CHECK ATUALIZADA
// -----------------------------------------------------------------
//
builder.Services.AddHealthChecks()
    .AddDbContextCheck<EcoLessonDbContext>("sql-db"); // TAG ATUALIZADA

// Configure Logging e OpenTelemetry (Seu código original, 100% correto)
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddConsoleExporter())
    .WithMetrics(metricsProviderBuilder =>
        metricsProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddConsoleExporter());

builder.Logging.AddOpenTelemetry(options =>
{
    options.AddConsoleExporter();
});

// =================================================================
var app = builder.Build();
// =================================================================

// Configure the HTTP request pipeline
// (Seu código original, 100% correto)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EcoLesson API v1");
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "EcoLesson API v2");
    c.RoutePrefix = string.Empty; 
});

app.UseHttpsRedirection();

// (Seu código original de Health Checks, 100% correto)
app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            // ... (resto do seu código)
        });
        await context.Response.WriteAsync(result);
    }
});
app.MapHealthChecks("/readyz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("sql-db"), // <-- TAG ATUALIZADA
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            // ... (resto do seu código)
        });
        await context.Response.WriteAsync(result);
    }
});

//
// -----------------------------------------------------------------
// MUDANÇA 4: ORDEM CORRETA DOS SERVIÇOS
// -----------------------------------------------------------------
//
// UseAuthentication() DEVE vir ANTES de UseAuthorization()
app.UseAuthentication();
app.UseAuthorization(); // (Esta é a linha que estava dando o erro)

app.MapControllers();

app.Run();

// Make Program class accessible for testing
public partial class Program { }