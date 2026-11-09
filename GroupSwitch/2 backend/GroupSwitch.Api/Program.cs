using GroupSwitch.Api.BackgroundServices;
using GroupSwitch.Api.Mapping;
using GroupSwitch.Business;
using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Data;


var builder = WebApplication.CreateBuilder(args);


// Data services 
builder.Services.AddDataServices(builder.Configuration);

// Business services
builder.Services.AddBusinessServices();

// Background services
builder.Services.AddHostedService<ImportBackgroundService>();

// Configuration
builder.Services.AddOptions<HyperplanningSettings>()
    .Bind(builder.Configuration.GetSection(HyperplanningSettings.SectionName))
    .ValidateOnStart();

// Mapping
builder.Services.AddSingleton<IImportDtoMapper, ImportDtoMapper>();
builder.Services.AddSingleton<ScheduleDtoMapper>();



// Add services to the container.
builder.Services.AddControllers();
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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


public partial class Program { }
