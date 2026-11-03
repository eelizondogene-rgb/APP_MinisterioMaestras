using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Controllers;
using NinosParaCristo.API.Hubs;
using NinosParaCristo.API.Services;
using NinosParaCristo.Application.DTOs.Asistencia;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;

var options=new DbContextOptionsBuilder<ApplicationDbContext>();
var connection=Environment.GetEnvironmentVariable("NPC_TEST_POSTGRES");
if(string.IsNullOrWhiteSpace(connection)) options.UseInMemoryDatabase(Guid.NewGuid().ToString());
else {
    var target=new Npgsql.NpgsqlConnectionStringBuilder(connection);
    if(target.Host!="127.0.0.1" || target.Database is null || !target.Database.StartsWith("npc_regression_"))
        throw new InvalidOperationException("Las pruebas PostgreSQL solo aceptan una base local npc_regression_.");
    options.UseNpgsql(connection);
}
using var db=new ApplicationDbContext(options.Options);
await db.Database.EnsureCreatedAsync();
var user = new Usuario { Id=1, NombreCompleto="Prueba", Email="prueba@example.invalid", PasswordHash="prueba", Rol="Administradora", Activo=true };
db.Usuarios.Add(user);
db.Ninos.Add(new Nino { Id=1, NombreCompleto="Prueba ficticia", Activo=true, FechaNacimiento=new DateOnly(2020,1,1) });
await db.SaveChangesAsync();
ClaimsPrincipal Principal(string role, int id=1) => new(new ClaimsIdentity(new[] {
    new Claim(ClaimTypes.NameIdentifier,id.ToString()), new Claim(ClaimTypes.Role,role)
}, "test"));
var count=0;
void Check(bool ok, string name) { if(!ok) throw new Exception(name); Console.WriteLine("OK: "+name); count++; }
Check(await SessionAccess.IsCurrentAsync(db,Principal("Administradora")), "Cuenta activa aceptada");
user.Activo=false; await db.SaveChangesAsync();
Check(!await SessionAccess.IsCurrentAsync(db,Principal("Administradora")), "Cuenta desactivada rechazada");
user.Activo=true; user.Rol="MaestraClase"; await db.SaveChangesAsync();
Check(!await SessionAccess.IsCurrentAsync(db,Principal("Administradora")), "Rol anterior rechazado");
Check(!await SessionAccess.IsCurrentAsync(db,Principal("Administradora",999)), "Cuenta inexistente rechazada");
var controller=new AsistenciasController(db,new EmptyHub()) {
    ControllerContext=new ControllerContext { HttpContext=new DefaultHttpContext { User=Principal("Administradora") } }
};
GuardarAsistenciaDto Request(params RegistroAsistenciaDto[] records) => new() { Fecha=new DateOnly(2026,11,1), Registros=records.ToList() };
Check(await controller.Save(Request(new RegistroAsistenciaDto { NinoId=1 })) is BadRequestObjectResult, "Estado omitido no se convierte en ausencia");
Check(await controller.Save(Request(new RegistroAsistenciaDto { NinoId=1,Presente=true },new RegistroAsistenciaDto { NinoId=1,Presente=false })) is BadRequestObjectResult, "Duplicados rechazados");
Check(await controller.Save(Request(new RegistroAsistenciaDto { NinoId=999,Presente=true })) is BadRequestObjectResult, "Niño inexistente rechazado");
Check(await controller.Save(Request()) is BadRequestObjectResult, "Lista vacía rechazada");
Check(!await db.Asistencias.AnyAsync(), "Solicitudes inválidas no escriben datos");
Check(await controller.Save(Request(new RegistroAsistenciaDto { NinoId=1,Presente=false })) is NoContentResult, "Ausencia explícita aceptada");
Check(await controller.Save(Request(new RegistroAsistenciaDto { NinoId=1,Presente=true })) is NoContentResult && await db.Asistencias.CountAsync()==1 && (await db.Asistencias.SingleAsync()).Presente, "Corrección actualiza el mismo registro");
controller.ControllerContext.HttpContext.User=Principal("MaestraClase");
Check(await controller.Save(Request(new RegistroAsistenciaDto { NinoId=1,Presente=false })) is ForbidResult, "Maestra sin grupo no modifica asistencia");
var results=new List<ValidationResult>();
var missing=new RegistroAsistenciaDto { NinoId=1 };
Check(!Validator.TryValidateObject(missing,new ValidationContext(missing),results,true), "DTO exige estado explícito");
var services=new ServiceCollection().AddLogging().AddAuthorization().BuildServiceProvider();
var provider=services.GetRequiredService<IAuthorizationPolicyProvider>();
var authorization=services.GetRequiredService<IAuthorizationService>();
async Task<bool> Allowed(string action,string role) {
    var type=typeof(ProtocoloController);
    var attributes=type.GetCustomAttributes<AuthorizeAttribute>().Concat(type.GetMethod(action)!.GetCustomAttributes<AuthorizeAttribute>());
    var policy=await AuthorizationPolicy.CombineAsync(provider,attributes);
    return (await authorization.AuthorizeAsync(Principal(role),null,policy!)).Succeeded;
}
Check(await Allowed("Merienda","MaestraClase"), "Maestra de clase puede consultar la merienda");
Check(!await Allowed("Get","MaestraClase"), "Maestra de clase no obtiene el calendario de protocolo");
Check(!await Allowed("Bitacora","MaestraClase"), "Maestra de clase no modifica bitácoras de protocolo");
Check(await Allowed("Get","MaestraProtocolo"), "Maestra de protocolo conserva acceso");
count += await OperacionTests.Run(db);
count += await ImportacionTests.Run();
count += await ExcelTests.Run();
Console.WriteLine($"{count} comprobaciones correctas. Proveedor: {db.Database.ProviderName}. No sustituye pruebas en navegador.");

class EmptyHub : IHubContext<UpdatesHub> {
    public IHubClients Clients { get; }=new EmptyClients();
    public IGroupManager Groups => throw new NotSupportedException();
}
class EmptyClients : IHubClients {
    public IClientProxy All { get; }=new EmptyProxy();
    public IClientProxy AllExcept(IReadOnlyList<string> x)=>All;
    public IClientProxy Client(string x)=>All;
    public IClientProxy Clients(IReadOnlyList<string> x)=>All;
    public IClientProxy Group(string x)=>All;
    public IClientProxy GroupExcept(string x,IReadOnlyList<string> y)=>All;
    public IClientProxy Groups(IReadOnlyList<string> x)=>All;
    public IClientProxy User(string x)=>All;
    public IClientProxy Users(IReadOnlyList<string> x)=>All;
}
class EmptyProxy : IClientProxy {
    public Task SendCoreAsync(string method,object?[] args,CancellationToken cancellationToken=default)=>Task.CompletedTask;
}
