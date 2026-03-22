using EVotingSystem.Options;
using EVotingSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FirebaseOptions>(builder.Configuration.GetSection(FirebaseOptions.SectionName));
builder.Services.Configure<MailCheckOptions>(builder.Configuration.GetSection(MailCheckOptions.SectionName));
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "evoting.auth";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<InMemoryElectionRepository>();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<EmailVerificationService>();
builder.Services.AddScoped<FirestoreRestClient>();
builder.Services.AddScoped<FirestoreElectionRepository>();
builder.Services.AddScoped<IElectionRepository>(serviceProvider =>
{
    var firebaseOptions = serviceProvider.GetRequiredService<IConfiguration>()
        .GetSection(FirebaseOptions.SectionName)
        .Get<FirebaseOptions>() ?? new FirebaseOptions();

    return firebaseOptions.IsConfigured
        ? serviceProvider.GetRequiredService<FirestoreElectionRepository>()
        : serviceProvider.GetRequiredService<InMemoryElectionRepository>();
});
builder.Services.AddScoped<ElectionService>();
builder.Services.AddHostedService<SeedHostedService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
