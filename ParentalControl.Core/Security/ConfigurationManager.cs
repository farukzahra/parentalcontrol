using Microsoft.Win32;

namespace ParentalControl.Core.Security;

/// <summary>
/// Gerenciador de configurações no Registry
/// </summary>
public static class ConfigurationManager
{
    private const string RegistryPath = @"SOFTWARE\ParentalControl";
    private const string SessionDataPath = @"SOFTWARE\ParentalControl\SessionData";

    /// <summary>
    /// Salva configuração de tempo limite no Registry (HKLM - requer admin)
    /// </summary>
    public static bool SaveTimeLimit(int maxMinutes, bool isEnabled, string action)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(RegistryPath, true);
            if (key == null) return false;

            key.SetValue("MaxMinutes", maxMinutes, RegistryValueKind.DWord);
            key.SetValue("IsEnabled", isEnabled ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue("Action", action, RegistryValueKind.String);

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Acesso negado ao Registry. Requer privilégios de administrador.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar no Registry: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Carrega configuração de tempo limite do Registry
    /// </summary>
    public static (int maxMinutes, bool isEnabled, string action) LoadTimeLimit()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryPath, false);
            if (key == null)
            {
                // Valores padrão
                return (60, true, "Lock");
            }

            var maxMinutes = (int)(key.GetValue("MaxMinutes") ?? 60);
            var isEnabledValue = (int)(key.GetValue("IsEnabled") ?? 1);
            var action = key.GetValue("Action") as string ?? "Lock";

            return (maxMinutes, isEnabledValue == 1, action);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao ler do Registry: {ex.Message}");
            return (60, true, "Lock");
        }
    }

    /// <summary>
    /// Salva horário de início da sessão
    /// </summary>
    public static bool SaveSessionStart(string userName, DateTime startTime)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(SessionDataPath, true);
            if (key == null) return false;

            key.SetValue("UserName", userName, RegistryValueKind.String);
            key.SetValue("StartTime", startTime.Ticks, RegistryValueKind.QWord);
            key.SetValue("LastUpdate", DateTime.Now.Ticks, RegistryValueKind.QWord);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar sessão: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Carrega dados da sessão
    /// </summary>
    public static (string userName, DateTime startTime) LoadSessionData()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(SessionDataPath, false);
            if (key == null)
            {
                return (string.Empty, DateTime.MinValue);
            }

            var userName = key.GetValue("UserName") as string ?? string.Empty;
            var startTicks = (long)(key.GetValue("StartTime") ?? 0L);
            var startTime = startTicks > 0 ? new DateTime(startTicks) : DateTime.MinValue;

            return (userName, startTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao ler sessão: {ex.Message}");
            return (string.Empty, DateTime.MinValue);
        }
    }

    /// <summary>
    /// Carrega apenas o horário de início da sessão
    /// </summary>
    public static DateTime? GetSessionStartTime()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(SessionDataPath, false);
            if (key == null) return null;

            var startTicks = (long)(key.GetValue("StartTime") ?? 0L);
            if (startTicks <= 0) return null;

            var startTime = new DateTime(startTicks);
            // Validar se a sessão é recente (não mais que 24h atrás)
            if (startTime > DateTime.Now.AddDays(-1))
            {
                return startTime;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Limpa dados da sessão
    /// </summary>
    public static bool ClearSessionData()
    {
        try
        {
            Registry.LocalMachine.DeleteSubKey(SessionDataPath, false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
