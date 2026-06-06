using DotNetEnv;
using FlashcardsApi.Data;
using FlashcardsApi.Endpoints;
using Microsoft.EntityFrameworkCore;

Env.Load(); // loads .env into environment variables; silently skipped if file doesn't exist

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FlashcardsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                "http://localhost:5029",
                "https://localhost:7001",
                "https://happy-sea-0b22b5300.7.azurestaticapps.net")
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

CollectionEndpoints.Map(app);
FlashcardEndpoints.Map(app);

app.Run();
