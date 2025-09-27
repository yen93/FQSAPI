using FQSAPI;
using Microsoft.AspNetCore.SignalR;
using Firebase.Database;
using Microsoft.AspNetCore.Cors;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR service
builder.Services.AddSignalR();

// Add CORS (FIXED VERSION)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Allow any origin for testing
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Crucial for SignalR
    });
});

// Firebase client
builder.Services.AddSingleton(new FirebaseClient(
    "https://fqsdb-f0707-default-rtdb.firebaseio.com/",
    new FirebaseOptions
    {
        AuthTokenAsyncFactory = () => Task.FromResult("HHYF9DMNTJco2vOBJqKLC3F8LCvgIVc9iHIMiw6v")
    }));


// Add HttpClient factory
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply CORS BEFORE other middleware
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<QueueHub>("/queueHub");

// Render sets $PORT automatically
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");