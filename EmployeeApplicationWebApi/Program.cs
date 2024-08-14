using EmployeeApplicationWebApi.Database;
using EmployeeApplicationWebApi.Models;
using EmployeeApplicationWebApi.Services.Consumer;
using EmployeeApplicationWebApi.Services.Producer;
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
    op.UseSqlServer(builder.Configuration.GetConnectionString("Conn")), ServiceLifetime.Singleton);

builder.Services.AddHostedService<Worker>();

builder.Services.AddTransient<KafkaProducerService>();

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
