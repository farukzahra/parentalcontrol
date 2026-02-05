namespace ParentalControl.Core.Models;

/// <summary>
/// Informações sobre a sessão atual do usuário
/// </summary>
public class SessionInfo
{
    /// <summary>
    /// Nome do usuário da sessão
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Horário de início da sessão (login)
    /// </summary>
    public DateTime SessionStartTime { get; set; }

    /// <summary>
    /// Minutos decorridos desde o início da sessão
    /// </summary>
    public int ElapsedMinutes => (int)(DateTime.Now - SessionStartTime).TotalMinutes;

    /// <summary>
    /// Minutos restantes até o limite
    /// </summary>
    public int RemainingMinutes(TimeLimit limit) => Math.Max(0, limit.MaxMinutes - ElapsedMinutes);

    /// <summary>
    /// Indica se o tempo expirou
    /// </summary>
    public bool IsExpired(TimeLimit limit) => limit.IsEnabled && ElapsedMinutes >= limit.MaxMinutes;

    /// <summary>
    /// Indica se deve exibir aviso agora
    /// </summary>
    public bool ShouldShowWarning(TimeLimit limit, out int minutesRemaining)
    {
        minutesRemaining = RemainingMinutes(limit);
        return limit.IsEnabled && 
               limit.WarningMinutes.Contains(minutesRemaining) && 
               minutesRemaining > 0;
    }
}
