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
                
                int remaining = _currentSession.RemainingMinutes(_currentConfig);
                RemainingText.Text = $"Tempo restante: {remaining} minutos";
                
                if (remaining <= 15)
                    RemainingText.Foreground = System.Windows.Media.Brushes.Red;
                else if (remaining <= 30)
                    RemainingText.Foreground = System.Windows.Media.Brushes.Orange;
                else
                    RemainingText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                StatusText.Text = "Serviço: ⚠️ Sem sessão ativa";
                StatusText.Foreground = System.Windows.Media.Brushes.Orange;
                SessionText.Text = "Nenhuma sessão monitorada";
                TimeText.Text = "Tempo usado: 0 minutos";
                RemainingText.Text = "Tempo restante: --";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "Serviço: ❌ Erro";
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
            SessionText.Text = $"Erro: {ex.Message}";
        }
    }

    private void TimeLimitSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (TimeLimitLabel == null) return;
        
        int minutes = (int)TimeLimitSlider.Value;
        int hours = minutes / 60;
        int remainingMinutes = minutes % 60;

        if (hours > 0)
            TimeLimitLabel.Text = remainingMinutes > 0 
                ? $"{minutes} min ({hours}h {remainingMinutes}m)" 
                : $"{minutes} min ({hours}h)";
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