using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MinimalApiNet6.Data;
using MinimalApiNet6.Models;
using MiniValidation;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//Add the IdentityDbContext configuration in youy program.cs
builder.Services.AddIdentityEntityFrameworkContextConfiguration(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
b => b.MigrationsAssembly("MinimalApiNet6")));

//Add identity configuration in configure method of your program.cs
builder.Services.AddIdentityConfiguration();

//Add jwtconfiguration in configure method of your program.cs
builder.Services.AddJwtConfiguration(builder.Configuration, "AppSettings");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//Add useauthconfiguration in configure method of your program.cs
//app.UseAuthConfiguration();

app.UseHttpsRedirection();

app.MapPost("/user", async (SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IOptions<AppJwtSettings> appJwtSettings, RegisterUser registerUser) =>
{
    if(registerUser == null)
        return Results.BadRequest("Uninformed user");

    if (!MiniValidator.TryValidate(registerUser, out var erros))
        return Results.ValidationProblem(erros);

    var user = new IdentityUser
    {
        UserName = registerUser.Email,
        Email = registerUser.Email,
        EmailConfirmed = true
    };

    var result = await userManager.CreateAsync(user, registerUser.Password);

    if (!result.Succeeded)
        return Results.BadRequest(result.Errors);

    var jwt = new JwtBuilder()
                  .WithUserManager(userManager)
                  .WithJwtSettings(appJwtSettings.Value)
                  .WithEmail(user.Email)
                  .WithJwtClaims()
                  .WithUserClaims()
                  .WithUserRoles()
                  .BuildUserResponse();

    return Results.Ok(jwt);

}).ProducesValidationProblem()
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("RegisterUser")
    .WithTags("User");

app.MapPost("/login", async (SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IOptions<AppJwtSettings> appJwtSettings, LoginUser loginUser) =>
{
    if (loginUser == null)
        return Results.BadRequest("Uninformed user");

    if (!MiniValidator.TryValidate(loginUser, out var erros))
        return Results.ValidationProblem(erros);

    var result = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

    if (!result.Succeeded)
        return Results.BadRequest("Username or password is invalid");

    var jwt = new JwtBuilder()
                  .WithUserManager(userManager)
                  .WithJwtSettings(appJwtSettings.Value)
                  .WithEmail(loginUser.Email)
                  .WithJwtClaims()
                  .WithUserClaims()
                  .WithUserRoles()
                  .BuildUserResponse();

    return Results.Ok(jwt);

}).ProducesValidationProblem()
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("LoginUser")
    .WithTags("User");

app.MapGet("/patient", async (ApplicationDbContext applicationContext) =>
    await applicationContext.Patients.ToListAsync())
    .WithName("GetPatient")
    .WithTags("Patient");

app.MapGet("/patient/{id}", async (Guid id, ApplicationDbContext applicationContext) =>
    await applicationContext.Patients.FindAsync(id)
    is Patient patient
        ? Results.Ok(patient)
        : Results.NotFound())
    .Produces<Patient>(StatusCodes.Status200OK)
    .Produces<Patient>(StatusCodes.Status404NotFound)
    .WithName("GetPatientToId")
    .WithTags("Patient");

app.MapPost("/patient", async (ApplicationDbContext applicationContext, Patient patient) =>
{
    if(!MiniValidator.TryValidate(patient, out var erros))
        return Results.ValidationProblem(erros);

    applicationContext.Patients.Add(patient);
    var result = await applicationContext.SaveChangesAsync();

    return result > 0
        ? Results.Created($"/pacient/{patient.Id}", patient)
        : Results.BadRequest("There was a problem saving the record.");

}).ProducesValidationProblem()
    .Produces<Patient>(StatusCodes.Status201Created)
    .Produces<Patient>(StatusCodes.Status400BadRequest)
    .WithName("PostPatient")
    .WithTags("Patient");

app.MapPut("/patient/{id}", async (Guid id, ApplicationDbContext applicationContext, Patient patient) =>
{
    var selectPatient = await applicationContext.Patients.AsNoTracking<Patient>().FirstOrDefaultAsync(p => p.Id == id);
    if(selectPatient == null)   return Results.NotFound();
    
    if (!MiniValidator.TryValidate(patient, out var erros))
        return Results.ValidationProblem(erros);

    applicationContext.Patients.Update(patient);
    var result = await applicationContext.SaveChangesAsync();

    return result > 0
        ? Results.NoContent()
        : Results.BadRequest("There was a problem saving the record.");

}).ProducesValidationProblem()
    .Produces<Patient>(StatusCodes.Status204NoContent)
    .Produces<Patient>(StatusCodes.Status400BadRequest)
    .WithName("PutPatient")
    .WithTags("Patient");

app.MapDelete("/patient/{id}", async (Guid id, ApplicationDbContext applicationContext) =>
{
    var patient = await applicationContext.Patients.FindAsync(id);
    if (patient == null) return Results.NotFound();

    applicationContext.Patients.Remove(patient);
    var result = await applicationContext.SaveChangesAsync();

    return result > 0
        ? Results.NoContent()
        : Results.BadRequest("There was a problem delete the record.");

}).Produces(StatusCodes.Status400BadRequest)
    .Produces<Patient>(StatusCodes.Status204NoContent)
    .Produces<Patient>(StatusCodes.Status404NotFound)
    .WithName("DeletePatient")
    .WithTags("Patient");

app.Run();
