var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", "Guest123!", secret: true);
var sql = builder.AddSqlServer("sql", sqlPassword, port: 1433)
                 .WithEnvironment("TZ", "Africa/Johannesburg");
var db = sql.AddDatabase("OT-Assessment-DB");

var rabbitPassword = builder.AddParameter("rabbit-password", "guest", secret: true);
var rabbitUsername = builder.AddParameter("rabbit-username", "guest");
var rabbit = builder.AddRabbitMQ("messaging", rabbitUsername, rabbitPassword)
                    .WithManagementPlugin();

var redis = builder.AddRedis("cache", port: 6379);

builder.AddProject<Projects.OT_Assessment_App>("api-app")
                 .WithHttpsEndpoint(port: 7120, name: "tester-endpoint") // given a unique name so it doesn't conflict with launchSettings
                 .WithReference(db)
                 .WithReference(rabbit)
                 .WithReference(redis);

builder.AddProject<Projects.OT_Assessment_Consumer>("consumer-worker")
       .WithReference(db)
       .WithReference(rabbit);

builder.Build()
       .Run();