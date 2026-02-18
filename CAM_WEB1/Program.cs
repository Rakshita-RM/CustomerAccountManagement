using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.EntityFrameworkCore;
using CAM_WEB1.Data;

var builder = WebApplication.CreateBuilder(args);

// --------------------

// Controllers

// --------------------

builder.Services.AddControllers();

// --------------------

// JWT Authentication

// --------------------
 
var jwtSettings = builder.Configuration.GetSection("Jwt");

var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication("Bearer")

.AddJwtBearer("Bearer", options =>

{

	options.TokenValidationParameters = new TokenValidationParameters

	{

		ValidateIssuer = true,

		ValidateAudience = true,

		ValidateLifetime = true,

		ValidateIssuerSigningKey = true,

		ValidIssuer = jwtSettings["Issuer"],

		ValidAudience = jwtSettings["Audience"],

		IssuerSigningKey = new SymmetricSecurityKey(key),

		ClockSkew = TimeSpan.Zero

	};

});

builder.Services.AddAuthorization();

// --------------------

// Swagger + Authorize Button

// --------------------

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>

{

	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme

	{

		Name = "Authorization",

		Type = SecuritySchemeType.Http,

		Scheme = "Bearer",

		BearerFormat = "JWT",

		In = ParameterLocation.Header,

		Description = "Enter: Bearer {your JWT token}"

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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --------------------

// Middleware

// --------------------

app.UseSwagger();

app.UseSwaggerUI();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.UseStaticFiles();

app.Run();

