using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using YMCA.Resources;

namespace YMCA
{
    internal class MediaConverter
    {
        public string ConvertMedia(string mediaPath)
        {
            // Создаем временную папку в системной временной директории
            string tempDir = Path.Combine(Path.GetTempPath(), "YMCA_Frames_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            MediaTools tools = new MediaTools();

            tools.getFrames(mediaPath, tempDir);
            MessageBox.Show("Файл не выбран!", "Ошибка загрузки файла");

            return "resultPath";
        }

        private int produce(string filename)
        {
            return 0;
        }
    }
}
