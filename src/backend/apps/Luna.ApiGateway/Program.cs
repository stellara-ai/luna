var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services
    .AddIdentityModule()
    .AddClassroomModule()
    .AddStudentsModule()
    .AddCurriculumModule()
    .AddMediaModule();

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Luna API"));
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Map module endpoints
app.MapClassroomEndpoints();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();
