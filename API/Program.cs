using Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ConduitDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration["ConnectionStrings:ConduitConnection"],
        sqlServerOptionsAction => sqlServerOptionsAction.MigrationsAssembly("Infrastructure")));
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();