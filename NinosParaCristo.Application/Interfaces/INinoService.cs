using NinosParaCristo.Application.DTOs.Ninos;

namespace NinosParaCristo.Application.Interfaces;

public interface INinoService
{
    Task<List<NinoDto>> ObtenerTodosAdminAsync();
    Task<List<NinoAulaDto>> ObtenerParaAulaAsync();
    Task<NinoDto?> ObtenerPorIdAsync(int id);
    Task<NinoDto> CrearAsync(CrearNinoDto dto);
    Task<NinoDto?> ActualizarAsync(int id, ActualizarNinoDto dto);
    Task<EncargadoDto?> GuardarEncargadoPrincipalAsync(int ninoId, GuardarEncargadoDto dto);
    Task<bool> DesactivarAsync(int id);
}
