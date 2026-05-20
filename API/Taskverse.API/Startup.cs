using CorrelationId;
using CorrelationId.DependencyInjection;
using log4net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Taskverse.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Npgsql;
using Npgsql.NameTranslation;
using System.Text;
using Taskverse.Api.Configuration;
using Taskverse.Api.Filters;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Orchestrators;
using Taskverse.Business.Configuration;
using Taskverse.Business.Interface;
using Taskverse.Business.Managers;
using Taskverse.Business.Orchestrators;
using Taskverse.Data.DataAccess;
using Taskverse.Business.Enums;
using Taskverse.Api.MicroServices;

namespace Taskverse.Api;

public class Startup
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(Startup));
    private readonly DateTime _startupTime = DateTime.UtcNow;
    private const string CorsPolicyName = "TaskverseCorsPolicy";

    private readonly IConfigurationBuilder _builder;

    public IConfigurationRoot Configuration { get; }

    public Startup(IWebHostEnvironment environment)
    {
        _builder = new ConfigurationBuilder()
            .SetBasePath(environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        Configuration = _builder.Build();

        var logConfigPath = Path.Combine(
            environment.ContentRootPath,
            Configuration["Logging:Log4NetConfigFileRelativePath"] ?? "Log4Net.config");

        log4net.Config.XmlConfigurator.Configure(
            LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly()!),
            new FileInfo(logConfigPath));

        Log.InfoFormat("Taskverse.Api Application Startup at: {0}", _startupTime);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        Log.DebugFormat("Configuring services... Startup time elapsed: {0}ms",
            (DateTime.UtcNow - _startupTime).TotalMilliseconds);

        services.AddDefaultCorrelationId(options =>
        {
            options.AddToLoggingScope = true;
            options.EnforceHeader = false;
            options.IgnoreRequestHeader = false;
            options.IncludeInResponse = true;
            options.RequestHeader = "X-CorrelationId";
            options.ResponseHeader = "X-CorrelationId";
            options.UpdateTraceIdentifier = false;
        });

        ConfigureConfigData(services);
        ConfigureAuthentication(services);
        ConfigureSwagger(services);
        ConfigureCors(services);
        ConfigureMvc(services);
        ConfigureHttpClients(services);
        ConfigureDatabase(services);
        ConfigureDependencyInjection(services);

        Log.DebugFormat("Services configured. Startup time elapsed: {0}ms",
            (DateTime.UtcNow - _startupTime).TotalMilliseconds);
    }

    public void Configure(WebApplication app, IWebHostEnvironment env)
    {
        Log.DebugFormat("Configuring HTTP request pipeline... Startup time elapsed: {0}ms",
            (DateTime.UtcNow - _startupTime).TotalMilliseconds);

        // Must be first — restores real client IP/protocol when behind a reverse proxy
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 3
        });

        app.UseRouting();

        // UseCors must come after UseRouting and before UseAuthentication/UseAuthorization
        app.UseCors(CorsPolicyName);

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseCorrelationId();

        app.UseSwagger(c =>
        {
            c.SerializeAsV2 = true;
        });

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("../swagger/v1/swagger.json", "Taskverse API");
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

        Log.DebugFormat("Application Startup complete. Total duration: {0}ms",
            (DateTime.UtcNow - _startupTime).TotalMilliseconds);
    }

    private void ConfigureAuthentication(IServiceCollection services)
    {
        var jwtSettings = Configuration.GetSection("JwtSettings").Get<Configuration.JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings section is missing from configuration.");

        if (string.IsNullOrWhiteSpace(jwtSettings.Key))
            throw new InvalidOperationException("JwtSettings.Key must be configured before starting the application.");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));

        services
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
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.Zero  // No tolerance on token expiry
                };
            });
    }

    private void ConfigureConfigData(IServiceCollection services)
    {
        services.AddSingleton<IConfigurationRoot>(Configuration);
        services.Configure<Configuration.JwtSettings>(Configuration.GetSection("JwtSettings"));
        services.Configure<CorsSettings>(Configuration.GetSection("TaskverseCorsPolicySettings"));
        services.Configure<MicroServiceSettings>(Configuration.GetSection("MicroServiceSettings"));
    }

    private void ConfigureHttpClients(IServiceCollection services)
    {
        services.AddHttpClient("TaskverseMicroServiceClient")
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            });
    }

    private void ConfigureDatabase(IServiceCollection services)
    {
        var connStr = Configuration.GetConnectionString("TaskverseDb")
            ?? throw new InvalidOperationException("Connection string 'TaskverseDb' is missing.");

        // Register the PostgreSQL user_status enum so Npgsql can serialize/deserialize it correctly.
        // NpgsqlNullNameTranslator preserves the UPPERCASE enum label names (PENDING_APPROVAL, ACTIVE, etc.)
        // matching the PostgreSQL enum values exactly. Without it, the default snake_case translator
        // would mangle ALL_CAPS names into p_e_n_d_i_n_g__a_p_p_r_o_v_a_l etc.

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStr);
        //dataSourceBuilder.MapEnum<UserStatus>(nameTranslator: new NpgsqlNullNameTranslator());
        var dataSource = dataSourceBuilder.Build();

        // Register dataSource as singleton to preserve enum mapping for application lifetime
        services.AddSingleton(dataSource);

        services.AddDbContext<TaskverseContext>(
            options => options.UseNpgsql(dataSource),
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Singleton);

        services.AddDbContextFactory<TaskverseContext>(options =>
            options.UseNpgsql(dataSource));
    }

    private void ConfigureDependencyInjection(IServiceCollection services)
    {
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Filters
        services.AddScoped<JwtTokenValidationFilter>();

        // Managers
        services.AddScoped<IUsersManager, UsersManager>();
        services.AddScoped<IAssessmentManager, AssessmentManager>();

        // MicroService layer
        services.AddScoped<IMicroServiceOrchestrator, MicroServiceOrchestrator>();

        // Business Orchestrators
        services.AddScoped<IAuthOrchestrator, AuthOrchestrator>();
        services.AddScoped<IUsersOrchestrator, UsersOrchestrator>();
        services.AddScoped<ISuperAdminOrchestrator, SuperAdminOrchestrator>();
        services.AddScoped<ICollegeAdminOrchestrator, CollegeAdminOrchestrator>();
    }

    private void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Taskverse API",
                Version = "v1",
                Description = "API for the Taskverse exam and assessment platform"
            });

            c.EnableAnnotations();

            // Add JWT bearer security definition so Swagger UI can send the Authorization header
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token. The 'Bearer ' prefix is added automatically."
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

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                c.IncludeXmlComments(xmlPath);
        });
    }

    private void ConfigureMvc(IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<TaskverseResponseHeaderFilter>();
            options.Filters.Add<AuditLoggingFilter>();
        })
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        });
    }

    private void ConfigureCors(IServiceCollection services)
    {
        var corsSettings = Configuration
            .GetSection("TaskverseCorsPolicySettings")
            .Get<CorsSettings>()
            ?? throw new InvalidOperationException("TaskverseCorsPolicySettings section is missing from configuration.");

        if (corsSettings.OriginUrls.Length == 0)
            throw new InvalidOperationException("TaskverseCorsPolicySettings.OriginUrls must contain at least one origin.");

        Log.InfoFormat("Configuring CORS policy with {0} origin(s): {1}",
            corsSettings.OriginUrls.Length,
            string.Join(", ", corsSettings.OriginUrls));

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                policy.WithOrigins(corsSettings.OriginUrls);

                // Exposed headers — always include the fixed API headers plus any configured ones
                var exposedHeaders = corsSettings.ExposedHeaders
                    .Union(["X-Pagination", TaskverseResponseHeaderFilter.ResponseHeaderName])
                    .Distinct()
                    .ToArray();
                policy.WithExposedHeaders(exposedHeaders);

                // Allowed methods
                if (corsSettings.AllowedMethods.Length == 0 || corsSettings.AllowedMethods.Contains("*"))
                    policy.AllowAnyMethod();
                else
                    policy.WithMethods(corsSettings.AllowedMethods);

                // Allowed headers
                if (corsSettings.AllowedHeaders.Length == 0 || corsSettings.AllowedHeaders.Contains("*"))
                    policy.AllowAnyHeader();
                else
                    policy.WithHeaders(corsSettings.AllowedHeaders);

                // Credentials
                if (corsSettings.AllowCredentials)
                    policy.AllowCredentials();
                else
                    policy.DisallowCredentials();
            });
        });
    }
}
