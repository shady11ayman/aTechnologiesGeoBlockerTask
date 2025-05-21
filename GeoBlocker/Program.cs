using GeoBlocker.Application.Interfaces;
using GeoBlocker.Application.Services;
using GeoBlocker.Infrastructure.BackgroundServices;
using GeoBlocker.Infrastructure.Geolocation;
using GeoBlocker.Infrastructure.Repositories;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;


var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Register Infrastructure
builder.Services.AddSingleton<IBlockedStore, InMemoryBlockedStore>();
builder.Services.AddHttpClient<IGeoService, IpApiService>();

// Register Application services
builder.Services.AddScoped<BlockCountryService>();
// (Add more use-case services as needed)

// Register background service
builder.Services.AddHostedService<TemporalBlockCleanupService>();

// Configure IpApi from appsettings.json
builder.Services.Configure<IpApiConfig>(builder.Configuration.GetSection("IpApi"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.MapControllers();
app.Run();