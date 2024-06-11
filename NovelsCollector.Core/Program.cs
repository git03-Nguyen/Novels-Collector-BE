using Microsoft.AspNetCore.ResponseCompression;
using NovelsCollector.Core.Exceptions;
using NovelsCollector.Core.Utils;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddLogging();

// Add services to the container
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddExceptionHandler<MyExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddPlugins();

// Add CORS for frontend: http://localhost:3000         TODO: move to config
var corsName = "Frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsName, builder =>
    {
        builder.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add compression for responses

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UsePlugins();

app.UseCors(corsName);

app.UseHttpsRedirection();


app.UseAuthorization();

app.MapControllers();

app.UseStatusCodePages(async context =>
    throw new NotFoundException("Not found!"));

app.Run();
