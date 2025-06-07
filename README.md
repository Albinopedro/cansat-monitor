# 🛰️ CanSat Monitor Dashboard

Um dashboard moderno e responsivo para monitoramento de dados de sensores CanSat em tempo real, desenvolvido com Avalonia UI e .NET 9.

![CanSat Monitor](https://img.shields.io/badge/Platform-.NET%209-blue)
![License](https://img.shields.io/badge/License-MIT-green)
![Framework](https://img.shields.io/badge/UI-Avalonia-purple)
![Build Status](https://github.com/Albinopedro/cansat-monitor/workflows/Build%20and%20Test/badge.svg)
![GitHub release](https://img.shields.io/github/v/release/Albinopedro/cansat-monitor)
![GitHub issues](https://img.shields.io/github/issues/Albinopedro/cansat-monitor)

## 📋 Sobre o Projeto

O **CanSat Monitor Dashboard** é uma aplicação desktop multiplataforma que permite monitorar em tempo real dados de sensores de temperatura e umidade provenientes de dispositivos CanSat via comunicação serial. A aplicação oferece uma interface moderna e intuitiva com gráficos interativos, análise estatística e persistência de dados.

### 🎯 Características Principais

- **📊 Visualização em Tempo Real**: Gráficos dinâmicos com LiveCharts2
- **📡 Comunicação Serial**: Conexão direta com dispositivos CanSat
- **💾 Persistência de Dados**: Salvamento automático em arquivo JSON
- **📈 Análise Estatística**: Máximos, mínimos e médias por período
- **🌓 Tema Escuro/Claro**: Interface adaptável com alternância de tema
- **📱 Design Responsivo**: Layout que se adapta ao tamanho da janela
- **🔄 Filtros Temporais**: Visualização por 1H, 6H, 24H ou 7 dias
- **📅 Filtro Personalizado**: Seleção de intervalo de datas específico

## 🚀 Tecnologias Utilizadas

- **[.NET 9](https://dotnet.microsoft.com/)** - Framework de desenvolvimento
- **[Avalonia UI 11.3](https://avaloniaui.net/)** - Framework UI multiplataforma
- **[LiveCharts2](https://livecharts.dev/)** - Biblioteca de gráficos interativos
- **[SkiaSharp](https://github.com/mono/SkiaSharp)** - Renderização gráfica 2D
- **[System.IO.Ports](https://www.nuget.org/packages/System.IO.Ports/)** - Comunicação serial

## 🖥️ Plataformas Suportadas

- ✅ **Windows** (7/8/10/11)
- ✅ **Linux** (Ubuntu, Debian, CentOS, etc.)
- ✅ **macOS** (10.13+)

## 📦 Instalação e Execução

### Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Porta serial disponível para conexão com o dispositivo CanSat

### Clonando o Repositório

```bash
git clone https://github.com/Albinopedro/cansat-monitor.git
cd cansat-monitor
```

### Compilação e Execução

```bash
# Restaurar dependências
dotnet restore

# Compilar o projeto
dotnet build

# Executar a aplicação
dotnet run
```

### Executável Independente

Para criar um executável independente:

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained

# macOS
dotnet publish -c Release -r osx-x64 --self-contained
```

## 🔧 Configuração do Dispositivo CanSat

O dashboard espera dados no seguinte formato via comunicação serial:

```
Humidade: 65.2, Temperatura: 23.8
```

### Configuração Serial

- **Baud Rate**: 9600
- **Data Bits**: 8
- **Parity**: None
- **Stop Bits**: 1

## 🎮 Como Usar

1. **Conectar Dispositivo**:

    - Conecte seu dispositivo CanSat via USB/Serial
    - Selecione a porta correspondente no dropdown
    - Clique em "Conectar"

2. **Monitoramento**:

    - Os dados aparecerão automaticamente nos cartões de sensores
    - O gráfico será atualizado em tempo real
    - O histórico será salvo automaticamente

3. **Filtros Temporais**:

    - Use os botões 1H, 6H, 24H, 7D para diferentes períodos
    - Use o filtro personalizado para datas específicas

4. **Temas**:
    - Clique no ícone de tema no canto superior direito
    - Ou use `Ctrl+D` para alternar rapidamente

## 📊 Funcionalidades do Dashboard

### Cartões de Sensores

- **Temperatura**: Valor atual, máximo/mínimo do período
- **Umidade**: Valor atual e média do período
- **Indicadores Visuais**: Barras de progresso e status de conexão

### Gráficos Interativos

- **Zoom**: Utilize o scroll do mouse no eixo X
- **Tooltip**: Passe o mouse sobre os pontos para detalhes
- **Duplo Eixo Y**: Temperatura (°C) e Umidade (%) em escalas independentes

### Histórico de Dados

- **Tabela Detalhada**: Todos os registros com timestamp
- **Scroll Automático**: Sempre mostra os dados mais recentes
- **Persistência**: Dados salvos automaticamente a cada 30 segundos

## 🗄️ Estrutura de Dados

Os dados são salvos em formato JSON no diretório:

- **Windows**: `%LocalAppData%\CanSatMonitor\sensor_data.json`
- **Linux**: `~/.local/share/CanSatMonitor/sensor_data.json`
- **macOS**: `~/Library/Application Support/CanSatMonitor/sensor_data.json`

Exemplo de estrutura:

```json
[
    {
        "Timestamp": "2025-05-27T14:30:00",
        "Temperature": 23.5,
        "Humidity": 65.2
    }
]
```

## 🎨 Interface

### Tema Claro

Interface clean e moderna com tons claros e azuis.

### Tema Escuro

Interface elegante com fundo escuro e acentos coloridos.

### Layout Responsivo

- **Tela Grande**: Layout completo com todos os componentes
- **Tela Média**: Reorganização automática dos elementos
- **Tela Pequena**: Layout compacto otimizado

> O layout responsivo ainda pode ser aprimorado, mas já oferece uma experiência razoável para as principais proporções de tela.

## 🛠️ Desenvolvimento

### Estrutura do Projeto

```
CanSat/
├── MainWindow.axaml       # Interface XAML
├── MainWindow.axaml.cs    # Lógica principal
├── App.axaml             # Configuração da aplicação
├── App.axaml.cs          # Inicialização
├── Program.cs            # Ponto de entrada
└── CanSat.csproj         # Configuração do projeto
```

### Contribuição

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📝 Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

## 🤝 Contribuidores

- **Albino Pedro** - _Desenvolvimento Principal_ - [@albinopedro](https://github.com/albinopedro)

## 📞 Suporte

Se encontrar algum problema ou tiver sugestões:

1. Abra uma [Issue](https://github.com/Albinopedro/cansat-monitor/issues)
2. Descreva o problema ou sugestão detalhadamente
3. Inclua screenshots se relevante

## 🔮 Roadmap

- [ ] **v2.0**: Suporte a múltiplos sensores
- [ ] **v2.1**: Exportação de dados para CSV/Excel
- [ ] **v2.2**: Alertas configuráveis por limites
- [ ] **v2.3**: Integração com APIs de clima
- [ ] **v2.4**: Modo offline com sincronização

---

_Este projeto visa facilitar o monitoramento de dados de sensores em tempo real para projetos educacionais e de pesquisa._
