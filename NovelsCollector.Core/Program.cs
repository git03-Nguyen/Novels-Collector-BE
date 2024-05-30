using NovelsCollector.Core.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddPluginManager();

//builder.Services.Configure<PluginsDbSettings>(builder.Configuration.GetSection("PluginsDatabase"));
//builder.Services.AddSingleton<PluginsService>();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UsePluginManager();

app.UseCors(corsName);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
