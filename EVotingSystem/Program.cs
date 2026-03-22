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

builder.Services.AddAuthorization();

builder.Services.AddSingleton<FirestoreUserStore>();
builder.Services.AddSingleton<IUserStore<ApplicationUser>>(serviceProvider => serviceProvider.GetRequiredService<FirestoreUserStore>());
builder.Services.AddSingleton<IUserEmailStore<ApplicationUser>>(serviceProvider => serviceProvider.GetRequiredService<FirestoreUserStore>());
builder.Services.AddSingleton<IUserPasswordStore<ApplicationUser>>(serviceProvider => serviceProvider.GetRequiredService<FirestoreUserStore>());
builder.Services.AddSingleton<IUserSecurityStampStore<ApplicationUser>>(serviceProvider => serviceProvider.GetRequiredService<FirestoreUserStore>());

// Firestore infrastructure is registered behind interfaces so we can swap the
// in-memory/demo behavior for a real Cloud Firestore implementation later.
builder.Services.AddSingleton<IFirestoreElectionRepository, FirestoreElectionRepository>();

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
