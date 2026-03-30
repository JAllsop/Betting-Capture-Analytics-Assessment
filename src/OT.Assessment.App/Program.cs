using MassTransit;
using OT.Assessment.App.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// Handles the conversion of GUIDs stored as strings in the database (set to string to as tester references them as such)
Dapper.SqlMapper.AddTypeHandler(new GuidAsStringHandler()); 
builder.Services.AddScoped<System.Data.IDbConnection>(sp =>
{
    var connStr = builder.Configuration.GetConnectionString("OT-Assessment-DB");
    return new Microsoft.Data.SqlClient.SqlConnection(connStr);
});
builder.Services.AddScoped<OT.Assessment.App.Services.ITestComparisonService, OT.Assessment.App.Services.TestComparisonService>();

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
