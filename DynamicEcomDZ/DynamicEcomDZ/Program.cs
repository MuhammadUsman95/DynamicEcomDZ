using DynamicEcomDZ.Services;
using DynamicEcomDZ.Services;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<RedirectionService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// 🔴 Your custom default (KEEP THIS)
app.MapControllerRoute(
    name: "redirection",
    pattern: "{controller=Redirection}/{action=RedirectionView}/{id?}");

// ✅ Standard MVC fallback (THIS FIXES /UsmanForm)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.Run();
