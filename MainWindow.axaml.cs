using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;
using System.IO;
using System.Text.Json;

// LiveCharts2 imports
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Defaults;

namespace CanSatMonitor
{
    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
    }

    public partial class MainWindow : Window
    {
        // UI Controls References
        private TextBlock? _temperatureValue;
        private TextBlock? _humidityValue;
        private TextBlock? _tempMaxValue;
        private TextBlock? _tempMinValue;
        private TextBlock? _tempMaxTime;
        private TextBlock? _tempMinTime;
        private TextBlock? _humidityAvgValue;
        private TextBlock? _statusText;
        private TextBlock? _headerSubTitle;
        private TextBlock? _connectionStatus;
        private Ellipse? _statusIndicator;
        private ProgressBar? _temperatureProgress;
        private ProgressBar? _humidityProgress;
        private Grid? _chartGrid;
        private StackPanel? _historyDataPanel;
        private ScrollViewer? _historyScrollViewer;
        private ComboBox? _portComboBox;
        private Button? _connectButton;
        private Button? _disconnectButton;

        // Time filter buttons
        private Button? _button1H;
        private Button? _button6H;
        private Button? _button24H;
        private Button? _button7D;
        private Button? _button30D;

        // LiveCharts components
        private CartesianChart? _cartesianChart;
        private ObservableCollection<DateTimePoint> _temperatureData;
        private ObservableCollection<DateTimePoint> _humidityData;

        // Serial communication
        private SerialPort? _serialPort;
        private bool _isConnected = false;
        private readonly Regex _dataRegex = new Regex(@"Humidade:\s*(\d+\.\d+),\s*Temperatura:\s*(\d+\.\d+)");

        // Data properties
        private string _selectedTimeFilter = "1H";
        private readonly List<DataPoint> _dataHistory = new();
        private DateTime _lastDataReceived = DateTime.MinValue;

        // Statistics tracking
        private double _maxTemperature = double.MinValue;
        private double _minTemperature = double.MaxValue;
        private DateTime _maxTempTime = DateTime.MinValue;
        private DateTime _minTempTime = DateTime.MinValue;
        private readonly List<double> _humidityValues = new();

        // Data persistence
        private readonly string _dataFilePath;
        private readonly Timer _saveTimer;

        // Theme
        private bool _isDarkTheme = false;

        // Responsive layout
        private bool _isCompactLayout = false;
        private double _currentWindowWidth = 1400;
        private double _currentWindowHeight = 900;

        public MainWindow()
        {
            // Setup data file path
            var appDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CanSatMonitor");
            Directory.CreateDirectory(appDataPath);
            _dataFilePath = System.IO.Path.Combine(appDataPath, "sensor_data.json");

            // Initialize LiveCharts collections FIRST
            _temperatureData = new ObservableCollection<DateTimePoint>();
            _humidityData = new ObservableCollection<DateTimePoint>();

            InitializeComponent();
            InitializeControls();

            // Load existing data BEFORE initializing charts
            LoadDataFromFile();

            InitializeLiveCharts();
            SetupEventHandlers();
            SetupThemeToggle();

            // Adiciona evento para limpar database
            var clearDbBtn = this.FindControl<Button>("ClearDatabaseButton");
            if (clearDbBtn != null)
                clearDbBtn.Click += async (s, e) =>
                {
                    var result = await ShowConfirmDialog("Tem certeza que deseja apagar todos os dados?");
                    if (result)
                    {
                        _dataHistory.Clear();
                        _temperatureData.Clear();
                        _humidityData.Clear();
                        UpdateChart();
                        UpdateHistoryTable();
                        ResetStatistics();
                        SaveDataToFile();
                        ShowFeedback("Dados apagados com sucesso!");
                    }
                };

            // Setup auto-save timer (save every 30 seconds)
            _saveTimer = new Timer(SaveDataToFile, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            // Configure window properties
            this.Title = "Dashboard CanSat - Monitor";
            this.Width = 1400;
            this.Height = 900;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SetMinimumWindowSize();

            // Atalho para alternar modo escuro/claro (Ctrl+D)
            this.KeyDown += (s, e) =>
            {
                if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control) && e.Key == Avalonia.Input.Key.D)
                {
                    _isDarkTheme = !_isDarkTheme;
                    SetThemeResources(_isDarkTheme);
                }
            };

            // Start connection status monitoring
            _ = StartConnectionMonitoring();

            // Update UI with loaded data
            UpdateHistoryTable();
            UpdateChart();
            RecalculateAllStatistics();

            // Initialize theme
            SetThemeResources(_isDarkTheme);

            // Setup responsive layout
            this.SizeChanged += OnWindowSizeChanged;
            this.PropertyChanged += OnWindowPropertyChanged;

            // Initial layout adjustment
            AdjustLayoutForWindowSize();

            if (_dataHistory.Count > 0)
            {
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_connectionStatus != null)
                        _connectionStatus.Text = $"Base de dados carregada: {_dataHistory.Count} registros";
                });
            }
        }

        private void LoadDataFromFile()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    var jsonData = File.ReadAllText(_dataFilePath);
                    var loadedData = JsonSerializer.Deserialize<List<DataPoint>>(jsonData);

                    if (loadedData != null && loadedData.Count > 0)
                    {
                        _dataHistory.Clear();
                        _dataHistory.AddRange(loadedData);

                        // Ordenar por timestamp para garantir ordem correta
                        _dataHistory.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

                        Console.WriteLine($"✅ Base de dados carregada: {_dataHistory.Count} registros");
                        Console.WriteLine($"📅 Período: {_dataHistory.First().Timestamp:dd/MM/yyyy HH:mm} até {_dataHistory.Last().Timestamp:dd/MM/yyyy HH:mm}");
                    }
                }
                else
                {
                    Console.WriteLine("🆕 Arquivo de dados não encontrado. Iniciando nova base de dados.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao carregar dados: {ex.Message}");
            }
        }

        private void SaveDataToFile(object? state = null)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(_dataHistory, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_dataFilePath, jsonData);
                Console.WriteLine($"💾 Base de dados salva: {_dataHistory.Count} registros");

                // Update UI status on main thread
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_connectionStatus != null && !_isConnected)
                    {
                        _connectionStatus.Text = $"Base de dados: {_dataHistory.Count} registros | Última atualização: {DateTime.Now:HH:mm:ss}";
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao salvar dados: {ex.Message}");
            }
        }

        private void RecalculateAllStatistics()
        {
            if (_dataHistory.Count == 0) return;

            // Reset statistics
            _maxTemperature = double.MinValue;
            _minTemperature = double.MaxValue;
            _humidityValues.Clear();

            // Calculate for the selected time period
            TimeSpan filterSpan = _selectedTimeFilter switch
            {
                "1H" => TimeSpan.FromHours(1),
                "6H" => TimeSpan.FromHours(6),
                "24H" => TimeSpan.FromHours(24),
                "7D" => TimeSpan.FromDays(7),
                "30D" => TimeSpan.FromDays(30),
                _ => TimeSpan.FromHours(1)
            };

            DateTime minTime = DateTime.Now - filterSpan;
            var filteredData = _dataHistory.Where(d => d.Timestamp >= minTime).ToList();

            if (filteredData.Count > 0)
            {
                var maxTempData = filteredData.OrderByDescending(d => d.Temperature).First();
                var minTempData = filteredData.OrderBy(d => d.Temperature).First();
                var avgHumidity = filteredData.Average(d => d.Humidity);

                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_tempMaxValue != null) _tempMaxValue.Text = $"{maxTempData.Temperature:F1}°";
                    if (_tempMinValue != null) _tempMinValue.Text = $"{minTempData.Temperature:F1}°";
                    if (_tempMaxTime != null) _tempMaxTime.Text = maxTempData.Timestamp.ToString("HH:mm");
                    if (_tempMinTime != null) _tempMinTime.Text = minTempData.Timestamp.ToString("HH:mm");
                    if (_humidityAvgValue != null) _humidityAvgValue.Text = $"{avgHumidity:F1}%";

                    // Update current values with the most recent data
                    var mostRecent = _dataHistory.LastOrDefault();
                    if (mostRecent != null)
                    {
                        UpdateSensorValues(mostRecent.Temperature, mostRecent.Humidity);
                    }
                });
            }
        }

        private void ResetStatistics()
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_tempMaxValue != null) _tempMaxValue.Text = "--°";
                if (_tempMinValue != null) _tempMinValue.Text = "--°";
                if (_tempMaxTime != null) _tempMaxTime.Text = "--:--";
                if (_tempMinTime != null) _tempMinTime.Text = "--:--";
                if (_humidityAvgValue != null) _humidityAvgValue.Text = "--%";
                if (_temperatureValue != null) _temperatureValue.Text = "--°C";
                if (_humidityValue != null) _humidityValue.Text = "--%";
                if (_temperatureProgress != null) _temperatureProgress.Value = 0;
                if (_humidityProgress != null) _humidityProgress.Value = 0;
            });
        }

        private void InitializeControls()
        {
            _temperatureValue = this.FindControl<TextBlock>("TemperatureValue");
            _humidityValue = this.FindControl<TextBlock>("HumidityValue");
            _tempMaxValue = this.FindControl<TextBlock>("TempMaxValue");
            _tempMinValue = this.FindControl<TextBlock>("TempMinValue");
            _tempMaxTime = this.FindControl<TextBlock>("TempMaxTime");
            _tempMinTime = this.FindControl<TextBlock>("TempMinTime");
            _humidityAvgValue = this.FindControl<TextBlock>("HumidityAvgValue");
            _statusText = this.FindControl<TextBlock>("StatusText");
            _headerSubTitle = this.FindControl<TextBlock>("HeaderSubTitle");
            _connectionStatus = this.FindControl<TextBlock>("ConnectionStatus");
            _statusIndicator = this.FindControl<Ellipse>("StatusIndicator");
            _temperatureProgress = this.FindControl<ProgressBar>("TemperatureProgress");
            _humidityProgress = this.FindControl<ProgressBar>("HumidityProgress");
            _chartGrid = this.FindControl<Grid>("ChartGrid");
            _historyDataPanel = this.FindControl<StackPanel>("HistoryDataPanel");
            _historyScrollViewer = this.FindControl<ScrollViewer>("HistoryScrollViewer");
            _portComboBox = this.FindControl<ComboBox>("PortComboBox");
            _connectButton = this.FindControl<Button>("ConnectButton");
            _disconnectButton = this.FindControl<Button>("DisconnectButton");

            _button1H = this.FindControl<Button>("Button1H");
            _button6H = this.FindControl<Button>("Button6H");
            _button24H = this.FindControl<Button>("Button24H");
            _button7D = this.FindControl<Button>("Button7D");
            _button30D = this.FindControl<Button>("Button30D");

            // Initial UI state
            if (_disconnectButton != null) _disconnectButton.IsEnabled = false;
        }

        private void InitializeLiveCharts()
        {
            if (_chartGrid == null) return;

            _cartesianChart = new CartesianChart
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Colors.White)
            };

            _cartesianChart.Series = new ISeries[]
            {
                new LineSeries<DateTimePoint>
                {
                    Values = _temperatureData,
                    Name = "Temperatura (°C)",
                    Stroke = new SolidColorPaint(new SKColor(255, 107, 107)) { StrokeThickness = 3 },
                    Fill = new SolidColorPaint(new SKColor(255, 107, 107, 50)) { },
                    GeometryStroke = new SolidColorPaint(new SKColor(255, 107, 107)) { StrokeThickness = 2 },
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometrySize = 0,
                    LineSmoothness = 0.5,
                    DataPadding = new LiveChartsCore.Drawing.LvcPoint(0.5f, 0.5f)
                },
                new LineSeries<DateTimePoint>
                {
                    Values = _humidityData,
                    Name = "Umidade (%)",
                    Stroke = new SolidColorPaint(new SKColor(78, 205, 196)) { StrokeThickness = 3 },
                    Fill = new SolidColorPaint(new SKColor(78, 205, 196, 30)) { },
                    GeometryStroke = new SolidColorPaint(new SKColor(78, 205, 196)) { StrokeThickness = 2 },
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometrySize = 0,
                    LineSmoothness = 0.5,
                    DataPadding = new LiveChartsCore.Drawing.LvcPoint(0.5f, 0.5f),
                    ScalesYAt = 1
                }
            };

            _cartesianChart.XAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Tempo",
                    NamePaint = new SolidColorPaint(SKColors.Black),
                    LabelsPaint = new SolidColorPaint(SKColors.Gray),
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 },
                    Labeler = value => {
                        try 
                        {
                            var ticks = (long)value;
                            if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
                                return "N/A";
                            
                            var dateTime = new DateTime(ticks);
                            
                            // Format based on current time filter
                            return _selectedTimeFilter switch
                            {
                                "30D" => dateTime.ToString("dd/MM"),
                                "7D" => dateTime.ToString("dd/MM\nHH:mm"),
                                _ => dateTime.ToString("HH:mm")
                            };
                        }
                        catch
                        {
                            return "N/A";
                        }
                    },
                    UnitWidth = TimeSpan.FromSeconds(1).Ticks,
                    MinStep = TimeSpan.FromSeconds(5).Ticks
                }
            };

            _cartesianChart.YAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Temperatura (°C)",
                    NamePaint = new SolidColorPaint(new SKColor(255, 107, 107)),
                    LabelsPaint = new SolidColorPaint(new SKColor(255, 107, 107)),
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(new SKColor(255, 107, 107, 50)) { StrokeThickness = 1 },
                    Position = LiveChartsCore.Measure.AxisPosition.Start,
                    Labeler = value => $"{value:F1}°C"
                },
                new Axis
                {
                    Name = "Umidade (%)",
                    NamePaint = new SolidColorPaint(new SKColor(78, 205, 196)),
                    LabelsPaint = new SolidColorPaint(new SKColor(78, 205, 196)),
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(new SKColor(78, 205, 196, 30)) { StrokeThickness = 1 },
                    Position = LiveChartsCore.Measure.AxisPosition.End,
                    Labeler = value => $"{value:F0}%",
                    ShowSeparatorLines = false
                }
            };

            _cartesianChart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Top;
            _cartesianChart.LegendTextPaint = new SolidColorPaint(SKColors.Black);
            _cartesianChart.LegendTextSize = 14;

            _cartesianChart.TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Top;
            _cartesianChart.TooltipTextPaint = new SolidColorPaint(SKColors.Black);
            _cartesianChart.TooltipBackgroundPaint = new SolidColorPaint(SKColors.White);
            
            // Configure tooltip
            _cartesianChart.TooltipFindingStrategy = LiveChartsCore.Measure.TooltipFindingStrategy.CompareOnlyX;

            // Habilitar zoom (apenas no eixo X)
            _cartesianChart.ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.X;

            _chartGrid.Children.Clear();
            _chartGrid.Children.Add(_cartesianChart);
            
            // Initialize X-axis formatting for default filter
            UpdateXAxisFormatting();
        }

        private void SetupEventHandlers()
        {
            if (_button1H != null) _button1H.Click += (s, e) => OnTimeFilterChanged("1H");
            if (_button6H != null) _button6H.Click += (s, e) => OnTimeFilterChanged("6H");
            if (_button24H != null) _button24H.Click += (s, e) => OnTimeFilterChanged("24H");
            if (_button7D != null) _button7D.Click += (s, e) => OnTimeFilterChanged("7D");
            if (_button30D != null) _button30D.Click += (s, e) => OnTimeFilterChanged("30D");

            if (_connectButton != null) _connectButton.Click += OnConnectClicked;
            if (_disconnectButton != null) _disconnectButton.Click += OnDisconnectClicked;

            // Filtro de data personalizado
            var startDatePicker = this.FindControl<DatePicker>("StartDatePicker");
            var endDatePicker = this.FindControl<DatePicker>("EndDatePicker");
            if (startDatePicker != null && endDatePicker != null)
            {
                startDatePicker.SelectedDateChanged += (s, e) => UpdateChartWithCustomDate();
                endDatePicker.SelectedDateChanged += (s, e) => UpdateChartWithCustomDate();
            }

            RefreshSerialPorts();
        }

        private void SetupThemeToggle()
        {
            var themeToggleButton = this.FindControl<Button>("ThemeToggleButton");
            if (themeToggleButton != null)
            {
                themeToggleButton.Click += (s, e) =>
                {
                    _isDarkTheme = !_isDarkTheme;
                    SetThemeResources(_isDarkTheme);
                    ApplyThemeToElements(_isDarkTheme);

                    // Update button icon based on theme
                    UpdateThemeToggleIcon(_isDarkTheme);
                };
            }
        }

        private void UpdateThemeToggleIcon(bool isDark)
        {
            var themeToggleButton = this.FindControl<Button>("ThemeToggleButton");
            var pathIcon = themeToggleButton?.Content as PathIcon;

            if (pathIcon != null)
            {
                if (isDark)
                {
                    // Moon icon for dark theme
                    pathIcon.Data = Avalonia.Media.Geometry.Parse("M17.293 13.293A8 8 0 016.707 2.707a8.001 8.001 0 1010.586 10.586z");
                }
                else
                {
                    // Sun icon for light theme
                    pathIcon.Data = Avalonia.Media.Geometry.Parse("M12 2.25a.75.75 0 01.75.75v2.25a.75.75 0 01-1.5 0V3a.75.75 0 01.75-.75zM7.5 12a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM18.894 6.166a.75.75 0 00-1.06-1.06l-1.591 1.59a.75.75 0 101.06 1.061l1.591-1.59zM21.75 12a.75.75 0 01-.75.75h-2.25a.75.75 0 010-1.5H21a.75.75 0 01.75.75zM17.834 18.894a.75.75 0 001.06-1.06l-1.59-1.591a.75.75 0 10-1.061 1.06l1.59 1.591zM12 18a.75.75 0 01.75.75V21a.75.75 0 01-1.5 0v-2.25A.75.75 0 0112 18zM7.758 17.303a.75.75 0 00-1.061-1.06l-1.591 1.59a.75.75 0 001.06 1.061l1.591-1.59zM6 12a.75.75 0 01-.75.75H3a.75.75 0 010-1.5h2.25A.75.75 0 016 12zM6.697 7.757a.75.75 0 001.06-1.06l-1.59-1.591a.75.75 0 00-1.061 1.06l1.59 1.591z");
                }
            }
        }

        private void RefreshSerialPorts()
        {
            if (_portComboBox == null) return;

            _portComboBox.Items.Clear();
            var ports = SerialPort.GetPortNames();

            foreach (var port in ports)
            {
                _portComboBox.Items.Add(port);
            }

            if (ports.Length > 0)
            {
                _portComboBox.SelectedIndex = 0;
            }
        }

        private void OnConnectClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_portComboBox?.SelectedItem == null) return;

            var selectedPort = _portComboBox.SelectedItem.ToString();
            if (string.IsNullOrEmpty(selectedPort)) return;

            try
            {
                _serialPort = new SerialPort(selectedPort, 9600, Parity.None, 8, StopBits.One);
                _serialPort.DataReceived += OnSerialDataReceived;
                _serialPort.Open();

                _isConnected = true;
                UpdateConnectionStatus($"Conectado em {selectedPort}", true);

                if (_connectButton != null) _connectButton.IsEnabled = false;
                if (_disconnectButton != null) _disconnectButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                UpdateConnectionStatus($"Erro: {ex.Message}", false);
            }
        }

        private void OnDisconnectClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            DisconnectSerial();
        }

        private void DisconnectSerial()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                }
                _serialPort = null;
                _isConnected = false;

                UpdateConnectionStatus($"Desconectado | Base de dados: {_dataHistory.Count} registros", false);

                if (_connectButton != null) _connectButton.IsEnabled = true;
                if (_disconnectButton != null) _disconnectButton.IsEnabled = false;

                SaveDataToFile();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao desconectar: {ex.Message}");
            }
        }

        private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen) return;

                string data = _serialPort.ReadLine().Trim();
                Console.WriteLine($"Dados recebidos: '{data}'");

                var match = _dataRegex.Match(data);
                if (match.Success)
                {
                    if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double humidity) &&
                        double.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double temperature))
                    {
                        _lastDataReceived = DateTime.Now;

                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            ProcessNewData(temperature, humidity);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao receber dados: {ex.Message}");
            }
        }

        private void ProcessNewData(double temperature, double humidity)
        {
            var dataPoint = new DataPoint
            {
                Timestamp = DateTime.Now,
                Temperature = temperature,
                Humidity = humidity
            };

            _dataHistory.Add(dataPoint);
            Console.WriteLine($"📊 Novo registro adicionado. Total: {_dataHistory.Count}");

            UpdateStatistics(temperature, humidity, dataPoint.Timestamp);
            UpdateSensorValues(temperature, humidity);
            UpdateHistoryTable();
            UpdateChart();
        }

        private void UpdateStatistics(double temperature, double humidity, DateTime timestamp)
        {
            if (temperature > _maxTemperature)
            {
                _maxTemperature = temperature;
                _maxTempTime = timestamp;
            }

            if (temperature < _minTemperature)
            {
                _minTemperature = temperature;
                _minTempTime = timestamp;
            }

            _humidityValues.Add(humidity);

            TimeSpan filterSpan = _selectedTimeFilter switch
            {
                "1H" => TimeSpan.FromHours(1),
                "6H" => TimeSpan.FromHours(6),
                "24H" => TimeSpan.FromHours(24),
                "7D" => TimeSpan.FromDays(7),
                "30D" => TimeSpan.FromDays(30),
                _ => TimeSpan.FromHours(1)
            };

            DateTime minTime = DateTime.Now - filterSpan;
            var filteredData = _dataHistory.Where(d => d.Timestamp >= minTime).ToList();

            if (filteredData.Count > 0)
            {
                var periodMaxTemp = filteredData.Max(d => d.Temperature);
                var periodMinTemp = filteredData.Min(d => d.Temperature);
                var periodAvgHumidity = filteredData.Average(d => d.Humidity);

                var maxTempData = filteredData.First(d => Math.Abs(d.Temperature - periodMaxTemp) < 0.01);
                var minTempData = filteredData.First(d => Math.Abs(d.Temperature - periodMinTemp) < 0.01);

                if (_tempMaxValue != null) _tempMaxValue.Text = $"{periodMaxTemp:F1}°";
                if (_tempMinValue != null) _tempMinValue.Text = $"{periodMinTemp:F1}°";
                if (_tempMaxTime != null) _tempMaxTime.Text = maxTempData.Timestamp.ToString("HH:mm");
                if (_tempMinTime != null) _tempMinTime.Text = minTempData.Timestamp.ToString("HH:mm");
                if (_humidityAvgValue != null) _humidityAvgValue.Text = $"{periodAvgHumidity:F1}%";
            }
        }

        private void UpdateSensorValues(double temperature, double humidity)
        {
            if (_temperatureValue != null)
            {
                _temperatureValue.Text = $"{temperature:F1}°C";
                TriggerValuePulseAnimation(_temperatureValue);
            }
            if (_humidityValue != null)
            {
                _humidityValue.Text = $"{humidity:F1}%";
                TriggerValuePulseAnimation(_humidityValue);
            }

            if (_temperatureProgress != null) _temperatureProgress.Value = Math.Min(temperature, 100);
            if (_humidityProgress != null) _humidityProgress.Value = Math.Min(humidity, 100);
        }

        private void TriggerValuePulseAnimation(TextBlock textBlock)
        {
            // Simple visual feedback without animations for compatibility
            textBlock.FontWeight = FontWeight.Bold;
            var timer = new System.Timers.Timer(200) { AutoReset = false };
            timer.Elapsed += (s, e) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    textBlock.FontWeight = FontWeight.Normal;
                });
                timer.Dispose();
            };
            timer.Start();
        }

        private void UpdateConnectionStatus(string status, bool isConnected)
        {
            if (_connectionStatus != null)
                _connectionStatus.Text = status;

            if (_headerSubTitle != null)
                _headerSubTitle.Text = isConnected
                    ? $"Recebendo dados do Arduino | Total de registros: {_dataHistory.Count}"
                    : $"Base de dados: {_dataHistory.Count} registros | Aguardando conexão...";

            if (_statusIndicator != null)
            {
                _statusIndicator.Classes.Clear();
                _statusIndicator.Classes.Add("status-indicator");
                if (isConnected)
                    _statusIndicator.Classes.Add("status-connected");
                else
                    _statusIndicator.Classes.Add("status-disconnected");
            }

            if (_statusText != null)
                _statusText.Text = isConnected ? "CONECTADO" : "DESCONECTADO";
        }

        private async Task StartConnectionMonitoring()
        {
            while (true)
            {
                await Task.Delay(5000);

                if (_isConnected && (DateTime.Now - _lastDataReceived).TotalSeconds > 10)
                {
                    UpdateConnectionStatus($"Sem dados há {(DateTime.Now - _lastDataReceived).TotalSeconds:F0}s | Total: {_dataHistory.Count} registros", false);
                }
            }
        }

        private void OnTimeFilterChanged(string filter)
        {
            _selectedTimeFilter = filter;
            UpdateTimeFilterButtons();
            UpdateChart();
            UpdateXAxisFormatting();
            RecalculateAllStatistics();
        }

        private void UpdateTimeFilterButtons()
        {
            ResetButtonStyle(_button1H);
            ResetButtonStyle(_button6H);
            ResetButtonStyle(_button24H);
            ResetButtonStyle(_button7D);
            ResetButtonStyle(_button30D);

            Button? selectedButton = _selectedTimeFilter switch
            {
                "1H" => _button1H,
                "6H" => _button6H,
                "24H" => _button24H,
                "7D" => _button7D,
                "30D" => _button30D,
                _ => _button1H
            };

            if (selectedButton != null)
            {
                selectedButton.Classes.Add("selected");
            }
        }

        private void ResetButtonStyle(Button? button)
        {
            if (button != null)
            {
                button.Classes.Remove("selected");
                // Ensure the button has the modern time-filter class
                if (!button.Classes.Contains("time-filter"))
                    button.Classes.Add("time-filter");
            }
        }

        private void UpdateHistoryTable()
        {
            if (_historyDataPanel == null || _dataHistory.Count == 0)
            {
                if (_historyDataPanel != null)
                {
                    _historyDataPanel.Children.Clear();
                    var waitingText = new TextBlock
                    {
                        Text = "Aguardando dados...",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20),
                        Foreground = _isDarkTheme ? new SolidColorBrush(Color.FromRgb(127, 140, 141)) : new SolidColorBrush(Color.FromRgb(127, 140, 141))
                    };
                    if (_isDarkTheme)
                    {
                        waitingText.Classes.Add("waiting-text");
                        waitingText.Classes.Add("dark");
                    }
                    _historyDataPanel.Children.Add(waitingText);
                }
                return;
            }

            _historyDataPanel.Children.Clear();

            var recentData = _dataHistory.TakeLast(50).Reverse().ToList();

            foreach (var data in recentData)
            {
                var row = CreateHistoryRow(
                    data.Timestamp.ToString("dd/MM HH:mm:ss"),
                    $"{data.Temperature:F1}°",
                    $"{data.Humidity:F0}%");

                _historyDataPanel.Children.Add(row);

                var separator = new Rectangle
                {
                    Height = 1,
                    Fill = _isDarkTheme ? new SolidColorBrush(Color.FromRgb(93, 109, 126)) : new SolidColorBrush(Color.FromRgb(236, 240, 241)),
                    Margin = new Thickness(0, 0, 0, 2)
                };
                _historyDataPanel.Children.Add(separator);
            }

            if (_historyScrollViewer != null)
            {
                _historyScrollViewer.ScrollToHome();
            }
        }

        private Border CreateHistoryRow(string time, string temp, string humidity)
        {
            var border = new Border
            {
                Classes = { "history-row" },
                Opacity = 1,
                RenderTransform = new TranslateTransform(0, 0)
            };

            if (_isDarkTheme)
            {
                border.Classes.Add("dark");
            }

            // Set initial state without animation for compatibility
            border.Opacity = 1;
            border.RenderTransform = new TranslateTransform(0, 0);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });

            var timeText = new TextBlock
            {
                Text = time,
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Classes = { "subtitle" }
            };

            var tempText = new TextBlock
            {
                Text = temp,
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            tempText.Foreground = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.FromRgb(255, 107, 107), 0),
                    new GradientStop(Color.FromRgb(255, 142, 83), 1)
                }
            };

            var humidityText = new TextBlock
            {
                Text = humidity,
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            humidityText.Foreground = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.FromRgb(78, 205, 196), 0),
                    new GradientStop(Color.FromRgb(68, 160, 141), 1)
                }
            };

            if (_isDarkTheme)
            {
                timeText.Classes.Add("dark");
            }

            Grid.SetColumn(timeText, 0);
            Grid.SetColumn(tempText, 1);
            Grid.SetColumn(humidityText, 2);

            grid.Children.Add(timeText);
            grid.Children.Add(tempText);
            grid.Children.Add(humidityText);

            border.Child = grid;

            return border;
        }

        private void UpdateChart()
        {
            if (_temperatureData == null || _humidityData == null)
                return;

            // Filtro de data personalizado
            var startDatePicker = this.FindControl<DatePicker>("StartDatePicker");
            var endDatePicker = this.FindControl<DatePicker>("EndDatePicker");
            DateTime? customStart = startDatePicker?.SelectedDate?.DateTime;
            DateTime? customEnd = endDatePicker?.SelectedDate?.DateTime;

            List<DataPoint> filteredData;
            if (customStart != null && customEnd != null && customEnd >= customStart)
            {
                filteredData = _dataHistory.Where(d => d.Timestamp >= customStart && d.Timestamp <= customEnd).ToList();
            }
            else
            {
                TimeSpan filterSpan = _selectedTimeFilter switch
                {
                    "1H" => TimeSpan.FromHours(1),
                    "6H" => TimeSpan.FromHours(6),
                    "24H" => TimeSpan.FromHours(24),
                    "7D" => TimeSpan.FromDays(7),
                    "30D" => TimeSpan.FromDays(30),
                    _ => TimeSpan.FromHours(1)
                };

                DateTime minTime = DateTime.Now - filterSpan;
                filteredData = _dataHistory.Where(d => d.Timestamp >= minTime).ToList();
            }

            _temperatureData.Clear();
            _humidityData.Clear();

            foreach (var point in filteredData)
            {
                var dateTimePoint = new DateTimePoint(point.Timestamp, point.Temperature);
                _temperatureData.Add(dateTimePoint);

                var humidityPoint = new DateTimePoint(point.Timestamp, point.Humidity);
                _humidityData.Add(humidityPoint);
            }

            if (_cartesianChart?.XAxes != null && filteredData.Count > 0)
            {
                var xAxesList = _cartesianChart.XAxes.ToList();
                if (xAxesList.Count > 0)
                {
                    var xAxis = xAxesList[0];
                    xAxis.MinLimit = filteredData.Min(d => d.Timestamp).Ticks;
                    xAxis.MaxLimit = filteredData.Max(d => d.Timestamp).Ticks;
                    
                    // Update axis step and unit based on time filter
                    UpdateXAxisFormatting();
                }
            }
        }

        private void UpdateChartWithCustomDate()
        {
            UpdateChart();
        }



        private void UpdateXAxisFormatting()
        {
            if (_cartesianChart?.XAxes == null) return;
            
            var xAxesList = _cartesianChart.XAxes.ToList();
            if (xAxesList.Count == 0) return;
            
            var xAxis = xAxesList[0];
            
            switch (_selectedTimeFilter)
            {
                case "1H":
                    xAxis.UnitWidth = TimeSpan.FromMinutes(5).Ticks;
                    xAxis.MinStep = TimeSpan.FromMinutes(1).Ticks;
                    break;
                case "6H":
                    xAxis.UnitWidth = TimeSpan.FromMinutes(30).Ticks;
                    xAxis.MinStep = TimeSpan.FromMinutes(10).Ticks;
                    break;
                case "24H":
                    xAxis.UnitWidth = TimeSpan.FromHours(2).Ticks;
                    xAxis.MinStep = TimeSpan.FromHours(1).Ticks;
                    break;
                case "7D":
                    xAxis.UnitWidth = TimeSpan.FromHours(12).Ticks;
                    xAxis.MinStep = TimeSpan.FromHours(6).Ticks;
                    break;
                case "30D":
                    xAxis.UnitWidth = TimeSpan.FromDays(2).Ticks;
                    xAxis.MinStep = TimeSpan.FromDays(1).Ticks;
                    break;
                default:
                    xAxis.UnitWidth = TimeSpan.FromMinutes(5).Ticks;
                    xAxis.MinStep = TimeSpan.FromMinutes(1).Ticks;
                    break;
            }
            
            // Update labeler for current filter
            xAxis.Labeler = value => {
                try 
                {
                    var ticks = (long)value;
                    if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
                        return "N/A";
                    
                    var dateTime = new DateTime(ticks);
                    
                    return _selectedTimeFilter switch
                    {
                        "30D" => dateTime.ToString("dd/MM"),
                        "7D" => dateTime.ToString("dd/MM\nHH:mm"),
                        _ => dateTime.ToString("HH:mm")
                    };
                }
                catch
                {
                    return "N/A";
                }
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // --- Diálogo de confirmação ---
        private async Task<bool> ShowConfirmDialog(string message)
        {
            var dialog = new Window
            {
                Width = 350,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Title = "Confirmação",
                CanResize = false,
                ShowInTaskbar = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(16),
                    Children =
                    {
                        new TextBlock { Text = message, Margin = new Thickness(0,0,0,16), FontSize = 16 },
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Children =
                            {
                                new Button { Content = "Sim", Width = 80, Margin = new Thickness(8,0,8,0) },
                                new Button { Content = "Não", Width = 80, Margin = new Thickness(8,0,8,0) }
                            }
                        }
                    }
                }
            };

            bool result = false;
            var yesBtn = ((dialog.Content as StackPanel)?.Children[1] as StackPanel)?.Children[0] as Button;
            var noBtn = ((dialog.Content as StackPanel)?.Children[1] as StackPanel)?.Children[1] as Button;

            if (yesBtn != null) yesBtn.Click += (_, __) => { result = true; dialog.Close(); };
            if (noBtn != null) noBtn.Click += (_, __) => { result = false; dialog.Close(); };

            await dialog.ShowDialog(this);
            return result;
        }

        // --- Feedback visual ---
        private void ShowFeedback(string message)
        {
            var rootGrid = this.FindControl<Grid>("RootGrid");
            if (rootGrid == null) return;

            var feedback = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 8),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Thickness(0, 24, 0, 0),
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.Bold,
                    FontSize = 16
                }
            };

            rootGrid.Children.Add(feedback);

            var timer = new System.Timers.Timer(1800) { AutoReset = false };
            timer.Elapsed += (s, e) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    rootGrid.Children.Remove(feedback);
                });
                timer.Dispose();
            };
            timer.Start();
        }

        // --- Alternância de tema ---
        private void SetThemeResources(bool isDark)
        {
            var app = Application.Current;
            if (app == null) return;

            var resources = app.Resources;
            if (isDark)
            {
                resources["AppBackground"] = new SolidColorBrush(Color.FromRgb(15, 20, 25));
                resources["CardBackground"] = new SolidColorBrush(Color.FromRgb(26, 32, 44));
                resources["CardBorder"] = new SolidColorBrush(Color.FromRgb(45, 55, 72));
                resources["TextPrimary"] = new SolidColorBrush(Color.FromRgb(247, 250, 252));
                resources["TextSecondary"] = new SolidColorBrush(Color.FromRgb(160, 174, 192));
            }
            else
            {
                resources["AppBackground"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                resources["CardBackground"] = new SolidColorBrush(Colors.White);
                resources["CardBorder"] = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                resources["TextPrimary"] = new SolidColorBrush(Color.FromRgb(26, 32, 44));
                resources["TextSecondary"] = new SolidColorBrush(Color.FromRgb(74, 85, 104));
            }

            // Aplicar classes dark aos elementos
            ApplyThemeToElements(isDark);
        }

        private void ApplyThemeToElements(bool isDark)
        {
            // Header container
            var headerContainer = this.FindControl<Border>("HeaderContainer");
            if (headerContainer != null)
            {
                if (isDark) headerContainer.Classes.Add("dark");
                else headerContainer.Classes.Remove("dark");
            }

            // All modern cards
            ApplyThemeToModernCards(isDark);

            // Time filter buttons
            ApplyThemeToTimeButtons(isDark);

            // Modern controls
            ApplyThemeToModernControls(isDark);

            // Chart container
            ApplyThemeToChart(isDark);

            // Text blocks
            ApplyThemeToTextBlocks(isDark);

            // History rows
            ApplyThemeToHistoryRows(isDark);
        }

        private void ApplyThemeToModernCards(bool isDark)
        {
            var cardNames = new[] { "ConnectionCard", "TemperatureCard", "HumidityCard", "StatsCard", "ChartCard", "HistoryCard" };

            foreach (var cardName in cardNames)
            {
                var card = this.FindControl<Border>(cardName);
                if (card != null)
                {
                    if (isDark)
                    {
                        card.Classes.Remove("modern-card");
                        card.Classes.Add("modern-card");
                        card.Classes.Add("dark");
                    }
                    else
                    {
                        card.Classes.Remove("dark");
                        card.Classes.Remove("modern-card");
                        card.Classes.Add("modern-card");
                    }
                }
            }
        }

        private void ApplyThemeToModernControls(bool isDark)
        {
            // ComboBox
            var portCombo = this.FindControl<ComboBox>("PortComboBox");
            if (portCombo != null)
            {
                if (isDark) portCombo.Classes.Add("dark");
                else portCombo.Classes.Remove("dark");
            }

            // DatePickers
            var startDatePicker = this.FindControl<DatePicker>("StartDatePicker");
            var endDatePicker = this.FindControl<DatePicker>("EndDatePicker");

            if (startDatePicker != null)
            {
                if (isDark) startDatePicker.Classes.Add("dark");
                else startDatePicker.Classes.Remove("dark");
            }

            if (endDatePicker != null)
            {
                if (isDark) endDatePicker.Classes.Add("dark");
                else endDatePicker.Classes.Remove("dark");
            }

            // ScrollViewers
            var scrollViewers = new[] { "LeftPanel", "RightPanel", "HistoryScrollViewer" };
            foreach (var scrollName in scrollViewers)
            {
                var scroll = this.FindControl<ScrollViewer>(scrollName);
                if (scroll != null)
                {
                    if (isDark) scroll.Classes.Add("dark");
                    else scroll.Classes.Remove("dark");
                }
            }
        }

        private void ApplyThemeToHistoryRows(bool isDark)
        {
            var historyPanel = this.FindControl<StackPanel>("HistoryDataPanel");
            if (historyPanel != null)
            {
                foreach (var child in historyPanel.Children)
                {
                    if (child is Border row)
                    {
                        if (isDark) row.Classes.Add("dark");
                        else row.Classes.Remove("dark");
                    }
                }
            }
        }

        private void ApplyThemeToAllCards(bool isDark)
        {
            // Encontrar todos os Border com classe "card" e aplicar tema
            var cards = new[]
            {
                this.FindControl<Border>("ConnectionCard"),
                this.FindControl<Border>("TemperatureCard"),
                this.FindControl<Border>("HumidityCard"),
                this.FindControl<Border>("StatsCard"),
                this.FindControl<Border>("ChartCard"),
                this.FindControl<Border>("HistoryCard")
            };

            foreach (var card in cards)
            {
                if (card != null)
                {
                    if (isDark)
                    {
                        card.Classes.Add("dark");
                        card.Background = new SolidColorBrush(Color.FromRgb(44, 62, 80));
                    }
                    else
                    {
                        card.Classes.Remove("dark");
                        card.Background = new SolidColorBrush(Colors.White);
                    }
                }
            }

            // Chart container
            var chartContainer = this.FindControl<Border>("ChartContainer");
            if (chartContainer != null)
            {
                if (isDark)
                {
                    chartContainer.Classes.Add("dark");
                    chartContainer.Background = new SolidColorBrush(Color.FromRgb(44, 62, 80));
                }
                else
                {
                    chartContainer.Classes.Remove("dark");
                    chartContainer.Background = new SolidColorBrush(Color.FromRgb(250, 250, 250));
                }
            }
        }

        private void ApplyThemeToTimeButtons(bool isDark)
        {
            var buttons = new[] { _button1H, _button6H, _button24H, _button7D, _button30D };
            foreach (var btn in buttons)
            {
                if (btn != null)
                {
                    // Ensure the button has the time-filter class
                    if (!btn.Classes.Contains("time-filter"))
                        btn.Classes.Add("time-filter");

                    if (isDark) btn.Classes.Add("dark");
                    else btn.Classes.Remove("dark");
                }
            }

            // Clear button
            var clearBtn = this.FindControl<Button>("ClearDatabaseButton");
            if (clearBtn != null)
            {
                if (isDark) clearBtn.Classes.Add("dark");
                else clearBtn.Classes.Remove("dark");
            }
        }

        private void ApplyThemeToOtherControls(bool isDark)
        {
            // Apply theme to main TextBlocks
            ApplyThemeToTextBlocks(isDark);

            // ComboBox
            if (_portComboBox != null)
            {
                if (isDark)
                {
                    _portComboBox.Classes.Add("dark");
                    _portComboBox.Background = new SolidColorBrush(Color.FromRgb(52, 73, 94));
                    _portComboBox.Foreground = new SolidColorBrush(Color.FromRgb(236, 240, 241));
                }
                else
                {
                    _portComboBox.Classes.Remove("dark");
                    _portComboBox.Background = new SolidColorBrush(Colors.White);
                    _portComboBox.Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80));
                }
            }

            // DatePickers
            var startDatePicker = this.FindControl<DatePicker>("StartDatePicker");
            var endDatePicker = this.FindControl<DatePicker>("EndDatePicker");

            foreach (var datePicker in new[] { startDatePicker, endDatePicker })
            {
                if (datePicker != null)
                {
                    if (isDark)
                    {
                        datePicker.Classes.Add("dark");
                        datePicker.Background = new SolidColorBrush(Color.FromRgb(52, 73, 94));
                        datePicker.Foreground = new SolidColorBrush(Color.FromRgb(236, 240, 241));
                    }
                    else
                    {
                        datePicker.Classes.Remove("dark");
                        datePicker.Background = new SolidColorBrush(Colors.White);
                        datePicker.Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80));
                    }
                }
            }

            // ScrollViewer
            if (_historyScrollViewer != null)
            {
                if (isDark)
                {
                    _historyScrollViewer.Classes.Add("dark");
                    _historyScrollViewer.Background = new SolidColorBrush(Color.FromRgb(44, 62, 80));
                }
                else
                {
                    _historyScrollViewer.Classes.Remove("dark");
                    _historyScrollViewer.Background = new SolidColorBrush(Colors.White);
                }
            }

            // Connection buttons
            if (_connectButton != null)
            {
                if (isDark) _connectButton.Classes.Add("dark");
                else _connectButton.Classes.Remove("dark");
            }

            if (_disconnectButton != null)
            {
                if (isDark) _disconnectButton.Classes.Add("dark");
                else _disconnectButton.Classes.Remove("dark");
            }

            // Progress bars
            if (_temperatureProgress != null)
            {
                if (isDark)
                {
                    _temperatureProgress.Classes.Add("dark");
                    _temperatureProgress.Background = new SolidColorBrush(Color.FromRgb(52, 73, 94));
                }
                else
                {
                    _temperatureProgress.Classes.Remove("dark");
                    _temperatureProgress.Background = new SolidColorBrush(Color.FromRgb(250, 219, 216));
                }
            }

            if (_humidityProgress != null)
            {
                if (isDark)
                {
                    _humidityProgress.Classes.Add("dark");
                    _humidityProgress.Background = new SolidColorBrush(Color.FromRgb(52, 73, 94));
                }
                else
                {
                    _humidityProgress.Classes.Remove("dark");
                    _humidityProgress.Background = new SolidColorBrush(Color.FromRgb(214, 234, 248));
                }
            }
        }

        private void ApplyThemeToChart(bool isDark)
        {
            if (_cartesianChart != null)
            {
                if (isDark)
                {
                    // Dark theme background
                    _cartesianChart.Background = new SolidColorBrush(Color.FromRgb(26, 32, 44));

                    // Update chart text colors
                    if (_cartesianChart.LegendTextPaint != null)
                        _cartesianChart.LegendTextPaint = new SolidColorPaint(new SKColor(247, 250, 252));

                    if (_cartesianChart.TooltipTextPaint != null)
                        _cartesianChart.TooltipTextPaint = new SolidColorPaint(new SKColor(247, 250, 252));

                    if (_cartesianChart.TooltipBackgroundPaint != null)
                        _cartesianChart.TooltipBackgroundPaint = new SolidColorPaint(new SKColor(45, 55, 72));
                }
                else
                {
                    // Light theme
                    _cartesianChart.Background = new SolidColorBrush(Colors.White);

                    if (_cartesianChart.LegendTextPaint != null)
                        _cartesianChart.LegendTextPaint = new SolidColorPaint(SKColors.Black);

                    if (_cartesianChart.TooltipTextPaint != null)
                        _cartesianChart.TooltipTextPaint = new SolidColorPaint(SKColors.Black);

                    if (_cartesianChart.TooltipBackgroundPaint != null)
                        _cartesianChart.TooltipBackgroundPaint = new SolidColorPaint(SKColors.White);
                }
            }
        }

        private void ApplyThemeToTextBlocks(bool isDark)
        {
            var darkColor = new SolidColorBrush(Color.FromRgb(236, 240, 241));
            var lightColor = new SolidColorBrush(Color.FromRgb(44, 62, 80));
            var textColor = isDark ? darkColor : lightColor;

            // Main value TextBlocks
            if (_temperatureValue != null)
            {
                _temperatureValue.Foreground = isDark ? new SolidColorBrush(Color.FromRgb(231, 76, 60)) : new SolidColorBrush(Color.FromRgb(231, 76, 60));
                if (isDark) _temperatureValue.Classes.Add("dark");
                else _temperatureValue.Classes.Remove("dark");
            }

            if (_humidityValue != null)
            {
                _humidityValue.Foreground = isDark ? new SolidColorBrush(Color.FromRgb(52, 152, 219)) : new SolidColorBrush(Color.FromRgb(52, 152, 219));
                if (isDark) _humidityValue.Classes.Add("dark");
                else _humidityValue.Classes.Remove("dark");
            }

            // Statistics TextBlocks
            var statsTextBlocks = new[] { _tempMaxValue, _tempMinValue, _humidityAvgValue };
            foreach (var textBlock in statsTextBlocks)
            {
                if (textBlock != null)
                {
                    if (isDark) textBlock.Classes.Add("dark");
                    else textBlock.Classes.Remove("dark");
                }
            }

            // Time TextBlocks
            var timeTextBlocks = new[] { _tempMaxTime, _tempMinTime };
            foreach (var textBlock in timeTextBlocks)
            {
                if (textBlock != null)
                {
                    textBlock.Foreground = textColor;
                    if (isDark) textBlock.Classes.Add("dark");
                    else textBlock.Classes.Remove("dark");
                }
            }

            // Status TextBlocks
            if (_statusText != null)
            {
                _statusText.Foreground = isDark ? darkColor : new SolidColorBrush(Colors.White);
                if (isDark) _statusText.Classes.Add("dark");
                else _statusText.Classes.Remove("dark");
            }

            if (_connectionStatus != null)
            {
                _connectionStatus.Foreground = textColor;
                if (isDark) _connectionStatus.Classes.Add("dark");
                else _connectionStatus.Classes.Remove("dark");
            }

            if (_headerSubTitle != null)
            {
                _headerSubTitle.Foreground = isDark ? new SolidColorBrush(Color.FromRgb(189, 195, 199)) : new SolidColorBrush(Color.FromRgb(236, 240, 241));
                if (isDark) _headerSubTitle.Classes.Add("dark");
                else _headerSubTitle.Classes.Remove("dark");
            }

            // Apply theme to title TextBlocks by finding them
            ApplyThemeToTitleTextBlocks(isDark);
        }

        private void ApplyThemeToTitleTextBlocks(bool isDark)
        {
            var textColor = isDark ? new SolidColorBrush(Color.FromRgb(236, 240, 241)) : new SolidColorBrush(Color.FromRgb(44, 62, 80));

            // Find all title TextBlocks in the UI
            var titles = new[]
            {
                "🔌 CONEXÃO ARDUINO",
                "🌡️ TEMPERATURA",
                "💧 UMIDADE",
                "Estatísticas do Período",
                "Gráfico em Tempo Real",
                "Histórico Recente"
            };

            foreach (var titleText in titles)
            {
                var titleBlock = FindTextBlockByText(titleText);
                if (titleBlock != null)
                {
                    titleBlock.Foreground = textColor;
                    if (isDark)
                    {
                        titleBlock.Classes.Add("title");
                        titleBlock.Classes.Add("dark");
                    }
                    else titleBlock.Classes.Remove("dark");
                }
            }
        }

        private TextBlock? FindTextBlockByText(string text)
        {
            // Helper method to find TextBlock by its text content
            return FindTextBlockRecursive(this.Content as Control, text);
        }

        private TextBlock? FindTextBlockRecursive(Control? control, string text)
        {
            if (control == null) return null;

            if (control is TextBlock textBlock && textBlock.Text == text)
            {
                return textBlock;
            }

            if (control is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    var result = FindTextBlockRecursive(child, text);
                    if (result != null) return result;
                }
            }
            else if (control is ContentControl contentControl && contentControl.Content is Control childControl)
            {
                return FindTextBlockRecursive(childControl, text);
            }

            return null;
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Save data before closing
                SaveDataToFile();

                // Dispose timer
                _saveTimer?.Dispose();

                // Disconnect serial
                DisconnectSerial();

                Console.WriteLine($"🚪 Aplicação fechada. Base de dados final: {_dataHistory.Count} registros");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao fechar aplicação: {ex.Message}");
            }

            base.OnClosed(e);
        }

        // --- Responsive Layout Methods ---
        private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            _currentWindowWidth = e.NewSize.Width;
            _currentWindowHeight = e.NewSize.Height;
            AdjustLayoutForWindowSize();
        }

        private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "Bounds" || e.Property.Name == "ClientSize")
            {
                AdjustLayoutForWindowSize();
            }
        }

        private void AdjustLayoutForWindowSize()
        {
            var mainGrid = this.FindControl<Grid>("MainGrid");
            if (mainGrid == null) return;

            // Clear existing column definitions
            mainGrid.ColumnDefinitions.Clear();

            if (_currentWindowWidth < 1000)
            {
                SwitchToCompactLayout();
                // Single column layout for small screens
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            else if (_currentWindowWidth < 1400)
            {
                SwitchToTabletLayout();
                // Two column layout for medium screens
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(380) });
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            else
            {
                SwitchToDesktopLayout();
                // Three column layout for large screens with proper spacing
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340) });
            }

            AdjustFontSizes();
            AdjustCardPadding(_currentWindowWidth / 1400.0);
        }

        private void SwitchToCompactLayout()
        {
            if (_isCompactLayout) return;
            _isCompactLayout = true;

            var mainGrid = this.FindControl<Grid>("MainGrid");
            if (mainGrid == null) return;

            // Switch to vertical stacking for small screens
            mainGrid.RowDefinitions.Clear();

            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Rearrange panels vertically
            var leftPanel = this.FindControl<ScrollViewer>("LeftPanel");
            var centerPanel = this.FindControl<Border>("ChartCard");
            var rightPanel = this.FindControl<ScrollViewer>("RightPanel");

            if (leftPanel != null)
            {
                Grid.SetRow(leftPanel, 0);
                Grid.SetColumn(leftPanel, 0);
                leftPanel.MaxHeight = 300;
            }
            if (centerPanel != null)
            {
                Grid.SetRow(centerPanel, 1);
                Grid.SetColumn(centerPanel, 0);
            }
            if (rightPanel != null)
            {
                Grid.SetRow(rightPanel, 2);
                Grid.SetColumn(rightPanel, 0);
                rightPanel.MaxHeight = 250;
            }
        }

        private void SwitchToTabletLayout()
        {
            _isCompactLayout = false;

            var mainGrid = this.FindControl<Grid>("MainGrid");
            if (mainGrid == null) return;

            // Two column layout for tablets
            mainGrid.RowDefinitions.Clear();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var leftPanel = this.FindControl<ScrollViewer>("LeftPanel");
            var centerPanel = this.FindControl<Border>("ChartCard");
            var rightPanel = this.FindControl<ScrollViewer>("RightPanel");

            if (leftPanel != null)
            {
                Grid.SetColumn(leftPanel, 0);
                Grid.SetRow(leftPanel, 0);
                Grid.SetColumnSpan(leftPanel, 1);
                leftPanel.MaxHeight = double.PositiveInfinity;
            }
            if (centerPanel != null)
            {
                Grid.SetColumn(centerPanel, 2);
                Grid.SetRow(centerPanel, 0);
                Grid.SetColumnSpan(centerPanel, 1);
            }
            if (rightPanel != null)
            {
                Grid.SetColumn(rightPanel, 2);
                Grid.SetRow(rightPanel, 0);
                rightPanel.MaxHeight = double.PositiveInfinity;
            }
        }

        private void SwitchToDesktopLayout()
        {
            _isCompactLayout = false;

            var mainGrid = this.FindControl<Grid>("MainGrid");
            if (mainGrid == null) return;

            // Reset to single row
            mainGrid.RowDefinitions.Clear();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var leftPanel = this.FindControl<ScrollViewer>("LeftPanel");
            var centerPanel = this.FindControl<Border>("ChartCard");
            var rightPanel = this.FindControl<ScrollViewer>("RightPanel");

            if (leftPanel != null)
            {
                Grid.SetColumn(leftPanel, 0);
                Grid.SetRow(leftPanel, 0);
                Grid.SetColumnSpan(leftPanel, 1);
                leftPanel.MaxHeight = double.PositiveInfinity;
            }
            if (centerPanel != null)
            {
                Grid.SetColumn(centerPanel, 2);
                Grid.SetRow(centerPanel, 0);
                Grid.SetColumnSpan(centerPanel, 1);
            }
            if (rightPanel != null)
            {
                Grid.SetColumn(rightPanel, 4);
                Grid.SetRow(rightPanel, 0);
                Grid.SetColumnSpan(rightPanel, 1);
                rightPanel.MaxHeight = double.PositiveInfinity;
            }
        }

        private void AdjustFontSizes()
        {
            var scaleFactor = Math.Min(_currentWindowWidth / 1400.0, _currentWindowHeight / 900.0);
            scaleFactor = Math.Max(0.7, Math.Min(1.3, scaleFactor)); // Limit scale between 0.7 and 1.3

            // Adjust header title font size
            var headerTitle = this.FindControl<TextBlock>("HeaderTitle");
            if (headerTitle != null)
            {
                headerTitle.FontSize = Math.Max(16, 28 * scaleFactor);
            }

            // Adjust header subtitle font size
            var headerSubtitle = this.FindControl<TextBlock>("HeaderSubtitle");
            if (headerSubtitle != null)
            {
                headerSubtitle.FontSize = Math.Max(10, 14 * scaleFactor);
            }

            // Adjust card padding based on scale
            AdjustCardPadding(scaleFactor);
        }

        private void AdjustCardPadding(double scaleFactor)
        {
            var cards = new[]
            {
                this.FindControl<Border>("ConnectionCard"),
                this.FindControl<Border>("TemperatureCard"),
                this.FindControl<Border>("HumidityCard"),
                this.FindControl<Border>("StatsCard"),
                this.FindControl<Border>("ChartCard"),
                this.FindControl<Border>("HistoryCard")
            };

            var basePadding = 16;
            var adjustedPadding = Math.Max(8, basePadding * scaleFactor);

            foreach (var card in cards)
            {
                if (card != null)
                {
                    card.Padding = new Thickness(adjustedPadding * 0.75, adjustedPadding);
                    card.Margin = new Thickness(4 * scaleFactor, 8 * scaleFactor);
                }
            }
        }

        private void SetMinimumWindowSize()
        {
            this.MinWidth = 1400;
            this.MinHeight = 600;
        }
    }
}
