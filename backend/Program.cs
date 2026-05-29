using DotNetEnv;
using FlashcardsApi.Data;
using FlashcardsApi.Endpoints;
using Microsoft.EntityFrameworkCore;

// Treat all DateTime values as unspecified kind so Npgsql doesn't reject
// DateTime.UtcNow comparisons against TIMESTAMP WITHOUT TIME ZONE columns
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

Env.Load(); // loads .env into environment variables; silently skipped if file doesn't exist

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FlashcardsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

CollectionEndpoints.Map(app);
FlashcardEndpoints.Map(app);

app.Run();
