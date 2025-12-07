using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace YMCA.Resources
{
    internal class MediaTools
    {
        public void getFrames(string mediapath, string framespath)
        {
            try
            {
                // Проверка существования видеофайла
                if (!File.Exists(mediapath))
                {
                    MessageBox.Show($"Видеофайл не найден: {mediapath}", "Ошибка");
                    return;
                }

                // Создаем папку для кадров, если её нет
                if (!Directory.Exists(framespath))
                {
                    Directory.CreateDirectory(framespath);
                }

                // Формируем аргументы для FFmpeg
                string ffmpegArgs = $"-i \"{mediapath}\" -vf fps=60 \"{Path.Combine(framespath, "frame_%04d.png")}\" -y";

                // Настройки процесса
                var processInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = ffmpegArgs,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                // Запускаем FFmpeg
                using (var process = Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        // Ждем завершения
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            MessageBox.Show($"Кадры успешно извлечены в: {framespath}", "Успех");
                        }
                        else
                        {
                            string error = process.StandardError.ReadToEnd();
                            MessageBox.Show($"Ошибка FFmpeg: {error}", "Ошибка");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка обработки видео");
            }
        }
    }
}