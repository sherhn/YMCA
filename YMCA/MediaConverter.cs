using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace YMCA
{
    public class Characteristics
    {
        public bool Supplemented { get; set; } = false;
        public string Extension { get; set; }
        public string Signature { get; set; }
        public int x { get; set; }
        public int y { get; set; }
    }

    internal class MediaConverter
    {
        public void ConvertMedia(string mediaPath, string filename, List<EncryptionSchema> schemas, ProgressBar progressBar, Label label, bool debug)
        {
            // Создаем временную папку в системной временной директории
            string tempDir = Path.Combine(Path.GetTempPath(), "YMCA_Frames_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            Characteristics characteristics = new Characteristics();
            MediaTools tools = new MediaTools();

            // Получаем все кадры
            tools.getFrames(mediaPath, tempDir);

            // Получаем инфу о файле
            tools.identifySignature(tempDir, ref characteristics, debug);

            // Создаем папку для результата
            string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"{Path.GetFileNameWithoutExtension(filename)}");

            // Получаем алгоритм по сигнатуре
            EncryptionSchema schema = schemas.FirstOrDefault(s => s.Signature == characteristics.Signature);

            if (schema == null)
            {
                MessageBox.Show("Не найден подходящий алгоритм для сигнатуры: " + characteristics.Signature, "Ошибка");
                return;
            }

            // Создаем массив массивов с цветами RGB
            int[][] colors = new int[2][];

            for (int i = 0; i < 2; i++)
            {
                Color color = tools.hexToColor(schema.Colors[i]);
                colors[i] = new int[] { color.R, color.G, color.B };
            }

            if (produce(tempDir, colors, schema.Schema, characteristics, outputPath, tools, progressBar, label, debug) == 0)
            {
                if (debug)
                {
                    // ВРЕМЕННАЯ ПАПКА НЕ УДАЛЯЕТСЯ
                    MessageBox.Show($"Файл успешно создан: {outputPath + characteristics.Extension}\nВременная папка сохранена: {tempDir}", "Успешно");
                } else
                {
                    MessageBox.Show($"Файл успешно создан: {outputPath + characteristics.Extension}");
                }
            }
        }

        private int produce(string tempDir, int[][] colors, int schema, Characteristics characteristics, string outputPath, MediaTools tools, ProgressBar progressBar, Label label, bool debug)
        {
            try
            {
                // Данные
                StringBuilder binaryFlow = new StringBuilder();
                StringBuilder currentFlow = new StringBuilder();
                List<string> frameBitStrings = new List<string>();

                // Получаем все кадры
                string[] allFrames = Directory.GetFiles(tempDir);
                int allFramesCount = allFrames.Length;
                int currentFrame = 0;

                // Обнуляем прогрессбар
                progressBar.Invoke((MethodInvoker)delegate
                {
                    progressBar.Minimum = 0;
                    progressBar.Maximum = allFramesCount;
                    progressBar.Value = 0;
                    progressBar.Step = 1;
                });

                int posX = characteristics.x;
                int posY = characteristics.y;

                foreach (string frame in allFrames)
                {
                    currentFrame++;

                    using (Bitmap bitmap = new Bitmap(frame))
                    {
                        // Используем LockBits для быстрого доступа к пикселям
                        BitmapData bmpData = bitmap.LockBits(
                            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            ImageLockMode.ReadOnly,
                            PixelFormat.Format32bppArgb);

                        try
                        {
                            int stride = bmpData.Stride;
                            int bytesPerPixel = 4; // 32bpp = 4 байта на пиксель
                            byte[] pixelData = new byte[stride * bitmap.Height];

                            // Копируем данные в массив
                            Marshal.Copy(bmpData.Scan0, pixelData, 0, pixelData.Length);

                            int width = bitmap.Width;
                            int height = bitmap.Height;
                            int step = schema;

                            // Функция для получения цвета пикселя
                            Color GetPixelFast(int xPos, int yPos)
                            {
                                int index = yPos * stride + xPos * bytesPerPixel;
                                byte b = pixelData[index];
                                byte g = pixelData[index + 1];
                                byte r = pixelData[index + 2];
                                return Color.FromArgb(r, g, b);
                            }

                            while (posY < height)
                            {
                                while (posX < width)
                                {
                                    Color pixel = GetPixelFast(posX, posY);
                                    currentFlow.Append(tools.DetermineColorIndex(colors, pixel).ToString());
                                    posX += step;
                                }

                                posY += step;
                                posX = Math.Max(0, step / 2 - 1);

                                //MessageBox.Show($"{characteristics.x} | {characteristics.y}", "Информация о файле");
                            }
                        }
                        finally
                        {
                            bitmap.UnlockBits(bmpData);
                        }
                    }

                    posX = Math.Max(0, schema / 2 - 1);
                    posY = Math.Max(0, schema / 2 - 1);

                    string currentBitString = currentFlow.ToString();

                    if (debug)
                    {
                        string bitStringsPath2 = Path.Combine(tempDir, "frame_bit_strings12.txt");
                        File.WriteAllLines(bitStringsPath2, currentBitString.Select((bits, index) =>
                            $"Frame {index + 1:0000}: {bits}"));
                    }

                    frameBitStrings.Add(currentBitString);

                    // Обработка последнего кадра, если файл был дополнен
                    if (currentFrame == allFramesCount && characteristics.Supplemented)
                    {
                        int detLength = currentBitString.Length - 1;

                        if (detLength >= 0)
                        {
                            char lastChar = currentBitString[detLength];
                            int i = detLength;

                            while (i >= 0 && currentBitString[i] == lastChar)
                                i--;

                            // i указывает на последний "полезный" символ
                            currentBitString = currentBitString.Substring(0, i + 1);
                            frameBitStrings[frameBitStrings.Count - 1] = currentBitString; // Обновляем последнюю строку
                        }
                    }

                    binaryFlow.Append(currentBitString);
                    currentFlow.Clear();

                    // Обновляем прогресс
                    float progressPercentage = (float)currentFrame / allFramesCount * 100;

                    label.Invoke((MethodInvoker)delegate
                    {
                        label.Text = $"{progressPercentage:F1}%";
                        label.Refresh();
                    });

                    progressBar.Invoke((MethodInvoker)delegate
                    {
                        progressBar.Value = currentFrame;
                        progressBar.Refresh();
                    });
                }

                if (debug)
                {
                    // Сохраняем битовые строки каждого кадра в файл
                    string bitStringsPath = Path.Combine(tempDir, "frame_bit_strings.txt");
                    File.WriteAllLines(bitStringsPath, frameBitStrings.Select((bits, index) =>
                        $"Frame {index + 1:0000}: {bits}"));
                }

                // Конвертация битовой строки в байты
                string bitString = binaryFlow.ToString();
                int byteCount = bitString.Length / 8;
                byte[] fileBytes = new byte[byteCount];

                for (int i = 0; i < byteCount; i++)
                {
                    string byteString = bitString.Substring(i * 8, 8);
                    fileBytes[i] = Convert.ToByte(byteString, 2);
                }

                if (debug)
                {
                    // Сохраняем восстановленные байты во временную папку (в шестнадцатеричном виде)
                    string recoveredBytesPath = Path.Combine(tempDir, "recovered_bytes_hex.txt");
                    File.WriteAllLines(recoveredBytesPath, fileBytes.Select(b => b.ToString("X2")));

                    // Сохраняем восстановленные байты во временную папку (в двоичном виде)
                    string recoveredBinaryPath = Path.Combine(tempDir, "recovered_bytes_binary.txt");
                    File.WriteAllLines(recoveredBinaryPath, fileBytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
                }

                // Сохраняем файл
                string finalOutputPath = outputPath + characteristics.Extension;
                File.WriteAllBytes(finalOutputPath, fileBytes);

                if (debug)
                {
                    // Сохраняем информацию о восстановленном файле
                    string recoveryInfoPath = Path.Combine(tempDir, "recovery_info.txt");
                    using (StreamWriter writer = new StreamWriter(recoveryInfoPath))
                    {
                        writer.WriteLine($"Recovered file: {finalOutputPath}");
                        writer.WriteLine($"File size: {fileBytes.Length} bytes");
                        writer.WriteLine($"File extension: {characteristics.Extension}");
                        writer.WriteLine($"Binary string length: {bitString.Length}");
                        writer.WriteLine($"Was supplemented: {characteristics.Supplemented}");
                    }

                    // Сохраняем общую битовую строку
                    string allBitsPath = Path.Combine(tempDir, "all_bits.txt");
                    File.WriteAllText(allBitsPath, $"Binary: {bitString}");
                }

                // Завершаем прогресс
                label.Invoke((MethodInvoker)delegate
                {
                    label.Text = "100.0%";
                    label.Refresh();
                });

                progressBar.Invoke((MethodInvoker)delegate
                {
                    progressBar.Value = progressBar.Maximum;
                });

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
                if (!debug && Directory.Exists(tempDir))
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
            }
        }
    }
}