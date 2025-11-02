param(
  [Parameter(Mandatory=$true, Position=0)]
  [ValidateSet('new-command','new-query','new-job')]
  [string]$Action,

  [Parameter(Mandatory=$true)]
  [string]$Feature,

  [Parameter(Mandatory=$true)]
  [string]$Name,

  [string]$Props = "",          # "Type Prop,Type Prop2,int? X"
  [string]$Summary = "",
  [string]$Command = "",        # para new-job: "Feature.CommandName"
  [string]$Cron = "0 0 2 * * ?",   # Quartz cron (2:00 AM)
  [string]$TimeZone = "America/Guayaquil"
)

function Ensure-Dir($p) { if (!(Test-Path $p)) { New-Item -ItemType Directory -Force -Path $p | Out-Null } }

# Rutas base relativas al script
$solutionRoot = Resolve-Path (Join-Path (Split-Path -Parent $PSCommandPath) "..")
$app  = Join-Path $solutionRoot "src\HrBackend.Application"
$infra= Join-Path $solutionRoot "src\HrBackend.Infrastructure"
$web  = Join-Path $solutionRoot "src\HrBackend.WebApi"

if (!(Test-Path $app) -or !(Test-Path $infra) -or !(Test-Path $web)) {
  Write-Error "No se encontró la estructura src/HrBackend.(Application|Infrastructure|WebApi). Ejecuta en la raíz del repo."
  exit 1
}

$NS_APP   = "HrBackend.Application"
$NS_INFRA = "HrBackend.Infrastructure"
$NS_WEB   = "HrBackend.WebApi"

$FeaturePascal = $Feature
$NamePascal = $Name

# Helpers para plantillas
function Fill-Template {
  param($template, $map)
  $out = $template
  foreach ($k in $map.Keys) { $out = $out.Replace($k, [string]$map[$k]) }
  return $out
}

switch ($Action) {

# =====================================================================
# new-command
# =====================================================================
'new-command' {
  $featureDir  = Join-Path $app "$FeaturePascal"
  $commandsDir = Join-Path $featureDir "Commands"
  Ensure-Dir $commandsDir

  $endpointsDir = Join-Path $web "Endpoints"
  Ensure-Dir $endpointsDir

  $recordProps = $Props

  $cmdFile = Join-Path $commandsDir "$($NamePascal)Command.cs"
  $endpointFile = Join-Path $endpointsDir "$($FeaturePascal)Endpoints.cs"

  # ----- TEMPLATE: Command + Validator + Handler -----
  $cmdTpl = @'
using FluentValidation;
using MediatR;
using __NS_APP__.Abstractions;
using System.Data;

namespace __NS_APP__.__FEATURE__.Commands;

public sealed record __NAME__Command(__RECORD_PROPS__) : IRequest<Unit>;

public sealed class __NAME__Validator : AbstractValidator<__NAME__Command>
{
    public __NAME__Validator()
    {
        // TODO: Reglas de validación
        // Example:
        // RuleFor(x => x.From).LessThanOrEqualTo(x => x.To);
    }
}

public sealed class __NAME__Handler(ISqlExecutor db) : IRequestHandler<__NAME__Command, Unit>
{
    public async Task<Unit> Handle(__NAME__Command req, CancellationToken ct)
    {
        // TODO: Invocar tus SPs aquí con db.ExecSPAsync(...)
        return Unit.Value;
    }
}
'@

  $cmdContent = Fill-Template $cmdTpl @{
    '__NS_APP__'       = $NS_APP
    '__FEATURE__'      = $FeaturePascal
    '__NAME__'         = $NamePascal
    '__RECORD_PROPS__' = $recordProps
  }
  Set-Content -Encoding UTF8 $cmdFile $cmdContent
  Write-Host "✔ Creado: $cmdFile"

  # ----- TEMPLATE: Endpoint Carter (crea o añade ruta) -----
  $endpointHeaderTpl = @'
using Carter;
using MediatR;
using __NS_APP__.__FEATURE__.Commands;

namespace __NS_WEB__.Endpoints;

public sealed class __FEATURE__Endpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // __SUMMARY__
        app.MapPost("/__feature_l__/__name_l__", async (IMediator mediator, __NAME__Command cmd, CancellationToken ct) =>
        {
            await mediator.Send(cmd, ct);
            return Results.Ok(new { message = "__NAME__ ok" });
        })
        .WithSummary("__SUMMARY__")
        .WithName("__FEATURE____NAME__");
    }
}
'@

  $endpointAppendTpl = @'
