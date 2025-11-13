using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YMCA
{
    internal class FileTools
    {
        public string getBytes(byte[] file_bytes, int system)
        {
            // Ковертируем бинарный массив в битовую строку
            return string.Concat(file_bytes.Select(b => Convert.ToString(b, system)));
        }

        public int[] calculateResolution(int fileLength)
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
            var minValue = int.MaxValue;

            // Определяем лучшее разрешения, из принципа оставления наименьшего кол-ва пустых пикселей
            foreach (var res in resolutions)
            {
                var value = fileLength % res.Size * res.Size;
                if (value < minValue)
                {
                    minValue = value;
                    bestResolution = res;
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
            List<string> binaryParts = new List<string>();

            foreach (char c in tenChars)
            {
                // Преобразуем символ в число, затем в двоичную строку
                int charCode = (int)c;
                string binary = Convert.ToString(charCode, 2);

                // Дополняем нулями слева до 8 бит
                string eightBits = binary.PadLeft(8, '0');

                binaryParts.Add(eightBits);
            }

            // Объединяем все биты в одну строку
            string result = string.Join("", binaryParts);
            return result;
        }
    }
}
