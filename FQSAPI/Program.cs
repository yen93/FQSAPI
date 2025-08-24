using FQSAPI;
using Microsoft.AspNetCore.SignalR;
using Firebase.Database;
using System.Net;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR service
builder.Services.AddSignalR();


var ipv4 = Dns.GetHostEntry(Dns.GetHostName())
    .AddressList.First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
    .ToString();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins($"http://{ipv4}:3000", "http://localhost:3000") // your HTML test origins
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // IMPORTANT for SignalR
    });
});

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
    // comment out for testing
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS before MapHub/MapControllers
app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<QueueHub>("/queueHub");

app.Run();
