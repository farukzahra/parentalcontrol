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
    /// Obtém o horário de login do usuário atual
    /// </summary>
    public static DateTime GetSessionStartTime()
    {
        try
        {
            // Calcular tempo desde o boot do sistema
            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            return DateTime.Now - uptime;
        }
        catch
        {
            return DateTime.Now;
        }
    }
}
