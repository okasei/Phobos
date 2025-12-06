using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Phobos.Manager.Arcusrix;
using Phobos.Shared.Class;

namespace Phobos.Components.Arcusrix.Sequencer
{
    /// <summary>
    /// Sequencer UserControl - i18n 字符串
    /// </summary>
    public static class SequencerLocalization
    {
        // Keys
        public const string Title = "title";
        public const string Subtitle = "subtitle";
        public const string Browse = "browse";
        public const string Start = "start";
        public const string Pause = "pause";
        public const string Resume = "resume";
        public const string Stop = "stop";
        public const string Ready = "ready";
        public const string Running = "running";
        public const string Paused = "paused";
        public const string Completed = "completed";
        public const string Cancelled = "cancelled";
        public const string Error = "error";
        public const string SelectFile = "select_file";
        public const string FileNotFound = "file_not_found";
        public const string StartingSequence = "starting_sequence";
        public const string TotalCommands = "total_commands";
        public const string Executing = "executing";
        public const string Output = "output";
        public const string SequenceCompleted = "sequence_completed";
        public const string SequenceCancelled = "sequence_cancelled";
        public const string Waited = "waited";
        public const string InvalidDelay = "invalid_delay";
        public const string ProcessError = "process_error";
        public const string FailedToStart = "failed_to_start";

        private static readonly Dictionary<string, Dictionary<string, string>> _strings = new()
        {
            [Title] = new() { { "en-US", "Sequencer" }, { "zh-CN", "序列执行器" }, { "zh-TW", "序列執行器" }, { "ja-JP", "シーケンサー" }, { "ko-KR", "시퀀서" } },
            [Subtitle] = new() { { "en-US", "Execute commands line by line" }, { "zh-CN", "逐行执行文件中的命令" }, { "zh-TW", "逐行執行檔案中的命令" }, { "ja-JP", "ファイルからコマンドを行ごとに実行" }, { "ko-KR", "파일에서 명령을 한 줄씩 실행" } },
            [Browse] = new() { { "en-US", "Browse" }, { "zh-CN", "浏览" }, { "zh-TW", "瀏覽" }, { "ja-JP", "参照" }, { "ko-KR", "찾아보기" } },
            [Start] = new() { { "en-US", "Start" }, { "zh-CN", "开始" }, { "zh-TW", "開始" }, { "ja-JP", "開始" }, { "ko-KR", "시작" } },
            [Pause] = new() { { "en-US", "Pause" }, { "zh-CN", "暂停" }, { "zh-TW", "暫停" }, { "ja-JP", "一時停止" }, { "ko-KR", "일시정지" } },
            [Resume] = new() { { "en-US", "Resume" }, { "zh-CN", "继续" }, { "zh-TW", "繼續" }, { "ja-JP", "再開" }, { "ko-KR", "재개" } },
            [Stop] = new() { { "en-US", "Stop" }, { "zh-CN", "停止" }, { "zh-TW", "停止" }, { "ja-JP", "停止" }, { "ko-KR", "중지" } },
            [Ready] = new() { { "en-US", "Ready" }, { "zh-CN", "就绪" }, { "zh-TW", "就緒" }, { "ja-JP", "準備完了" }, { "ko-KR", "준비" } },
            [Running] = new() { { "en-US", "Running" }, { "zh-CN", "运行中" }, { "zh-TW", "運行中" }, { "ja-JP", "実行中" }, { "ko-KR", "실행 중" } },
            [Paused] = new() { { "en-US", "Paused" }, { "zh-CN", "已暂停" }, { "zh-TW", "已暫停" }, { "ja-JP", "一時停止中" }, { "ko-KR", "일시정지됨" } },
            [Completed] = new() { { "en-US", "Completed" }, { "zh-CN", "已完成" }, { "zh-TW", "已完成" }, { "ja-JP", "完了" }, { "ko-KR", "완료" } },
            [Cancelled] = new() { { "en-US", "Cancelled" }, { "zh-CN", "已取消" }, { "zh-TW", "已取消" }, { "ja-JP", "キャンセル" }, { "ko-KR", "취소됨" } },
            [Error] = new() { { "en-US", "Error" }, { "zh-CN", "错误" }, { "zh-TW", "錯誤" }, { "ja-JP", "エラー" }, { "ko-KR", "오류" } },
            [SelectFile] = new() { { "en-US", "Please select a file" }, { "zh-CN", "请选择一个文件" }, { "zh-TW", "請選擇一個檔案" }, { "ja-JP", "ファイルを選択してください" }, { "ko-KR", "파일을 선택하세요" } },
            [FileNotFound] = new() { { "en-US", "File not found" }, { "zh-CN", "文件未找到" }, { "zh-TW", "檔案未找到" }, { "ja-JP", "ファイルが見つかりません" }, { "ko-KR", "파일을 찾을 수 없습니다" } },
            [StartingSequence] = new() { { "en-US", "Starting sequence: {0}" }, { "zh-CN", "开始执行序列: {0}" }, { "zh-TW", "開始執行序列: {0}" }, { "ja-JP", "シーケンス開始: {0}" }, { "ko-KR", "시퀀스 시작: {0}" } },
            [TotalCommands] = new() { { "en-US", "Total commands: {0}" }, { "zh-CN", "命令总数: {0}" }, { "zh-TW", "命令總數: {0}" }, { "ja-JP", "コマンド総数: {0}" }, { "ko-KR", "총 명령어: {0}" } },
            [Executing] = new() { { "en-US", "[{0}/{1}] Executing: {2}" }, { "zh-CN", "[{0}/{1}] 执行: {2}" }, { "zh-TW", "[{0}/{1}] 執行: {2}" }, { "ja-JP", "[{0}/{1}] 実行: {2}" }, { "ko-KR", "[{0}/{1}] 실행: {2}" } },
            [Output] = new() { { "en-US", "  Output: {0}" }, { "zh-CN", "  输出: {0}" }, { "zh-TW", "  輸出: {0}" }, { "ja-JP", "  出力: {0}" }, { "ko-KR", "  출력: {0}" } },
            [SequenceCompleted] = new() { { "en-US", "Sequence completed." }, { "zh-CN", "序列执行完成。" }, { "zh-TW", "序列執行完成。" }, { "ja-JP", "シーケンス完了。" }, { "ko-KR", "시퀀스 완료." } },
            [SequenceCancelled] = new() { { "en-US", "Sequence cancelled." }, { "zh-CN", "序列已取消。" }, { "zh-TW", "序列已取消。" }, { "ja-JP", "シーケンスがキャンセルされました。" }, { "ko-KR", "시퀀스가 취소되었습니다." } },
            [Waited] = new() { { "en-US", "Waited {0}ms" }, { "zh-CN", "等待了 {0}ms" }, { "zh-TW", "等待了 {0}ms" }, { "ja-JP", "{0}ms 待機しました" }, { "ko-KR", "{0}ms 대기함" } },
            [InvalidDelay] = new() { { "en-US", "Invalid delay value" }, { "zh-CN", "无效的延迟值" }, { "zh-TW", "無效的延遲值" }, { "ja-JP", "無効な遅延値" }, { "ko-KR", "잘못된 지연 값" } },
            [ProcessError] = new() { { "en-US", "Process error: {0}" }, { "zh-CN", "进程错误: {0}" }, { "zh-TW", "進程錯誤: {0}" }, { "ja-JP", "プロセスエラー: {0}" }, { "ko-KR", "프로세스 오류: {0}" } },
            [FailedToStart] = new() { { "en-US", "Failed to start process" }, { "zh-CN", "无法启动进程" }, { "zh-TW", "無法啟動進程" }, { "ja-JP", "プロセスを開始できませんでした" }, { "ko-KR", "프로세스를 시작할 수 없습니다" } },
        };

