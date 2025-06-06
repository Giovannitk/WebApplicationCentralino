using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApplicationCentralino.Services;
using WebApplicationCentralino.Managers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add JWT Token Service
builder.Services.AddScoped<JwtTokenService>();

// Register all services
builder.Services.AddScoped<ChiamataService>();
builder.Services.AddScoped<ContattoService>();
builder.Services.AddScoped<GestioneChiamataService>();
builder.Services.AddScoped<IContattoService, ContattoService>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();
builder.Services.AddScoped<ILoginAttemptService, LoginAttemptService>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add SystemLogProvider after registering ISystemLogService
var serviceProvider = builder.Services.BuildServiceProvider();
builder.Logging.AddProvider(new SystemLogProvider(
    serviceProvider.GetRequiredService<ISystemLogService>(),
    "Application"
));

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = builder.Configuration.GetValue<TimeSpan>("Authentication:ExpireTimeSpan", TimeSpan.FromDays(7));
        options.Cookie.Name = "Centralino.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

var address = builder.Configuration.GetValue<string>("BaseUrl", "http://10.36.150.250:5000/");

// Create a function to configure HTTP clients with JWT token handler
void ConfigureHttpClientWithJwtToken(IServiceCollection services, string name, Action<HttpClient> configureClient = null)
{
    services.AddHttpClient(name, client =>
    {
        client.BaseAddress = new Uri(address);
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        configureClient?.Invoke(client);
    }).AddHttpMessageHandler(() => new JwtTokenHandler(services.BuildServiceProvider().GetRequiredService<JwtTokenService>()));
}

// Configure all HTTP clients with JWT token handler
ConfigureHttpClientWithJwtToken(builder.Services, "ApiClient");
ConfigureHttpClientWithJwtToken(builder.Services, "ChiamataService");
ConfigureHttpClientWithJwtToken(builder.Services, "ContattoService");

// Leggi il parametro --port dalla riga di comando (default: 1085)
int port = builder.Configuration.GetValue<int>("port", 1085);

// Configure GestioneChiamataService with custom base address
ConfigureHttpClientWithJwtToken(builder.Services, "GestioneChiamataService", client =>
{
    //client.BaseAddress = new Uri($"http://localhost:{port}/");
    client.BaseAddress = new Uri($"{address}");
});

// Imposta la porta dinamicamente
builder.WebHost.UseUrls($"http://localhost:{port}");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add custom exception handler middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (HttpRequestException ex) when (ex.Message.Contains("401"))
    {
        // Clear the JWT token cookie
        context.Response.Cookies.Delete("JWTToken");
        
        // Sign out the user
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        // Redirect to login page
        context.Response.Redirect("/Auth/Login");
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
