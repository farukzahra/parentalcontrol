using System.Windows;
using System.Windows.Controls;
using ParentalControl.Core.Security;
using ParentalControl.Core.Models;

namespace ParentalControl.ConfigApp;

public partial class MainWindow : Window
{
    private TimeLimit _currentConfig = new() { MaxMinutes = 60 };
    private SessionInfo? _currentSession;

    public MainWindow()
    {
        InitializeComponent();
        
        CheckAdminPrivileges();
        LoadConfiguration();
        UpdateStatus();
        
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
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
                "Para salvar as configurações, execute como Administrador.\n\n" +
                "Deseja fechar e reabrir como Administrador?",
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
                Verb = "runas"
            };

            System.Diagnostics.Process.Start(processInfo);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Erro ao reiniciar como Admin: " + ex.Message,
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
            MessageBox.Show("Erro ao carregar: " + ex.Message, "Erro", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            _currentConfig = new TimeLimit { MaxMinutes = 60 };
        }
    }

    private void UpdateStatus()
    {
        try
        {
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
                
                SessionText.Text = "Usuário: " + _currentSession.UserName;
                
                // Calcular tempo usado em segundos totais
                var elapsed = DateTime.Now - _currentSession.SessionStartTime;
                int totalUsedSeconds = (int)elapsed.TotalSeconds;
                int usedHours = totalUsedSeconds / 3600;
                int usedMinutes = (totalUsedSeconds % 3600) / 60;
                int usedSeconds = totalUsedSeconds % 60;
                TimeText.Text = $"Tempo usado: {usedHours:D2}:{usedMinutes:D2}:{usedSeconds:D2}";
                
                // Calcular tempo restante em segundos
                int totalAllowedSeconds = _currentConfig.MaxMinutes * 60;
                int remainingSeconds = Math.Max(0, totalAllowedSeconds - totalUsedSeconds);
                int remHours = remainingSeconds / 3600;
                int remMinutes = (remainingSeconds % 3600) / 60;
                int remSeconds = remainingSeconds % 60;
                RemainingText.Text = $"Tempo restante: {remHours:D2}:{remMinutes:D2}:{remSeconds:D2}";
                
                int remainingMinutes = remainingSeconds / 60;
                if (remainingMinutes <= 15)
                    RemainingText.Foreground = System.Windows.Media.Brushes.Red;
                else if (remainingMinutes <= 30)
                    RemainingText.Foreground = System.Windows.Media.Brushes.Orange;
                else
                    RemainingText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                StatusText.Text = "Serviço: ⚠️ Sem sessão";
                StatusText.Foreground = System.Windows.Media.Brushes.Orange;
                SessionText.Text = "Nenhuma sessão ativa";
                TimeText.Text = "Tempo usado: 00:00:00";
                RemainingText.Text = "Tempo restante: --:--:--";
            }
        }
        catch
        {
            StatusText.Text = "Serviço: ❌ Erro";
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private void TimeLimitSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (TimeLimitLabel == null) return;
        
        double minutes = TimeLimitSlider.Value;
        
        // Para valores menores que 1 minuto (testes)
        if (minutes < 1)
        {
            int seconds = (int)(minutes * 60);
            TimeLimitLabel.Text = $"{seconds} seg";
            return;
        }
        
        int totalMinutes = (int)minutes;
        int hours = totalMinutes / 60;
        int remainingMin = totalMinutes % 60;

        if (hours > 0)
            TimeLimitLabel.Text = remainingMin > 0 
                ? $"{totalMinutes} min ({hours}h {remainingMin}m)" 
                : $"{totalMinutes} min ({hours}h)";
        else
            TimeLimitLabel.Text = $"{totalMinutes} min";
    }

    private void SetPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag != null)
        {
            double minutes = double.Parse(button.Tag.ToString()!);
            
            // Para valores menores que 1 minuto (para testes)
            if (minutes < 1)
            {
                TimeLimitSlider.Minimum = 0.1;
                TimeLimitSlider.TickFrequency = 0.1;
            }
            else
            {
                TimeLimitSlider.Minimum = 15;
                TimeLimitSlider.TickFrequency = 15;
            }
            
            TimeLimitSlider.Value = minutes;
            
            // Atualizar o label manualmente para valores pequenos
            if (minutes < 1)
            {
                int seconds = (int)(minutes * 60);
                TimeLimitLabel.Text = $"{seconds} seg";
            }
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!WindowsAuthenticator.IsCurrentUserAdmin())
        {
            MessageBox.Show(
                "❌ Você NÃO é Administrador!\n\nExecute o app como Admin para salvar.",
                "Acesso Negado",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        try
        {
            // Suportar valores menores que 1 minuto (para testes)
            double valueInMinutes = TimeLimitSlider.Value;
            int maxMinutes;
            
            if (valueInMinutes < 1)
            {
                // Converter segundos para minutos (arredondar para cima para garantir pelo menos 1)
                maxMinutes = 1;
                
                // Avisar o usuário
                var result = MessageBox.Show(
                    $"⚠️ Modo de TESTE ativado!\n\nVocê configurou {(int)(valueInMinutes * 60)} segundos.\n\nO sistema vai bloquear em aproximadamente {(int)(valueInMinutes * 60)} segundos.\n\nDeseja continuar?",
                    "Atenção - Modo Teste",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                    
                if (result != MessageBoxResult.Yes)
                    return;
            }
            else
            {
                maxMinutes = (int)valueInMinutes;
            }
            
            string action = LockRadio.IsChecked == true ? "Lock" : "Logout";
            
            bool success = Core.Security.ConfigurationManager.SaveTimeLimit(
                maxMinutes, true, action);

            if (success)
            {
                _currentConfig.MaxMinutes = maxMinutes;
                _currentConfig.Action = action == "Lock" ? ExpirationAction.Lock : ExpirationAction.Logout;
                
                MessageBox.Show(
                    "✅ Configurações salvas!\n\nLimite: " + maxMinutes + " minutos\nAção: " + (action == "Lock" ? "Bloquear" : "Logout"),
                    "Sucesso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                UpdateStatus();
            }
            else
            {
                MessageBox.Show(
                    "❌ Erro ao salvar no Registry.\n\nExecute como Administrador.",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show(
                "❌ ACESSO NEGADO!\n\nExecute como Administrador.",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro: " + ex.Message, "Erro", 
                MessageBoxButton.OK, MessageBoxImage.Error);
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
            "Isso vai bloquear a tela AGORA!\n\nContinuar?",
            "Confirmar",
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
                    MessageBox.Show("Erro ao executar.", "Erro", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Erro", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
