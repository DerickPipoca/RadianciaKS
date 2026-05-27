using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RadianciaKS.Api.Services;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Infrastructure.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<RadianciaKS.Api.Swagger.TenantHeaderFilter>();
});

builder.Services.AddHttpContextAccessor();

//Dependency Injections
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddValidatorsFromAssembly(typeof(CategoryService).Assembly);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.UseHttpsRedirection();

app.Run();
