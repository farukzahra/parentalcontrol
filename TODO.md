# TODO - Controle Parental

## Funcionalidades Principais Pendentes

### 1. Agente de Notificações (Alta prioridade)
- [ ] Implementar toast notifications do Windows 10/11
- [ ] Conectar ao serviço via Named Pipes
- [ ] Exibir avisos aos 15, 10 e 5 minutos restantes
- [ ] Adicionar ícone na bandeja do sistema
- [ ] Configurar para iniciar automaticamente no login do usuário

### 2. Proteção do Serviço (Alta prioridade)
- [ ] Configurar ACLs para impedir que usuários comuns parem o serviço
- [ ] Proteger contra encerramento via Task Manager
- [ ] Configurar permissões no Registry para impedir alterações não autorizadas
- [ ] Implementar recuperação automática se o serviço for encerrado

### 3. Instalador (Alta prioridade)
- [ ] Criar instalador MSI ou EXE com WiX ou Inno Setup
- [ ] Incluir instalação do serviço
- [ ] Incluir configuração inicial
- [ ] Adicionar atalho no Menu Iniciar
- [ ] Configurar desinstalador

## Melhorias e Funcionalidades Opcionais

### 4. Autenticação e Segurança
- [ ] Validar senha do administrador antes de alterar configurações
- [ ] Criptografar configurações sensíveis no Registry
- [ ] Adicionar auditoria de tentativas de alteração

### 5. Histórico e Relatórios
- [ ] Implementar logs detalhados de uso
- [ ] Criar interface para visualizar histórico
- [ ] Gráfico de uso diário/semanal/mensal
- [ ] Exportar relatórios em PDF ou CSV

### 6. Interface e UX
- [ ] Adicionar ícone personalizado ao aplicativo
- [ ] Melhorar design da interface
- [ ] Adicionar modo escuro
- [ ] Internacionalização (suporte a múltiplos idiomas)

### 7. Funcionalidades Avançadas
- [ ] Múltiplos perfis de usuário
- [ ] Limites diferentes por dia da semana
- [ ] Horários permitidos (ex: apenas 14h-18h)
- [ ] Lista de aplicativos bloqueados/permitidos
- [ ] Filtro de sites (integração com DNS/hosts)
- [ ] Pausar temporariamente o controle (com senha admin)

### 8. Testes
- [ ] Criar testes unitários
- [ ] Criar testes de integração
- [ ] Testar em diferentes versões do Windows (10/11)
- [ ] Testar com múltiplos usuários

## Bugs Conhecidos
- [ ] Verificar cálculo correto do tempo em casos de suspend/hibernate
- [ ] Testar comportamento em múltiplas sessões simultâneas (RDP)

## Documentação
- [ ] Manual do usuário
- [ ] Documentação técnica da arquitetura
- [ ] Guia de contribuição
- [ ] FAQ

## Status Atual ✅

### Implementado e Funcionando
- ✅ Estrutura de solution .NET com 4 projetos
- ✅ Biblioteca Core (modelos, comunicação, segurança)
- ✅ Windows Service com monitoramento de tempo
- ✅ Cálculo correto do tempo desde login (via explorer.exe)
- ✅ Bloqueio de tela ou logout ao atingir limite
- ✅ Aplicativo WPF de configuração
- ✅ Interface com presets rápidos (30min, 1h, 2h, 3h, 4h)
- ✅ Salvamento de configuração no Registry (HKLM)
- ✅ Auto-start do serviço no boot do Windows
- ✅ Status em tempo real na interface
- ✅ Verificação de privilégios de administrador
- ✅ Scripts de instalação/desinstalação do serviço
