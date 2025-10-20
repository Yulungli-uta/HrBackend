param(
  [Parameter(Mandatory=$true, Position=0)]
  [ValidateSet('new-command','new-query','new-job')]
  [string]$Action,

  [Parameter(Mandatory=$true)]
  [string]$Feature,

  [Parameter(Mandatory=$true)]
  [string]$Name,

  [string]$Props,          # "Type Prop,Type Prop2,int? X"
  [string]$Summary = "",
  [string]$Command,        # para new-job: "Feature.CommandName"
  [string]$Cron = "0 0 2 * * ?",   # Quartz cron (2:00 AM)
  [string]$TimeZone = "America/Guayaquil"
)

function Ensure-Dir($p) { if (!(Test-Path $p)) { New-Item -ItemType Directory -Force -Path $p | Out-Null } }

$root = Split-Path -Parent $PSCommandPath
$solutionRoot = Resolve-Path "$root\.."
$app = Join-Path $solutionRoot "src\HrBackend.Application"
$infra = Join-Path $solutionRoot "src\HrBackend.Infrastructure"
$web = Join-Path $solutionRoot "src\HrBackend.WebApi"

if (!(Test-Path $app) -or !(Test-Path $infra) -or !(Test-Path $web)) {
  Write-Error "No se encontró la estructura src/HrBackend.(Application|Infrastructure|WebApi). Ejecuta en la raíz del repo."
  exit 1
}

$nsApp   = "HrBackend.Application"
$nsInfra = "HrBackend.Infrastructure"
$nsWeb   = "HrBackend.WebApi"

$FeaturePascal = $Feature
$NamePascal = $Name

