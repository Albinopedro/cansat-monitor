# Política de Segurança

## Versões Suportadas

Atualmente, as seguintes versões do CanSat Monitor Dashboard recebem atualizações de segurança:

| Versão | Suportada          |
| ------ | ------------------ |
| 1.0.x  | :white_check_mark: |
| < 1.0  | :x:                |

## Relatando uma Vulnerabilidade

A segurança do CanSat Monitor Dashboard é levada a sério. Se você descobrir uma vulnerabilidade de segurança, pedimos que nos informe de forma responsável.

### Como Relatar

1. **NÃO** abra uma issue pública para vulnerabilidades de segurança
2. Envie um email para o mantenedor principal através do GitHub
3. Ou abra uma [Security Advisory](https://github.com/Albinopedro/cansat-monitor/security/advisories/new) no GitHub

### Informações a Incluir

Ao relatar uma vulnerabilidade, inclua o máximo de informações possível:

- Tipo de problema (ex: buffer overflow, SQL injection, cross-site scripting, etc.)
- Caminhos completos dos arquivos de código fonte relacionados à manifestação do problema
- Localização do código afetado (tag/branch/commit ou URL direto)
- Qualquer configuração especial necessária para reproduzir o problema
- Instruções passo a passo para reproduzir o problema
- Prova de conceito ou código de exploit (se possível)
- Impacto do problema, incluindo como um atacante pode explorar o problema

### Processo de Resposta

1. **Confirmação**: Confirmaremos o recebimento do seu relatório dentro de 48 horas
2. **Avaliação**: Avaliaremos o problema e determinaremos sua severidade
3. **Correção**: Trabalharemos em uma correção para o problema
4. **Disclosure**: Coordenaremos a divulgação responsável da vulnerabilidade

### Timeframe Esperado

- **Resposta inicial**: 48 horas
- **Avaliação**: 7 dias
- **Correção**: 30 dias (dependendo da complexidade)

## Configurações de Segurança Recomendadas

### Comunicação Serial
- Use apenas portas seriais confiáveis
- Valide todos os dados recebidos dos dispositivos CanSat
- Monitore conexões incomuns ou suspeitas

### Dados Persistidos
- Os arquivos de dados são salvos no diretório local do usuário
- Não compartilhe arquivos de dados que possam conter informações sensíveis
- Faça backup regular dos dados importantes

### Rede
- O aplicativo não faz conexões de rede por padrão
- Se modificar o código para incluir funcionalidades de rede, use HTTPS/TLS

## Atualizações de Segurança

Atualizações de segurança serão lançadas através de:
- Releases do GitHub
- GitHub Security Advisories
- Atualização da documentação

Recomendamos manter sempre a versão mais recente instalada.

## Recursos de Segurança

Para mais informações sobre segurança em aplicações .NET:
- [.NET Security Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [OWASP .NET Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/DotNet_Security_Cheat_Sheet.html)
