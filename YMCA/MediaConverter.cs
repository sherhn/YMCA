using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                string bytesFlow = "";
                string currenFlow = "";

                // Получаем все кадры
                string[] allFrames = Directory.GetFiles(tempDir);

                // Переменные для обнаружения последнего фрейма
                int allFramesCount = allFrames.Length;
                int currentFrame = 1;

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
                    Bitmap bitmap = new Bitmap(frame);
                    MessageBox.Show($"Ошибка:asa", "Ошибка обработки файла");
                    int width = bitmap.Width;
                    int height = bitmap.Height;

                    int step = schema;

                    int posX = characteristics.x;
                    int posY = characteristics.y;

                    while (posY < height)
                    {
                        while (posX < width)
                        {
                            Color pixel = bitmap.GetPixel(posX, posY);
                            currenFlow += tools.DetermineColorIndex(colors, pixel).ToString();

                            posX += step;
                        }

                        posY += step;
                        posX = characteristics.x;
                    }

                    currentFrame++;

                    if (currentFrame == allFramesCount && characteristics.Supplemented)
                    {
                        int detLenght = currenFlow.Length - 1;

                        char lastChar = currenFlow[detLenght];

                        int i = detLenght;
                        while (i >= 0 && currenFlow[i] == lastChar)
                            i--;

                        // i теперь указывает на последний "полезный" символ
                        currenFlow = currenFlow.Substring(0, i + 1);
                    }

                    bytesFlow += currenFlow;
                    currenFlow = "";

                    characteristics.x = Math.Max(0, schema / 2 - 1);
                    characteristics.y = Math.Max(0, schema / 2 - 1);

                    // Обновляем лейбл процента
                    label.Invoke((MethodInvoker)delegate
                    {
                        label.Text = $"{((float)currentFrame / allFramesCount * 100):F1}%";
                        label.Refresh();
                    });

                    // Обновляем прогрессбар
                    progressBar.Invoke((MethodInvoker)delegate
                    {
                        progressBar.Value = currentFrame + 1;
                        progressBar.Refresh();
                    });
                }

                // ТУТ ПРЕОБРАЗОВАНИЕ СТРОКИ БИТОВ В БАЙТЫ И СОЗДАНИЕ ФАЙЛА

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