switch ($Action) {
# ===========================
# new-command
# ===========================
'new-command' {
  # Rutas destino
  $featureDir = Join-Path $app "$FeaturePascal"
  $commandsDir = Join-Path $featureDir "Commands"
  Ensure-Dir $commandsDir
  $endpointsDir = Join-Path $web "Endpoints"
  Ensure-Dir $endpointsDir

  # Parse Props -> firma record/handler & mapping
  $propList = @()
  if ($Props) { $propList = $Props.Split(",") | ForEach-Object { $_.Trim() } }
  $recordProps = if ($Props) { $Props } else { "" }

  # Archivos
  $cmdFile = Join-Path $commandsDir "$($NamePascal)Command.cs"
  $endpointFile = Join-Path $endpointsDir "$($FeaturePascal)Endpoints.cs"

  # ----- Command + Validator + Handler -----
  $cmdContent = @"
using FluentValidation;
using MediatR;
using $nsApp.Abstractions;
using System.Data;

namespace $nsApp.$FeaturePascal.Commands;

public sealed record $($NamePascal)Command($recordProps) : IRequest<Unit>;

public sealed class $($NamePascal)Validator : AbstractValidator<$($NamePascal)Command>
{
    public $($NamePascal)Validator()
    {
        // TODO: Reglas mínimas
        // RuleFor(x => x.From).LessThanOrEqualTo(x => x.To);
    }
}

public sealed class $($NamePascal)Handler(ISqlExecutor db) : IRequestHandler<$($NamePascal)Command, Unit>
{
    public async Task<Unit> Handle($($NamePascal)Command req, CancellationToken ct)
    {
        // TODO: Invocar SPs necesarios (ejemplo)
        // await db.ExecSPAsync("HR.sp_Attendance_CalculateRange", cmd =>
        // {
        //     var p1 = cmd.CreateParameter(); p1.ParameterName="@FromDate"; p1.Value=req.From.ToDateTime(TimeOnly.MinValue); cmd.Parameters.Add(p1);
        //     var p2 = cmd.CreateParameter(); p2.ParameterName="@ToDate";   p2.Value=req.To.ToDateTime(TimeOnly.MinValue);   cmd.Parameters.Add(p2);
        //     if (req.EmployeeId is int id) { var p3=cmd.CreateParameter(); p3.ParameterName="@EmployeeID"; p3.Value=id; cmd.Parameters.Add(p3); }
        // }, ct);

        return Unit.Value;
    }
}
"@

  Set-Content -Encoding UTF8 $cmdFile $cmdContent
  Write-Host "✔ Command/Validator/Handler: $cmdFile"

  # ----- Endpoint Carter (append por feature) -----
  $endpointExists = Test-Path $endpointFile
  if (-not $endpointExists) {
    $moduleContent = @"
using Carter;
using MediatR;
using $nsApp.$FeaturePascal.Commands;

namespace $nsWeb.Endpoints;

public sealed class $($FeaturePascal)Endpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // $Summary
        app.MapPost("/$($FeaturePascal.ToLower())/$($NamePascal.ToLower())", 
            async (IMediator mediator, [$([string]::IsNullOrEmpty($Props) ? "" : "AsParameters")] $($NamePascal)Command cmd, CancellationToken ct) =>
        {
            await mediator.Send(cmd, ct);
            return Results.Ok(new { message = "$($NamePascal) ok" });
        })
        .WithSummary("$($Summary)")
        .WithName("$($FeaturePascal)_$($NamePascal)");
    }
}
"@
    Set-Content -Encoding UTF8 $endpointFile $moduleContent
    Write-Host "✔ Endpoint Carter creado: $endpointFile"
  }
  else {
    # Anexar otra ruta en el mismo módulo
    $append = @"
// $Summary
app.MapPost("/$($FeaturePascal.ToLower())/$($NamePascal.ToLower())", 
    async (IMediator mediator, [$([string]::IsNullOrEmpty($Props) ? "" : "AsParameters")] $($NamePascal)Command cmd, CancellationToken ct) =>
{
    await mediator.Send(cmd, ct);
    return Results.Ok(new { message = "$($NamePascal) ok" });
})
.WithSummary("$($Summary)")
.WithName("$($FeaturePascal)_$($NamePascal)");
"
    Add-Content -Encoding UTF8 $endpointFile $append
    Write-Host "✔ Endpoint Carter actualizado: $endpointFile"
  }
}
# ===========================
# new-query (similar a command, pero IRequest<T>)
# ===========================
'new-query' {
  $featureDir = Join-Path $app "$FeaturePascal"
  $queriesDir = Join-Path $featureDir "Queries"
  Ensure-Dir $queriesDir
  $endpointsDir = Join-Path $web "Endpoints"
  Ensure-Dir $endpointsDir

  $recordProps = if ($Props) { $Props } else { "" }
  $dtoName = "$($NamePascal)Dto"

  $queryFile = Join-Path $queriesDir "$($NamePascal)Query.cs"
  $endpointFile = Join-Path $endpointsDir "$($FeaturePascal)Endpoints.cs"

  $qContent = @"
using FluentValidation;
using MediatR;
using $nsApp.Abstractions;
using System.Data;

namespace $nsApp.$FeaturePascal.Queries;

public sealed record $($NamePascal)Query($recordProps) : IRequest<IReadOnlyList<$dtoName>>;
public sealed record $dtoName();

public sealed class $($NamePascal)Validator : AbstractValidator<$($NamePascal)Query>
{
    public $($NamePascal)Validator()
    {
        // TODO: Reglas
    }
}

public sealed class $($NamePascal)Handler(ISqlExecutor db) : IRequestHandler<$($NamePascal)Query, IReadOnlyList<$dtoName>>
{
    public async Task<IReadOnlyList<$dtoName>> Handle($($NamePascal)Query req, CancellationToken ct)
    {
        // TODO: Ejecutar SELECT / SP y mapear a DTO
        return Array.Empty<$dtoName>();
    }
}
"
  Set-Content -Encoding UTF8 $queryFile $qContent
  Write-Host "✔ Query + DTO: $queryFile"

  if (-not (Test-Path $endpointFile)) {
    $moduleContent = @"
using Carter;
using MediatR;
using $nsApp.$FeaturePascal.Queries;

namespace $nsWeb.Endpoints;

public sealed class $($FeaturePascal)Endpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/$($FeaturePascal.ToLower())/$($NamePascal.ToLower())", 
            async (IMediator mediator, [$([string]::IsNullOrEmpty($Props) ? "" : "AsParameters")] $($NamePascal)Query query, CancellationToken ct) =>
        {
            var data = await mediator.Send(query, ct);
            return Results.Ok(data);
        })
        .WithSummary("$($Summary)")
        .WithName("$($FeaturePascal)_$($NamePascal)_Query");
    }
}
"
    Set-Content -Encoding UTF8 $endpointFile $moduleContent
    Write-Host "✔ Endpoint Carter creado: $endpointFile"
  } else {
    $append = @"
app.MapGet("/$($FeaturePascal.ToLower())/$($NamePascal.ToLower())", 
    async (IMediator mediator, [$([string]::IsNullOrEmpty($Props) ? "" : "AsParameters")] $($NamePascal)Query query, CancellationToken ct) =>
{
    var data = await mediator.Send(query, ct);
    return Results.Ok(data);
})
.WithSummary("$($Summary)")
.WithName("$($FeaturePascal)_$($NamePascal)_Query");
"
    Add-Content -Encoding UTF8 $endpointFile $append
    Write-Host "✔ Endpoint Carter actualizado: $endpointFile"
  }
}
# ===========================
# new-job (Quartz que dispara un Command)
# ===========================
'new-job' {
  if (-not $Command) {
    Write-Error "-Command es requerido (formato Feature.CommandName)"
    exit 1
  }

  $parts = $Command.Split(".")
  if ($parts.Count -ne 2) {
    Write-Error "-Command debe ser 'Feature.CommandName'"
    exit 1
  }
  $cmdFeature = $parts[0]
  $cmdName = $parts[1]

  $jobsDir = Join-Path $infra "Jobs"
  Ensure-Dir $jobsDir

  $jobFile = Join-Path $jobsDir "$($NamePascal)Job.cs"
  $jobClass = "$($NamePascal)Job"

  $jobContent = @"
using MediatR;
using Quartz;
using $nsApp.$cmdFeature.Commands;

namespace $nsInfra.Jobs;

[DisallowConcurrentExecution]
public sealed class $jobClass(IMediator mediator) : IJob
{
    public async Task Execute(IJobExecutionContext ctx)
    {
        var tzId = ctx.MergedJobDataMap.GetString(""TimeZone"") ?? ""$TimeZone"";
        var tz   = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var now  = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

        // TODO: Ajustar parámetros reales del Command
        var cmd = new $cmdNameCommand();

        await mediator.Send(cmd, ctx.CancellationToken);
    }
}
"
  Set-Content -Encoding UTF8 $jobFile $jobContent
  Write-Host "✔ Job Quartz: $jobFile"

  # Instrucciones para agregar el trigger (una sola vez en Infrastructure.DependencyInjection)
  Write-Host ""
  Write-Host "🔧 Agrega este bloque en Infrastructure.DependencyInjection (dentro de services.AddQuartz):"
  Write-Host "--------------------------------------------------------------------------"
  Write-Host @"
    var $($NamePascal.ToLower())Key = new JobKey(""$jobClass"");
    q.AddJob<$jobClass>(o => o.WithIdentity($($NamePascal.ToLower())Key));
    q.AddTrigger(t => t.ForJob($($NamePascal.ToLower())Key)
        .WithIdentity(""$($jobClass)Trigger"")
        .WithSchedule(CronScheduleBuilder.CronSchedule(""$Cron"")
            .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(""$TimeZone"")))
        .UsingJobData(""TimeZone"", ""$TimeZone""));
"@
  Write-Host "--------------------------------------------------------------------------"
}
}
