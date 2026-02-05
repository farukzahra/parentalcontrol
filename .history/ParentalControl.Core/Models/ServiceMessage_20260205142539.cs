namespace ParentalControl.Core.Models;

/// <summary>
/// Mensagem de comunicação entre serviço e agente de notificações
/// </summary>
public class ServiceMessage
{
    /// <summary>
    /// Tipo de mensagem
    /// </summary>
    public MessageType Type { get; set; }

    /// <summary>
    /// Dados da mensagem (JSON serializado)
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp da mensagem
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// Tipos de mensagem do serviço
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Aviso de tempo restante
    /// </summary>
    Warning,

    /// <summary>
    /// Notificação de bloqueio iminente
    /// </summary>
    ImmediateLock,

    /// <summary>
    /// Atualização de status
    /// </summary>
    StatusUpdate,

    /// <summary>
    /// Tentativa de tamper detectada
    /// </summary>
    TamperAttempt,

    /// <summary>
    /// Ping/Keep-alive
    /// </summary>
    Ping
}

/// <summary>
/// Dados de aviso de tempo
/// </summary>
public class WarningData
{
    public int MinutesRemaining { get; set; }
    public string UserName { get; set; } = string.Empty;
}
