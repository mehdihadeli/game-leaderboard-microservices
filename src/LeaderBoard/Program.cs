using System.Reflection;
using LeaderBoard;
using LeaderBoard.Infrastructure.Data;
using LeaderBoard.Infrastructure.Data.EFContext;
using LeaderBoard.Services;
using LeaderBoard.SharedKernel.Data;
using LeaderBoard.SharedKernel.Data.Contracts;
using LeaderBoard.SharedKernel.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOptions<LeaderBoardOptions>().BindConfiguration(nameof(LeaderBoardOptions));

builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
builder.Services.AddMediatR(c => c.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.AddCustomRedis();
builder.AddPostgresDbContext<LeaderBoardDBContext>();

builder.Services.AddTransient<ISeeder, DataSeeder>();
builder.Services.AddTransient<IPlayerScoreService, PlayerScoreService>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

var seeders = app.Services.GetServices<ISeeder>();
foreach (var seeder in seeders)
    await seeder.SeedAsync();

app.Run();
