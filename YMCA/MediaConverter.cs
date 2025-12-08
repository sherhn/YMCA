using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using YMCA.Resources;

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
        public void ConvertMedia(string mediaPath, string filename, List<EncryptionSchema> schemas, ProgressBar progressBar, Label label)
        {
            // Создаем временную папку в системной временной директории
            string tempDir = Path.Combine(Path.GetTempPath(), "YMCA_Frames_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            Characteristics characteristics = new Characteristics();
            MediaTools tools = new MediaTools();

            // Получаем все кадры
            tools.getFrames(mediaPath, tempDir);

            // Получаем инфу о файле
            tools.identifySignature(tempDir, ref characteristics);

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
            int[][] colors = new int[schema.Colors.Length][];

            for (int i = 0; i < schema.Colors.Length; i++)
            {
                Color color = tools.hexToColor(schema.Colors[i]);
                colors[i] = new int[] { color.R, color.G, color.B };
            }

            if (produce(tempDir, colors, schema.Schema, characteristics, outputPath, tools, progressBar, label) == 0)
            {
                MessageBox.Show($"Файл успешно создан: {outputPath}", "Успешно");
            }
        }

        private int produce(string tempDir, int[][] colors, int schema, Characteristics characteristics, string outputPath, MediaTools tools, ProgressBar progressBar, Label label)
        {
            try
            {
                // Данные
                StringBuilder bytesFlow = new StringBuilder();
                StringBuilder currentFlow = new StringBuilder();

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

                            int posX = characteristics.x;
                            int posY = characteristics.y;

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
                                posX = characteristics.x;
                            }
                        }
                        finally
                        {
                            bitmap.UnlockBits(bmpData);
                        }
                    }

                    // Обработка последнего кадра, если файл был дополнен
                    if (currentFrame == allFramesCount && characteristics.Supplemented)
                    {
                        int detLength = currentFlow.Length - 1;

                        if (detLength >= 0)
                        {
                            char lastChar = currentFlow[detLength];
                            int i = detLength;

                            while (i >= 0 && currentFlow[i] == lastChar)
                                i--;

                            // i теперь указывает на последний "полезный" символ
                            currentFlow.Length = i + 1;
                        }
                    }

                    bytesFlow.Append(currentFlow);
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

                // Конвертация битовой строки в байты и создание файла
                string bitString = bytesFlow.ToString();
                int byteCount = bitString.Length / 8;
                byte[] fileBytes = new byte[byteCount];

                for (int i = 0; i < byteCount; i++)
                {
                    string byteString = bitString.Substring(i * 8, 8);
                    fileBytes[i] = Convert.ToByte(byteString, 2);
                }

                // Добавляем расширение к пути
                outputPath = outputPath + "." + characteristics.Extension;

                // Сохраняем файл
                File.WriteAllBytes(outputPath, fileBytes);

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
        }
    }
}