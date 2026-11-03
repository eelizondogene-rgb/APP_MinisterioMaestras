using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NinosParaCristo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SupabaseReady : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Encargados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NinoId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Parentesco = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    EsPrincipal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Encargados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Encargados_Ninos_NinoId",
                        column: x => x.NinoId,
                        principalTable: "Ninos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GruposClase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EdadMinima = table.Column<int>(type: "integer", nullable: false),
                    EdadMaxima = table.Column<int>(type: "integer", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposClase", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventarioItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    StockMinimo = table.Column<int>(type: "integer", nullable: false),
                    Unidad = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventarioItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreCompleto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Rol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AsignacionesClaseMensuales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anio = table.Column<int>(type: "integer", nullable: false),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    GrupoClaseId = table.Column<int>(type: "integer", nullable: false),
                    MaestraId = table.Column<int>(type: "integer", nullable: false),
                    Tema = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MaterialUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesClaseMensuales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsignacionesClaseMensuales_GruposClase_GrupoClaseId",
                        column: x => x.GrupoClaseId,
                        principalTable: "GruposClase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsignacionesClaseMensuales_Usuarios_MaestraId",
                        column: x => x.MaestraId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Asistencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NinoId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Presente = table.Column<bool>(type: "boolean", nullable: false),
                    Observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RegistradaPorUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asistencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asistencias_Ninos_NinoId",
                        column: x => x.NinoId,
                        principalTable: "Ninos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Asistencias_Usuarios_RegistradaPorUsuarioId",
                        column: x => x.RegistradaPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Comunicaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NinoId = table.Column<int>(type: "integer", nullable: false),
                    EncargadoId = table.Column<int>(type: "integer", nullable: true),
                    Asunto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Mensaje = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Canal = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Destinatario = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comunicaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comunicaciones_Encargados_EncargadoId",
                        column: x => x.EncargadoId,
                        principalTable: "Encargados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Comunicaciones_Ninos_NinoId",
                        column: x => x.NinoId,
                        principalTable: "Ninos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comunicaciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InventarioItemId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Nota = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosInventario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosInventario_InventarioItems_InventarioItemId",
                        column: x => x.InventarioItemId,
                        principalTable: "InventarioItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovimientosInventario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Mensaje = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaProgramada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Leida = table.Column<bool>(type: "boolean", nullable: false),
                    Enviada = table.Column<bool>(type: "boolean", nullable: false),
                    ClaveUnica = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificaciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Protocolos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    ResponsableMeriendaId = table.Column<int>(type: "integer", nullable: true),
                    ResponsableProtocoloId = table.Column<int>(type: "integer", nullable: true),
                    Merienda = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Tareas = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Bitacora = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Completado = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Protocolos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Protocolos_Usuarios_ResponsableMeriendaId",
                        column: x => x.ResponsableMeriendaId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Protocolos_Usuarios_ResponsableProtocoloId",
                        column: x => x.ResponsableProtocoloId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PushSuscripciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    P256dh = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Auth = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSuscripciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushSuscripciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TurnosClase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    MaestraId = table.Column<int>(type: "integer", nullable: false),
                    Tema = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MaterialUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnosClase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TurnosClase_Usuarios_MaestraId",
                        column: x => x.MaestraId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesClaseMensuales_Anio_Mes_GrupoClaseId_MaestraId",
                table: "AsignacionesClaseMensuales",
                columns: new[] { "Anio", "Mes", "GrupoClaseId", "MaestraId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesClaseMensuales_GrupoClaseId",
                table: "AsignacionesClaseMensuales",
                column: "GrupoClaseId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesClaseMensuales_MaestraId",
                table: "AsignacionesClaseMensuales",
                column: "MaestraId");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_NinoId_Fecha",
                table: "Asistencias",
                columns: new[] { "NinoId", "Fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_RegistradaPorUsuarioId",
                table: "Asistencias",
                column: "RegistradaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Comunicaciones_EncargadoId",
                table: "Comunicaciones",
                column: "EncargadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Comunicaciones_NinoId",
                table: "Comunicaciones",
                column: "NinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Comunicaciones_UsuarioId",
                table: "Comunicaciones",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Encargados_NinoId",
                table: "Encargados",
                column: "NinoId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposClase_Nombre",
                table: "GruposClase",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_InventarioItemId",
                table: "MovimientosInventario",
                column: "InventarioItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_UsuarioId",
                table: "MovimientosInventario",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_ClaveUnica",
                table: "Notificaciones",
                column: "ClaveUnica",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_UsuarioId",
                table: "Notificaciones",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Protocolos_Fecha",
                table: "Protocolos",
                column: "Fecha",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Protocolos_ResponsableMeriendaId",
                table: "Protocolos",
                column: "ResponsableMeriendaId");

            migrationBuilder.CreateIndex(
                name: "IX_Protocolos_ResponsableProtocoloId",
                table: "Protocolos",
                column: "ResponsableProtocoloId");

            migrationBuilder.CreateIndex(
                name: "IX_PushSuscripciones_Endpoint",
                table: "PushSuscripciones",
                column: "Endpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushSuscripciones_UsuarioId",
                table: "PushSuscripciones",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnosClase_MaestraId",
                table: "TurnosClase",
                column: "MaestraId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsignacionesClaseMensuales");

            migrationBuilder.DropTable(
                name: "Asistencias");

            migrationBuilder.DropTable(
                name: "Comunicaciones");

            migrationBuilder.DropTable(
                name: "MovimientosInventario");

            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropTable(
                name: "Protocolos");

            migrationBuilder.DropTable(
                name: "PushSuscripciones");

            migrationBuilder.DropTable(
                name: "TurnosClase");

            migrationBuilder.DropTable(
                name: "GruposClase");

            migrationBuilder.DropTable(
                name: "Encargados");

            migrationBuilder.DropTable(
                name: "InventarioItems");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
