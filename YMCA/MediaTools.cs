using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace YMCA
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

        public void identifySignature(string tempDir, ref Characteristics characteristics)
        {
            // Базовый массив черного и белого
            int[][] baseColors = new int[][]
            {
                new int[] {0, 0, 0},      // Черный
                new int[] {255, 255, 255} // Белый
            };

            string framePath = Path.Combine(tempDir, "frame_0001.png");

            using (Bitmap bitmap = new Bitmap(framePath))
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

                    // Позиция
                    int x = 0;
                    int y = 0;
                    int step = 0;

                    // Функция для получения цвета пикселя
                    Color GetPixelFast(int xPos, int yPos)
                    {
                        int index = yPos * stride + xPos * bytesPerPixel;
                        byte b = pixelData[index];
                        byte g = pixelData[index + 1];
                        byte r = pixelData[index + 2];
                        return Color.FromArgb(r, g, b);
                    }

                    // Определяем был ли файл дополнен
                    int firstPixel = DetermineColorIndex(baseColors, GetPixelFast(x, 0));

                    if (firstPixel == 0)
                    {
                        characteristics.Supplemented = true;
                    }

                    // Определяем шаг (кол-во пикселей на бит)
                    int secondPixel = firstPixel;

                    while (firstPixel == secondPixel)
                    {
                        x++;
                        step++;
                        secondPixel = DetermineColorIndex(baseColors, GetPixelFast(x, 0));
                    }

                    // Берем середину по X для более устойчивой защиты от помех сжатия
                    x = Math.Max(0, step / 2 - 1);

                    // Пропускаем идентификатор дополнения
                    x += step;

                    // Берем середину по Y для более устойчивой защиты от помех сжатия
                    y = Math.Max(0, step / 2 - 1);

                    // Получаем id алгоритма
                    int pos = x;

                    StringBuilder signatureBuilder = new StringBuilder();

                    for (int i = 0; i < 8; i++)
                    {
                        x += step;

                        if (x >= bitmap.Width)
                        {
                            y += step;
                            x = Math.Max(0, step / 2 - 1);
                        }

                        signatureBuilder.Append(DetermineColorIndex(baseColors, GetPixelFast(x, y)).ToString());
                    }

                    characteristics.Signature = signatureBuilder.ToString();

                    // Читаем расширение файла
                    StringBuilder bitExtension = new StringBuilder();
                    List<char> chars = new List<char>();

                    for (int j = 0; j < 80; j++)
                    {
                        x += step;

                        if (x >= bitmap.Width)
                        {
                            y += step;
                            x = Math.Max(0, step / 2 - 1);
                        }

                        bitExtension.Append(DetermineColorIndex(baseColors, GetPixelFast(x, y)).ToString());

                        if (bitExtension.Length == 8)
                        {
                            int charCode = Convert.ToInt32(bitExtension.ToString(), 2);
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
                    characteristics.Extension = extension.TrimEnd('\0');

                    x += step;

                    if (x >= bitmap.Width)
                    {
                        y += step;
                        x = Math.Max(0, step / 2 - 1);
                    }

                    characteristics.x = x;
                    characteristics.y = y;
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }
            }

#if DEBUG
             MessageBox.Show($"{characteristics.Supplemented} | {characteristics.Signature} | {characteristics.x} | {characteristics.y} | {characteristics.Extension}", "Информация о файле");
#endif
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