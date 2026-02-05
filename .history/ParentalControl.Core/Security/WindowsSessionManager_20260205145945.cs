using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ParentalControl.Core.Security;

/// <summary>
/// APIs do Windows para bloqueio e logout
/// </summary>
public static class WindowsSessionManager
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool LockWorkStation();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemTimes(out long lpIdleTime, out long lpKernelTime, out long lpUserTime);

    private const uint EWX_LOGOFF = 0x00000000;
    private const uint EWX_FORCE = 0x00000004;
    private const uint SHTDN_REASON_MAJOR_OTHER = 0x00000000;
    private const uint SHTDN_REASON_MINOR_OTHER = 0x00000000;
    private const uint SHTDN_REASON_FLAG_PLANNED = 0x80000000;

    /// <summary>
    /// Bloqueia a estação de trabalho
    /// </summary>
    /// <returns>True se bloqueio bem-sucedido</returns>
    public static bool LockScreen()
    {
        try
        {
            return LockWorkStation();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao bloquear tela: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Faz logout do usuário atual
    /// </summary>
    /// <param name="force">Forçar logout mesmo com aplicativos abertos</param>
    /// <returns>True se logout bem-sucedido</returns>
    public static bool LogoutUser(bool force = false)
    {
        try
        {
            uint flags = EWX_LOGOFF;
            if (force)
            {
                flags |= EWX_FORCE;
            }

            uint reason = SHTDN_REASON_MAJOR_OTHER | 
                         SHTDN_REASON_MINOR_OTHER | 
                         SHTDN_REASON_FLAG_PLANNED;

            return ExitWindowsEx(flags, reason);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao fazer logout: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtém o horário de login do usuário atual usando evento de login no Event Log
    /// </summary>
    public static DateTime GetSessionStartTime()
    {
        try
        {
            // Primeiro, tentar obter do Registry (se foi salvo)
            var saved = Core.Security.ConfigurationManager.GetSessionStartTime();
            if (saved.HasValue)
            {
                return saved.Value;
            }

            // Caso contrário, calcular pelo Windows Event Log
            var currentUser = WindowsAuthenticator.GetCurrentUserName();
            var sessionStart = GetSessionStartFromEventLog(currentUser);

            // Se conseguiu encontrar no Event Log, retornar
            if (sessionStart != DateTime.MinValue)
            {
                return sessionStart;
            }

            // Fallback: usar agora (vai contar como se tivesse começado agora)
            return DateTime.Now;
        }
        catch
        {
            return DateTime.Now;
        }
    }

    /// <summary>
    /// Obtém o tempo de login do usuário pelo Windows Event Log
    /// </summary>
    private static DateTime GetSessionStartFromEventLog(string userName)
    {
        try
        {
            var eventLog = new EventLog("Security");
            
            // Procurar por eventos de login recentes (Event ID 4624 = Successful Logon)
            // Mas como pode ter permissão limitada, vamos simplificar
            // Usar o processo explorer.exe do usuário como referência
            
            var processes = Process.GetProcessesByName("explorer");
            foreach (var process in processes)
            {
                try
                {
                    if (process.ProcessName == "explorer")
                    {
                        // O tempo de criação do explorer.exe é aproximadamente o tempo de login
                        var startTime = process.StartTime;
                        if (startTime > DateTime.Now.AddDays(-1)) // Se for do mesmo dia ou anterior
                        {
                            return startTime;
                        }
                    }
                }
                catch
                {
                    // Ignorar erros de acesso a processo
                }
            }

            return DateTime.MinValue;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}
