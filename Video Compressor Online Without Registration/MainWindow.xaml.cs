using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using NReco.VideoInfo;

namespace Video_Compressor_Online_Without_Registration
{
    public partial class MainWindow
    {
        private readonly FFProbe _ffProbe = new FFProbe();

        public MainWindow()
        {
            InitializeComponent();
        }

        private double GetVideoDuration(string videoFilePath)
        {
            MediaInfo mediaInfo = _ffProbe.GetMediaInfo(videoFilePath);

            if (mediaInfo != null && mediaInfo.Duration != TimeSpan.Zero)
            {
                return mediaInfo.Duration.TotalSeconds;
            }
            return 0; // Если не удалось получить длительность
        }
        
        // Определяем, поддерживается ли выбранный кодек на текущей системе
        bool IsCodecSupported(string codec)
        {
            var ffMpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = $"-hide_banner -codecs",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            ffMpegProcess.Start();
            string output = ffMpegProcess.StandardOutput.ReadToEnd();
            ffMpegProcess.WaitForExit();

            return output.Contains(codec);
        }

        // Выбираем подходящее аппаратное ускорение для кодирования видео
        string GetHardwareAcceleration(string codec)
        {
            if (codec == "h264" || codec == "h265")
            {
                if (IsCodecSupported("nvenc"))
                {
                    return "-c:v h264_nvenc";
                }
                else if (IsCodecSupported("h264_qsv"))
                {
                    return "-c:v h264_qsv";
                }
                else if (IsCodecSupported("h264_amf"))
                {
                    return "-c:v h264_amf";
                }
            }
            return ""; // Возвращаем пустую строку, если подходящее аппаратное ускорение не найдено
        }

        public async Task CompressVideoAsync(string videoFilePath, string outputFilePath, long targetSizeMb)
        {
            try
            {
                // Получим информацию о видео
                var ffProbe = new FFProbe();
                MediaInfo mediaInfo = ffProbe.GetMediaInfo(videoFilePath);

                // Получим длительность видео
                double totalVideoDurationInSeconds = mediaInfo.Duration.TotalSeconds;

                // Пересчитаем целевой размер в байтах
                long targetSizeBytes = targetSizeMb * 1024 * 1024;

                // Определим целевой битрейт видео и аудио
                long videoBitrate = (long) ((targetSizeBytes * 0.9 * 8) / totalVideoDurationInSeconds); // 90% на видео
                long audioBitrate = (long) ((targetSizeBytes * 0.1 * 8) / totalVideoDurationInSeconds); // 10% на аудио

                // Выбираем подходящее аппаратное ускорение для выбранного кодека
                string hardwareAcceleration = GetHardwareAcceleration(mediaInfo.Streams[0].CodecName);

                // Выполним сжатие видео
                var ffMpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg.exe",
                        Arguments =
                            $"-y -i \"{videoFilePath}\" {hardwareAcceleration} -b:v {videoBitrate} -b:a {audioBitrate} -threads 4 \"{outputFilePath}\"",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                ffMpegProcess.ErrorDataReceived += (s, args) =>
                {
                    if (args.Data != null)
                    {
                        string line = args.Data;
                        if (line.Contains("time="))
                        {
                            Match match = Regex.Match(line, @"time=([0-9:.]+)");
                            if (match.Success)
                            {
                                string time = match.Groups[1].Value;
                                TimeSpan duration = TimeSpan.Parse(time);

                                if (totalVideoDurationInSeconds > 0 && !double.IsNaN(duration.TotalSeconds) &&
                                    !double.IsInfinity(duration.TotalSeconds))
                                {
                                    double progress = duration.TotalSeconds / totalVideoDurationInSeconds * 100.0;

                                    Dispatcher.Invoke(() => { convertationProgress.Value = progress; });
                                }
                            }
                        }
                    }
                };

                ffMpegProcess.Exited += (s, args) =>
                {
                    Dispatcher.Invoke(() => { convertationProgress.Value = 100; });
                };

                ffMpegProcess.Start();
                ffMpegProcess.BeginErrorReadLine();

                await Task.Run(() => ffMpegProcess.WaitForExit());

                MessageBox.Show("Видео было успешно сжато и сохранено вместе с оригиналом в том же месте.");

                ResetUI();
            }
            
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при сжатии видео: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void ResetUI()
        {
            OriginalFilePath.Text = "";
            CompressedFilePath.Text = "";
            convertationProgress.Value = 0;
            SizeOfFile.Text = "";
        }

        private async void Convert_OnClick(object sender, RoutedEventArgs e)
        {
            string videoFilePath = OriginalFilePath.Text;
            string outputFilePath = CompressedFilePath.Text;

            if (long.TryParse(SizeOfFile.Text, out long fileSize) && File.Exists(videoFilePath) && !string.IsNullOrEmpty(outputFilePath))
            {
                await CompressVideoAsync(videoFilePath, outputFilePath, fileSize);
            }
            else
            {
                MessageBox.Show("Не все параметры были указаны, или указаны верно", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OriginalFilePathButtonClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".mp4", // Default file extension
                Filter = "Video Files (*.mp4, *.mov, *.avi, *.wmv, *.mpeg, *.m4v)|*.mp4;*.mov;*.avi;*.wmv;*.mpeg;*.m4v"
            };

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                string filepath = dialog.FileName;
                string filename = dialog.SafeFileName;
                OriginalFilePath.Text = filepath;
                CompressedFilePath.Text = Path.Combine(Path.GetDirectoryName(filepath), Path.GetFileNameWithoutExtension(filepath) + "_COMPRESSED" + Path.GetExtension(filepath));
            }
        }

        private void CompressFilePathButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(OriginalFilePath.Text))
            {
                MessageBox.Show("Для начала укажите оригинал файла.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = Path.GetFileNameWithoutExtension(OriginalFilePath.Text) + "_COMPRESSED",
                DefaultExt = ".mp4", // Default file extension
                Filter = "Video Files (*.mp4, *.mov, *.avi, *.wmv, *.mpeg, *.m4v)|*.mp4;*.mov;*.avi;*.wmv;*.mpeg;*.m4v"
            };

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                CompressedFilePath.Text = dialog.FileName;
            }
        }
    }
}
