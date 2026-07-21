using FakeNewsScamDetector.Core.Interfaces;
using FakeNewsScamDetector.Data;
using FakeNewsScamDetector.Data.Repositories;
using FakeNewsScamDetector.ML.Prediction;
using FakeNewsScamDetector.Services;
using FakeNewsScamDetector.Services.AI;
using FakeNewsScamDetector.Services.ExternalApis;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();
builder.Services.AddScoped<IScamRuleEngine, ScamRuleEngine>();
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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
