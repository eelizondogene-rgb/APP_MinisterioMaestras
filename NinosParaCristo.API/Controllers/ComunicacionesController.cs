using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using NinosParaCristo.API.Extensions;
using NinosParaCristo.Application.Common;
using NinosParaCristo.Application.DTOs.Comunicaciones;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Controllers;

[ApiController]
[Route("api/comunicaciones")]
[Authorize(Roles = Roles.Administradora)]
public class ComunicacionesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    public ComunicacionesController(ApplicationDbContext context, IConfiguration configuration)
    { _context=context; _configuration=configuration; }

    [HttpGet]
    public async Task<ActionResult> Get() => Ok(await _context.Comunicaciones.AsNoTracking().Include(x=>x.Nino)
        .OrderByDescending(x=>x.FechaCreacion)
        .Select(x=>new {x.Id,x.NinoId,Nino=x.Nino.NombreCompleto,x.Asunto,x.Mensaje,x.Canal,x.Destinatario,x.Estado,x.FechaCreacion,x.FechaEnvio})
        .Take(200).ToListAsync());

    [HttpPost]
    public async Task<ActionResult> Create(CrearComunicacionDto dto)
    {
        var encargado = dto.EncargadoId.HasValue
            ? await _context.Encargados.FirstOrDefaultAsync(x=>x.Id==dto.EncargadoId && x.NinoId==dto.NinoId)
            : await _context.Encargados.FirstOrDefaultAsync(x=>x.NinoId==dto.NinoId && x.EsPrincipal);

        var destino = dto.Canal.Equals("Email", StringComparison.OrdinalIgnoreCase) ? encargado?.Email : encargado?.Telefono;
        var item = new ComunicacionPadre {
            NinoId=dto.NinoId, EncargadoId=encargado?.Id, Asunto=dto.Asunto.Trim(), Mensaje=dto.Mensaje.Trim(),
            Canal=dto.Canal.Trim(), Destinatario=destino, Estado="Registrado", FechaCreacion=DateTime.UtcNow, UsuarioId=User.UserId()
        };

        if (dto.Canal.Equals("Email", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(destino))
        {
            try
            {
                var host=_configuration["Smtp:Host"];
                var from=_configuration["Smtp:From"];
                if(!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(from))
                {
                    using var client=new SmtpClient(host, int.TryParse(_configuration["Smtp:Port"], out var port)?port:587)
                    {
                        EnableSsl=true,
                        Credentials=new NetworkCredential(_configuration["Smtp:User"],_configuration["Smtp:Password"])
                    };
                    using var mail=new MailMessage(from,destino,dto.Asunto,dto.Mensaje);
                    await client.SendMailAsync(mail);
                    item.Estado="Enviado"; item.FechaEnvio=DateTime.UtcNow;
                }
                else item.Estado="PendienteConfigEmail";
            }
            catch { item.Estado="ErrorEnvio"; }
        }

        _context.Comunicaciones.Add(item); await _context.SaveChangesAsync();
        return Ok(item);
    }
}
