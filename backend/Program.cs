using DotNetEnv;
using FlashcardsApi.Data;
using FlashcardsApi.Endpoints;
using Microsoft.EntityFrameworkCore;

Env.Load(); // loads .env into environment variables; silently skipped if file doesn't exist

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FlashcardsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var frontendOrigin = builder.Configuration["AllowedOrigin"] ?? "";

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
    {
        // Allow localhost in CORS policy for development and testing
        var origins = new List<string> { "http://localhost:5029" };
        if (!string.IsNullOrEmpty(frontendOrigin)) origins.Add(frontendOrigin);
        policy.WithOrigins([.. origins]).AllowAnyHeader().AllowAnyMethod();
    }));

var app = builder.Build();

app.UseCors();

CollectionEndpoints.Map(app);
FlashcardEndpoints.Map(app);

app.Run();
