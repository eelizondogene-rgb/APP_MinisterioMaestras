namespace NinosParaCristo.Application.Common;

public static class Roles
{
    public const string Administradora = "Administradora";
    public const string MaestraClase = "MaestraClase";
    public const string MaestraProtocolo = "MaestraProtocolo";

    public static readonly string[] Todos = [Administradora, MaestraClase, MaestraProtocolo];
}
