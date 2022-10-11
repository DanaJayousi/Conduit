using System.Reflection;
using Domain.Article;
using Domain.Comment;
using Domain.Interfaces;
using Domain.User;
using FluentValidation.AspNetCore;
using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers()
    .AddFluentValidation(c => c.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly()))
    .AddNewtonsoftJson();
builder.Services.AddDbContext<ConduitDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration["ConnectionStrings:ConduitConnection"],
        sqlServerOptionsAction => sqlServerOptionsAction.MigrationsAssembly("Infrastructure")));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
app.Run();