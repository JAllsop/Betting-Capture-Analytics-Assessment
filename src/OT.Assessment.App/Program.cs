using MassTransit;
using Microsoft.AspNetCore.Diagnostics;
using OT.Assessment.App.Data;
using StackExchange.Redis;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connStr = builder.Configuration.GetConnectionString("cache") ?? "localhost";
    return ConnectionMultiplexer.Connect(connStr);
});

builder.Services.AddControllers();

// Database Connection
builder.Services.AddScoped<System.Data.IDbConnection>(sp =>
{
    var connStr = builder.Configuration.GetConnectionString("OT-Assessment-DB");
    return new Microsoft.Data.SqlClient.SqlConnection(connStr);
});

// Repositories & Services
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddHostedService<CacheWarmerService>();

// Audit & Comparison Services
builder.Services.AddScoped<ITestComparisonService, TestComparisonService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckl
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        cfg.Host(connectionString);
        cfg.ConfigureEndpoints(context);
    });
});

builder.AddServiceDefaults();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            // In a real world application we would hide error details, as this is a demo/assessment we want to see the error details in the response
            //Detail = app.Environment.IsDevelopment() ? exception?.Message : "Contact support.",
            Detail = exception?.Message ?? "An unexpected error occurred",
            Instance = context.Request.Path
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

// As this is an assessment/demo we always want to have Swagger available
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI(opts =>
{
    opts.EnableTryItOutByDefault();
    opts.DocumentTitle = "OT Assessment App";
    opts.DisplayRequestDuration();
});
//}

// Redirect root URL to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger"))
   .ExcludeFromDescription();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
