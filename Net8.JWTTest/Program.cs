using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Net8.JWTTest.Data;
using Net8.JWTTest;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<Net8JWTTestContext>(options =>
    options.UseInMemoryDatabase("Net8JWTTest")
    );

var key = "dh9h3297c8bvwcc9je0j01jcm0eaposc$@@q0io9kw0-1";

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();


// Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "your_issuer",
            ValidAudience = "your_audience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add JWT authentication
app.UseAuthentication();
app.UseAuthorization();

//Add Init User
app.MapPost("/api/init", (Net8JWTTestContext db) =>
{
    db.User.Add(new User
    {
        Id = 1,
        Name = "admin",
        Password = "admin",
    });

    db.User.Add(new User
    {
        Id = 2,
        Name = "user",
        Password = "user",
    });

    db.SaveChanges();

    return Results.Ok("Init User Success");
});

//Web API:Generate JWT Token
app.MapPost("/api/token", (Net8JWTTestContext db, string? username, string? password) =>
{
#if DEBUG
    username = "admin";
    password = "admin";
#endif

    //JwtTestContext
    var exist = db.User.Any(u => u.Name == username);

    if (exist)
    {
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        var tokenOptions = new JwtSecurityToken(
            issuer: "your_issuer",
            audience: "your_audience",
            claims: new List<Claim> {
                new Claim(ClaimTypes.Name, username),
                new Claim("company", "your_company")
            },
            expires: DateTime.Now.AddMinutes(5),
            signingCredentials: signinCredentials
        );


        var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        return Results.Ok($"Bearer {tokenString}");
    }
    else
    {
        return Results.BadRequest("Invalid username or password");
    }
});

//Web API:Authorize
app.MapGet("/api/authorize", (ClaimsPrincipal user) =>
{
    var username = user.Identity?.Name;
    var company = user.Claims.FirstOrDefault(c => c.Type == "company")?.Value;

    return Results.Ok($"Hello {username}, Company: {company}");
}).RequireAuthorization();

app.Run();

//user class
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Password { get; set; }
}

