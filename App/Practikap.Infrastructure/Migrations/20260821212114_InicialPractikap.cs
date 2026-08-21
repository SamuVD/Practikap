using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Practikap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InicialPractikap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "empresas",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    razon_social = table.Column<string>(type: "varchar(200)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nit = table.Column<string>(type: "varchar(20)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    jefe_inmediato_nombre = table.Column<string>(type: "varchar(150)", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    jefe_inmediato_correo = table.Column<string>(type: "varchar(180)", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    jefe_inmediato_telefono = table.Column<string>(type: "varchar(20)", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresas", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "programas",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(150)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descripcion = table.Column<string>(type: "varchar(255)", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_programas", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(30)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descripcion = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "fichas",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    numero_ficha = table.Column<string>(type: "varchar(20)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    programa_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fichas", x => x.id);
                    table.ForeignKey(
                        name: "fk_fichas_programa",
                        column: x => x.programa_id,
                        principalTable: "programas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    rol_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    correo = table.Column<string>(type: "varchar(180)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    contrasena_hash = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nombre = table.Column<string>(type: "varchar(150)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    apellido = table.Column<string>(type: "varchar(150)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    telefono = table.Column<string>(type: "varchar(20)", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    estado = table.Column<string>(type: "enum('Activo','Inactivo')", nullable: false, defaultValue: "Activo", collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_creacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    fecha_actualizacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_usuarios_rol",
                        column: x => x.rol_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "auditoria",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    usuario_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    entidad_afectada = table.Column<string>(type: "varchar(50)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entidad_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    accion = table.Column<string>(type: "enum('Anulacion','Retroceso_estado','Cambio_rol','Reasignacion','Configuracion_regla','Otro')", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    detalle = table.Column<string>(type: "varchar(255)", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_registro = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria", x => x.id);
                    table.ForeignKey(
                        name: "fk_auditoria_usuario",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "configuracion",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    clave = table.Column<string>(type: "varchar(100)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    valor = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descripcion = table.Column<string>(type: "varchar(255)", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    actualizado_por = table.Column<uint>(type: "int unsigned", nullable: false),
                    fecha_actualizacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracion", x => x.id);
                    table.ForeignKey(
                        name: "fk_configuracion_actualizador",
                        column: x => x.actualizado_por,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "practicas",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ficha_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    empresa_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    instructor_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    aprendiz_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    modalidad = table.Column<string>(type: "enum('Contrato de aprendizaje','Pasantía','Proyecto productivo','Monitoría')", nullable: false, defaultValue: "Contrato de aprendizaje", collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    estado = table.Column<string>(type: "enum('Pendiente','En curso','Finalizada','En riesgo')", nullable: false, defaultValue: "Pendiente", collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_practicas", x => x.id);
                    table.CheckConstraint("chk_practicas_empresa_modalidad", "(modalidad = 'Proyecto productivo' AND empresa_id IS NULL) OR (modalidad <> 'Proyecto productivo' AND empresa_id IS NOT NULL)");
                    table.CheckConstraint("chk_practicas_fechas", "fecha_fin IS NULL OR fecha_fin >= fecha_inicio");
                    table.ForeignKey(
                        name: "fk_practicas_aprendiz",
                        column: x => x.aprendiz_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_practicas_empresa",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_practicas_ficha",
                        column: x => x.ficha_id,
                        principalTable: "fichas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_practicas_instructor",
                        column: x => x.instructor_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "reglas",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(150)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    campo_evaluado = table.Column<string>(type: "varchar(100)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    operador = table.Column<string>(type: "enum('>','>=','<','<=','=','!=')", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    valor_condicion = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    umbral = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    accion_resultante = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    prioridad = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    activa = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    creado_por = table.Column<uint>(type: "int unsigned", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    fecha_actualizacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reglas", x => x.id);
                    table.ForeignKey(
                        name: "fk_reglas_creador",
                        column: x => x.creado_por,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "reportes",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    tipo = table.Column<string>(type: "enum('Individual','Grupal')", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    filtros = table.Column<string>(type: "json", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    generado_por = table.Column<uint>(type: "int unsigned", nullable: false),
                    fecha_generacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reportes", x => x.id);
                    table.ForeignKey(
                        name: "fk_reportes_generador",
                        column: x => x.generado_por,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "tokens_revocados",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    usuario_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    referencia_token = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    motivo = table.Column<string>(type: "enum('Logout','Cambio de contraseña')", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_revocacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tokens_revocados", x => x.id);
                    table.ForeignKey(
                        name: "fk_tokens_revocados_usuario",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "calificaciones_aprendiz",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    practica_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    valor = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    comentario = table.Column<string>(type: "text", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_registro = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    anulado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    anulado_por = table.Column<uint>(type: "int unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calificaciones_aprendiz", x => x.id);
                    table.CheckConstraint("chk_calificaciones_aprendiz_valor", "valor >= 0.0 AND valor <= 5.0");
                    table.ForeignKey(
                        name: "fk_calificaciones_aprendiz_anulador",
                        column: x => x.anulado_por,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_calificaciones_aprendiz_practica",
                        column: x => x.practica_id,
                        principalTable: "practicas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "calificaciones_instructor",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    practica_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    valor = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    comentario = table.Column<string>(type: "text", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_registro = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    anulado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    anulado_por = table.Column<uint>(type: "int unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calificaciones_instructor", x => x.id);
                    table.CheckConstraint("chk_calificaciones_instructor_valor", "valor >= 0.0 AND valor <= 5.0");
                    table.ForeignKey(
                        name: "fk_calificaciones_instructor_anulador",
                        column: x => x.anulado_por,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_calificaciones_instructor_practica",
                        column: x => x.practica_id,
                        principalTable: "practicas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "mensajes",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    practica_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    emisor_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    receptor_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    contenido = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_envio = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    leido = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mensajes", x => x.id);
                    table.ForeignKey(
                        name: "fk_mensajes_emisor",
                        column: x => x.emisor_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mensajes_practica",
                        column: x => x.practica_id,
                        principalTable: "practicas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mensajes_receptor",
                        column: x => x.receptor_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "seguimientos",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    practica_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    avance = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    etapa = table.Column<string>(type: "varchar(100)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_registro = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    anulado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    anulado_por = table.Column<uint>(type: "int unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seguimientos", x => x.id);
                    table.ForeignKey(
                        name: "fk_seguimientos_anulador",
                        column: x => x.anulado_por,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_seguimientos_practica",
                        column: x => x.practica_id,
                        principalTable: "practicas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "notificaciones",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    usuario_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    regla_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    tipo = table.Column<string>(type: "enum('Calificacion','Mensaje','Observacion','Riesgo')", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    contenido = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    leida = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    fecha_generacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificaciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_notificaciones_regla",
                        column: x => x.regla_id,
                        principalTable: "reglas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notificaciones_usuario",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "reporte_practica",
                columns: table => new
                {
                    reporte_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    practica_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reporte_practica", x => new { x.reporte_id, x.practica_id });
                    table.ForeignKey(
                        name: "fk_reporte_practica_practica",
                        column: x => x.practica_id,
                        principalTable: "practicas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reporte_practica_reporte",
                        column: x => x.reporte_id,
                        principalTable: "reportes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "observaciones",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    seguimiento_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    contenido = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_registro = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    anulado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    anulado_por = table.Column<uint>(type: "int unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_observaciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_observaciones_anulador",
                        column: x => x.anulado_por,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_observaciones_seguimiento",
                        column: x => x.seguimiento_id,
                        principalTable: "seguimientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "descripcion", "nombre" },
                values: new object[,]
                {
                    { 1u, "Gobierna la plataforma, configura el Motor de Reglas y genera reportes globales.", "Administrador" },
                    { 2u, "Responsable pedagógico: asigna aprendices, registra seguimiento y califica.", "Instructor" },
                    { 3u, "Realiza la práctica: consulta su seguimiento y evalua al instructor.", "Aprendiz" }
                });

            migrationBuilder.CreateIndex(
                name: "idx_auditoria_entidad",
                table: "auditoria",
                columns: new[] { "entidad_afectada", "entidad_id" });

            migrationBuilder.CreateIndex(
                name: "idx_auditoria_usuario",
                table: "auditoria",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "idx_calificaciones_aprendiz_anulado_por",
                table: "calificaciones_aprendiz",
                column: "anulado_por");

            migrationBuilder.CreateIndex(
                name: "idx_calificaciones_aprendiz_practica",
                table: "calificaciones_aprendiz",
                column: "practica_id");

            migrationBuilder.CreateIndex(
                name: "idx_calificaciones_instructor_anulado_por",
                table: "calificaciones_instructor",
                column: "anulado_por");

            migrationBuilder.CreateIndex(
                name: "idx_calificaciones_instructor_practica",
                table: "calificaciones_instructor",
                column: "practica_id");

            migrationBuilder.CreateIndex(
                name: "idx_configuracion_actualizado_por",
                table: "configuracion",
                column: "actualizado_por");

            migrationBuilder.CreateIndex(
                name: "uq_configuracion_clave",
                table: "configuracion",
                column: "clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_empresas_nit",
                table: "empresas",
                column: "nit",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_fichas_programa",
                table: "fichas",
                column: "programa_id");

            migrationBuilder.CreateIndex(
                name: "uq_fichas_numero",
                table: "fichas",
                column: "numero_ficha",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_mensajes_emisor",
                table: "mensajes",
                column: "emisor_id");

            migrationBuilder.CreateIndex(
                name: "idx_mensajes_practica",
                table: "mensajes",
                column: "practica_id");

            migrationBuilder.CreateIndex(
                name: "idx_mensajes_receptor",
                table: "mensajes",
                column: "receptor_id");

            migrationBuilder.CreateIndex(
                name: "idx_notificaciones_regla",
                table: "notificaciones",
                column: "regla_id");

            migrationBuilder.CreateIndex(
                name: "idx_notificaciones_usuario",
                table: "notificaciones",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "idx_observaciones_anulado_por",
                table: "observaciones",
                column: "anulado_por");

            migrationBuilder.CreateIndex(
                name: "idx_observaciones_seguimiento",
                table: "observaciones",
                column: "seguimiento_id");

            migrationBuilder.CreateIndex(
                name: "idx_practicas_aprendiz",
                table: "practicas",
                column: "aprendiz_id");

            migrationBuilder.CreateIndex(
                name: "idx_practicas_empresa",
                table: "practicas",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "idx_practicas_estado",
                table: "practicas",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "idx_practicas_ficha",
                table: "practicas",
                column: "ficha_id");

            migrationBuilder.CreateIndex(
                name: "idx_practicas_instructor",
                table: "practicas",
                column: "instructor_id");

            migrationBuilder.CreateIndex(
                name: "uq_programas_nombre",
                table: "programas",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_reglas_activa_prioridad",
                table: "reglas",
                columns: new[] { "activa", "prioridad" });

            migrationBuilder.CreateIndex(
                name: "idx_reglas_creado_por",
                table: "reglas",
                column: "creado_por");

            migrationBuilder.CreateIndex(
                name: "idx_reporte_practica_practica",
                table: "reporte_practica",
                column: "practica_id");

            migrationBuilder.CreateIndex(
                name: "idx_reportes_generador",
                table: "reportes",
                column: "generado_por");

            migrationBuilder.CreateIndex(
                name: "uq_roles_nombre",
                table: "roles",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_seguimientos_anulado_por",
                table: "seguimientos",
                column: "anulado_por");

            migrationBuilder.CreateIndex(
                name: "idx_seguimientos_practica",
                table: "seguimientos",
                column: "practica_id");

            migrationBuilder.CreateIndex(
                name: "idx_tokens_revocados_usuario",
                table: "tokens_revocados",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "uq_tokens_revocados_referencia",
                table: "tokens_revocados",
                column: "referencia_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_usuarios_rol",
                table: "usuarios",
                column: "rol_id");

            migrationBuilder.CreateIndex(
                name: "uq_usuarios_correo",
                table: "usuarios",
                column: "correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria");

            migrationBuilder.DropTable(
                name: "calificaciones_aprendiz");

            migrationBuilder.DropTable(
                name: "calificaciones_instructor");

            migrationBuilder.DropTable(
                name: "configuracion");

            migrationBuilder.DropTable(
                name: "mensajes");

            migrationBuilder.DropTable(
                name: "notificaciones");

            migrationBuilder.DropTable(
                name: "observaciones");

            migrationBuilder.DropTable(
                name: "reporte_practica");

            migrationBuilder.DropTable(
                name: "tokens_revocados");

            migrationBuilder.DropTable(
                name: "reglas");

            migrationBuilder.DropTable(
                name: "seguimientos");

            migrationBuilder.DropTable(
                name: "reportes");

            migrationBuilder.DropTable(
                name: "practicas");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "empresas");

            migrationBuilder.DropTable(
                name: "fichas");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "programas");
        }
    }
}
