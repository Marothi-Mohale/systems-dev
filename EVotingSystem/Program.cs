using EVotingSystem.Infrastructure.Firestore;
using EVotingSystem.Infrastructure.Identity;
using EVotingSystem.Models.Identity;
using EVotingSystem.Options;
using EVotingSystem.Services;
using EVotingSystem.Services.Interfaces;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Net.Http.Headers;
using FirestoreElectionRepository = EVotingSystem.Infrastructure.Firestore.FirestoreElectionRepository;

var builder = WebApplication.CreateBuilder(args);
var requireSecureCookies = !builder.Environment.IsDevelopment();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug();
}

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddRazorPages();
builder.Services.AddProblemDetails();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["image/svg+xml"]);
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "__Host-ElectionPlatform.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = requireSecureCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
    options.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        return new ValueTask(context.HttpContext.Response.WriteAsync("Too many requests. Please wait a moment and try again.", cancellationToken));
    };

    options.AddPolicy("auth-post", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("vote-submit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 6,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.Configure<FirebaseOptions>(builder.Configuration.GetSection(FirebaseOptions.SectionName));
builder.Services.Configure<FirestoreOptions>(builder.Configuration.GetSection(FirestoreOptions.SectionName));
builder.Services.Configure<MailCheckOptions>(builder.Configuration.GetSection(MailCheckOptions.SectionName));
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "__Host-ElectionPlatform.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.Cookie.SecurePolicy = requireSecureCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.Cookie.IsEssential = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddAuthorization();
builder.Services.AddHttpClient("Firestore", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient("Mailcheck", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MailCheckOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

builder.Services.AddSingleton<FirestoreUserStore>();
builder.Services.AddSingleton<IUserStore<ApplicationUser>>(serviceProvider => serviceProvider.GetRequiredService<FirestoreUserStore>());
builder.Services.AddSingleton<IUserEmailStore<ApplicationUser>>(serviceProvider => serviceProvider.GetRequiredService<FirestoreUserStore>());
builder.Services.AddSingleton<IUserPasswordStore<ApplicationUser>>(serviceProvider => serviceProvider.GetRequiredService<FirestoreUserStore>());
builder.Services.AddSingleton<IUserSecurityStampStore<ApplicationUser>>(serviceProvider => serviceProvider.GetRequiredService<FirestoreUserStore>());

builder.Services.AddSingleton<IFirestoreDocumentClient, FirestoreRestClient>();
builder.Services.AddSingleton<IFirestoreCollectionNameProvider, FirestoreCollectionNameProvider>();
builder.Services.AddScoped<ICandidateRepository, FirestoreCandidateRepository>();
builder.Services.AddScoped<IVoteRepository, FirestoreVoteRepository>();
builder.Services.AddScoped<IElectionStatisticsRepository, FirestoreElectionStatisticsRepository>();
builder.Services.AddScoped<IVoterProfileRepository, FirestoreVoterProfileRepository>();
builder.Services.AddScoped<IFirestoreVotingTransaction, FirestoreVotingTransaction>();
builder.Services.AddScoped<IFirestoreSeedService, FirestoreSeedService>();
builder.Services.AddScoped<IFirestoreElectionRepository, FirestoreElectionRepository>();
builder.Services.AddHostedService<FirestoreSeedHostedService>();

// Mailcheck.ai is wrapped in a service abstraction so registration logic stays
// independent from the external API contract and can be tested cleanly.
builder.Services.AddScoped<IEmailValidationService, MailcheckEmailValidationService>();
builder.Services.AddScoped<IMailcheckClient, MailcheckClient>();
builder.Services.AddSingleton<IResultsDashboardCalculator, ResultsDashboardCalculator>();
builder.Services.AddScoped<IResultsService, ResultsService>();
builder.Services.AddScoped<IVotingService, VotingService>();

var app = builder.Build();

var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
var firebaseSettings = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<FirebaseOptions>>().Value;
var firestoreSettings = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<FirestoreOptions>>().Value;
var mailcheckSettings = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<MailCheckOptions>>().Value;
var allowedHosts = builder.Configuration["AllowedHosts"];
if (firebaseSettings.HasPlaceholderSecrets)
{
    startupLogger.LogWarning("Firebase configuration contains placeholder values. Use secure secret sources such as environment variables or user secrets.");
}

if (app.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(allowedHosts) ||
        string.Equals(allowedHosts, "*", StringComparison.Ordinal) ||
        allowedHosts.Contains("your-production-hostname", StringComparison.OrdinalIgnoreCase))
    {
        startupLogger.LogWarning("AllowedHosts is not restricted for production. Set it to your real hostname list before deployment.");
    }

    if (firestoreSettings.SeedOnStartup)
    {
        startupLogger.LogWarning("Firestore startup seeding is enabled in production. Disable Firestore:SeedOnStartup unless you are deploying an intentional demo environment.");
    }

    if (!firebaseSettings.IsConfigured)
    {
        startupLogger.LogWarning("Firebase is not fully configured for production. Firestore-backed data access will fail until secure credentials are supplied.");
    }

    if (!mailcheckSettings.IsConfigured)
    {
        startupLogger.LogWarning("Mailcheck is not configured for production. New registrations will be blocked until MailCheck__ApiKey is supplied.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd(HeaderNames.XContentTypeOptions, "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

    await next();
});
app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (!app.Environment.IsDevelopment())
        {
            context.Context.Response.Headers[HeaderNames.CacheControl] =
                context.Context.Request.Query.ContainsKey("v")
                    ? "public,max-age=31536000,immutable"
                    : "public,max-age=604800";
        }
    }
});
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
