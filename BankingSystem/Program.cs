
using BankingSystem.Repository.Interface;
using BankingSystem.Repository.Repo;
using BankingSystem.Services.Interface;
using BankingSystem.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Data;
using Microsoft.OpenApi.Models;
using BankingSystem.RBAC;
using Microsoft.AspNetCore.HttpOverrides;

namespace BankingSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Bind to the port provided by the hosting environment (Render sets PORT)
            var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
            builder.WebHost.UseUrls($"http://*:{port}");

            // 1. Add Controllers
            builder.Services.AddControllers();

            // 2️ Database connection (Dapper - manual DB)
            builder.Services.AddScoped<IDbConnection>(sp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                return new System.Data.SqlClient.SqlConnection(connectionString);
            });

            // 3.Register Repositories
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IRolesRepository, RolesRepository>();
            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

            // 4. Register Services
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddScoped<IAuthService, AuthService>();


            var jwtSecret = builder.Configuration["Jwt:Key"]
                ?? Environment.GetEnvironmentVariable("Jwt__SecretKey")
                ?? throw new InvalidOperationException("JWT Secret Key is not configured");
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!))
                };
            });

            // Add services to the container.
            builder.Services.AddAuthorization();

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("Redis");
                options.InstanceName = "BankingSystem_";
            });

            // When running behind a proxy/load-balancer (Render), forward the original
            // scheme so middleware like HTTPS redirection can be aware of the original request.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                // Clear the known networks/proxies so forwarded headers are accepted from the proxy
                // (on some hosts you may want to restrict this).
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            //configure swagger to use the jwt bearer token 
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Banking System API",
                    Version = "v1"
                });
                // Add JWT Authentication to Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Enter JWT token like this: Bearer {your token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
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
                        new string[] {}
                    }
                });
            });

            var app = builder.Build();


            app.UseAuthentication(); // must come before authorization
            app.UseAuthorization();

            // Configure the HTTP request pipeline.


            app.UseSwagger();
            app.UseSwaggerUI();



            // Apply forwarded headers (should be before authentication/https redirection)
            app.UseForwardedHeaders();

            // Only use HTTPS redirection in Development or when an HTTPS port is explicitly configured.
            // Render terminates TLS at the load balancer and forwards to the container over HTTP,
            // so calling HTTPS redirection in Production without proper configuration can raise the
            // "Failed to determine the https port for redirect" warning. Keep it off in Production.
            if (app.Environment.IsDevelopment() || !string.IsNullOrEmpty(app.Configuration["ASPNETCORE_HTTPS_PORT"]))
            {
                app.UseHttpsRedirection();
            }

            // Add a simple root endpoint to avoid 404 at '/'. This makes a basic health/check response
            // and helps when visiting the site URL in a browser.
            app.MapGet("/", () => Results.Text("Banking System API is running."));

            app.MapControllers();

            app.Run();
        }
    }
}
