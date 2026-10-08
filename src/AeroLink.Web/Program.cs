using AeroLink.Web.Data;
using AeroLink.Web.Services.Hr;
using AeroLink.Web.Services.Login;
using AeroLink.Web.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Razor Pages handle the application's web pages.
// T2: every page under /Baggage requires a signed-in baggage employee.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddFolderApplicationModelConvention("/Baggage",
        pageModel => pageModel.Filters.Add(new RequireBaggageEmployeeFilter()));
});

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

// T2: typed HttpClient for the HR API. The base URL comes from HrApi:BaseUrl in appsettings.json.
builder.Services.AddHttpClient<IHrApiClient, HrApiClient>(httpClient =>
{
    var hrApiBaseUrl = builder.Configuration["HrApi:BaseUrl"]
        ?? throw new InvalidOperationException("HrApi:BaseUrl is missing from appsettings.json.");
    httpClient.BaseAddress = new Uri(hrApiBaseUrl);
    httpClient.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<ILoginService, LoginService>();

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