// __SUMMARY__
app.MapPost("/__feature_l__/__name_l__", async (IMediator mediator, __NAME__Command cmd, CancellationToken ct) =>
{
    await mediator.Send(cmd, ct);
    return Results.Ok(new { message = "__NAME__ ok" });
})
.WithSummary("__SUMMARY__")
.WithName("__FEATURE____NAME__");
'@

  if (!(Test-Path $endpointFile)) {
    $endpointContent = Fill-Template $endpointHeaderTpl @{
      '__NS_APP__'     = $NS_APP
      '__NS_WEB__'     = $NS_WEB
      '__FEATURE__'    = $FeaturePascal
      '__feature_l__'  = $FeaturePascal.ToLower()
      '__NAME__'       = $NamePascal
      '__name_l__'     = $NamePascal.ToLower()
      '__SUMMARY__'    = $Summary
    }
    Set-Content -Encoding UTF8 $endpointFile $endpointContent
    Write-Host "✔ Creado: $endpointFile"
  } else {
    $append = Fill-Template $endpointAppendTpl @{
      '__FEATURE__'    = $FeaturePascal
      '__feature_l__'  = $FeaturePascal.ToLower()
      '__NAME__'       = $NamePascal
      '__name_l__'     = $NamePascal.ToLower()
      '__SUMMARY__'    = $Summary
    }
    Add-Content -Encoding UTF8 $endpointFile $append
    Write-Host "✔ Actualizado: $endpointFile (ruta añadida)"
  }
}
# =====================================================================
# new-query
# =====================================================================
'new-query' {
  $featureDir  = Join-Path $app "$FeaturePascal"
  $queriesDir  = Join-Path $featureDir "Queries"
  Ensure-Dir $queriesDir

  $endpointsDir = Join-Path $web "Endpoints"
  Ensure-Dir $endpointsDir

  $recordProps = $Props
  $dtoName = "${NamePascal}Dto"

  $queryFile = Join-Path $queriesDir "$($NamePascal)Query.cs"
  $endpointFile = Join-Path $endpointsDir "$($FeaturePascal)Endpoints.cs"

  $queryTpl = @'
using FluentValidation;
using MediatR;
using __NS_APP__.Abstractions;
using System.Data;

namespace __NS_APP__.__FEATURE__.Queries;

public sealed record __NAME__Query(__RECORD_PROPS__) : IRequest<IReadOnlyList<__DTO__>>;
public sealed record __DTO__;

public sealed class __NAME__Validator : AbstractValidator<__NAME__Query>
{
    public __NAME__Validator()
    {
        // TODO: Reglas
    }
}

public sealed class __NAME__Handler(ISqlExecutor db) : IRequestHandler<__NAME__Query, IReadOnlyList<__DTO__>>
{
    public async Task<IReadOnlyList<__DTO__>> Handle(__NAME__Query req, CancellationToken ct)
    {
        // TODO: SELECT/SP -> mapear a DTO
        return Array.Empty<__DTO__>();
    }
}
'@

  $qContent = Fill-Template $queryTpl @{
    '__NS_APP__'       = $NS_APP
    '__FEATURE__'      = $FeaturePascal
    '__NAME__'         = $NamePascal
    '__DTO__'          = $dtoName
    '__RECORD_PROPS__' = $recordProps
  }
  Set-Content -Encoding UTF8 $queryFile $qContent
  Write-Host "✔ Creado: $queryFile"

  # ----- TEMPLATE: Endpoint Carter (crea o añade ruta) -----
  $endpointHeaderTpl = @'
using Carter;
using MediatR;
using __NS_APP__.__FEATURE__.Queries;

namespace __NS_WEB__.Endpoints;

public sealed class __FEATURE__Endpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/__feature_l__/__name_l__", async (IMediator mediator, [AsParameters] __NAME__Query query, CancellationToken ct) =>
        {
            var data = await mediator.Send(query, ct);
            return Results.Ok(data);
        })
        .WithSummary("__SUMMARY__")
        .WithName("__FEATURE____NAME___Query");
    }
}
'@

  $endpointAppendTpl = @'
