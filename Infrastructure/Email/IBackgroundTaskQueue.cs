using System.Threading.Channels;

namespace WsUtaSystem.Infrastructure.Email
{
    public interface IBackgroundTaskQueue<T>
    {
        ValueTask EnqueueAsync(T item, CancellationToken ct = default);
        ValueTask<T> DequeueAsync(CancellationToken ct);
        int Capacity { get; }
        int Count { get; }
    }

    /// <summary>
    /// Cola bounded en memoria con manejo thread-safe mejorado.
    /// CORREGIDO: Sincronización apropiada del contador y mejor manejo de excepciones.
    /// </summary>
    public sealed class BackgroundTaskQueue<T> : IBackgroundTaskQueue<T>
    {
        private readonly Channel<T> _channel;
        private readonly TimeSpan _enqueueTimeout;
        private readonly ILogger<BackgroundTaskQueue<T>> _logger;

        public int Capacity { get; }

        // El Channel ya maneja el conteo internamente, usamos su Reader.Count
        public int Count => _channel.Reader.Count;

        public BackgroundTaskQueue(
            int capacity,
            TimeSpan? enqueueTimeout = null,
            ILogger<BackgroundTaskQueue<T>>? logger = null)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than 0.");

            Capacity = capacity;
            _enqueueTimeout = enqueueTimeout ?? TimeSpan.FromSeconds(2);
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<BackgroundTaskQueue<T>>.Instance;

            _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                SingleWriter = false,

                // DropOldest: descarta el item más antiguo cuando la cola está llena
                FullMode = BoundedChannelFullMode.DropOldest
            });
        }

        public async ValueTask EnqueueAsync(T item, CancellationToken ct = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // Intento rápido sin allocación
            if (_channel.Writer.TryWrite(item))
            {
                return;
            }

            // Si la cola está llena, intentamos con timeout
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(_enqueueTimeout);

            try
            {
                await _channel.Writer.WriteAsync(item, linkedCts.Token);
            }
            catch (OperationCanceledException) when (linkedCts.Token.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                // Timeout propio de enqueue (no del caller)
                _logger.LogWarning(
                    "[QUEUE] Enqueue timeout after {TimeoutMs}ms. Queue full (capacity={Capacity}). Item dropped.",
                    _enqueueTimeout.TotalMilliseconds,
                    Capacity);

                throw new InvalidOperationException(
                    $"Background queue is full (capacity={Capacity}). Enqueue timed out after {_enqueueTimeout.TotalMilliseconds}ms. Item was dropped.");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Cancelación del caller
                _logger.LogInformation("[QUEUE] Enqueue cancelled by caller.");
                throw;
            }
        }

        public async ValueTask<T> DequeueAsync(CancellationToken ct)
        {
            try
            {
                return await _channel.Reader.ReadAsync(ct);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[QUEUE] Dequeue cancelled.");
                throw;
            }
        }
    }
}
