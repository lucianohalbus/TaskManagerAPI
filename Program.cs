using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TaskManagerApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// JWT configs validation
// ----------------------------
var jwtSection = builder.Configuration.GetSection("Jwt");
foreach (var key in new[] { "Key", "Issuer", "Audience" })
{
    if (string.IsNullOrWhiteSpace(jwtSection[key]))
        throw new InvalidOperationException($"Configuração obrigatória ausente: Jwt:{key}");
}

// ----------------------------
// JWT Authentication
// ----------------------------
var secretKey = jwtSection["Key"]!;
var jwtKeyBytes = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o =>
{
    if (builder.Environment.IsDevelopment())
    {
        // Dev
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes)
        };
    }
    else
    {
        // Prod
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"]!,
            ValidAudience = jwtSection["Audience"]!,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),
            ClockSkew = TimeSpan.Zero
        };
    }
});

var devCorsPolicy = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(devCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials() // ok mesmo se não usar cookies; pode deixar
    );
});

// ----------------------------
// Controllers + Versioning + Swagger
// ----------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(2, 0);
    opt.AssumeDefaultVersionWhenUnspecified = false;
    opt.ReportApiVersions = true;
    opt.ApiVersionReader = new UrlSegmentApiVersionReader();
});

builder.Services.AddVersionedApiExplorer(opt =>
{
    opt.GroupNameFormat = "'v'VVV";
    opt.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen();
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

// ----------------------------
// DbContext
// ----------------------------
builder.Services.AddDbContext<TaskManagerContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ----------------------------
// App
// ----------------------------
var app = builder.Build();

// Swagger Development only
if (app.Environment.IsDevelopment())
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        foreach (var d in provider.ApiVersionDescriptions)
            c.SwaggerEndpoint($"/swagger/{d.GroupName}/swagger.json", $"TaskManager API {d.GroupName.ToUpperInvariant()}");
    });
}
else
{
    // Produção: HSTS
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(devCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();


