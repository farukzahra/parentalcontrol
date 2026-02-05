namespace ParentalControl.Core.Models;

/// <summary>
/// Representa a configuração de limite de tempo
/// </summary>
public class TimeLimit
{
    /// <summary>
    /// Duração máxima permitida em minutos
    /// </summary>
    public int MaxMinutes { get; set; } = 60;

    /// <summary>
    /// Indica se o limite de tempo está ativo
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Ação a executar quando o tempo expirar (Lock ou Logout)
    /// </summary>
    public ExpirationAction Action { get; set; } = ExpirationAction.Lock;

    /// <summary>
    /// Minutos de aviso antes de expirar (múltiplos valores)
    /// </summary>
    public int[] WarningMinutes { get; set; } = [15, 10, 5];
}

/// <summary>
/// Ação a executar quando o tempo expirar
/// </summary>
public enum ExpirationAction
{
    /// <summary>
    /// Bloquear a tela
    /// </summary>
    Lock,
    
    /// <summary>
    /// Fazer logout completo
    /// </summary>
    Logout
}
