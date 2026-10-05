using Microsoft.EntityFrameworkCore;
using TrilhaNetAzureDesafio.Context;
using Azure.Data.Tables;
using TrilhaNetAzureDesafio.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<RHContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConexaoPadrao")));

builder.Services.AddControllers();
builder.Services.AddSingleton(_ => new TableClient(
    builder.Configuration.GetConnectionString("SAConnectionString"),
    builder.Configuration.GetConnectionString("AzureTableName")));
builder.Services.AddHostedService<PublicadorLogs>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
