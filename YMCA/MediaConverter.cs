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
    }

    internal class MediaConverter
    {
        public string ConvertMedia(string mediaPath)
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




            return "resultPath";
        }

        private int produce(string filename)
        {
            return 0;
        }
    }
}
