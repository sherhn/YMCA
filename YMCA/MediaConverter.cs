using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YMCA
{
    internal class MediaConverter
    {
        public string Convert(string binary, string filename, EncryptionSchema algorithm)
        {
            // Конечный пункт сохранения файла
            string resultPath = "";

            string tempDir = "temp_frames";
            Directory.CreateDirectory(tempDir);

            return resultPath;
        }

        private int produce(string filename)
        {
            return 0;
        }
    }
}
