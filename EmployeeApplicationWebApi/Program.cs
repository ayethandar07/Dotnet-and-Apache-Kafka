using EmployeeApplicationWebApi.Database;
using EmployeeApplicationWebApi.Models;
using EmployeeApplicationWebApi.Services.Consumer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("KafkaSettings"));

builder.Services.AddDbContext<EmployeeDbContext>(op => op.UseInMemoryDatabase("EmployeeDb"));

builder.Services.AddDbContext<EmployeeReportDbContext>(op =>
    op.UseSqlServer("Data Source=DESKTOP-MEOFS93; Initial Catalog=Consumer; User Id = sa; Password = sasa; Integrated Security=True; TrustServerCertificate=true;"), ServiceLifetime.Singleton);
builder.Services.AddHostedService<Worker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
