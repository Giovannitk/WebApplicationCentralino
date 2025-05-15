using WebApplicationCentralino.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<ChiamataService>();
builder.Services.AddHttpClient<ContattoService>();


// Leggi il parametro --port dalla riga di comando (default: 1085)
int port = builder.Configuration.GetValue<int>("port", 1085);

//builder.Services.AddHttpClient<GestioneChiamataService>();
builder.Services.AddHttpClient<GestioneChiamataService>(client =>
{
    client.BaseAddress = new Uri($"http://localhost:{port}/");
});

//builder.Services.AddScoped<IContattoService, ContattoService>();

//builder.WebHost.UseUrls("http://localhost:1085");



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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
