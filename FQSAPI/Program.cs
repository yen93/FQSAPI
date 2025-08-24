using FQSAPI;
using Microsoft.AspNetCore.SignalR;
using Firebase.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR service
builder.Services.AddSignalR();

// Add CORS (single definition)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Firebase client
builder.Services.AddSingleton(new FirebaseClient(
    "https://fqsdb-f0707-default-rtdb.firebaseio.com/",
    new FirebaseOptions
    {
        AuthTokenAsyncFactory = () => Task.FromResult("HHYF9DMNTJco2vOBJqKLC3F8LCvgIVc9iHIMiw6v")
    }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable swagger locally if you want
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

// Render provides HTTPS already
// app.UseHttpsRedirection();

// Apply CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<QueueHub>("/queueHub");

// Render sets $PORT automatically
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");
