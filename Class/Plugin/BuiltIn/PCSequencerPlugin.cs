using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;

namespace Phobos.Class.Plugin.BuiltIn
{
    /// <summary>
    /// 序列执行状态
    /// </summary>
    public enum SequenceState
    {
        Idle,
        Running,
        Paused,
        Completed,
        Error
    }

    /// <summary>
    /// Sequencer 插件 - 逐行执行目标文件中的命令
    /// </summary>
    public class PCSequencerPlugin : PCPluginBase
    {
        private Grid? _contentGrid;
        private TextBox? _filePathTextBox;
        private TextBox? _outputTextBox;
        private ProgressBar? _progressBar;
        private TextBlock? _statusText;
        private Button? _startButton;
        private Button? _pauseButton;
        private Button? _stopButton;

        private CancellationTokenSource? _cts;
        private SequenceState _state = SequenceState.Idle;
        private bool _isPaused = false;
        private readonly ManualResetEventSlim _pauseEvent = new(true);

        public override PluginMetadata Metadata { get; } = new PluginMetadata
        {
            Name = "Sequencer",
            PackageName = "com.phobos.sequencer",
            Manufacturer = "Phobos Team",
            Version = "1.0.0",
            Secret = "phobos_sequencer_secret_djf901las0pd",
            DatabaseKey = "seq",
            LocalizedNames = new Dictionary<string, string>
            {
                { "en-US", "Sequencer" },
                { "zh-CN", "序列执行器" },
                { "zh-TW", "序列執行器" },
                { "ja-JP", "シーケンサー" },
                { "ko-KR", "시퀀서" }
            },
            LocalizedDescriptions = new Dictionary<string, string>
            {
                { "en-US", "Execute commands line by line from a file" },
                { "zh-CN", "逐行执行文件中的命令" },
                { "zh-TW", "逐行執行檔案中的命令" },
                { "ja-JP", "ファイルからコマンドを行ごとに実行" },
                { "ko-KR", "파일에서 명령을 한 줄씩 실행" }
            }
        };

        public override FrameworkElement? ContentArea => _contentGrid;

        public override async Task<RequestResult> OnInstall(params object[] args)
        {
            // 注册协议
            var protocols = new[] { "seq", "sequence", "Phobos.Sequencer" };
            foreach (var protocol in protocols)
            {
                await Link(new LinkAssociation
                {
                    Protocol = protocol,
                    Name = "SequencerHandler_General",
                    Description = "Sequencer Protocol Handler",
                    Command = "seq://v1?file=%0"
                });
            }

            return await base.OnInstall(args);
        }

        public override Task<RequestResult> OnLaunch(params object[] args)
        {
            InitializeUI();
            return base.OnLaunch(args);
        }

        private void InitializeUI()
        {
            _contentGrid = new Grid
            {
                Margin = new Thickness(10)
            };

            _contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            _contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            _contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            _contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });

            // 标题
            var titleLabel = new TextBlock
            {
                Text = Metadata.GetLocalizedName(LocalizationManager.Instance.CurrentLanguage),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(titleLabel, 0);

            // 文件选择
            var filePanel = new Grid();
            Grid.SetRow(filePanel, 1);
            filePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            filePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

            _filePathTextBox = new TextBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(10, 0, 10, 0)
            };
            Grid.SetColumn(_filePathTextBox, 0);

            var browseButton = new Button
            {
                Content = "Browse",
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(5, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            Grid.SetColumn(browseButton, 1);
            browseButton.Click += BrowseFile;

            filePanel.Children.Add(_filePathTextBox);
            filePanel.Children.Add(browseButton);

            // 输出区域
            _outputTextBox = new TextBox
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
                Foreground = Brushes.LightGreen,
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas"),
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 10, 0, 10)
            };
            Grid.SetRow(_outputTextBox, 2);

            // 进度条和状态
            var progressPanel = new Grid();
            Grid.SetRow(progressPanel, 3);
            progressPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            progressPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

