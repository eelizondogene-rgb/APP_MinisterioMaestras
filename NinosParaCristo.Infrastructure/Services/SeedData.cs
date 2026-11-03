using Microsoft.EntityFrameworkCore;
using NinosParaCristo.Application.Common;
using NinosParaCristo.Application.Interfaces;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.Infrastructure.Services;

public static class SeedData
{
    public static async Task EnsureAdminAsync(ApplicationDbContext context, IPasswordHasher hasher, string email, string password, string nombre)
    {
        email = email.Trim().ToLowerInvariant();
        if (!await context.Usuarios.AnyAsync(x => x.Email == email))
        {
            context.Usuarios.Add(new Usuario
            {
                NombreCompleto = nombre,
                Email = email,
                PasswordHash = hasher.Hash(password),
                Rol = Roles.Administradora,
                Activo = true,
                FechaRegistro = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }

    public static async Task EnsureGruposClaseAsync(ApplicationDbContext context)
    {
        var grupos = new[]
        {
            new GrupoClase { Nombre = "Cuna", EdadMinima = 1, EdadMaxima = 3, Orden = 1, Activo = true },
            new GrupoClase { Nombre = "Párvulos", EdadMinima = 4, EdadMaxima = 5, Orden = 2, Activo = true },
            new GrupoClase { Nombre = "Primarios", EdadMinima = 6, EdadMaxima = 8, Orden = 3, Activo = true },
            new GrupoClase { Nombre = "Intermedios y Adolescentes", EdadMinima = 9, EdadMaxima = 14, Orden = 4, Activo = true }
        };

        foreach (var grupo in grupos)
        {
            if (await context.GruposClase.AnyAsync(x => x.Nombre == grupo.Nombre)) continue;
            context.GruposClase.Add(grupo);
        }

        await context.SaveChangesAsync();
    }
}
