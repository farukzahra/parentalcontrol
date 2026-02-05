using ParentalControl.Service;

var builder = Host.CreateApplicationBuilder(args);

// Configurar serviço Windows
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ParentalControlService";
});

// Adicionar o worker do controle parental
builder.Services.AddHostedService<ParentalControlWorker>();

// Configurar logging para Event Log do Windows
builder.Services.AddLogging(logging =>
{
    logging.AddEventLog(settings =>
    {
        settings.SourceName = "ParentalControl";
    });
});

var host = builder.Build();
host.Run();
