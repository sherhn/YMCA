using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace YMCA
{
    public class EncryptionSchema
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Signature { get; set; }
        public string[] Colors { get; set; }
        public int Schema { get; set; }

        // Статический метод для загрузки схем из JSON файла
        public static List<EncryptionSchema> LoadSchemasFromJson(string filePath)
        {
            // Читаем JSON
            string jsonString = File.ReadAllText(filePath);

            var options = new JsonSerializerOptions
            {
                // Игнорирование регистра и разрешение на запятые после последнего элемента (не является обязательным, прописано для стабильности)
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            // Возвращаем список схем
            return JsonSerializer.Deserialize<List<EncryptionSchema>>(jsonString, options);
        }
    }
}
