using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YMCA
{
    internal class FileConverter
    {
        public async Task ConvertFileAsync(byte[] bytes, string filename, EncryptionSchema schema, ProgressBar progressBar, Label label)
        {
            await Task.Run(() => ConvertFile(bytes, filename, schema, progressBar, label));
        }

        public void ConvertFile(byte[] bytes, string filename, EncryptionSchema schema, ProgressBar progressBar, Label label)
        {
            // Создаем временную папку
            string tempDir = Path.Combine(Path.GetTempPath(), "YMCA_Frames_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            string originalBitString = "";

#if DEBUG
            // Сохраняем оригинальные байты файла во временную папку
            string originalBytesPath = Path.Combine(tempDir, $"{Path.GetFileNameWithoutExtension(filename)}_original_bytes.txt");
            File.WriteAllLines(originalBytesPath, bytes.Select(b => b.ToString("X2"))); // в шестнадцатеричном виде
#endif

            string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"{filename}.mp4");

            FileTools tools = new FileTools();

            // Получаем двоичную строку из бинарного потока
            string byte_flow = tools.getBytes(bytes);

#if DEBUG
            // Сохраняем исходную двоичную строку
            originalBitString = byte_flow;

            // Сохраняем оригинальную двоичную строку во временную папку
            string originalBitsPath = Path.Combine(tempDir, "original_bits.txt");
            File.WriteAllText(originalBitsPath, originalBitString);
#endif

            // Прибавляем расширение файла и алгоритм к началу двоичной строки
            StringBuilder byteFlowBuilder = new StringBuilder();
            byteFlowBuilder.Append(schema.Signature); // 8 бит (только 0 и 1)
            byteFlowBuilder.Append(tools.getSignature(Path.GetExtension(filename))); // 80 бит
            byteFlowBuilder.Append(byte_flow);
            byte_flow = byteFlowBuilder.ToString();

            // Один раз вычисляем длину двоичной строки
            int byte_length = byte_flow.Length;

            // Считаем подход. разрешение
            int[] frame_scale = tools.calculateResolution((byte_length + 2) * schema.Schema * schema.Schema);

            // Кол-во кадров с учетом схемы масштабирования
            int bits_per_frame = frame_scale[2] / (schema.Schema * schema.Schema);
            int frame_count = (int)Math.Ceiling((double)(byte_length + 2) / bits_per_frame);

            // Механизм, который помогает дополнить последний кадр до конца
            string supplementedBitString = byte_flow;
            bool wasSupplemented = false;
            string supplementChar = "";

            if ((byte_length + 2) % bits_per_frame == 0)
            {
                // Метка, что последний кадр заполнен до конца
                byte_flow = "10" + byte_flow;
            }
            else
            {
                // Метка, что последний кадр был дополнен
                byte_flow = "01" + byte_flow;
                wasSupplemented = true;

                // Последний бит
                char lastBit = byte_flow[byte_length - 1];

                // Определяем последний бит, чтобы понять каким битом закрашивать остаток
                char free_pixel = (lastBit == '0') ? '1' : '0';
                supplementChar = free_pixel.ToString();

                // Дополняем двоичную строку до конца (до полного количества битов во всех кадрах)
                int total_bits_needed = frame_count * bits_per_frame;
                byte_flow = byte_flow.PadRight(total_bits_needed, free_pixel);
            }

#if DEBUG
            // Сохраняем информацию о двоичных строках
            string bitInfoPath = Path.Combine(tempDir, "bit_strings_info.txt");
            using (StreamWriter writer = new StreamWriter(bitInfoPath))
            {
                writer.WriteLine($"Original bit string (без сигнатуры и меток): {originalBitString}");
                writer.WriteLine($"Signature: {schema.Signature}");
                writer.WriteLine($"File signature: {tools.getSignature(Path.GetExtension(filename))}");
                writer.WriteLine($"Combined (с сигнатурой): {supplementedBitString}");
                writer.WriteLine($"Final (с метками): {byte_flow}");
                writer.WriteLine($"Was supplemented: {wasSupplemented}");
                writer.WriteLine($"Supplement character: {supplementChar}");
                writer.WriteLine($"Bits per frame: {bits_per_frame}");
                writer.WriteLine($"Frame count: {frame_count}");
                writer.WriteLine($"Total bits: {byte_flow.Length}");
            }
#endif

            if (produce(tempDir, schema.Colors, schema.Schema, schema.Crf, frame_scale, frame_count, byte_flow, originalBitString, outputPath, tools, progressBar, label) == 0)
            {
#if DEBUG
            MessageBox.Show($"Файл успешно создан: {outputPath}\nВременная папка сохранена: {tempDir}\nОригинальные байты сохранены в: {originalBytesPath}", "Успешно");
#else
            MessageBox.Show($"Файл успешно создан: {outputPath}");
#endif
            }
        }

        private int produce(string tempDir, string[] colors, int schema, int crf, int[] frame_scale, int frame_count, string byte_flow, string originalBitString, string outputPath, FileTools tools, ProgressBar progressBar, Label label)
        {
            try
            {
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

                    StringBuilder frameBitString = new StringBuilder();

                    // Заполнение кадров
                    for (int j = cur_pos; j < next_pos && j < byte_flow.Length; j++)
                    {
                        char bit = byte_flow[j];
                        frameBitString.Append(bit);

                        int colorIndex = bit - '0';

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

#if DEBUG
                    // Сохраняем двоичную строку для этого кадра (без сигнатуры и доп битов)
                    string frameBits = frameBitString.ToString();
                    string frameBitsPath = Path.Combine(tempDir, $"frame_{i:0000}_bits.txt");

                    // Удаляем первые 2 бита (метки) для первого кадра
                    if (i == 0 && frameBits.Length >= 2)
                    {
                        frameBits = frameBits.Substring(2); // Убираем метки "10" или "01"
                    }

                    File.WriteAllText(frameBitsPath, frameBits);
#endif

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

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка обработки файла");
                return -1;
            }
            finally
            {
                // Удаляем временную папку, если не в режиме отладки
#if !DEBUG
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Не удалось удалить временную папку: {ex.Message}", "Предупреждение");
                    }
                }
#endif
            }
        }
    }
}