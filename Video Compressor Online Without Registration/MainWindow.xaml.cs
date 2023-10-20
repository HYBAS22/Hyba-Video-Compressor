using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using NReco.VideoInfo;

namespace Video_Compressor_Online_Without_Registration
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        double GetVideoDuration(string videoFilePath)
        {
            var ffProbe = new FFProbe();
            MediaInfo mediaInfo = ffProbe.GetMediaInfo(videoFilePath);

            if (mediaInfo != null && mediaInfo.Duration != TimeSpan.Zero)
            {
                return mediaInfo.Duration.TotalSeconds;
            }
            return 0; // Если не удалось получить длительность
        }

        public async void CompressVideo(string videoFilePath, string outputFilePath, double targetSizeMB)
        {
            // Получим информацию о видео
            var ffProbe = new FFProbe();
            MediaInfo mediaInfo = ffProbe.GetMediaInfo(videoFilePath);

            // Получим длительность видео
            double totalVideoDurationInSeconds = mediaInfo.Duration.TotalSeconds;

            // Пересчитаем целевой размер в байтах
            long targetSizeBytes = (long)(targetSizeMB * 1024 * 1024);

            // Определим целевой битрейт видео и аудио
            long videoBitrate = (long)((targetSizeBytes * 0.9 * 8) / totalVideoDurationInSeconds); // 90% на видео
            long audioBitrate = (long)((targetSizeBytes * 0.1 * 8) / totalVideoDurationInSeconds); // 10% на аудио

            // Выполним сжатие видео
            var ffMpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe", // Путь к исполняемому файлу FFmpeg
                    Arguments = $"-i \"{videoFilePath}\" -b:v {videoBitrate} -b:a {audioBitrate} \"{outputFilePath}\"",
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

                            if (totalVideoDurationInSeconds > 0 && !double.IsNaN(duration.TotalSeconds) && !double.IsInfinity(duration.TotalSeconds))
                            {
                                double progress = duration.TotalSeconds / totalVideoDurationInSeconds * 100.0;

                                Dispatcher.Invoke(() =>
                                {
                                    convertationProgress.Value = progress;
                                });
                            }
                        }
                    }
                }
            };

            ffMpegProcess.Exited += (s, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    convertationProgress.Value = 100;
                });
            };

            ffMpegProcess.Start();
            ffMpegProcess.BeginErrorReadLine();

            await Task.Run(() =>
            {
                ffMpegProcess.WaitForExit();
                MessageBox.Show("Видео было успешно сжато и сохранено вместе с оригиналом в том же месте");
            });

            OriginalFilePath.Text = "";
            CompressedFilePath.Text = "";
            convertationProgress.Value = 0;
        }

        private void Convert_OnClick(object sender, RoutedEventArgs e)
        {
            string videoFilePath = OriginalFilePath.Text;
            string outputFilePath = CompressedFilePath.Text;
            double targetSizeInKb;

            if (double.TryParse(SizeOfFile.Text, out double fileSize))
            {
                targetSizeInKb = fileSize;
            }
            else
            {
                throw new Exception("Не удалось преобразовать размер в числовое значение");
            }
            
            if (!string.IsNullOrEmpty(OriginalFilePath.Text)) 
                CompressVideo(videoFilePath, outputFilePath, targetSizeInKb);
            else
                MessageBox.Show("Укажите размер сжатия файла", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        private void OriginalFilePathButtonClick(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".mp4"; // Default file extension
            dialog.Filter = "Video Files (*.mp4, *.mov, *.avi, *.wmv, *.mpeg, *.m4v)|*.mp4;*.mov;*.avi;*.wmv;*.mpeg;*.m4v";

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filepath = dialog.FileName;
                string filename = dialog.SafeFileName;
                OriginalFilePath.Text = filepath;
                CompressedFilePath.Text = filepath.Replace(filename.Split('.')[0], filename.Split('.')[0] + "_COMPRESSED");
            }
        }
        
        private void CompressFilePathButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(OriginalFilePath.Text))
            {
                MessageBox.Show("Для начала укажите оригинал файла.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            else
            {
                // Configure save file dialog box
                var dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.FileName = OriginalFilePath.Text; // Default file name
                dialog.DefaultExt = ".mp4"; // Default file extension
                dialog.Filter = "Video Files (*.mp4, *.mov, *.avi, *.wmv, *.mpeg, *.m4v)|*.mp4;*.mov;*.avi;*.wmv;*.mpeg;*.m4v";

                // Show save file dialog box
                bool? result = dialog.ShowDialog();

                // Process save file dialog box results
                if (result == true)
                {
                    // Save document
                    string filename = dialog.FileName;
                }
            }
        }
    }
}