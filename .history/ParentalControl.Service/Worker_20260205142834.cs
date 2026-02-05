using ParentalControl.Core.Communication;
using ParentalControl.Core.Models;
using ParentalControl.Core.Security;
using System.Text.Json;

namespace ParentalControl.Service;

public class ParentalControlWorker : BackgroundService
{
    private readonly ILogger<ParentalControlWorker> _logger;
    private readonly PipeServer _pipeServer;
    private SessionInfo? _currentSession;
    private TimeLimit _timeLimit;
    private DateTime _lastWarning = DateTime.MinValue;
    private readonly HashSet<int> _warningsShown = new();

    public ParentalControlWorker(ILogger<ParentalControlWorker> logger)
    {
        _logger = logger;
        _pipeServer = new PipeServer();
        
        // Carregar configurações do Registry
        var config = Core.Security.ConfigurationManager.LoadTimeLimit();
        _timeLimit = new TimeLimit
        {
            MaxMinutes = config.maxMinutes,
            IsEnabled = config.isEnabled,
            Action = config.action == "Logout" ? ExpirationAction.Logout : ExpirationAction.Lock
        };

        _logger.LogInformation("Serviço de Controle Parental iniciado");
        _logger.LogInformation("Limite de tempo: {Minutes} minutos, Ação: {Action}", 
            _timeLimit.MaxMinutes, _timeLimit.Action);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Iniciar servidor de pipe em background
        _ = Task.Run(() => _pipeServer.StartAsync(), stoppingToken);

        // Inicializar sessão
        InitializeSession();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_timeLimit.IsEnabled && _currentSession != null)
                {
                    // Verificar se o tempo expirou
                    if (_currentSession.IsExpired(_timeLimit))
                    {
                        _logger.LogWarning("Tempo limite atingido! Executando ação: {Action}", _timeLimit.Action);
                        await ExecuteExpirationAction();
                        
                        // Aguardar um pouco antes de verificar novamente
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                        continue;
                    }

                    // Verificar avisos
                    if (_currentSession.ShouldShowWarning(_timeLimit, out int minutesRemaining))
                    {
                        if (!_warningsShown.Contains(minutesRemaining))
                        {
                            await SendWarningNotification(minutesRemaining);
                            _warningsShown.Add(minutesRemaining);
                            _lastWarning = DateTime.Now;
                            
                            _logger.LogInformation("Aviso enviado: {Minutes} minutos restantes", minutesRemaining);
                        }
                    }

                    // Log periódico de status
                    if (DateTime.Now.Minute % 10 == 0 && DateTime.Now.Second < 10)
                    {
                        _logger.LogInformation("Status: {Elapsed}min de {Max}min usados, {Remaining}min restantes",
                            _currentSession.ElapsedMinutes, 
                            _timeLimit.MaxMinutes,
                            _currentSession.RemainingMinutes(_timeLimit));
                    }
                }

                // Verificar a cada 10 segundos
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no loop principal do serviço");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private void InitializeSession()
    {
        try
        {
            var userName = WindowsAuthenticator.GetCurrentUserName();
            var sessionStart = WindowsSessionManager.GetSessionStartTime();

            _currentSession = new SessionInfo
            {
                UserName = userName,
                SessionStartTime = sessionStart
            };

            // Salvar no Registry
            Core.Security.ConfigurationManager.SaveSessionStart(userName, sessionStart);

            _logger.LogInformation("Sessão inicializada: Usuário={User}, Início={Start:yyyy-MM-dd HH:mm:ss}",
                userName, sessionStart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inicializar sessão");
        }
    }

    private async Task SendWarningNotification(int minutesRemaining)
    {
        try
        {
            var warningData = new WarningData
            {
                MinutesRemaining = minutesRemaining,
                UserName = _currentSession?.UserName ?? "Usuário"
            };

            var message = new ServiceMessage
            {
                Type = MessageType.Warning,
                Data = JsonSerializer.Serialize(warningData)
            };

            await _pipeServer.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar notificação de aviso");
        }
    }

    private async Task ExecuteExpirationAction()
    {
        try
        {
            // Enviar notificação de bloqueio iminente
            var message = new ServiceMessage
            {
                Type = MessageType.ImmediateLock,
                Data = JsonSerializer.Serialize(new { Action = _timeLimit.Action.ToString() })
            };
            await _pipeServer.SendMessageAsync(message);

            // Aguardar 5 segundos para notificação ser exibida
            await Task.Delay(5000);

            // Executar ação
            bool success = _timeLimit.Action == ExpirationAction.Lock
                ? WindowsSessionManager.LockScreen()
                : WindowsSessionManager.LogoutUser(force: false);

            if (success)
            {
                _logger.LogInformation("Ação {Action} executada com sucesso", _timeLimit.Action);
                
                // Limpar dados da sessão
                ConfigurationManager.ClearSessionData();
                _warningsShown.Clear();
            }
            else
            {
                _logger.LogError("Falha ao executar ação {Action}", _timeLimit.Action);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar ação de expiração");
        }
    }

    public override void Dispose()
    {
        _pipeServer?.Dispose();
        base.Dispose();
    }
}
