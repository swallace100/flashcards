using DotNetEnv;
using FlashcardsApi.Data;
using FlashcardsApi.Endpoints;
using Microsoft.EntityFrameworkCore;

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
