using URl.Services;
using URl.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Đăng ký MongoDbContext với IConfiguration
builder.Services.AddSingleton<MongoDbContext>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new MongoDbContext(configuration);
});

// Đăng ký IMemoryCache
builder.Services.AddMemoryCache();

// Đăng ký UrlShortenerService (constructor của service sẽ nhận MongoDbContext và IMemoryCache từ DI)
builder.Services.AddSingleton<UrlShortenerService>();

// Đăng ký dịch vụ CORS
builder.Services.AddCors();

// Thêm MVC
builder.Services.AddControllersWithViews();

// 🔹 Cấu hình Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "URL Shortener API",
        Version = "v1",
        Description = "API rút gọn URL sử dụng .NET Core và MongoDB"
    });

    c.AddServer(new OpenApiServer { Url = "https://localhost:7140" });
});

var app = builder.Build();

// Check if HTTPS is properly configured
if (app.Environment.IsDevelopment())
{
    // In development, use the developer exception page
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "URL Shortener API v1");
    });
}
else
{
    // In production, use HTTPS redirection, exception handler, and HSTS
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Always use HTTPS redirection
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

// 🔥 Kích hoạt CORS (đặt sau UseRouting nhưng trước UseAuthorization)
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthorization();

// Map controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