        public static string Get(string key)
        {
            var lang = LocalizationManager.Instance.CurrentLanguage;
            if (_strings.TryGetValue(key, out var dict))
            {
                if (dict.TryGetValue(lang, out var str)) return str;
                if (dict.TryGetValue("en-US", out var enStr)) return enStr;
            }
            return key;
        }

        public static string GetFormat(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }
    }

    /// <summary>
    /// PCOSequencer.xaml 的交互逻辑
    /// </summary>
    public partial class PCOSequencer : UserControl
    {
        private CancellationTokenSource? _cts;
        private bool _isPaused = false;
        private readonly ManualResetEventSlim _pauseEvent = new(true);

        public PCOSequencer()
        {
            InitializeComponent();
            Loaded += PCOSequencer_Loaded;
        }

        private void PCOSequencer_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLocalizedText();
        }

        /// <summary>
        /// 更新本地化文本
        /// </summary>
        private void UpdateLocalizedText()
        {
            TitleText.Text = SequencerLocalization.Get(SequencerLocalization.Title);
            SubtitleText.Text = SequencerLocalization.Get(SequencerLocalization.Subtitle);
            BrowseButtonText.Text = SequencerLocalization.Get(SequencerLocalization.Browse);
            StartButtonText.Text = SequencerLocalization.Get(SequencerLocalization.Start);
            PauseButtonText.Text = SequencerLocalization.Get(SequencerLocalization.Pause);
            StopButtonText.Text = SequencerLocalization.Get(SequencerLocalization.Stop);
            StatusText.Text = SequencerLocalization.Get(SequencerLocalization.Ready);
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Sequence files (*.seq;*.txt)|*.seq;*.txt|All files (*.*)|*.*",
                Title = SequencerLocalization.Get(SequencerLocalization.Browse)
            };

            if (dialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = dialog.FileName;
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(FilePathTextBox.Text))
            {
                SetStatus(SequencerLocalization.Get(SequencerLocalization.SelectFile));
                return;
            }

            if (!File.Exists(FilePathTextBox.Text))
            {
                SetStatus(SequencerLocalization.Get(SequencerLocalization.FileNotFound));
                return;
            }

            _cts = new CancellationTokenSource();
            _isPaused = false;
            _pauseEvent.Set();

            UpdateButtonStates(true);
            ClearOutput();

