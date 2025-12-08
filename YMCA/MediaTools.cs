using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace YMCA.Resources
{
    internal class MediaTools
    {
        public void getFrames(string mediapath, string framespath)
        {
            string ffmpegArgs = $"-i \"{mediapath}\" -vf fps=60 \"{Path.Combine(framespath, "frame_%04d.png")}\" -y";

            // Настройки процесса
            var processInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = ffmpegArgs,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (var process = Process.Start(processInfo))
            {
                process?.WaitForExit();
            }
        }

        public int DetermineColorIndex(int[][] colors, System.Drawing.Color color)
        {
            int targetRed = color.R;
            int targetGreen = color.G;
            int targetBlue = color.B;

            int minDistance = int.MaxValue;
            int minIndex = 0;

            for (int i = 0; i < colors.Length; i++)
            {
                int[] baseColor = colors[i];
                if (baseColor == null || baseColor.Length < 3)
                {
                    continue;
                }

                int red = baseColor[0];
                int green = baseColor[1];
                int blue = baseColor[2];

                // Вычисляем расстояния
                int distance =
                    (red - targetRed) * (red - targetRed) +
                    (green - targetGreen) * (green - targetGreen) +
                    (blue - targetBlue) * (blue - targetBlue);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    minIndex = i;
                }
            }

            return minIndex;
        }

        public void identifySignature(string mediapath, ref Characteristics characteristics)
        {
            // Базовый массив черного и белого
            int[][] baseColors = new int[][]
            {
                new int[] {0, 0, 0},      // Черный
                new int[] {255, 255, 255} // Белый
            };

            Bitmap bitmap = new Bitmap(Path.Combine(mediapath, "frame_0001.png"));

            // Позиция
            int x = 0;
            int y = 0;
            int step = 0;

            // Определяем был ли файл дополнен
            int firstPixel = DetermineColorIndex(baseColors, bitmap.GetPixel(x, 0));

            if (firstPixel == 0)
            {
                characteristics.Supplemented = true;
            }

            // Определяем шаг (кол-во пикселей на байт)
            int secondPixel = firstPixel;

            while (firstPixel == secondPixel)
            {
                x++;
                step++;
                secondPixel = DetermineColorIndex(baseColors, bitmap.GetPixel(x, 0));
            }

            // Берем середину по X для более устойчивой защмиы от помех сжатия
            x = Math.Max(0, step / 2 - 1);

            // Пропускаем идентификатор дополнения
            x += step;

            // Берем середину по Y для более устойчивой защмиы от помех сжатия
            y = Math.Max(0, step / 2 - 1);

            // Получаем id алгоритма
            int pos = x;

            StringBuilder signatureBuilder = new StringBuilder();

            for (int i = 0; i < 8; i++)
            {
                x += step;

                if (x > bitmap.Width)
                {
                    y += step;
                    x = Math.Max(0, step / 2 - 1);
                }

                signatureBuilder.Append(DetermineColorIndex(baseColors, bitmap.GetPixel(x, y)).ToString());
            }

            characteristics.Signature = signatureBuilder.ToString();

            // Читаем расширение файла
            StringBuilder bitExtension = new StringBuilder();
            List<char> chars = new List<char>();

            for (int j = 0; j < 80; j++)
            {
                x += step;

                if (x > bitmap.Width)
                {
                    y += step;
                    x = Math.Max(0, step / 2 - 1);
                }

                bitExtension.Append(DetermineColorIndex(baseColors, bitmap.GetPixel(x, y)).ToString());

                if (bitExtension.Length == 8)
                {
                    int charCode = Convert.ToInt32(bitExtension.ToString(), 2);

                    // Конвертируем число в символ
                    char c = (char)charCode;

                    chars.Add(c);

                    bitExtension.Clear();
                }
            }

            // Объединяем символы в строку
            StringBuilder extensionBuilder = new StringBuilder();
            foreach (char c in chars)
            {
                extensionBuilder.Append(c);
            }

            string extension = extensionBuilder.ToString();

            // Убираем нулевые символы в конце
            characteristics.Extension = extension.TrimEnd('\0');


            MessageBox.Show($"{characteristics.Supplemented} | {characteristics.Signature} | {x} | {y} | {step} | {characteristics.Extension}", "Ошибка загрузки файла");
        }

        public Color hexToColor(string hex)
        {
            // Убираем решетку
            hex = hex.TrimStart('#');

            // Разбираем компоненты
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);

            return Color.FromArgb(r, g, b);
        }
    }
}