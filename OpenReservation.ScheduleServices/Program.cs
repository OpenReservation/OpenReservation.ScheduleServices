using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using dotenv.net;
using Hangfire;
using Hangfire.MemoryStorage;
using OpenReservation.ScheduleServices;
using OpenReservation.ScheduleServices.Services;
using ReferenceResolver;
using WeihanLi.Web.Extensions;

DotEnv.Load();

// register to support Chinese encoding
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateSlimBuilder(args);

// Add services to the container.
builder.Services.AddLogging(x => x.AddJsonConsole(options =>
{
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
    options.JsonWriterOptions = new JsonWriterOptions()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        MaxDepth = 32
    };
}));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<INuGetHelper, NuGetHelper>();

builder.Services.AddHangfire(config =>
{
    config.UseMemoryStorage(new MemoryStorageOptions()
    {
        FetchNextJobTimeout = TimeSpan.FromDays(30)
    });
});
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 4;
});
// register jobs
builder.Services.RegisterAssemblyTypesAsImplementedInterfaces(t => t.IsAssignableTo(typeof(IJob)), typeof(IJob).Assembly);
builder.Services.AddHostedService<JobRegisterService>();

builder.Services.AddHttpClient<INotificationService, NotificationService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetRequiredAppSetting("NotificationUrl"));
    client.DefaultRequestHeaders.TryAddWithoutValidation("X-ApiKey", 
        builder.Configuration.GetRequiredAppSetting("NotificationAuthApiKey"));
}).AddStandardResilienceHandler();

var app = builder.Build();

app.MapHangfireDashboard();

app.Map("/", () => "Hello world").ShortCircuit().DisableHttpMetrics();
app.MapConfigInspector().ShortCircuit().DisableHttpMetrics();
app.MapRuntimeInfo().ShortCircuit().DisableHttpMetrics();

var healthGroup = app.MapGroup("/api/health");
healthGroup.ShortCircuit().DisableHttpMetrics();
healthGroup.MapGet("/live", () => Results.Ok());
healthGroup.MapGet("/ready", () => Results.Ok());

app.Run();
