using DataAccess.Context;
using DataAccess.Entities;
using Interfaces.IManagers;
using Interfaces.IRepository;
using Managers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Serilog;
using DataAccess.Repository;
using EcomComplete.Mappings;
var builder = WebApplication.CreateBuilder(args);

//logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("Logs/logs.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
// Add services to the container.
builder.Host.UseSerilog();

//db
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name="Authorization",
        Type=Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme="Bearer",
        BearerFormat="JWT",
        In=Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description= "Enter 'Bearer' followed by space and JWT token"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {{
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Reference= new Microsoft.OpenApi.Models.OpenApiReference
            {
                Type=Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                Id="Bearer"
            }
        },
        new string[]{}
        }
    });
});
//identity
builder.Services.AddIdentity<AppUser,IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

//authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;

}).AddCookie(options =>
{
    options.LoginPath = "/api/auth/login";
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    };

    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    options.SlidingExpiration = true;
    options.Cookie.Name = "PolicyBasedAuthCookie";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
//builder.Services.AddCors(options =>
//  {
//        options.AddPolicy("AllowFrontend", policy =>
//          policy.WithOrigins("http://localhost:7117","http://localhost:5059") // Add YOUR Swagger/Frontend URLs
//                .AllowAnyHeader()
//                .AllowAnyMethod()
//                .AllowCredentials());
//  });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
       // options.AddPolicy("MinimumAgePolicy", policy => policy.RequireClaim("Age", "18"));
        options.AddPolicy("MinimumAgePolicy", policy =>
        policy.RequireAssertion(context =>
    {
        var ageClaim = context.User.FindFirst(c => c.Type == "Age");
        if (ageClaim != null && int.TryParse(ageClaim.Value, out int age))
        {
            return age >= 18;
        }
        return false;
    }));
        options.AddPolicy("CustomerPolicy", policy => policy.RequireRole("Customer"));
    });
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IAuthManager, AuthManager>();
builder.Services.AddScoped<IProductManager, ProductManager>();
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddMemoryCache();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
//app.UseCors("AllowFrontend");

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
