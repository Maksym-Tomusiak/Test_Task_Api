using System.Text;
using Api.Modules;
using Api.OptionsSetup;
using Application;
using Application.Common.Jobs;
using Hangfire;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers(options =>
{
    options.Filters.Add<Api.Modules.Validators.ValidationFilter>();
});
builder.Services.AddApplication();
builder.Services.SetupServices();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Authentication and Authorization setup
var jwtSettings = builder.Configuration.GetSection("JwtOptions");

builder.Services
    .AddAuthentication(options =>
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
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]))
        };
    })
    .AddCookie();

builder.Services.ConfigureOptions<JwtOptionsSetup>();
builder.Services.ConfigureOptions<JwtBearerOptionsSetup>();

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(ConfigureApplication).Assembly);
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseHangfireDashboard();
RecurringJob.AddOrUpdate<UserCleanupJob>(
    "purge-deleted-users",
    job => job.Execute(CancellationToken.None),
    Cron.Hourly);

app.UseAuthentication();
app.UseAuthorization();

await app.InitializeDb();
app.MapControllers();

app.Run();