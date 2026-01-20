using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace WsUtaSystem.Infrastructure.Interceptors
{
    public sealed class SqlErrorLoggingInterceptor : DbCommandInterceptor
    {
        private readonly ILogger<SqlErrorLoggingInterceptor> _logger;

        public SqlErrorLoggingInterceptor(ILogger<SqlErrorLoggingInterceptor> logger)
        {
            _logger = logger;
        }

        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
            _logger.LogError(
                eventData.Exception,
                "EF SQL FAILED\nCommandText:\n{Sql}\nParameters:\n{Params}",
                command.CommandText,
                string.Join(", ", command.Parameters
                    .Cast<DbParameter>()
                    .Select(p => $"{p.ParameterName}={p.Value ?? "NULL"}"))
            );

            base.CommandFailed(command, eventData);
        }
    }
}
