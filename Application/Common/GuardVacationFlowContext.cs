namespace WsUtaSystem.Application.Common
{
    /// <summary>
    /// Marca, dentro de la petición actual, que las vacaciones se están registrando desde el flujo de
    /// planes de guardias (supervisor planifica, dirección aprueba). VacationsService rechaza que un
    /// guardia cree o modifique vacaciones por el módulo general, salvo cuando este contexto está activo.
    /// Es de ámbito por petición (scoped): no se puede activar desde la API.
    /// </summary>
    public sealed class GuardVacationFlowContext
    {
        private int _depth;

        /// <summary>Indica si el código actual corre dentro del flujo de planes de vacaciones de guardias.</summary>
        public bool IsActive => _depth > 0;

        /// <summary>Activa el contexto hasta que se libere el objeto devuelto.</summary>
        public IDisposable Begin()
        {
            _depth++;
            return new Scope(this);
        }

        private sealed class Scope : IDisposable
        {
            private GuardVacationFlowContext? _owner;
            public Scope(GuardVacationFlowContext owner) => _owner = owner;

            public void Dispose()
            {
                if (_owner is null) return;
                _owner._depth--;
                _owner = null;
            }
        }
    }
}
