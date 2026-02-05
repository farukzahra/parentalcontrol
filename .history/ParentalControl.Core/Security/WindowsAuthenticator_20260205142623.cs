using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ParentalControl.Core.Security;

/// <summary>
/// Gerenciador de autenticação Windows
/// </summary>
public static class WindowsAuthenticator
{
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LogonUser(
        string lpszUsername,
        string? lpszDomain,
        string lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out IntPtr phToken);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const int LOGON32_LOGON_INTERACTIVE = 2;
    private const int LOGON32_PROVIDER_DEFAULT = 0;

    /// <summary>
    /// Autentica usuário com credenciais Windows
    /// </summary>
    /// <param name="username">Nome do usuário (pode incluir domínio)</param>
    /// <param name="password">Senha</param>
    /// <returns>True se autenticação bem-sucedida</returns>
    public static bool Authenticate(string username, string password)
    {
        string? domain = null;
        string user = username;

        // Separar domínio se fornecido (DOMAIN\User ou User@DOMAIN)
        if (username.Contains('\\'))
        {
            var parts = username.Split('\\');
            domain = parts[0];
            user = parts[1];
        }
        else if (username.Contains('@'))
        {
            var parts = username.Split('@');
            user = parts[0];
            domain = parts[1];
        }

        IntPtr token = IntPtr.Zero;
        try
        {
            bool success = LogonUser(
                user,
                domain,
                password,
                LOGON32_LOGON_INTERACTIVE,
                LOGON32_PROVIDER_DEFAULT,
                out token);

            return success;
        }
        finally
        {
            if (token != IntPtr.Zero)
            {
                CloseHandle(token);
            }
        }
    }

    /// <summary>
    /// Verifica se o usuário atual é administrador
    /// </summary>
    public static bool IsCurrentUserAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Obtém o nome do usuário atual
    /// </summary>
    public static string GetCurrentUserName()
    {
        return Environment.UserName;
    }

    /// <summary>
    /// Obtém o domínio do computador
    /// </summary>
    public static string GetComputerDomain()
    {
        return Environment.UserDomainName;
    }
}
