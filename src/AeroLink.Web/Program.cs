using AeroLink.Web.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Razor Pages handle the application's web pages.
builder.Services.AddRazorPages();

// Read the database location from appsettings.json.
builder.Services.AddDbContext<AppDbContext>(databaseOptions =>
    databaseOptions.UseSqlite(builder.Configuration.GetConnectionString("AeroLinkDb")));

// Keep the signed-in employee in session so it is available across requests.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(sessionOptions =>
{
    sessionOptions.IdleTimeout = TimeSpan.FromHours(8);
    sessionOptions.Cookie.HttpOnly = true;
    sessionOptions.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// Register feature services here as they are added to the application.

var app = builder.Build();

DatabaseInitializer.Initialise(app.Services, app.Configuration);

if (!app.Environment.IsDevelopment())
{
    // Show a friendly error page and use secure transport outside development.
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
