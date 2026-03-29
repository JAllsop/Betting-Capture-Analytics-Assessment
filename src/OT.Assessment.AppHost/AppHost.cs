var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter("sql-password", "Guest123!", secret: true);
var sql = builder.AddSqlServer("sql", password)
                 .WithEnvironment("TZ", "Africa/Johannesburg");
                 

var db = sql.AddDatabase("OT-Assessment-DB");

var rabbit = builder.AddRabbitMQ("messaging");

var redis = builder.AddRedis("cache");

builder.AddProject<Projects.OT_Assessment_App>("api-app")
                 .WithReference(db)
                 .WithReference(rabbit)
                 .WithReference(redis);

builder.AddProject<Projects.OT_Assessment_Consumer>("consumer-worker")
       .WithReference(sql)
       .WithReference(rabbit)
       .WithReference(redis);

builder.Build()
       .Run();