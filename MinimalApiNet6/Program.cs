using Microsoft.EntityFrameworkCore;
using MinimalApiNet6.Data;
using MinimalApiNet6.Models;
using MiniValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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
