using System.Diagnostics;
using WsUtaSystem.Application.DTOs.Email;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Email
{
    /// <summary>
    /// MEJORADO: Retry policy con exponential backoff y circuit breaker para evitar cascada de fallos.
    /// </summary>
    public class EmailQueueWorker : BackgroundService
    {
        private readonly IBackgroundTaskQueue<EmailSendRequestDto> _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailQueueWorker> _logger;

        // Circuit breaker configuration
        private int _consecutiveFailures = 0;
        private DateTime? _circuitOpenedAt = null;
        private const int MaxConsecutiveFailures = 5;
        private static readonly TimeSpan CircuitBreakerResetTime = TimeSpan.FromMinutes(5);

        // Retry configuration
        private const int MaxRetries = 3;
        private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(2);

        public EmailQueueWorker(
            IBackgroundTaskQueue<EmailSendRequestDto> queue,
            IServiceScopeFactory scopeFactory,
            ILogger<EmailQueueWorker> logger)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[EMAIL-WORKER] START");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Circuit breaker check
                if (IsCircuitOpen())
                {
                    _logger.LogWarning(
                        "[EMAIL-WORKER] Circuit OPEN. Waiting {Seconds}s before retry. Failures={Count}",
                        CircuitBreakerResetTime.TotalSeconds,
                        _consecutiveFailures);

                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                    // Check if circuit should be reset
                    if (DateTime.Now - _circuitOpenedAt >= CircuitBreakerResetTime)
                    {
                        ResetCircuitBreaker();
                        _logger.LogInformation("[EMAIL-WORKER] Circuit HALF-OPEN. Attempting recovery.");
                    }

                    continue;
                }

                EmailSendRequestDto? req = null;

                try
                {
                    req = await _queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EMAIL-WORKER] Error reading from queue.");
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                // Process with retry
                await ProcessEmailWithRetryAsync(req, stoppingToken);
            }

            _logger.LogInformation("[EMAIL-WORKER] STOP");
        }

        private async Task ProcessEmailWithRetryAsync(
            EmailSendRequestDto req,
            CancellationToken stoppingToken)
        {
            var sw = Stopwatch.StartNew();
            Exception? lastException = null;

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sender = scope.ServiceProvider.GetRequiredService<IEmailSenderService>();

                    // Timeout del worker (independiente del HTTP request)
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        stoppingToken,
                        timeoutCts.Token);

                    if (attempt > 0)
                    {
                        _logger.LogInformation(
                            "[EMAIL-WORKER] RETRY attempt={Attempt}/{Max} to={To} subject={Subject}",
                            attempt, MaxRetries, req.To, req.Subject);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "[EMAIL-WORKER] SEND START to={To} subject={Subject} attachments={AttCount}",
                            req.To, req.Subject, req.Attachments?.Count ?? 0);
                    }

                    var res = await sender.SendAsync(req, linkedCts.Token);

                    sw.Stop();

                    if (!res.Success)
                    {
                        _logger.LogWarning(
                            "[EMAIL-WORKER] SEND FAILED EmailLogID={Id} Msg={Msg} in {Ms}ms attempt={Attempt}",
                            res.EmailLogID, res.Message, sw.ElapsedMilliseconds, attempt);

                        lastException = new InvalidOperationException(res.Message ?? "Send failed");

                        // Si es el último intento, incrementar failures
                        if (attempt == MaxRetries)
                        {
                            IncrementCircuitBreakerFailure();
                        }
                        else
                        {
                            // Exponential backoff
                            var delay = InitialRetryDelay * Math.Pow(2, attempt);
                            await Task.Delay(delay, stoppingToken);
                            continue; // Retry
                        }
                    }
                    else
                    {
                        _logger.LogInformation(
                            "[EMAIL-WORKER] SEND OK EmailLogID={Id} in {Ms}ms attempt={Attempt}",
                            res.EmailLogID, sw.ElapsedMilliseconds, attempt);

                        // Success: reset circuit breaker
                        ResetCircuitBreaker();
                        return; // Success, no retry needed
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    sw.Stop();
                    _logger.LogInformation(
                        "[EMAIL-WORKER] SEND CANCELLED (shutdown) to={To} in {Ms}ms",
                        req.To, sw.ElapsedMilliseconds);
                    return; // Don't retry on shutdown
                }
                catch (OperationCanceledException)
                {
                    sw.Stop();
                    _logger.LogWarning(
                        "[EMAIL-WORKER] SEND TIMEOUT to={To} subject={Subject} in {Ms}ms attempt={Attempt}",
                        req.To, req.Subject, sw.ElapsedMilliseconds, attempt);

                    lastException = new TimeoutException("Send operation timed out");

                    if (attempt == MaxRetries)
                    {
                        IncrementCircuitBreakerFailure();
                    }
                    else
                    {
                        var delay = InitialRetryDelay * Math.Pow(2, attempt);
                        await Task.Delay(delay, stoppingToken);
                        continue; // Retry
                    }
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _logger.LogError(ex,
                        "[EMAIL-WORKER] Error processing email to={To} subject={Subject} in {Ms}ms attempt={Attempt}",
                        req.To, req.Subject, sw.ElapsedMilliseconds, attempt);

                    lastException = ex;

                    if (attempt == MaxRetries)
                    {
                        IncrementCircuitBreakerFailure();
                    }
                    else
                    {
                        var delay = InitialRetryDelay * Math.Pow(2, attempt);
                        await Task.Delay(delay, stoppingToken);
                        continue; // Retry
                    }
                }
            }

            // All retries exhausted
            _logger.LogError(
                lastException,
                "[EMAIL-WORKER] SEND FAILED after {MaxRetries} retries. to={To} subject={Subject} total={Ms}ms",
                MaxRetries, req.To, req.Subject, sw.ElapsedMilliseconds);
        }

        private bool IsCircuitOpen()
        {
            return _consecutiveFailures >= MaxConsecutiveFailures && _circuitOpenedAt.HasValue;
        }

        private void IncrementCircuitBreakerFailure()
        {
            _consecutiveFailures++;

            if (_consecutiveFailures >= MaxConsecutiveFailures && !_circuitOpenedAt.HasValue)
            {
                _circuitOpenedAt = DateTime.Now;
                _logger.LogError(
                    "[EMAIL-WORKER] Circuit breaker OPENED after {Count} consecutive failures",
                    _consecutiveFailures);
            }
        }

        private void ResetCircuitBreaker()
        {
            if (_consecutiveFailures > 0 || _circuitOpenedAt.HasValue)
            {
                _logger.LogInformation(
                    "[EMAIL-WORKER] Circuit breaker RESET. Previous failures={Count}",
                    _consecutiveFailures);
            }

            _consecutiveFailures = 0;
            _circuitOpenedAt = null;
        }
    }
}
