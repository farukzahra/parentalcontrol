# Plano: Controle Parental Windows 11 com Proteção Anti-Tamper

## Objetivo
Criar aplicativo de controle parental em **C# com .NET** que monitora tempo desde o login, bloqueia ao atingir limite, exibe notificações de aviso, e protege contra encerramento não autorizado.

## Arquitetura
A solução usa três componentes:
1. **Windows Service** (núcleo protegido) - monitora tempo e aplica bloqueio
2. **Agente de Notificações** (sessão do usuário) - exibe avisos toast
3. **Aplicativo de Configuração** (WPF) - interface com autenticação

## Tecnologia Escolhida
**C# com .NET 8/9** - Melhor opção por:
- Acesso nativo às APIs do Windows (bloqueio, logout, serviços)
- Framework robusto para Windows Services
- Integração com notificações modernas do Windows 11
- Facilidade de implementação de segurança e autenticação

## Passos de Implementação

### 1. Windows Service Protegido
- Criar serviço Windows em C# rodando como SYSTEM
- Configurar ACLs (Security Descriptor) para impedir que usuários padrão parem/modifiquem o serviço
- Implementar auto-reinício automático em caso de falha ou encerramento forçado
- Configurar NTFS permissions nos arquivos executáveis (apenas admin pode modificar/deletar)

### 2. Rastreamento de Tempo de Sessão
- Monitorar tempo desde o login do usuário (session start time)
- Persistir dados de tempo em Registry (HKLM) com permissões admin-only
- Contador reinicia a cada login (não persiste entre reinicializações)
- Armazenar configuração de tempo limite (X minutos/horas)

### 3. Agente de Notificações na Sessão do Usuário
- Criar aplicativo leve que executa na sessão do usuário (não Session 0)
- Exibir Windows Toast Notifications usando `Microsoft.Toolkit.Uwp.Notifications`
- Mostrar avisos progressivos: 15 minutos, 10 minutos, 5 minutos restantes
- Comunicação com serviço via Named Pipes para receber comandos de notificação
- Registrar aplicativo no Start Menu para Toast Notifications funcionarem

### 4. Bloqueio/Logout Automático
- Implementar chamada à API Win32 `LockWorkStation()` para bloquear tela
- Alternativamente usar `ExitWindowsEx()` para logout completo
- Serviço aciona bloqueio quando tempo configurado expirar
- Notificar agente antes de bloquear para feedback visual

### 5. Aplicativo de Configuração WPF
- Interface gráfica moderna (WPF) para configurar tempo limite
- Autenticação obrigatória com senha de administrador Windows via `LogonUser` API (advapi32.dll)
- Verificar se usuário pertence ao grupo Administrators
- Permitir iniciar/parar serviço (apenas após autenticação)
- Permitir ajustar tempo limite e visualizar tempo restante
- Interface em português

### 6. Proteções de Sistema de Arquivos
- Configurar NTFS permissions: apenas Administrators podem write/delete nos executáveis
- Armazenar configurações em Registry HKLM com ACLs restritas
- Proteger logs de auditoria contra modificação
- Criar backup automático de configurações

## Requisitos de Segurança

### Proteções Implementadas
- ✅ Serviço não pode ser parado por usuário padrão
- ✅ Executáveis protegidos contra deleção/modificação
- ✅ Configurações protegidas em Registry (admin-only)
- ✅ Auto-reinício do serviço se encerrado
- ✅ Autenticação Windows para mudanças de configuração
- ✅ Logging de tentativas de tamper

### Pré-requisitos para Funcionamento
- **Conta do filho DEVE ser Usuário Padrão** (não Administrator)
- Conta do pai/responsável deve ter privilégios de Administrator
- Windows 11 (compatível com Windows 10)

### Limitações Conhecidas
- ⚠️ Administrador pode sempre contornar as proteções
- ⚠️ Safe Mode permite bypass (mitigação: senha de BIOS)
- ⚠️ Acesso físico ao hardware permite bypass (boot USB)
- ℹ️ Foco: proteção adequada para idade contra usuários padrão

## Considerações Adicionais

### 1. Configuração da Conta do Filho
- Confirmar que filho usa conta de Usuário Padrão (não Administrator)
- Remover privilégios admin se necessário
- Pai deve ter senha forte na conta de Administrator

### 2. Proteção de BIOS/UEFI
- Recomendado: configurar senha de BIOS para evitar boot em Safe Mode
- Desabilitar boot de USB/CD no BIOS
- Incluir instruções de configuração no manual de instalação

### 3. Sistema de Notificações
- Avisos progressivos antes do bloqueio (15, 10, 5 minutos)
- Notificações quando serviço detectar tentativas de encerramento
- Alertas para o pai via email/SMS (funcionalidade futura opcional)

### 4. Logs e Auditoria
- Registrar todas as sessões (hora início, fim, duração)
- Log de tentativas de parar o serviço
- Log de falhas de autenticação no app de configuração
- Logs protegidos contra modificação

## Estrutura do Projeto

```
ParentalControl/
├── ParentalControl.Service/        # Windows Service (núcleo)
├── ParentalControl.NotificationAgent/  # Agente de notificações
├── ParentalControl.ConfigApp/      # Aplicativo WPF de configuração
├── ParentalControl.Core/           # Biblioteca compartilhada
│   ├── Models/                     # Modelos de dados
│   ├── Security/                   # Autenticação e criptografia
│   └── Communication/              # IPC (Named Pipes)
└── ParentalControl.Installer/      # Projeto de instalação (WiX ou similar)
```

## Próximos Passos para Implementação

1. Criar estrutura de projeto .NET Solution com os 4 projetos
2. Implementar biblioteca Core com modelos e comunicação
3. Desenvolver Windows Service com proteções
4. Criar agente de notificações
5. Desenvolver interface WPF de configuração
6. Implementar sistema de autenticação
7. Configurar ACLs e permissions
8. Criar instalador com configuração automática de segurança
9. Testes em ambiente Windows 11
10. Documentação de usuário em português