app.MapGet("/__feature_l__/__name_l__", async (IMediator mediator, [AsParameters] __NAME__Query query, CancellationToken ct) =>
{
    var data = await mediator.Send(query, ct);
    return Results.Ok(data);
})
.WithSummary("__SUMMARY__")
.WithName("__FEATURE____NAME___Query");
'@

  if (!(Test-Path $endpointFile)) {
    $endpointContent = Fill-Template $endpointHeaderTpl @{
      '__NS_APP__'     = $NS_APP
      '__NS_WEB__'     = $NS_WEB
      '__FEATURE__'    = $FeaturePascal
      '__feature_l__'  = $FeaturePascal.ToLower()
      '__NAME__'       = $NamePascal
      '__name_l__'     = $NamePascal.ToLower()
      '__SUMMARY__'    = $Summary
    }
    Set-Content -Encoding UTF8 $endpointFile $endpointContent
    Write-Host "✔ Creado: $endpointFile"
  } else {
    $append = Fill-Template $endpointAppendTpl @{
      '__FEATURE__'    = $FeaturePascal
      '__feature_l__'  = $FeaturePascal.ToLower()
      '__NAME__'       = $NamePascal
      '__name_l__'     = $NamePascal.ToLower()
      '__SUMMARY__'    = $Summary
    }
    Add-Content -Encoding UTF8 $endpointFile $append
    Write-Host "✔ Actualizado: $endpointFile (ruta añadida)"
  }
}
# =====================================================================
# new-job
# =====================================================================
'new-job' {
  if ([string]::IsNullOrWhiteSpace($Command)) {
    Write-Error "-Command es requerido (formato Feature.CommandName)"
    exit 1
  }
  $parts = $Command.Split(".")
  if ($parts.Count -ne 2) {
    Write-Error "-Command debe ser 'Feature.CommandName'"
    exit 1
  }
  $cmdFeature = $parts[0]
  $cmdName    = $parts[1]

  $jobsDir = Join-Path $infra "Jobs"
  Ensure-Dir $jobsDir

  $jobFile  = Join-Path $jobsDir "$($NamePascal)Job.cs"
  $jobClass = "$($NamePascal)Job"

  $jobTpl = @'
using MediatR;
using Quartz;
using __NS_APP__.__CMDFEATURE__.Commands;

namespace __NS_INFRA__.Jobs;

[DisallowConcurrentExecution]
public sealed class __JOBCLASS__(IMediator mediator) : IJob
{
    public async Task Execute(IJobExecutionContext ctx)
    {
        var tzId = ctx.MergedJobDataMap.GetString("TimeZone") ?? "__TIMEZONE__";
        var tz   = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var now  = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

        // TODO: Ajustar parámetros del Command real
        var cmd = new __CMDNAME__Command();

        await mediator.Send(cmd, ctx.CancellationToken);
    }
}
'@

  $jobContent = Fill-Template $jobTpl @{
    '__NS_APP__'     = $NS_APP
    '__NS_INFRA__'   = $NS_INFRA
    '__CMDFEATURE__' = $cmdFeature
    '__JOBCLASS__'   = $jobClass
    '__TIMEZONE__'   = $TimeZone
    '__CMDNAME__'    = $cmdName
  }
  Set-Content -Encoding UTF8 $jobFile $jobContent
  Write-Host "✔ Creado: $jobFile"

  Write-Host ""
  Write-Host "🔧 Pega este bloque en tu configuración de Quartz (Infrastructure.DependencyInjection) dentro de AddQuartz:"
  Write-Host "--------------------------------------------------------------------------------"
  Write-Host @"
    var $($NamePascal.ToLower())Key = new JobKey(""$jobClass"");
    q.AddJob<$jobClass>(o => o.WithIdentity($($NamePascal.ToLower())Key));
    q.AddTrigger(t => t.ForJob($($NamePascal.ToLower())Key)
        .WithIdentity(""$($jobClass)Trigger"")
        .WithSchedule(CronScheduleBuilder.CronSchedule(""$Cron"")
            .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(""$TimeZone"")))
        .UsingJobData(""TimeZone"", ""$TimeZone""));
"@
  Write-Host "--------------------------------------------------------------------------------"
}
}
