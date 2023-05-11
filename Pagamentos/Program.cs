using Microsoft.EntityFrameworkCore;
using Pagamentos.Context;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<BancoP>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Conexao"));
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    options.EnableSensitiveDataLogging();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
