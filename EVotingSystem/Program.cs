using EVotingSystem.Infrastructure.Firestore;
using EVotingSystem.Infrastructure.Identity;
using EVotingSystem.Models.Identity;
using EVotingSystem.Options;
using EVotingSystem.Services;
using EVotingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using FirestoreElectionRepository = EVotingSystem.Infrastructure.Firestore.FirestoreElectionRepository;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddProblemDetails();

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
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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
builder.Services.AddScoped<IFirestoreSeedService, FirestoreSeedService>();
builder.Services.AddScoped<IFirestoreElectionRepository, FirestoreElectionRepository>();
builder.Services.AddHostedService<FirestoreSeedHostedService>();

// Mailcheck.ai is wrapped in a service abstraction so registration logic stays
// independent from the external API contract and can be tested cleanly.
builder.Services.AddScoped<IEmailValidationService, MailcheckEmailValidationService>();
builder.Services.AddScoped<IResultsService, ResultsService>();
builder.Services.AddScoped<IVotingService, VotingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
