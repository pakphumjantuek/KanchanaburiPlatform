global using Core.Entities;
global using Infrastructure;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.AspNetCore.Identity;
global using Infrastructure.Data;
global using Core.Interfaces;
global using API.Middleware;
global using Infrastructure.Services;
global using KanchanaburiPlatform.Domain.Entities;
global using System.Security.Claims;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
global using Application.DTOs;
global using Application.Results;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<StoreContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

// CORS
builder.Services.AddCors();

// Identity
builder.Services
    .AddIdentityApiEndpoints<AppUser>(options =>
    {
        // Password
        options.Password.RequiredLength = 3;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 1;

        // User
        options.User.RequireUniqueEmail = true;

        // Lockout
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<StoreContext>();

// Authorization
builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<ShopBusinessService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IImageUploadService, ImageUploadService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IPaymentSlipService, PaymentSlipService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderExpiryService, OrderExpiryService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddHostedService<API.Services.OrderPaymentExpiryWorker>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductDealService, ProductDealService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAdminExportService, AdminExportService>();
builder.Services.AddScoped<IMerchantExportService, MerchantExportService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<IProductReviewService, ProductReviewService>();

builder.Services.AddScoped(
    typeof(IGenericRepository<>),
    typeof(GenericRepository<>));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

var app = builder.Build();


// ========================
// Swagger
// ========================


// เปิด Swagger ทั้ง Development และ Production
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "./v1/swagger.json",
        "Kanchanaburi Platform API V1"
    );

    options.RoutePrefix = "swagger";
});


// ========================
// Middleware
// ========================

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    // index.html ห้ามถูกแคช เพื่อให้หลัง deploy ใหม่ผู้ใช้ได้ไฟล์ assets ชุดล่าสุดเสมอ
    // (ไฟล์ใน /assets มี hash ในชื่ออยู่แล้ว จึงแคชได้ตามปกติ)
    OnPrepareResponse = ctx =>
    {
        if (string.Equals(ctx.File.Name, "index.html", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        }
    }
});

app.UseMiddleware<ExceptionMiddleware>();


// ========================
// CORS
// ========================

app.UseCors(x => x
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithOrigins(
        "http://localhost:5173",
        "https://localhost:5173"
    ));


// ========================
// Authentication
// ========================

app.UseAuthentication();
app.UseAuthorization();


// ========================
// Controllers
// ========================

app.MapControllers();


// ========================
// Identity API
// ========================

app.MapGroup("/api")
    .MapIdentityApi<AppUser>();


// ========================
// SPA Fallback
// ========================

// ให้ Vue Router (history mode) ทำงานได้เมื่อรีเฟรชหรือเปิดลิงก์ตรง เช่น /orders/{id}
// เส้นทางที่ไม่ใช่ API/Swagger/ไฟล์อัปโหลด และไม่ตรงไฟล์จริง จะได้ index.html กลับไป
// ไม่ครอบคลุม path ที่มีนามสกุลไฟล์ (เช่น .js .css .png) เพื่อให้ไฟล์ที่หายไปได้ 404 จริง
// แทนที่จะได้ HTML กลับไป ซึ่งทำให้เบราว์เซอร์ฟ้อง MIME type ผิด
app.MapFallbackToFile("/{*path:regex(^(?!api(/|$)|swagger|uploads)(?!.*[.][A-Za-z0-9]+$).*$)}", "index.html");


// ========================
// Database Migration
// ========================

// ปิดไว้ เพราะจะใช้ Update-Database เอง

// using (var scope = app.Services.CreateScope())
// {
//     try
//     {
//         var context = scope.ServiceProvider
//             .GetRequiredService<StoreContext>();

//         await context.Database.MigrateAsync();
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine(
//             $"Database Migration Error: {ex.Message}"
//         );
//     }
// }


app.Run();
