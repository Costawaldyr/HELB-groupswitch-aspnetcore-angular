using GroupSwitch.Api.BackgroundServices;
using GroupSwitch.Api.Mapping;
using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using GroupSwitch.Business.Scraping;
using GroupSwitch.Business.Services;
using GroupSwitch.Business;
using GroupSwitch.Data;
using GroupSwitch.Data.Interfaces;
using GroupSwitch.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDataServices(builder.Configuration);

builder.Services.AddBusinessServices();

builder.Services.AddHostedService<ImportBackgroundService>();

builder.Services.Configure<HyperplanningSettings>(
    builder.Configuration.GetSection(HyperplanningSettings.SectionName));

builder.Services.AddSingleton(provider =>
    new SessionTimeCalculator(provider.GetRequiredService<IOptions<HyperplanningSettings>>().Value.FirstMonday));

builder.Services.AddSingleton<IImportDtoMapper, ImportDtoMapper>();



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