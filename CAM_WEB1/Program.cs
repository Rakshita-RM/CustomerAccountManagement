using Microsoft.EntityFrameworkCore;
using CAM_WEB1.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Services
builder.Services.AddControllers();

// Register DbContext with the connection string from appsettings.json
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Swagger configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. Configure HTTP Pipeline
//if (app.Environment.IsDevelopment())
//{
//	app.UseSwagger();
//	app.UseSwaggerUI();
//}
app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers(); // This makes your controllers visible in Swagger

app.Run();