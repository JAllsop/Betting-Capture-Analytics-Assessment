var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
                 .AddDatabase("OT-Assessment-DB");

var rabbit = builder.AddRabbitMQ("messaging");

var redis = builder.AddRedis("cache");

builder.AddProject<Projects.OT_Assessment_App>("api-app")
                 .WithReference(sql)
                 .WithReference(rabbit)
                 .WithReference(redis);

builder.AddProject<Projects.OT_Assessment_Consumer>("consumer-worker")
       .WithReference(sql)
       .WithReference(rabbit)
       .WithReference(redis);

builder.Build()
       .Run();