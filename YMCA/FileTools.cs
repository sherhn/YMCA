using System;
using System.Drawing;
using System.Linq;
using System.Text;

namespace YMCA
{
    internal class FileTools
    {
        public string getBytes(byte[] file_bytes)
        {
            // Конвертируем бинарный массив в двоичную строку
            return string.Concat(file_bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
        }

        public int[] calculateResolution(int totalBits)
        {
            // Все разрешения
            var resolutions = new[]
                    {
                new { Width = 256, Height = 144, Size = 36864 },
                new { Width = 426, Height = 240, Size = 102240 },
                new { Width = 640, Height = 360, Size = 230400 },
                new { Width = 1280, Height = 720, Size = 921600 },
                new { Width = 1920, Height = 1080, Size = 2073600 },
                new { Width = 2560, Height = 1440, Size = 3686400 },
                new { Width = 3840, Height = 2160, Size = 8294400 }
            };

            var bestResolution = resolutions[0];
            int minWastePixels = int.MaxValue;

            foreach (var res in resolutions)
            {
                // Сколько пикселей нужно для всех битов
                int pixelsNeeded = (int)Math.Ceiling((double)totalBits / 1);

                // Если всё помещается в один кадр
                if (pixelsNeeded <= res.Size)
                {
                    int wastePixels = res.Size - pixelsNeeded;
                    if (wastePixels < minWastePixels)
                    {
                        minWastePixels = wastePixels;
                        bestResolution = res;
                    }
                }
                // Если нужно несколько кадров
                else
                {
                    // Количество полных кадров
                    int fullFrames = pixelsNeeded / res.Size;
                    // Пиксели в последнем кадре
                    int lastFramePixels = pixelsNeeded % res.Size;
                    // Пустые пиксели в последнем кадре (если есть)
                    int wastePixels = lastFramePixels == 0 ? 0 : res.Size - lastFramePixels;

                    if (wastePixels < minWastePixels)
                    {
                        minWastePixels = wastePixels;
                        bestResolution = res;
                    }
                }
            }

            return new[] { bestResolution.Width, bestResolution.Height, bestResolution.Size };
        }

        public string getSignature(string extension)
        {
            // Дополняем расширение до 10 символов нулевыми байтами
            string paddedExtension = extension.PadRight(10, '\0');

            // Берем только первые 10 символов (на случай если расширение длиннее)
            string tenChars = paddedExtension.Substring(0, 10);

            // Преобразуем каждый символ в двоичную строку
            StringBuilder binaryParts = new StringBuilder();

            foreach (char c in tenChars)
            {
                // Преобразуем символ в число, затем в двоичную строку
                int charCode = (int)c;
                string binary = Convert.ToString(charCode, 2);

                // Дополняем нулями слева до 8 бит
                string eightBits = binary.PadLeft(8, '0');

                binaryParts.Append(eightBits);
            }

            // Объединяем все биты в одну строку
            return binaryParts.ToString();
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