using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using ParentalControl.Core.Models;

namespace ParentalControl.Core.Communication;

/// <summary>
/// Cliente de Named Pipe para receber mensagens do serviço
/// </summary>
public class PipeClient : IDisposable
{
    private const string PipeName = "ParentalControlPipe";
    private bool _isRunning;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public event EventHandler<ServiceMessage>? MessageReceived;

    /// <summary>
    /// Inicia escuta de mensagens do serviço
    /// </summary>
    public async Task StartListeningAsync()
    {
        _isRunning = true;

        while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.In);
                
                await pipeClient.ConnectAsync(5000, _cancellationTokenSource.Token);

                var buffer = new byte[4096];
                var bytesRead = await pipeClient.ReadAsync(buffer, _cancellationTokenSource.Token);

                if (bytesRead > 0)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var message = JsonSerializer.Deserialize<ServiceMessage>(json);
                    
                    if (message != null)
                    {
                        MessageReceived?.Invoke(this, message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (TimeoutException)
            {
                // Timeout normal, tentar novamente
                await Task.Delay(1000, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no pipe client: {ex.Message}");
                await Task.Delay(2000, _cancellationTokenSource.Token);
            }
        }
    }

    /// <summary>
    /// Envia resposta para o serviço
    /// </summary>
    public async Task<bool> SendResponseAsync(string response)
    {
        try
        {
            using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            await pipeClient.ConnectAsync(500);

            var bytes = Encoding.UTF8.GetBytes(response);
            await pipeClient.WriteAsync(bytes);
            await pipeClient.FlushAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Para a escuta
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _cancellationTokenSource.Cancel();
    }

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource.Dispose();
        GC.SuppressFinalize(this);
    }
}
