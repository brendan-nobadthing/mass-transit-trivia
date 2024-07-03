using MassTransit;
using MassTransit.Logging;
using MttApi;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

Console.Out.WriteLine($"Env: {builder.Environment.EnvironmentName}");
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();    
}

// Add services to the container.

builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService("MTT API",
        serviceVersion: "Version 0.1",
        serviceInstanceId: Environment.MachineName))
    .WithTracing(b => b.AddSource(DiagnosticHeaders.DefaultListenerName)
        .AddConsoleExporter()
    );
builder.Services.AddSendObserver<MttSendObserver>();
builder.Services.AddPublishObserver<MttPublishObserver>();
builder.Services.AddReceiveObserver<MttReceiveObserver>();

builder.Services.AddHttpClient();
builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("brendan-trivia", false));
    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host("ap-southeast-2", h =>
        {
            h.AccessKey(builder.Configuration["user-access-key"]);
            h.SecretKey(builder.Configuration["user-secret"]);
            h.EnableScopedTopics();
            h.Scope("brendan-trivia", true);
        });
        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

// Add AWS Lambda support. When application is run in Lambda, Kestrel is swapped out as the web server with Amazon.Lambda.AspNetCoreServer.
// This package will act as the webserver translating request and responses between the Lambda event source and ASP.NET Core.

builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "Welcome to running ASP.NET Core Minimal API on AWS Lambda");

app.UseSwagger();
app.UseSwaggerUI();

app.Run();