            _progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 150, 255))
            };
            Grid.SetColumn(_progressBar, 0);

            _statusText = new TextBlock
            {
                Text = "Ready",
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10, 0, 0, 0)
            };
            Grid.SetColumn(_statusText, 1);

            progressPanel.Children.Add(_progressBar);
            progressPanel.Children.Add(_statusText);

            // 按钮区
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(buttonPanel, 4);

            _startButton = CreateButton("Start", StartSequence);
            _pauseButton = CreateButton("Pause", PauseSequence);
            _stopButton = CreateButton("Stop", StopSequence);

            _pauseButton.IsEnabled = false;
            _stopButton.IsEnabled = false;

            buttonPanel.Children.Add(_startButton);
            buttonPanel.Children.Add(_pauseButton);
            buttonPanel.Children.Add(_stopButton);

            _contentGrid.Children.Add(titleLabel);
            _contentGrid.Children.Add(filePanel);
            _contentGrid.Children.Add(_outputTextBox);
            _contentGrid.Children.Add(progressPanel);
            _contentGrid.Children.Add(buttonPanel);
        }

        private Button CreateButton(string text, Action onClick)
        {
            var button = new Button
            {
                Content = text,
                Width = 100,
                Height = 35,
                Margin = new Thickness(5, 0, 5, 0),
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            button.Click += (s, e) => onClick();
            return button;
        }

        private void BrowseFile(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Sequence files (*.seq;*.txt)|*.seq;*.txt|All files (*.*)|*.*",
                Title = "Select Sequence File"
            };

            if (dialog.ShowDialog() == true && _filePathTextBox != null)
            {
                _filePathTextBox.Text = dialog.FileName;
            }
        }

        private async void StartSequence()
        {
            if (string.IsNullOrEmpty(_filePathTextBox?.Text))
            {
                SetStatus("Please select a file");
                return;
            }

            if (!File.Exists(_filePathTextBox.Text))
            {
                SetStatus("File not found");
                return;
            }

            _cts = new CancellationTokenSource();
            _isPaused = false;
            _pauseEvent.Set();

            UpdateButtonStates(true);
            ClearOutput();

            try
            {
                await ExecuteSequenceFile(_filePathTextBox.Text, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                AppendOutput("Sequence cancelled.");
                SetStatus("Cancelled");
            }
            catch (Exception ex)
            {
                AppendOutput($"Error: {ex.Message}");
                SetStatus("Error");
            }
            finally
            {
                UpdateButtonStates(false);
            }
        }

        private void PauseSequence()
        {
            if (_isPaused)
            {
                _isPaused = false;
                _pauseEvent.Set();
                _pauseButton!.Content = "Pause";
                SetStatus("Running");
            }
            else
            {
                _isPaused = true;
                _pauseEvent.Reset();
                _pauseButton!.Content = "Resume";
                SetStatus("Paused");
            }
        }

        private void StopSequence()
        {
            _cts?.Cancel();
            _pauseEvent.Set();
        }

        private async Task ExecuteSequenceFile(string filePath, CancellationToken token)
        {
            var lines = await File.ReadAllLinesAsync(filePath, token);
            var totalLines = lines.Length;
            var currentLine = 0;

            SetStatus("Running");
            AppendOutput($"Starting sequence: {Path.GetFileName(filePath)}");
            AppendOutput($"Total commands: {totalLines}");
            AppendOutput(new string('-', 50));

            foreach (var line in lines)
            {
                token.ThrowIfCancellationRequested();
                _pauseEvent.Wait(token);

                currentLine++;
                var trimmedLine = line.Trim();

                // 跳过空行和注释
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith("//"))
                {
                    UpdateProgress(currentLine, totalLines);
                    continue;
                }

                AppendOutput($"[{currentLine}/{totalLines}] Executing: {trimmedLine}");

                try
                {
                    var result = await ExecuteCommand(trimmedLine, token);
                    if (!string.IsNullOrEmpty(result))
                    {
                        AppendOutput($"  Output: {result}");
                    }
                }
                catch (Exception ex)
                {
                    AppendOutput($"  Error: {ex.Message}");
                }

                UpdateProgress(currentLine, totalLines);
            }

            AppendOutput(new string('-', 50));
            AppendOutput("Sequence completed.");
            SetStatus("Completed");
        }

        private async Task<string> ExecuteCommand(string command, CancellationToken token)
        {
            // 解析命令
            var parts = command.Split(' ', 2);
            var cmd = parts[0].ToLowerInvariant();
            var args = parts.Length > 1 ? parts[1] : string.Empty;

            switch (cmd)
            {
                case "wait":
                case "delay":
                    if (int.TryParse(args, out var ms))
                    {
                        await Task.Delay(ms, token);
                        return $"Waited {ms}ms";
                    }
                    return "Invalid delay value";

                case "echo":
                case "print":
                    return args;

                case "run":
                case "exec":
                    return await RunProcess(args, token);

                case "phobos":
                    // 调用 Phobos 命令
                    return $"Phobos command: {args}";

                default:
                    // 默认作为系统命令执行
                    return await RunProcess(command, token);
            }
        }

        private async Task<string> RunProcess(string command, CancellationToken token)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                    return "Failed to start process";

                var output = await process.StandardOutput.ReadToEndAsync(token);
                var error = await process.StandardError.ReadToEndAsync(token);
                await process.WaitForExitAsync(token);

                return string.IsNullOrEmpty(error) ? output.Trim() : $"{output}\nError: {error}".Trim();
            }
            catch (Exception ex)
            {
                return $"Process error: {ex.Message}";
            }
        }

        private void UpdateProgress(int current, int total)
        {
            if (_progressBar == null) return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                _progressBar.Value = (double)current / total * 100;
            });
        }

        private void SetStatus(string status)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_statusText != null)
                    _statusText.Text = status;
            });
        }

        private void AppendOutput(string text)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_outputTextBox != null)
                {
                    _outputTextBox.AppendText(text + Environment.NewLine);
                    _outputTextBox.ScrollToEnd();
                }
            });
        }

        private void ClearOutput()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_outputTextBox != null)
                    _outputTextBox.Clear();
                if (_progressBar != null)
                    _progressBar.Value = 0;
            });
        }

        private void UpdateButtonStates(bool isRunning)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_startButton != null) _startButton.IsEnabled = !isRunning;
                if (_pauseButton != null) _pauseButton.IsEnabled = isRunning;
                if (_stopButton != null) _stopButton.IsEnabled = isRunning;
                if (_pauseButton != null) _pauseButton.Content = "Pause";
            });
        }

        public override async Task<RequestResult> Run(params object[] args)
        {
            if (args.Length > 0 && args[0] is string action)
            {
                switch (action.ToLowerInvariant())
                {
                    case "execute":
                        if (args.Length > 1 && args[1] is string filePath)
                        {
                            if (_filePathTextBox != null)
                                _filePathTextBox.Text = filePath;
                            StartSequence();
                            return new RequestResult { Success = true, Message = "Sequence started" };
                        }
                        break;

                    case "stop":
                        StopSequence();
                        return new RequestResult { Success = true, Message = "Sequence stopped" };

                    case "pause":
                        PauseSequence();
                        return new RequestResult { Success = true, Message = _isPaused ? "Paused" : "Resumed" };
                }
            }

            return await base.Run(args);
        }

        public override Task<RequestResult> OnClosing(params object[] args)
        {
            StopSequence();
            _cts?.Dispose();
            _pauseEvent.Dispose();
            return base.OnClosing(args);
        }
    }
}