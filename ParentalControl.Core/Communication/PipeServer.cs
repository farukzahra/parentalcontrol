using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using ParentalControl.Core.Models;

namespace ParentalControl.Core.Communication;

/// <summary>
/// Servidor de Named Pipe para comunicação do serviço com agentes
/// </summary>
public class PipeServer : IDisposable
{
    private const string PipeName = "ParentalControlPipe";
    private NamedPipeServerStream? _pipeServer;
    private bool _isRunning;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public event EventHandler<string>? MessageReceived;

    /// <summary>
    /// Inicia o servidor de pipe
    /// </summary>
    public async Task StartAsync()
    {
        _isRunning = true;
        
        while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                _pipeServer = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                await _pipeServer.WaitForConnectionAsync(_cancellationTokenSource.Token);

                // Ler mensagem do cliente (se houver)
                var buffer = new byte[4096];
                var bytesRead = await _pipeServer.ReadAsync(buffer, _cancellationTokenSource.Token);
                
                if (bytesRead > 0)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    MessageReceived?.Invoke(this, message);
                }

                _pipeServer.Disconnect();
                _pipeServer.Dispose();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no pipe server: {ex.Message}");
                await Task.Delay(1000, _cancellationTokenSource.Token);
            }
        }
    }

    /// <summary>
    /// Envia mensagem para todos os clientes conectados
    /// </summary>
    public async Task<bool> SendMessageAsync(ServiceMessage message)
    {
        try
        {
            using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            await pipeClient.ConnectAsync(500); // timeout 500ms

            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            await pipeClient.WriteAsync(bytes);
            await pipeClient.FlushAsync();

            return true;
        }
        catch
        {
            return false; // Cliente não conectado
        }
    }

    /// <summary>
    /// Para o servidor
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _cancellationTokenSource.Cancel();
        _pipeServer?.Dispose();
    }

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource.Dispose();
        GC.SuppressFinalize(this);
    }
}
