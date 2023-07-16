using System.Text;
using dotenv.net;
using Hangfire;
using Hangfire.MemoryStorage;
using OpenReservation.ScheduleServices;

DotEnv.Load();
// register to support chinese encoding
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHangfire(config =>
{
    config.UseMemoryStorage(new MemoryStorageOptions()
    {
        FetchNextJobTimeout = TimeSpan.FromDays(30)
    });
});
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
});

builder.Services.RegisterAssemblyTypesAsImplementedInterfaces(typeof(IJob).Assembly);
// builder.Services.AddHostedService<JobRegisterService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHangfireDashboard();
app.MapControllers();

app.Run();