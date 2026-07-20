using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Data.Seed;
using Ahk.Web.Server.Auth;
using Ahk.Web.Server.Configuration;
using Ahk.Web.Server.CourseContext;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- Options ----
builder.Services.Configure<OidcOptions>(builder.Configuration.GetSection(OidcOptions.SectionName));
var oidc = builder.Configuration.GetSection(OidcOptions.SectionName).Get<OidcOptions>() ?? new OidcOptions();

// ---- EF Core + course scoping ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentCourseProvider>();
builder.Services.AddScoped<ICurrentCourseProvider>(sp => sp.GetRequiredService<CurrentCourseProvider>());
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ---- Identity (cookie-based; no MapIdentityApi — see plan) ----
builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.User.RequireUniqueEmail = false;
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// The SPA calls the API; make the cookie handler return 401/403 instead of redirecting to login/denied pages.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// ---- Generic OIDC external login (registered only when configured) ----
if (oidc.IsEnabled)
{
    builder.Services.AddAuthentication().AddOpenIdConnect(ExternalAuthController.Scheme, options =>
    {
        options.Authority = oidc.Authority;
        options.ClientId = oidc.ClientId;
        options.ClientSecret = oidc.ClientSecret;
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.CallbackPath = "/signin-oidc";
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        foreach (var scope in oidc.Scopes)
            options.Scope.Add(scope);
        options.GetClaimsFromUserInfoEndpoint = true;
    });
}

// ---- Authorization ----
builder.Services.AddScoped<IAuthorizationHandler, CourseMembershipAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(CourseMembershipRequirement.PolicyName, policy =>
        policy.Requirements.Add(new CourseMembershipRequirement()));
});

// ---- MVC + OpenAPI (NSwag document consumed by the Angular code generator) ----
builder.Services.AddControllers();
builder.Services.AddOpenApiDocument(settings =>
{
    settings.DocumentName = "v1";
    settings.Title = "AHK API";
    settings.Version = "v1";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();       // /swagger/v1/swagger.json
    app.UseSwaggerUi();     // /swagger
    await DevDataSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<CourseResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program
{
}
