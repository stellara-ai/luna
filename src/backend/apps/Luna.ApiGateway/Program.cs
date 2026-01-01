using Microsoft.AspNetCore.HttpOverrides;
using Luna.ApiGateway.Modules;
using Luna.Classroom.Endpoints;
using Microsoft.OpenApi;
using Luna.SharedKernel.Time;


var builder = WebApplication.CreateBuilder(args);

// ------------------------------
// Services
// ------------------------------
builder.Services.AddAllModules(builder.Configuration);
builder.Services.AddSingleton<ISystemClock, SystemClock>();

// OpenAPI (keep dev-only mapping later)
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
});

// CORS: allow-all ONLY for dev; in prod use allowed origins from config
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        if (builder.Environment.IsDevelopment() || allowedOrigins.Length == 0)
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // only if you use cookies; otherwise remove
        }
    });
});

// If you are behind AWS ALB / reverse proxy, this avoids incorrect scheme/host issues
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// ------------------------------
// Middleware (order matters)
// ------------------------------
app.UseWebSockets();
app.UseForwardedHeaders();

// For dev only; in prod use exception handler + HSTS.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "Luna API"));
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// If you terminate TLS at ALB, keep this ON in prod
app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

// AuthZ pipeline (IdentityModule should register authN/authZ services)
app.UseAuthentication();
app.UseAuthorization();

// Optional: correlation/request id (helps logs + tracing)
// If you already do this in a module/middleware, remove this.
app.Use(async (ctx, next) =>
{
    const string header = "X-Correlation-Id";
    if (!ctx.Request.Headers.TryGetValue(header, out var cid) || string.IsNullOrWhiteSpace(cid))
        cid = Guid.NewGuid().ToString("N");

    ctx.Response.Headers[header] = cid.ToString();
    await next();
});

// ------------------------------
// Endpoints
// ------------------------------

// Error endpoint for prod exception handler
app.MapGet("/error", () => Results.Problem("An unexpected error occurred."))
   .ExcludeFromDescription();

// Map module endpoints (map ALL modules here)
//app.MapIdentityEndpoints();
app.MapClassroomEndpoints();
//app.MapStudentsEndpoints();
//app.MapCurriculumEndpoints();
//app.MapMediaEndpoints();

// Health endpoints (commonly used naming)
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }))
   .WithName("Healthz");

app.MapGet("/readyz", () => Results.Ok(new { status = "ready" }))
   .WithName("Readyz");

app.Run();