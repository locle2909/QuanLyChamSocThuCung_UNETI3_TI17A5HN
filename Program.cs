// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: Phần chung nhóm trưởng: cấu hình EF Core, Session, route mặc định.
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data.Seed;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<EmailService>();
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromMinutes(30);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
});

var app = builder.Build();

// Chạy seed SAU KHI đã có Migration và đã chạy Update-Database:
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Module1Seed.Seed(db);   // SV1
    Module3Seed.Seed(db);   // SV3: dịch vụ và gói dịch vụ mẫu
    // Module2Seed.Seed(db); // SV2 ... mỗi người thêm một dòng của mình
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();      // sau UseRouting, trước MapControllerRoute
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=TaiKhoan}/{action=DangNhap}/{id?}");

app.Run();