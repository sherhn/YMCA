using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.AxHost;

namespace YMCA
{
    internal class FileConverter
    {
        public string ConvertMedia(byte[] bytes, string filename, EncryptionSchema schema)
        {
            // Конечный пункт сохранения файла
            string resultPath = "";

            FileTools tools = new FileTools();
            
            // Получаем строку битов из бинарного потока
            string byte_flow = tools.getBytes(bytes, schema.Colors.Length);

            // Прибавляем расширение файла и алгоритм к началу строки битов
            byte_flow = tools.getSignature(Path.GetExtension(filename)) + schema.Signature + byte_flow;

            // Один раз вычисляем длину битовой строки
            int byte_length = byte_flow.Length;

            // Считаем подход. разрешение
            int[] frame_scale = tools.calculateResolution(byte_length + 2);

            // Куросоры
            int cur_position = 0;
            int step = frame_scale[2];

            // Кол-во кадров
            int frame_count = (int)Math.Ceiling((double)(byte_length + 2) / frame_scale[2]);

            // Механизм, который помогает дополнить последнйи кадр до конца или создать последний отдельный кадр, чтобы при декодировании можно было найти конец записи
            if ((byte_length + 2) % frame_scale[2] == 0 )
            {
                // Метка, что последний кадр заполнен до конца
                byte_flow = "10" + byte_flow;
            }
            else
            {
                // Метка, что последний кадр был дополнен
                byte_flow = "01" + byte_flow;

                // Последний пиксель
                int n = string.IsNullOrEmpty(byte_flow) ? 0 : byte_flow[byte_length + 2] - '0';

                // Определяем последний пиксель, чтобы понять каким пикселем закрашивать остаток
                int free_pixel_index = 1 / (1 + n) * (1 + n);

                // Дополняем битовую строку до конца
                byte_flow.PadRight(frame_count * frame_scale[2], Convert.ToChar(schema.Colors[free_pixel_index]));
            }

            if (produce(schema.Colors, schema.Schema, frame_count, cur_position, step, byte_flow) == 0)
            {
                // Возвращаем путь к сохраненному видео
                return resultPath;
            }

            return "";
        }

        private int produce(string[] colors, int[] schema, int frame_count, int cur_position, int step, string byte_flow)
        {
            try
            {

                // Временная папка для хранения кадров
                string tempDir = "temp_frames";
                Directory.CreateDirectory(tempDir);

                for (int i = 0; i < frame_count; i++)
                {
                    // Тут будет код


                   
                }

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("При обработке файла произошла ошибка!", "Ошибка обработки файла");
                return -1;
            }
           
        }
    }
}
