using NinosParaCristo.Application.Common;
namespace NinosParaCristo.API.Services;

// Transcripción confirmada por la administradora: solo noviembre y diciembre de 2026.
public static class CalendarioInicial2026
{
    public record Maestra(string Nombre, string Rol);
    public record Clase(int Mes, string Grupo, string Maestra);
    public record Protocolo(DateOnly Fecha, string Responsable);
    public static readonly Maestra[] Maestras = [
        new("Abigail", Roles.MaestraProtocolo),
        new("Adriana", Roles.MaestraClase),
        new("Conchita", Roles.MaestraClase),
        new("Evelyn", Roles.MaestraProtocolo),
        new("Genesis", Roles.MaestraProtocolo),
        new("Isabel", Roles.MaestraClase),
        new("Jackeline", Roles.MaestraClase),
        new("Karla", Roles.MaestraClase),
        new("Kennia", Roles.MaestraClase),
        new("Mary Luz", Roles.MaestraProtocolo),
        new("Ninosca", Roles.MaestraProtocolo),
        new("Osleiby", Roles.MaestraClase),
        new("Patricia", Roles.MaestraClase),
        new("Rebeca", Roles.MaestraRotativa),
        new("Sharon", Roles.MaestraClase),
        new("Shirley", Roles.MaestraClase),
        new("Svetlana", Roles.MaestraClase),
        new("Yuzmary", Roles.MaestraClase)
    ];
    public static readonly Clase[] Clases = [
        new(11, "Cuna", "Conchita"),
        new(11, "Cuna", "Kennia"),
        new(11, "Párvulos", "Jackeline"),
        new(11, "Párvulos", "Patricia"),
        new(11, "Primarios", "Sharon"),
        new(11, "Intermedios y Adolescentes", "Adriana"),
        new(12, "Cuna", "Karla"),
        new(12, "Cuna", "Rebeca"),
        new(12, "Párvulos", "Yuzmary"),
        new(12, "Párvulos", "Osleiby"),
        new(12, "Primarios", "Svetlana"),
        new(12, "Intermedios y Adolescentes", "Shirley")
    ];
    public static readonly Protocolo[] Protocolos = [
        new(new DateOnly(2026, 11, 1), "Genesis"),
        new(new DateOnly(2026, 11, 8), "Mary Luz"),
        new(new DateOnly(2026, 11, 15), "Rebeca"),
        new(new DateOnly(2026, 11, 22), "Evelyn"),
        new(new DateOnly(2026, 11, 29), "Ninosca"),
        new(new DateOnly(2026, 12, 6), "Genesis"),
        new(new DateOnly(2026, 12, 13), "Mary Luz"),
        new(new DateOnly(2026, 12, 20), "Ninosca"),
        new(new DateOnly(2026, 12, 27), "Evelyn")
    ];
}
