using FakeNewsScamDetector.Core.Interfaces;
using FakeNewsScamDetector.Data;
using FakeNewsScamDetector.Data.Repositories;
using FakeNewsScamDetector.ML.Prediction;
using FakeNewsScamDetector.Services;
using FakeNewsScamDetector.Services.AI;
using FakeNewsScamDetector.Services.ExternalApis;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

// App startup and dependency-injection wiring for the whole site. Every
// interface used by a controller (IAnalysisRepository, ITextClassifierService,
// etc.) gets its concrete implementation registered here - see each
// interface's own file in FakeNewsScamDetector.Core for what it's for.
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// SQLite file path comes from appsettings.json's "DefaultConnection"
// connection string (just "Data Source=app.db" - a relative path next to
// wherever the app is run from).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMemoryCache();
// Scoped = one instance per HTTP request (matches AppDbContext's own
// lifetime, since both depend on EF Core's per-request DbContext).
builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();
builder.Services.AddScoped<IScamRuleEngine, ScamRuleEngine>();
// Singleton = one instance for the whole app's lifetime, shared by every
// request. Safe here since WhoisLookupClient has its own IMemoryCache for
// per-domain results rather than any mutable per-request state.
builder.Services.AddSingleton<IWhoisLookupClient, WhoisLookupClient>();
// These all default to a 100-second HttpClient timeout, which meant a slow
// external API (including the Gemini "AI" call) could leave a user staring
// at a spinner for well over a minute before anything happened. Fail faster
// so the UI can show a friendly error instead.
builder.Services.AddHttpClient<ISafeBrowsingClient, SafeBrowsingClient>(c => c.Timeout = TimeSpan.FromSeconds(8));
builder.Services.AddHttpClient<IFactCheckClient, FactCheckClient>(c => c.Timeout = TimeSpan.FromSeconds(8));
builder.Services.AddHttpClient<IConversationalVerifierService, GeminiVerifierService>(c => c.Timeout = TimeSpan.FromSeconds(25));
builder.Services.AddScoped<IUrlAnalyzerService, UrlAnalyzerService>();
builder.Services.AddScoped<VerdictAggregator>();

// Registered with a factory (instead of just AddSingleton<TextClassifierService>)
// because the constructor needs resolved file paths, not raw config values -
// the factory runs once, on first request, and the same instance (with its
// two loaded ML models) is reused for the rest of the app's lifetime.
builder.Services.AddSingleton<ITextClassifierService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    // The model .zip files are copied next to the built assembly (see the
    // Content item in the .csproj), not into the source content root — use
    // AppContext.BaseDirectory so this resolves correctly regardless of the
    // working directory the app was launched from.
    var fakeNewsModelPath = Path.Combine(AppContext.BaseDirectory, config["MlModels:FakeNewsModelPath"] ?? string.Empty);
    var scamModelPath = Path.Combine(AppContext.BaseDirectory, config["MlModels:ScamModelPath"] ?? string.Empty);
    return new TextClassifierService(fakeNewsModelPath, scamModelPath);
});

var app = builder.Build();

// Creates the SQLite database file and schema on first run if it doesn't
// already exist. EnsureCreated() (not Migrate()) is used here, which means
// the Migrations folder is effectively unused for this SQLite database -
// fine for a project this size, but a bigger app would want Migrate()
// instead so schema changes can be applied incrementally.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Render (and most PaaS hosts) terminate TLS at their edge and forward plain
// HTTP to the container, so Kestrel sees every request as http. Without this,
// UseHttpsRedirection below sees scheme=http on every request - including
// ones the browser made over https - and keeps redirecting forever. Clearing
// KnownNetworks/KnownProxies is required here because the proxy's IP isn't
// static in a container platform, unlike a fixed on-prem reverse proxy.
var forwardedHeaderOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeaderOptions.KnownIPNetworks.Clear();
forwardedHeaderOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaderOptions);

app.UseHttpsRedirection();
app.UseRouting();

// No actual authentication/authorization is configured anywhere in this
// app (no login, no [Authorize] attributes) - this middleware is just part
// of the default ASP.NET Core template pipeline and is effectively a no-op
// here, but harmless to leave in for if that ever changes.
app.UseAuthorization();

// Serves wwwroot files (CSS, JS, images, the bundled Bootstrap/jQuery
// libs) with build-time fingerprinting and pre-compression - see the
// MapStaticAssets docs for details on how this differs from the older
// UseStaticFiles().
app.MapStaticAssets();

// Single conventional route for every controller: /{controller}/{action}/{id?},
// defaulting to HomeController.Index when nothing is specified (i.e. "/").
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
