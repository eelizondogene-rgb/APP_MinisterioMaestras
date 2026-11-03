using Microsoft.EntityFrameworkCore;
using NinosParaCristo.Application.DTOs.Ninos;
using NinosParaCristo.Application.Interfaces;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.Infrastructure.Services;

public class NinoService : INinoService
{
    private readonly ApplicationDbContext _context;
    public NinoService(ApplicationDbContext context) => _context = context;

    public async Task<List<NinoDto>> ObtenerTodosAdminAsync() =>
        await _context.Ninos.AsNoTracking()
            .Include(x => x.Encargados)
            .OrderBy(x => x.NombreCompleto)
            .Select(x => new NinoDto
            {
                Id = x.Id,
                NombreCompleto = x.NombreCompleto,
                FechaNacimiento = x.FechaNacimiento,
                Alergias = x.Alergias,
                Comentarios = x.Comentarios,
                Activo = x.Activo,
                FechaRegistro = x.FechaRegistro,
                EncargadoPrincipal = x.Encargados.Where(e => e.EsPrincipal).Select(e => new EncargadoDto
                {
                    Id = e.Id, Nombre = e.Nombre, Telefono = e.Telefono, Email = e.Email, Parentesco = e.Parentesco
                }).FirstOrDefault()
            }).ToListAsync();

    public async Task<List<NinoAulaDto>> ObtenerParaAulaAsync() =>
        await _context.Ninos.AsNoTracking().Where(x => x.Activo).OrderBy(x => x.NombreCompleto)
            .Select(x => new NinoAulaDto { Id = x.Id, NombreCompleto = x.NombreCompleto, Alergias = x.Alergias })
            .ToListAsync();

    public async Task<NinoDto?> ObtenerPorIdAsync(int id) =>
        await _context.Ninos.AsNoTracking().Where(x => x.Id == id).Include(x => x.Encargados)
            .Select(x => new NinoDto
            {
                Id = x.Id, NombreCompleto = x.NombreCompleto, FechaNacimiento = x.FechaNacimiento,
                Alergias = x.Alergias, Comentarios = x.Comentarios, Activo = x.Activo, FechaRegistro = x.FechaRegistro,
                EncargadoPrincipal = x.Encargados.Where(e => e.EsPrincipal).Select(e => new EncargadoDto
                {
                    Id = e.Id, Nombre = e.Nombre, Telefono = e.Telefono, Email = e.Email, Parentesco = e.Parentesco
                }).FirstOrDefault()
            }).FirstOrDefaultAsync();

    public async Task<NinoDto> CrearAsync(CrearNinoDto dto)
    {
        var nino = new Nino
        {
            NombreCompleto = dto.NombreCompleto.Trim(),
            FechaNacimiento = dto.FechaNacimiento,
            Alergias = string.IsNullOrWhiteSpace(dto.Alergias) ? null : dto.Alergias.Trim(),
            Comentarios = string.IsNullOrWhiteSpace(dto.Comentarios) ? null : dto.Comentarios.Trim(),
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(dto.EncargadoNombre))
        {
            nino.Encargados.Add(new Encargado
            {
                Nombre = dto.EncargadoNombre.Trim(),
                Telefono = dto.EncargadoTelefono?.Trim(),
                Email = dto.EncargadoEmail?.Trim(),
                Parentesco = dto.EncargadoParentesco?.Trim(),
                EsPrincipal = true
            });
        }

        _context.Ninos.Add(nino);
        await _context.SaveChangesAsync();
        return (await ObtenerPorIdAsync(nino.Id))!;
    }

    public async Task<NinoDto?> ActualizarAsync(int id, ActualizarNinoDto dto)
    {
        var nino = await _context.Ninos.FindAsync(id);
        if (nino is null) return null;

        nino.NombreCompleto = dto.NombreCompleto.Trim();
        nino.FechaNacimiento = dto.FechaNacimiento;
        nino.Alergias = string.IsNullOrWhiteSpace(dto.Alergias) ? null : dto.Alergias.Trim();
        nino.Comentarios = string.IsNullOrWhiteSpace(dto.Comentarios) ? null : dto.Comentarios.Trim();
        nino.Activo = dto.Activo;
        await _context.SaveChangesAsync();
        return await ObtenerPorIdAsync(id);
    }

    public async Task<EncargadoDto?> GuardarEncargadoPrincipalAsync(int ninoId, GuardarEncargadoDto dto)
    {
        if (!await _context.Ninos.AnyAsync(x => x.Id == ninoId)) return null;
        var encargado = await _context.Encargados.FirstOrDefaultAsync(x => x.NinoId == ninoId && x.EsPrincipal);
        if (encargado is null)
        {
            encargado = new Encargado { NinoId = ninoId, EsPrincipal = true };
            _context.Encargados.Add(encargado);
        }

        encargado.Nombre = dto.Nombre.Trim();
        encargado.Telefono = dto.Telefono?.Trim();
        encargado.Email = dto.Email?.Trim();
        encargado.Parentesco = dto.Parentesco?.Trim();
        await _context.SaveChangesAsync();

        return new EncargadoDto
        {
            Id = encargado.Id, Nombre = encargado.Nombre, Telefono = encargado.Telefono,
            Email = encargado.Email, Parentesco = encargado.Parentesco
        };
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var nino = await _context.Ninos.FindAsync(id);
        if (nino is null) return false;
        nino.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
