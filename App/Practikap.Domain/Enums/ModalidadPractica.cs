namespace Practikap.Domain.Enums;

/// <summary>
/// Modalidad bajo la cual el aprendiz desarrolla su practica productiva.
/// Corresponde a la columna practicas.modalidad del Script_DDL.sql.
/// Solo <see cref="ProyectoProductivo"/> admite practica sin empresa asociada,
/// restriccion que la base de datos garantiza con chk_practicas_empresa_modalidad.
/// </summary>
public enum ModalidadPractica
{
    /// <summary>Literal en base de datos: "Contrato de aprendizaje".</summary>
    ContratoDeAprendizaje = 1,

    /// <summary>Literal en base de datos: "Pasantia" (con tilde en la i).</summary>
    Pasantia = 2,

    /// <summary>Unica modalidad sin empresa. Literal en base de datos: "Proyecto productivo".</summary>
    ProyectoProductivo = 3,

    /// <summary>Literal en base de datos: "Monitoria" (con tilde en la i).</summary>
    Monitoria = 4
}
