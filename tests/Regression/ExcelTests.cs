using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Controllers;
using NinosParaCristo.API.Services;
using NinosParaCristo.Infrastructure.Persistence;
using NinosParaCristo.Infrastructure.Services;

static class ExcelTests
{
    public static async Task<int> Run()
    {
        var options=new DbContextOptionsBuilder<ApplicationDbContext>();var conn=Environment.GetEnvironmentVariable("NPC_TEST_POSTGRES");
        if(string.IsNullOrEmpty(conn))options.UseInMemoryDatabase(Guid.NewGuid().ToString());
        else{var b=new Npgsql.NpgsqlConnectionStringBuilder(conn);if(b.Host!="127.0.0.1"||b.Database?.StartsWith("npc_regression_")!=true)throw new Exception("Destino no permitido");b.Database+="_excel";options.UseNpgsql(b.ConnectionString);}
        await using var db=new ApplicationDbContext(options.Options);await db.Database.EnsureCreatedAsync();await SeedData.EnsureGruposClaseAsync(db);
        var service=new ExcelImportService(db,new PasswordHasher());int count=0;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("OK: "+text);count++;}
        ImportRow Row(int number,params string[] fields){var d=new Dictionary<string,string>();for(int i=0;i<fields.Length;i+=2)d[fields[i]]=fields[i+1];return new(number,d);}
        var teacher=Row(2,"Nombre","Maestra Prueba","Correo","prueba@example.invalid","Funcion","Rotativa");
        var preview=await service.Process("maestras",[teacher],false);
        Check(preview.Cambios.Count==1&&!await db.Usuarios.AnyAsync()&&preview.Credenciales.Count==0,"Vista previa no crea cuentas ni contraseñas");
        var saved=await service.Process("maestras",[teacher],true);
        Check(saved.Guardado&&saved.Credenciales.Count==1&&await db.Usuarios.CountAsync()==1,"Confirmar crea una cuenta con contraseña aleatoria");
        var hash=(await db.Usuarios.SingleAsync()).PasswordHash;
        var controller=new ImportacionesController(db,new PasswordHasher()){ControllerContext=new ControllerContext{HttpContext=new Microsoft.AspNetCore.Http.DefaultHttpContext()}};
        var file=(FileContentResult)await controller.Template("clases");
        using(var template=new XLWorkbook(new MemoryStream(file.FileContents)))
            Check(template.Worksheet("Referencias").Cell(3,2).GetString()=="prueba@example.invalid"&&template.Worksheet("Referencias").Cell(2,5).GetString()=="Cuna","Plantilla incluye correos y grupos actuales sin contraseñas");
        var repeated=await service.Process("maestras",[teacher],true);
        Check(repeated.Omitidas==1&&repeated.Credenciales.Count==0&&(await db.Usuarios.SingleAsync()).PasswordHash==hash,"Repetir Excel no cambia contraseñas");
        var invalid=Row(3,"Nombre","Otra Maestra","Correo","otra@example.invalid","Funcion","Administradora");
        Check((await service.Process("maestras",[invalid],true)).Errores.Count==1,"Excel no crea administradoras");
        invalid.Valores["Funcion"]="Clase";
        var bad=Row(4,"Nombre","Error","Correo","no-es-correo","Funcion","Clase");
        Check((await service.Process("maestras",[invalid,bad],true)).Errores.Count==1&&await db.Usuarios.CountAsync()==1,"Fila inválida evita guardar todo el lote");
        Check((await service.Process("maestras",[invalid,invalid],true)).Errores.Count==1&&await db.Usuarios.CountAsync()==1,"Correos duplicados dentro del archivo rechazados");
        var classes=Row(2,"Anio","2027","MesInicio","1","MesFin","3","Grupo","Cuna","Correo","prueba@example.invalid","Tema","");
        Check((await service.Process("clases",[classes],false)).Cambios.Count==3&&!await db.AsignacionesClaseMensuales.AnyAsync(),"Vista previa expande un trimestre sin guardar");
        Check((await service.Process("clases",[classes],true)).Guardado&&await db.AsignacionesClaseMensuales.CountAsync()==3,"Un solo lote guarda los tres meses");
        Check((await service.Process("clases",[classes],true)).Omitidas==3&&await db.AsignacionesClaseMensuales.CountAsync()==3,"Calendario repetido no duplica meses");
        var pro=Row(2,"Fecha","2027-01-03","CorreoProtocolo","prueba@example.invalid","CorreoMerienda","prueba@example.invalid","Merienda","Fruta","Tareas","");
        Check((await service.Process("protocolo",[pro],true)).Errores.Count==1&&!await db.Protocolos.AnyAsync(),"Importar protocolo respeta conflicto con clases");
        pro.Valores["Fecha"]="2027-04-04";
        Check((await service.Process("protocolo",[pro],true)).Guardado&&await db.Protocolos.CountAsync()==1,"Una persona puede hacer merienda y protocolo");
        Check((await service.Process("protocolo",[pro],true)).Omitidas==1,"Repetir protocolo omite el registro idéntico");
        pro.Valores["Merienda"]="Otra";
        Check((await service.Process("protocolo",[pro],true)).Errores.Count==1&&(await db.Protocolos.SingleAsync()).Merienda=="Fruta","Importar no sobrescribe datos diferentes");
        pro.Valores["Fecha"]="2027-04-05";
        Check((await service.Process("protocolo",[pro],true)).Errores.Count==1,"Fecha que no es domingo rechazada");
        classes.Valores["MesInicio"]="4";classes.Valores["MesFin"]="4";
        Check((await service.Process("clases",[classes],true)).Errores.Count==1,"Clases también respetan conflicto con protocolo existente");
        foreach(var kind in ExcelImportService.Columns.Keys){using var stream=new MemoryStream(ExcelImportService.Template(kind));Check(ExcelImportService.Read(stream,kind).Count==0,"Plantilla XLSX legible: "+kind);}
        using(var book=new XLWorkbook(new MemoryStream(ExcelImportService.Template("maestras")))){
            var s=book.Worksheet("Datos");s.Cell(2,1).Value="Nombre con ñ";s.Cell(2,2).Value="nueva@example.invalid";s.Cell(2,3).Value="Clase";
            using var stream=new MemoryStream();book.SaveAs(stream);stream.Position=0;
            Check(ExcelImportService.Read(stream,"maestras")[0].Valores["Nombre"]=="Nombre con ñ","Lee acentos y columnas de Excel real");
            s.Cell(2,1).FormulaA1="1+1";using var formula=new MemoryStream();book.SaveAs(formula);formula.Position=0;bool blocked=false;try{ExcelImportService.Read(formula,"maestras");}catch(InvalidDataException){blocked=true;}Check(blocked,"Fórmulas rechazadas sin ejecutarlas en importación");
        }
        var manager=new ApplicationPartManager();manager.ApplicationParts.Add(new AssemblyPart(typeof(CargaInicialController).Assembly));manager.FeatureProviders.Add(new ControllerFeatureProvider());var feature=new ControllerFeature();manager.PopulateFeature(feature);
        Check(!feature.Controllers.Any(c=>c.AsType()==typeof(CargaInicialController)),"Carga inicial retirada de las rutas HTTP");
        return count;
    }
}
