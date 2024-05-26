using NovelsCollector.Core.Extensions;
using NovelsCollector.Core.Models.Plugins;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddPluginManager();

//builder.Services.Configure<PluginsDbSettings>(builder.Configuration.GetSection("PluginsDatabase"));
//builder.Services.AddSingleton<PluginsService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UsePluginManager();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
