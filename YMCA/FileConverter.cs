using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace YMCA
{
    internal class FileConverter
    {
        public void ConvertFile(byte[] bytes, string filename, EncryptionSchema schema, ProgressBar progressBar, Label label)
        {
            string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"{filename}.mp4");

            FileTools tools = new FileTools();

            // Получаем строку битов из бинарного потока
            string byte_flow = tools.getBytes(bytes, schema.Colors.Length);

            // Прибавляем расширение файла и алгоритм к началу строки битов
            StringBuilder byteFlowBuilder = new StringBuilder();
            byteFlowBuilder.Append(schema.Signature);
            byteFlowBuilder.Append(tools.getSignature(Path.GetExtension(filename)));
            byteFlowBuilder.Append(byte_flow);
            byte_flow = byteFlowBuilder.ToString();

            // Один раз вычисляем длину битовой строки
            int byte_length = byte_flow.Length;

            // Считаем подход. разрешение
            int[] frame_scale = tools.calculateResolution((byte_length + 2) * schema.Schema);

            // Кол-во кадров с учетом схемы масштабирования
            int bits_per_frame = frame_scale[2] / (schema.Schema * schema.Schema);
            int frame_count = (int)Math.Ceiling((double)(byte_length + 2) / bits_per_frame);

            // Механизм, который помогает дополнить последний кадр до конца
            if ((byte_length + 2) % bits_per_frame == 0)
            {
                // Метка, что последний кадр заполнен до конца
                byte_flow = "10" + byte_flow;
            }
            else
            {
                // Метка, что последний кадр был дополнен
                byte_flow = "01" + byte_flow;

                // Последний пиксель
                int n = (int)char.GetNumericValue(byte_flow[byte_length - 1]);

                // Определяем последний пиксель, чтобы понять каким пикселем закрашивать остаток
                int free_pixel_index = (n == 0) ? 1 : 0;

                // Дополняем битовую строку до конца (до полного количества битов во всех кадрах)
                int total_bits_needed = frame_count * bits_per_frame;
                byte_flow = byte_flow.PadRight(total_bits_needed, (char)('0' + free_pixel_index));
            }

            if (produce(schema.Colors, schema.Schema, schema.Crf, frame_scale, frame_count, byte_flow, outputPath, tools, progressBar, label) == 0)
            {
                MessageBox.Show($"Файл успешно создан: {outputPath}", "Успешно");
            }
        }

        private int produce(string[] colors, int schema, int crf, int[] frame_scale, int frame_count, string byte_flow, string outputPath, FileTools tools, ProgressBar progressBar, Label label)
        {
            try
            {
                // Создаем временную папку в системной временной директории
                string tempDir = Path.Combine(Path.GetTempPath(), "YMCA_Frames_" + Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                // Обнуляем прогрессбар
                progressBar.Invoke((MethodInvoker)delegate
                {
                    progressBar.Minimum = 0;
                    progressBar.Maximum = frame_count;
                    progressBar.Value = 0;
                    progressBar.Step = 1;
                });

                // Вычисление масштаба для нужной схемы
                int cur_pos = 0;
                int real_scale_step = frame_scale[2] / (schema * schema);
                int real_scale_x = frame_scale[0] / schema;
                int real_scale_y = frame_scale[1] / schema;

                // Создаем кадры
                for (int i = 0; i < frame_count; i++)
                {
                    Bitmap bitmap = new Bitmap(frame_scale[0], frame_scale[1]);

                    int next_pos = cur_pos + real_scale_step;
                    int global_x = 0;
                    int global_y = 0;

                    // Заполнение кадров
                    for (int j = cur_pos; j < next_pos && j < byte_flow.Length; j++)
                    {
                        int colorIndex = byte_flow[j] - '0';
                        if (colorIndex >= 0 && colorIndex < colors.Length)
                        {
                            Color pixelColor = tools.hexToColor(colors[colorIndex]);

                            for (int x = 0; x < schema; x++)
                            {
                                for (int y = 0; y < schema; y++)
                                {
                                    int pixelX = global_x + x;
                                    int pixelY = global_y + y;

                                    if (pixelX < frame_scale[0] && pixelY < frame_scale[1])
                                    {
                                        bitmap.SetPixel(pixelX, pixelY, pixelColor);
                                    }
                                }
                            }
                        }

                        global_x += schema;

                        if (global_x >= frame_scale[0])
                        {
                            global_x = 0;
                            global_y += schema;
                        }
                    }

                    // Сохраняем фрейм
                    string outpuFrametPath = Path.Combine(tempDir, $"frame_{i:0000}.png");
                    bitmap.Save(outpuFrametPath, ImageFormat.Png);
                    bitmap.Dispose();

                    // Обновляем лейбл процента
                    label.Invoke((MethodInvoker)delegate
                    {
                        label.Text = $"{((float)i / frame_count * 100):F1}%";
                        label.Refresh();
                    });

                    // Обновляем прогрессбар
                    progressBar.Invoke((MethodInvoker)delegate
                    {
                        progressBar.Value = i + 1;
                        progressBar.Refresh();
                    });

                    cur_pos = next_pos;
                }

                // Завершаем лейбл процента
                label.Invoke((MethodInvoker)delegate
                {
                    label.Text = $"100.0%";
                    label.Refresh();
                });

                // Завершаем прогресс-бар
                progressBar.Invoke((MethodInvoker)delegate
                {
                    progressBar.Value = progressBar.Maximum;
                });

                // Собираем видео из кадров с помощью FFmpeg
                string ffmpegPath = "ffmpeg.exe";

                string ffmpegArgs = $"-framerate 60 -i \"{Path.Combine(tempDir, "frame_%04d.png")}\" -c:v libx264 -crf {crf.ToString()} -preset ultrafast -pix_fmt yuv420p -y \"{outputPath}\"";

                try
                {
                    var processInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = ffmpegArgs,
                        CreateNoWindow = true,      // Не показывать окно консоли
                        UseShellExecute = false    // Не использовать оболочку системы
                    };

                    using (var process = System.Diagnostics.Process.Start(processInfo))
                    {
                        process?.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка FFmpeg: {ex.Message}", "Ошибка создания видео");
                    return -1;
                }

                // Удаляем временную папку
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch { }

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка обработки файла");
                return -1;
            }
        }
    }
}