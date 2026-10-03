using GritTrack.Api.Repositories;
using GritTrack.Api.Services;
using Microsoft.AspNetCore.Mvc.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    // makes a validation error's key match the camelCase JSON field name
    // instead of the C# property name, so the client can actually match
    // the error to the field it sent
    options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
});

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// one JSON file backs the whole app, so the repo has to be a singleton —
// a new instance per request would just mean every request fighting over
// its own copy of the file
builder.Services.AddSingleton<ICourseRepository, JsonCourseRepository>();
builder.Services.AddScoped<ICourseService, CourseService>();

var app = builder.Build();

// one place that turns an unhandled exception into a proper ProblemDetails
// response, instead of a try/catch in every action
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
