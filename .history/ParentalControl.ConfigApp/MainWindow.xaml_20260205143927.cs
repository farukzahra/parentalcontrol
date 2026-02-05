using System.Windows;
using System.Windows.Controls;
using ParentalControl.Core.Security;
using ParentalControl.Core.Models;

namespace ParentalControl.ConfigApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private TimeLimit _currentConfig;
    private SessionInfo? _currentSession;

    public MainWindow()
    {
        InitializeComponent();
        
        // Verificar se está rodando como admin
        CheckAdminPrivileges();
        
        LoadConfiguration();
        UpdateStatus();
        
        // Timer para atualizar status a cada 10 segundos
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        timer.Tick += (s, e) => UpdateStatus();
        timer.Start();
    }

    private void CheckAdminPrivileges()
    {
        if (!WindowsAuthenticator.IsCurrentUserAdmin())
        {
            var result = MessageBox.Show(
                "⚠️ AVISO: Você não está executando como Administrador!\n\n" +
                "Para salvar as configurações no sistema, você precisa executar este aplicativo como Administrador.\n\n" +
                "Deseja fechar e reabrir como Administrador agora?",
                "Privilégios Insuficientes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                RestartAsAdmin();
            }
            else
            {
                SaveButton.IsEnabled = false;
                SaveButton.Content = "🔒 Salvar (Requer Admin)";
                SaveButton.ToolTip = "Execute o aplicativo como Administrador para salvar";
            }
        }
    }

    private void RestartAsAdmin()
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Environment.ProcessPath ?? "ParentalControl.ConfigApp.exe",
                UseShellExecute = true,
                Verb = "runas" // Solicita elevação
            };

            System.Diagnostics.Process.Start(processInfo);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível reiniciar como Administrador.\n\n" +
                $"Por favor, feche este aplicativo e execute-o manualmente como Administrador.\n\n" +
                $"Erro: {ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void LoadConfiguration()
    {
        try
        {
            var config = Core.Security.ConfigurationManager.LoadTimeLimit();
            _currentConfig = new TimeLimit
            {
                MaxMinutes = config.maxMinutes,
                IsEnabled = config.isEnabled,
                Action = config.action == "Logout" ? ExpirationAction.Logout : ExpirationAction.Lock
            };

            TimeLimitSlider.Value = _currentConfig.MaxMinutes;
            
            if (_currentConfig.Action == ExpirationAction.Lock)
                LockRadio.IsChecked = true;
            else
                LogoutRadio.IsChecked = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar configuração: {ex.Message}", 
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            
            // Valores padrão
            _currentConfig = new TimeLimit { MaxMinutes = 60 };
        }
    }

    private void UpdateStatus()
    {
        try
        {
            // Carregar dados da sessão
            var sessionData = Core.Security.ConfigurationManager.LoadSessionData();
            
            if (!string.IsNullOrEmpty(sessionData.userName) && sessionData.startTime != DateTime.MinValue)
            {
                _currentSession = new SessionInfo
                {
                    UserName = sessionData.userName,
                    SessionStartTime = sessionData.startTime
                };

                StatusText.Text = "Serviço: ✅ Ativo";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                
                SessionText.Text = $"Usuário: {_currentSession.UserName}";
                TimeText.Text = $"Tempo usado: {_currentSession.ElapsedMinutes} minutos";
        // Verificar novamente se é admin
        if (!WindowsAuthenticator.IsCurrentUserAdmin())
        {
            var result = MessageBox.Show(
                "❌ Você NÃO é Administrador!\n\n" +
                "Não é possível salvar as configurações sem privilégios de administrador.\n\n" +
                "Deseja reiniciar o aplicativo como Administrador?",
                "Acesso Negado",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
            {
                RestartAsAdmin();
            }
            return;
        }

        try
        {
            int maxMinutes = (int)TimeLimitSlider.Value;
            string action = LockRadio.IsChecked == true ? "Lock" : "Logout";
            
            bool success = Core.Security.ConfigurationManager.SaveTimeLimit(
                maxMinutes, 
                true, 
                action);

            if (success)
            {
                _currentConfig.MaxMinutes = maxMinutes;
                _currentConfig.Action = action == "Lock" ? ExpirationAction.Lock : ExpirationAction.Logout;
                
                MessageBox.Show(
                    $"✅ Configurações salvas com sucesso!\n\n" +
                    $"📊 Limite: {maxMinutes} minutos ({maxMinutes/60}h {maxMinutes%60}m)\n" +
                    $"🔒 Ação: {(action == "Lock" ? "Bloquear tela" : "Fazer logout")}\n\n" +
                    $"⚠️ Nota: Para aplicar as mudanças, reinicie o serviço Windows.\n" +
                    $"Use: sc stop ParentalControlService && sc start ParentalControlService",
                    "Sucesso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                UpdateStatus();
            }
            else
            {
                MessageBox.Show(
                    "❌ Erro ao salvar configurações no Registry.\n\n" +
                    "Possíveis causas:\n" +
                    "• Permissões insuficientes no Registry\n" +
                    "• Chave bloqueada por outro processo\n" +
                    "• Antivírus bloqueando acesso\n\n" +
                    "Tente executar como Administrador ou verificar o Event Viewer para mais detalhes.",
                    "Erro ao Salvar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show(
                "❌ ACESSO NEGADO ao Registry!\n\n" +
                "Você DEVE executar este aplicativo como Administrador.\n\n" +
                "Como fazer:\n" +
                "1. Feche este aplicativo\n" +
                "2. Clique com botão direito no executável\n" +
                "3. Selecione 'Executar como administrador'",
                "Acesso Negado",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (System.Security.SecurityException secEx)
        {
            MessageBox.Show(
                $"❌ Erro de Segurança!\n\n" +
                $"O Windows bloqueou o acesso ao Registry.\n\n" +
                $"Detalhes: {secEx.Message}\n\n" +
                $"Execute como Administrador.",
                "Erro de Segurança",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"❌ Erro inesperado ao salvar!\n\n" +
                $"Tipo: {ex.GetType().Name}\n" +
                $"Mensagem: {ex.Message}\n\n" +
                $"Stack: {ex.StackTrace?.Substring(0, Math.Min(200, ex.StackTrace?.Length ?? 0))}...",
                "Erro",
                MessageBoxButton.OK,
               ";
        else
            TimeLimitLabel.Text = $"{minutes} min";
    }

    private void SetPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag != null)
        {
            int minutes = int.Parse(button.Tag.ToString()!);
            TimeLimitSlider.Value = minutes;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int maxMinutes = (int)TimeLimitSlider.Value;
            string action = LockRadio.IsChecked == true ? "Lock" : "Logout";
            
            bool success = Core.Security.ConfigurationManager.SaveTimeLimit(
                maxMinutes, 
                true, 
                action);

            if (success)
            {
                _currentConfig.MaxMinutes = maxMinutes;
                _currentConfig.Action = action == "Lock" ? ExpirationAction.Lock : ExpirationAction.Logout;
                
                MessageBox.Show(
                    $"Configurações salvas com sucesso!\n\n" +
                    $"Limite: {maxMinutes} minutos\n" +
                    $"Ação: {(action == "Lock" ? "Bloquear tela" : "Fazer logout")}\n\n" +
                    $"⚠️ Nota: Para aplicar as mudanças, reinicie o serviço Windows.",
                    "Sucesso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                UpdateStatus();
            }
            else
            {
                MessageBox.Show(
                    "Erro ao salvar configurações.\n\n" +
                    "Certifique-se de que está executando como Administrador.",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar: {ex.Message}", 
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateStatus();
        LoadConfiguration();
        MessageBox.Show("Status atualizado!", "Info", 
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void TestButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Isso vai bloquear/deslogar a tela AGORA para testar!\n\n" +
            "Tem certeza que deseja continuar?",
            "Confirmar Teste",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                bool success = _currentConfig.Action == ExpirationAction.Lock
                    ? WindowsSessionManager.LockScreen()
                    : WindowsSessionManager.LogoutUser(false);

                if (!success)
                {
                    MessageBox.Show("Erro ao executar teste.", "Erro", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro no teste: {ex.Message}", "Erro", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}