            try
            {
                await ExecuteSequenceFile(FilePathTextBox.Text, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                AppendOutput(SequencerLocalization.Get(SequencerLocalization.SequenceCancelled));
                SetStatus(SequencerLocalization.Get(SequencerLocalization.Cancelled));
            }
            catch (Exception ex)
            {
                AppendOutput($"{SequencerLocalization.Get(SequencerLocalization.Error)}: {ex.Message}");
                SetStatus(SequencerLocalization.Get(SequencerLocalization.Error));
            }
            finally
            {
                UpdateButtonStates(false);
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPaused)
            {
                _isPaused = false;
                _pauseEvent.Set();
                PauseButtonText.Text = SequencerLocalization.Get(SequencerLocalization.Pause);
                SetStatus(SequencerLocalization.Get(SequencerLocalization.Running));
            }
            else
            {
                _isPaused = true;
                _pauseEvent.Reset();
                PauseButtonText.Text = SequencerLocalization.Get(SequencerLocalization.Resume);
                SetStatus(SequencerLocalization.Get(SequencerLocalization.Paused));
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _pauseEvent.Set();
        }

        private async Task ExecuteSequenceFile(string filePath, CancellationToken token)
        {
            var lines = await File.ReadAllLinesAsync(filePath, token);
            var totalLines = lines.Length;
            var currentLine = 0;

            SetStatus(SequencerLocalization.Get(SequencerLocalization.Running));
            AppendOutput(SequencerLocalization.GetFormat(SequencerLocalization.StartingSequence, Path.GetFileName(filePath)));
            AppendOutput(SequencerLocalization.GetFormat(SequencerLocalization.TotalCommands, totalLines));
            AppendOutput(new string('-', 50));

            foreach (var line in lines)
            {
                token.ThrowIfCancellationRequested();
                _pauseEvent.Wait(token);

                currentLine++;
                var trimmedLine = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith("//"))
                {
                    UpdateProgress(currentLine, totalLines);
                    continue;
                }

                AppendOutput(SequencerLocalization.GetFormat(SequencerLocalization.Executing, currentLine, totalLines, trimmedLine));

                try
                {
                    var result = await ExecuteCommand(trimmedLine, token);
                    if (!string.IsNullOrEmpty(result))
                    {
                        AppendOutput(SequencerLocalization.GetFormat(SequencerLocalization.Output, result));
                    }
                }
                catch (Exception ex)
                {
                    AppendOutput($"  {SequencerLocalization.Get(SequencerLocalization.Error)}: {ex.Message}");
                }

                UpdateProgress(currentLine, totalLines);
            }

            AppendOutput(new string('-', 50));
            AppendOutput(SequencerLocalization.Get(SequencerLocalization.SequenceCompleted));
            SetStatus(SequencerLocalization.Get(SequencerLocalization.Completed));
        }

        private async Task<string> ExecuteCommand(string command, CancellationToken token)
        {
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
                        return SequencerLocalization.GetFormat(SequencerLocalization.Waited, ms);
                    }
                    return SequencerLocalization.Get(SequencerLocalization.InvalidDelay);

                case "echo":
                case "print":
                    return args;

                case "run":
                case "exec":
                    return await RunProcess(args, token);

                case "phobos":
                    return $"Phobos command: {args}";

                default:
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
                    return SequencerLocalization.Get(SequencerLocalization.FailedToStart);

                var output = await process.StandardOutput.ReadToEndAsync(token);
                var error = await process.StandardError.ReadToEndAsync(token);
                await process.WaitForExitAsync(token);

                return string.IsNullOrEmpty(error) ? output.Trim() : $"{output}\n{SequencerLocalization.Get(SequencerLocalization.Error)}: {error}".Trim();
            }
            catch (Exception ex)
            {
                return SequencerLocalization.GetFormat(SequencerLocalization.ProcessError, ex.Message);
            }
        }

        private void UpdateProgress(int current, int total)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = (double)current / total * 100;
            });
        }

        private void SetStatus(string status)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                StatusText.Text = status;
            });
        }

        private void AppendOutput(string text)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                OutputTextBox.AppendText(text + Environment.NewLine);
                OutputTextBox.ScrollToEnd();
            });
        }

        private void ClearOutput()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                OutputTextBox.Clear();
                ProgressBar.Value = 0;
            });
        }

        private void UpdateButtonStates(bool isRunning)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                StartButton.IsEnabled = !isRunning;
                PauseButton.IsEnabled = isRunning;
                StopButton.IsEnabled = isRunning;
                PauseButtonText.Text = SequencerLocalization.Get(SequencerLocalization.Pause);
            });
        }

        /// <summary>
        /// 设置文件路径（供外部调用）
        /// </summary>
        public void SetFilePath(string path)
        {
            FilePathTextBox.Text = path;
        }

        /// <summary>
        /// 开始执行（供外部调用）
        /// </summary>
        public void Start()
        {
            StartButton_Click(this, new RoutedEventArgs());
        }

        /// <summary>
        /// 停止执行（供外部调用）
        /// </summary>
        public void Stop()
        {
            StopButton_Click(this, new RoutedEventArgs());
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _pauseEvent.Dispose();
        }
    }
}
