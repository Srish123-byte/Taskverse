var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// College Service endpoints
app.MapGet("/api/colleges", GetColleges)
    .WithName("GetColleges")
    .WithOpenApi();

app.MapGet("/api/colleges/{id}", GetCollegeById)
    .WithName("GetCollegeById")
    .WithOpenApi();

app.MapPost("/api/colleges", CreateCollege)
    .WithName("CreateCollege")
    .WithOpenApi();

app.MapPut("/api/colleges/{id}", UpdateCollege)
    .WithName("UpdateCollege")
    .WithOpenApi();

app.MapDelete("/api/colleges/{id}", DeleteCollege)
    .WithName("DeleteCollege")
    .WithOpenApi();

app.Run();

// College endpoints implementation
async Task<IResult> GetColleges()
{
    var colleges = new[]
    {
        new College { Id = 1, Name = "Engineering College", City = "New York", Country = "USA" },
        new College { Id = 2, Name = "Medical College", City = "Boston", Country = "USA" }
    };
    return Results.Ok(colleges);
}

async Task<IResult> GetCollegeById(int id)
{
    var college = new College { Id = id, Name = $"College {id}", City = "City", Country = "Country" };
    return Results.Ok(college);
}

async Task<IResult> CreateCollege(College college)
{
    return Results.Created($"/api/colleges/{college.Id}", college);
}

async Task<IResult> UpdateCollege(int id, College college)
{
    college.Id = id;
    return Results.Ok(college);
}

async Task<IResult> DeleteCollege(int id)
{
    return Results.NoContent();
}

record College
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
