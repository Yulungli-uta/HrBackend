namespace WsUtaSystem.Application.DTOs.Dinardap;

/// <summary>
/// Misma forma que WsUtaDinardap.Api.Domain (RegistroCivilDto). Deliberadamente NO se
/// comparte el ensamblado entre ambos proyectos - están a los dos lados de un límite HTTP,
/// solo se comparte el contrato/forma de los datos (mismo criterio que DatosDINARDAP.Client).
/// </summary>
public sealed record DinardapRegistroCivilDto(
    string? Codigo,
    string? NombreCompleto,
    string? Nombres,
    string? Apellido1,
    string? Apellido2,
    string? Genero,
    string? CondicionCiudadano,
    DateTime? FechaNacimiento,
    string? LugarNacimiento,
    string? LugarNacimientoProvincia,
    string? LugarNacimientoCiudad,
    string? LugarNacimientoParroquia,
    string? Nacionalidad,
    string? EstadoCivil,
    string? Conyuge,
    string? NombrePadre,
    string? NombreMadre);

public sealed record DinardapTceDto(
    string? NumeroCertificado,
    DateTime? FechaSufragio,
    bool? Sufrago);

public sealed record DinardapTituloDto(
    string? NivelNombre,
    int Nivel,
    DateTime? FechaGrado,
    DateTime? FechaRegistro,
    string? InstitucionEducacionSuperior,
    string? NombreTitulo,
    string? NumeroRegistro,
    string? Tipo,
    string? TipoExtranjeroColegio);
