# Contribuindo para o CanSat Monitor Dashboard

Obrigado por considerar contribuir para o CanSat Monitor Dashboard! 🛰️

## Como Contribuir

### Reportando Bugs

Se você encontrar um bug, por favor:

1. Verifique se o bug já não foi reportado nas [Issues](https://github.com/Albinopedro/cansat-monitor/issues)
2. Se não existir, crie uma nova issue com:
   - Descrição clara do problema
   - Passos para reproduzir
   - Comportamento esperado vs atual
   - Screenshots (se aplicável)
   - Informações do sistema (OS, versão do .NET, etc.)

### Sugerindo Melhorias

Para sugerir novas funcionalidades:

1. Abra uma issue com a tag "enhancement"
2. Descreva detalhadamente a funcionalidade
3. Explique por que seria útil
4. Inclua mockups ou exemplos se possível

### Contribuindo com Código

1. **Fork** o repositório
2. **Clone** seu fork localmente
3. **Crie** uma branch para sua feature: `git checkout -b feature/nome-da-feature`
4. **Faça** suas alterações
5. **Teste** suas alterações
6. **Commit** suas mudanças: `git commit -m 'Add: nova funcionalidade'`
7. **Push** para sua branch: `git push origin feature/nome-da-feature`
8. **Abra** um Pull Request

### Padrões de Código

- Use **C# 12** e **.NET 9**
- Siga as convenções de nomenclatura do C#
- Mantenha o código limpo e bem documentado
- Use comentários XML para documentar métodos públicos
- Teste suas alterações em múltiplas plataformas quando possível

### Estrutura de Commits

Use mensagens de commit descritivas:

- `feat:` para novas funcionalidades
- `fix:` para correções de bugs
- `docs:` para alterações na documentação
- `style:` para formatação, etc.
- `refactor:` para refatoração de código
- `test:` para adicionar testes
- `chore:` para tarefas de manutenção

### Configuração do Ambiente de Desenvolvimento

1. Instale o [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Clone o repositório: `git clone https://github.com/Albinopedro/cansat-monitor.git`
3. Restaure as dependências: `dotnet restore`
4. Execute o projeto: `dotnet run`

### Testando

Antes de submeter um PR:

1. Verifique se o projeto compila sem erros
2. Teste a funcionalidade em diferentes plataformas (se possível)
3. Verifique se não há regressões
4. Teste com diferentes dispositivos seriais (se aplicável)

### Licença

Ao contribuir, você concorda que suas contribuições serão licenciadas sob a [Licença MIT](LICENSE).

## Dúvidas?

Se tiver dúvidas sobre como contribuir, abra uma issue ou entre em contato!

Obrigado pela sua contribuição! 🚀
