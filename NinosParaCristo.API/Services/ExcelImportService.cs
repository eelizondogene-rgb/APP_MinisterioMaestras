using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.Application.Common;
using NinosParaCristo.Application.Interfaces;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Services;

public record ImportRow(int Fila, Dictionary<string,string> Valores);
public record ImportMessage(int Fila, string Detalle);
public record ImportCredential(string Nombre,string Email,string Password);
public class ImportResult
{
    public List<ImportMessage> Errores {get;}=[];
    public List<ImportMessage> Cambios {get;}=[];
    public List<ImportCredential> Credenciales {get;}=[];
    public int Omitidas {get;set;}
    public bool Guardado {get;set;}
}

public class ExcelImportService(ApplicationDbContext db, IPasswordHasher hasher)
{
    public static readonly Dictionary<string,string[]> Columns=new() {
        ["maestras"]=["Nombre","Correo","Funcion"],
        ["clases"]=["Anio","MesInicio","MesFin","Grupo","Correo","Tema"],
        ["protocolo"]=["Fecha","CorreoProtocolo","CorreoMerienda","Merienda","Tareas"]
    };
    public static List<ImportRow> Read(Stream stream,string kind)
    {
        if(!Columns.ContainsKey(kind))throw new InvalidDataException("Tipo de carga inválido.");
        // Limitar tamaño expandido antes de abrir un documento comprimido.
        using(var zip=new ZipArchive(stream,ZipArchiveMode.Read,true)) {
            if(zip.Entries.Count>300 || zip.Entries.Sum(e=>e.Length)>20_000_000)
                throw new InvalidDataException("El archivo es demasiado grande. Use la plantilla sin imágenes.");
        }
        stream.Position=0;
        using var book=new XLWorkbook(stream);
        if(!book.TryGetWorksheet("Datos",out var sheet))throw new InvalidDataException("Debe existir la hoja Datos de la plantilla.");
        var cols=Columns[kind];
        if((sheet.LastRowUsed()?.RowNumber()??0)>501 || (sheet.LastColumnUsed()?.ColumnNumber()??0)>cols.Length)
            throw new InvalidDataException("Máximo 500 filas; conserve las columnas de la plantilla.");
        for(int c=0;c<cols.Length;c++)
            if(sheet.Cell(1,c+1).HasFormula || sheet.Cell(1,c+1).GetString().Trim()!=cols[c])
                throw new InvalidDataException($"Columna {c+1}: se esperaba {cols[c]}.");
        var rows=new List<ImportRow>();
        for(int r=2;r<=(sheet.LastRowUsed()?.RowNumber()??1);r++) {
            var values=new Dictionary<string,string>();
            for(int c=0;c<cols.Length;c++) {
                var cell=sheet.Cell(r,c+1);
                if(cell.HasFormula)throw new InvalidDataException($"Fila {r}: use valores, no fórmulas.");
                var value=cell.DataType==XLDataType.DateTime?cell.GetDateTime().ToString("yyyy-MM-dd"):cell.GetString().Trim();
                if(value.Length>1000)throw new InvalidDataException($"Fila {r}: texto demasiado largo.");
                values[cols[c]]=value;
            }
            if(values.Values.Any(v=>v.Length>0))rows.Add(new(r,values));
        }
        return rows;
    }
    public static byte[] Template(string kind)
    {
        using var book=new XLWorkbook();var sheet=book.AddWorksheet("Datos");
        var columns=Columns[kind];
        for(int c=0;c<columns.Length;c++){sheet.Cell(1,c+1).Value=columns[c];sheet.Column(c+1).Width=28;}
        sheet.Range(1,1,1,columns.Length).Style.Font.Bold=true;
        sheet.Range(1,1,1,columns.Length).Style.Fill.BackgroundColor=XLColor.LightBlue;
        sheet.SheetView.FreezeRows(1);
        var help=book.AddWorksheet("Instrucciones");help.Column(1).Width=110;
        string[] notes=["Complete Datos desde la fila 2. No cambie los encabezados. Máximo 500 filas.",
            "La vista previa no guarda nada. Los errores deben corregirse antes de confirmar.",
            "Maestras: Nombre, Correo único, Funcion: Clase, Protocolo o Rotativa. No se importan administradoras.",
            "Clases: Anio (2020 a 2100), MesInicio y MesFin (1 a 12), Grupo exacto, Correo de cuenta existente; Tema opcional.",
            "Para un mes use el mismo MesInicio y MesFin; para un trimestre use 1-3, 4-6, 7-9 o 10-12.",
            "Una fila por maestra y grupo. Puede haber varias maestras en un grupo.",
            "Protocolo: Fecha de domingo AAAA-MM-DD (o fecha de Excel); al menos un correo responsable.",
            "CorreoMerienda, Merienda y Tareas son opcionales. La misma persona puede tener ambos encargos.",
            "No se reemplazan asignaciones ni cuentas diferentes. Edite los registros existentes en la aplicación.",
            "No use fórmulas, imágenes ni macros. Guarde como Libro de Excel (.xlsx)."];
        for(int i=0;i<notes.Length;i++)help.Cell(i+1,1).Value=notes[i];
        using var ms=new MemoryStream();book.SaveAs(ms);return ms.ToArray();
    }
    public async Task<ImportResult> Process(string kind,List<ImportRow> rows,bool confirm)
    {
        var result=new ImportResult();
        if(!Columns.ContainsKey(kind)||rows is null||rows.Count is <1 or >500){result.Errores.Add(new(0,"Seleccione un tipo válido y entre 1 y 500 filas."));return result;}
        await using var tx=db.Database.IsRelational()?await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable):null;
        var users=await db.Usuarios.ToListAsync();var groups=await db.GruposClase.ToListAsync();
        var classes=await db.AsignacionesClaseMensuales.ToListAsync();var protocols=await db.Protocolos.ToListAsync();
        var changes=new List<Action>();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var row in rows) {
            try {
                if(row is null||row.Valores is null)throw new InvalidDataException("Fila inválida.");
                string V(string key)=>row.Valores.GetValueOrDefault(key)?.Trim()??"";
                void Error(string msg)=>throw new InvalidDataException(msg);
                int Num(string key,int min,int max){if(!int.TryParse(V(key),out var n)||n<min||n>max)Error($"{key}: use un número de {min} a {max}.");return n;}
                Usuario User(string email,bool clase) {
                    var u=users.SingleOrDefault(u=>u.Email.Equals(email,StringComparison.OrdinalIgnoreCase));
                    if(u is null||!u.Activo)throw new InvalidDataException($"No existe una cuenta activa con correo {email}.");
                    if(u.Rol!=Roles.Administradora&&u.Rol!=Roles.MaestraRotativa&&u.Rol!=(clase?Roles.MaestraClase:Roles.MaestraProtocolo))Error($"{u.NombreCompleto} no tiene la función requerida.");
                    return u;
                }
                if(row.Valores.Values.Any(v=>v is null||v.Length>1000))Error("Texto demasiado largo o inválido.");
                if(kind=="maestras") {
                    var name=V("Nombre");var email=V("Correo").ToLowerInvariant();
                    var role=V("Funcion").ToLowerInvariant() switch {"clase"=>Roles.MaestraClase,"protocolo"=>Roles.MaestraProtocolo,"rotativa"=>Roles.MaestraRotativa,_=>""};
                    if(name.Length is <2 or >150||email.Length>200||!new EmailAddressAttribute().IsValid(email)||role=="")Error("Revise Nombre, Correo y Funcion (Clase, Protocolo o Rotativa).");
                    if(!seen.Add(email))Error("Correo repetido en el archivo.");
                    var existing=users.SingleOrDefault(u=>u.Email.Equals(email,StringComparison.OrdinalIgnoreCase));
                    if(existing!=null) {
                        if(!existing.Activo||!existing.NombreCompleto.Equals(name,StringComparison.OrdinalIgnoreCase)||existing.Rol!=role)Error("El correo ya existe con otros datos. Revise Usuarios.");
                        result.Omitidas++;continue;
                    }
                    result.Cambios.Add(new(row.Fila,$"Crear cuenta: {name} ({email})"));
                    changes.Add(()=>{var password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));db.Usuarios.Add(new Usuario {NombreCompleto=name,Email=email,Rol=role,PasswordHash=hasher.Hash(password),Activo=true});result.Credenciales.Add(new(name,email,password));});
                } else if(kind=="clases") {
                    int year=Num("Anio",2020,2100),start=Num("MesInicio",1,12),end=Num("MesFin",1,12);
                    if(end<start)Error("MesFin no puede ser anterior a MesInicio.");
                    var group=groups.SingleOrDefault(g=>g.Activo&&g.Nombre.Equals(V("Grupo"),StringComparison.OrdinalIgnoreCase));
                    if(group==null)throw new InvalidDataException("Grupo desconocido: use el nombre del calendario.");
                    var u=User(V("Correo"),true);var topic=V("Tema");if(topic.Length>200)Error("Tema: máximo 200 caracteres.");
                    for(int m=start;m<=end;m++) {
                        int month=m;var key=$"{year}:{month}:{group.Id}:{u.Id}";
                        if(!seen.Add(key))Error("Asignación repetida en el archivo.");
                        if(protocols.Any(p=>p.Activo&&p.Fecha.Year==year&&p.Fecha.Month==month&&(p.ResponsableMeriendaId==u.Id||p.ResponsableProtocoloId==u.Id)))Error($"{u.NombreCompleto} tiene merienda o protocolo en el mes {month}.");
                        var old=classes.SingleOrDefault(c=>c.Anio==year&&c.Mes==month&&c.GrupoClaseId==group.Id&&c.MaestraId==u.Id);
                        if(old?.Activo==true){if(topic.Length>0&&old.Tema!=topic)Error($"Ya existe con otro tema en el mes {month}. Edite el calendario.");result.Omitidas++;continue;}
                        result.Cambios.Add(new(row.Fila,$"{month}/{year} · {group.Nombre} · {u.NombreCompleto}"));
                        changes.Add(()=>{if(old==null)db.AsignacionesClaseMensuales.Add(new(){Anio=year,Mes=month,GrupoClaseId=group.Id,MaestraId=u.Id,Tema=topic,Activo=true});else{old.Activo=true;old.Tema=topic;}});
                    }
                } else {
                    if(!DateOnly.TryParseExact(V("Fecha"),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)||date.Year is <2020 or >2100||date.DayOfWeek!=DayOfWeek.Sunday)Error("Fecha: use un domingo AAAA-MM-DD.");
                    if(!seen.Add(date.ToString("O")))Error("Fecha repetida en el archivo.");
                    var p=V("CorreoProtocolo")==""?null:User(V("CorreoProtocolo"),false);var s=V("CorreoMerienda")==""?null:User(V("CorreoMerienda"),false);
                    if(p==null&&s==null)Error("Indique al menos una responsable.");
                    if(classes.Any(c=>c.Activo&&c.Anio==date.Year&&c.Mes==date.Month&&(c.MaestraId==p?.Id||c.MaestraId==s?.Id)))Error("Una responsable tiene clases en ese mes.");
                    var snack=V("Merienda");var tasks=V("Tareas");
                    if(snack.Length>300)Error("Merienda: máximo 300 caracteres.");
                    var old=protocols.SingleOrDefault(x=>x.Fecha==date);
                    if(old?.Activo==true&&(old.ResponsableProtocoloId!=null||old.ResponsableMeriendaId!=null||!string.IsNullOrEmpty(old.Merienda)||!string.IsNullOrEmpty(old.Tareas)||old.Completado||!string.IsNullOrEmpty(old.Bitacora))) {
                        if(old.ResponsableProtocoloId!=p?.Id||old.ResponsableMeriendaId!=s?.Id||(old.Merienda??"")!=snack||(old.Tareas??"")!=tasks)Error("La fecha ya tiene otros datos. Edítela desde Meriendas y protocolo.");
                        result.Omitidas++;continue;
                    }
                    if(old?.Activo==false&&(old.Completado||!string.IsNullOrEmpty(old.Bitacora)))Error("La fecha retirada tiene una bitácora. Revísela antes de reutilizarla.");
                    result.Cambios.Add(new(row.Fila,$"{date:dd/MM/yyyy} · Protocolo: {p?.NombreCompleto??"—"} · Merienda: {s?.NombreCompleto??"—"}"));
                    changes.Add(()=>{var entity=old??new ProtocoloSemana{Fecha=date};if(old==null)db.Protocolos.Add(entity);entity.Activo=true;entity.ResponsableProtocoloId=p?.Id;entity.ResponsableMeriendaId=s?.Id;entity.Merienda=snack;entity.Tareas=tasks;});
                }
            } catch(InvalidDataException ex){result.Errores.Add(new(row?.Fila??0,ex.Message));}
        }
        if(result.Cambios.Count>1000)result.Errores.Add(new(0,"Máximo 1000 asignaciones por carga. Divida el archivo."));
        if(confirm&&result.Errores.Count==0) {
            foreach(var change in changes)change();
            await db.SaveChangesAsync();if(tx!=null)await tx.CommitAsync();result.Guardado=true;
        }
        return result;
    }
}
