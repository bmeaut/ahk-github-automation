using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Data.Seed;
using Ahk.Web.Server.Auth;
using Ahk.Web.Server.Configuration;
using Ahk.Web.Server.CourseContext;
using Ahk.Web.Server.MockOidc;
using Ahk.Web.Services;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Ahk.Web.Server;

public class Program
{
    /// <summary>
    /// Session cookie name. Deliberately app-specific: cookies are scoped by host and ignore the port, so the
    /// framework default would be shared with every other ASP.NET Identity app running on localhost.
    /// </summary>
    public const string ApplicationCookieName = "ahk.auth";

    /// <summary>Cookie holding the external identity between the OIDC callback and sign-in.</summary>
    public const string ExternalCookieName = "ahk.auth.external";

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder);

        var app = builder.Build();
        await ConfigurePipelineAsync(app);

        await app.RunAsync();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
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
            // Named, not left at ASP.NET's default ".AspNetCore.Identity.Application". Browsers scope cookies
            // by host and ignore the port, so on localhost every ASP.NET Identity app shares that default name
            // — another project's cookie lands on this one, and because this app uses int keys, its GUID user
            // id blows up SecurityStampValidator with "not a valid value for Int32".
            options.Cookie.Name = ApplicationCookieName;
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

            // Belt and braces: a cookie this app cannot make sense of must sign the caller out, not throw.
            // AddIdentity points OnValidatePrincipal at the security-stamp validator, so it is called here
            // rather than replaced.
            options.Events.OnValidatePrincipal = async context =>
            {
                try
                {
                    await SecurityStampValidator.ValidatePrincipalAsync(context);
                }
                catch (Exception ex) when (ex is ArgumentException or FormatException)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                }
            };
        });

        // Same collision, shorter-lived cookie: this one carries the external identity between the OIDC
        // callback and sign-in.
        builder.Services.ConfigureExternalCookie(options =>
        {
            options.Cookie.Name = ExternalCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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

                // query, not the ASP.NET default of form_post: form_post makes the callback a cross-site POST,
                // so the correlation cookie is dropped under SameSite=Lax and login fails with "Correlation failed".
                options.ResponseMode = oidc.ResponseMode;

                // Off for BME: the IdP does not advertise code_challenge_methods_supported, and we authenticate
                // with client_secret_post, so PKCE is defence-in-depth rather than required.
                options.UsePkce = oidc.UsePkce;

                options.SaveTokens = oidc.SaveTokens;
                options.CallbackPath = "/signin-oidc";
                options.SignedOutCallbackPath = "/signout-callback-oidc";
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.GetClaimsFromUserInfoEndpoint = true;

                // Exactly the registered scopes — requesting an unregistered one (e.g. profile) gets rejected.
                options.Scope.Clear();
                foreach (var scope in oidc.Scopes)
                    options.Scope.Add(scope);

                // OpenIdConnectOptions ships only DeleteClaim actions — it maps NO standard claims. Anything
                // arriving from the userinfo endpoint (rather than inside the id_token) is therefore dropped
                // unless mapped explicitly. BME returns these via userinfo, so they must be listed here.
                options.ClaimActions.MapUniqueJsonKey(ClaimTypes.Email, "email");
                options.ClaimActions.MapUniqueJsonKey(ClaimTypes.Name, "name");
                options.ClaimActions.MapUniqueJsonKey(ClaimTypes.GivenName, "given_name");
                options.ClaimActions.MapUniqueJsonKey(ClaimTypes.Surname, "family_name");

                // BME-specific claims. neptun_code is single-valued; eduperson_scoped_affiliation is an array,
                // so it needs a custom action that joins the values. Note the resolver receives the whole
                // userinfo payload, not just the claim, so the property is looked up explicitly.
                options.ClaimActions.MapUniqueJsonKey(BmeClaimTypes.NeptunCode, BmeClaimTypes.NeptunCode);
                options.ClaimActions.MapCustomJson(BmeClaimTypes.Affiliation, root =>
                {
                    if (!root.TryGetProperty(BmeClaimTypes.Affiliation, out var value))
                        return null;

                    return value.ValueKind == JsonValueKind.Array
                        ? string.Join(';', value.EnumerateArray().Select(v => v.ToString()))
                        : value.ToString();
                });

                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    // The dev proxy rewrites Host, so the computed redirect_uri would point at the backend port
                    // instead of the browser's origin. In production this is left unset and computed normally.
                    if (!string.IsNullOrWhiteSpace(oidc.RedirectUri))
                        context.ProtocolMessage.RedirectUri = oidc.RedirectUri;

                    return Task.CompletedTask;
                };

                if (oidc.UseMockProvider)
                {
                    // The app fetches discovery/token/jwks from its own HTTPS endpoint, whose dev certificate is
                    // self-signed. Development only.
                    options.BackchannelHttpHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                    };
                }
            });
        }

        // ---- Authorization ----
        builder.Services.AddScoped<IAuthorizationHandler, CourseMembershipAuthorizationHandler>();
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(CourseMembershipRequirement.PolicyName, policy =>
                policy.Requirements.Add(new CourseMembershipRequirement()));
        });

        // ---- Development-only mock OpenID provider ----
        if (oidc.UseMockProvider)
        {
            builder.Services.AddSingleton<MockOidcSigningKey>();
            builder.Services.AddSingleton<MockOidcCodeStore>();
        }

        // Behind the production reverse proxy (TLS terminated upstream) the original scheme/host must be
        // honoured, otherwise the generated redirect_uri would be http://internal instead of https://ahk.aut.bme.hu.
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedFor;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // ---- Domain services (Ahk.Web.Services) ----
        builder.Services.AddMemoryCache();

        // The CI callback rejects a request whose Date header has drifted more than ten minutes, so it needs a
        // clock a test can move. TimeProvider is the framework's answer to grade-management's IDateTimeProvider.
        builder.Services.TryAddSingleton(TimeProvider.System);

        builder.Services.AddAhkServices();

        // ---- MVC + OpenAPI (NSwag document consumed by the Angular code generator) ----
        // Enums travel as names, not ordinals: the generated TypeScript client then models them as string
        // literal unions ('Instructor' | 'Admin'), which survives reordering an enum member.
        builder.Services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddOpenApiDocument(settings =>
        {
            settings.DocumentName = "v1";
            settings.Title = "AHK API";
            settings.Version = "v1";
        });
    }

    private static async Task ConfigurePipelineAsync(WebApplication app)
    {
        // Must run before anything that builds absolute URLs (the OIDC redirect_uri in particular).
        app.UseForwardedHeaders();

        var oidc = app.Services.GetRequiredService<IOptions<OidcOptions>>().Value;

        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();       // /swagger/v1/swagger.json
            app.UseSwaggerUi();     // /swagger
            await DevDataSeeder.SeedAsync(app.Services);
        }

        app.UseHttpsRedirection();

        // Serve the Angular SPA (published into wwwroot). Same-origin with the API, so the generated
        // clients' empty API_BASE_URL keeps issuing relative /api/... requests. Must run before routing.
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseAuthentication();
        app.UseMiddleware<CourseResolutionMiddleware>();
        app.UseAuthorization();

        // Development-only stand-in for the BME IdP; never mapped outside Development.
        if (app.Environment.IsDevelopment() && oidc.UseMockProvider)
            app.MapMockOidcProvider();

        app.MapControllers();

        // Client-side routes (e.g. /admin/courses) have no server endpoint; hand them index.html and let
        // the Angular router take over. Matched controller/static routes win first, so /api/... is untouched.
        app.MapFallbackToFile("index.html");
    }
}
