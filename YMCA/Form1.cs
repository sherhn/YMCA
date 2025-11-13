using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Text.Json;

namespace YMCA
{
    public partial class Form1 : Form
    {
        // Путь до файла
        string filePath;
        List<EncryptionSchema> schemas;

        // Разрешения видео
        int[,] scales = {
            {256, 144},
            {426, 240},
            {640, 360},
            {1280, 720},
            {1920, 1080},
            {2560, 1440},
            {3840, 2160}
        };

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;

            // Для первой панели
            Label dropZoneLabel1 = new Label();
            dropZoneLabel1.Text = "Перетащите файл сюда";
            dropZoneLabel1.Dock = DockStyle.Fill;
            dropZoneLabel1.TextAlign = ContentAlignment.MiddleCenter;
            dropZoneLabel1.Font = new Font(dropZoneLabel1.Font, FontStyle.Italic);
            dropZoneLabel1.ForeColor = Color.Gray;

            // Для второй панели
            Label dropZoneLabel2 = new Label();
            dropZoneLabel2.Text = "Перетащите файл сюда";
            dropZoneLabel2.Dock = DockStyle.Fill;
            dropZoneLabel2.TextAlign = ContentAlignment.MiddleCenter;
            dropZoneLabel2.Font = new Font(dropZoneLabel2.Font, FontStyle.Italic);
            dropZoneLabel2.ForeColor = Color.Gray;

            // Добавляем в соответствующие панели
            panel1.Controls.Add(dropZoneLabel1);
            panel2.Controls.Add(dropZoneLabel2);

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // Подгружаем схему
                List<EncryptionSchema> schemas = EncryptionSchema.LoadSchemasFromJson(@"Resources/algorithms.json");

                // Фильтруем null-элементы и добавляем только валидные
                foreach (var schema in schemas)
                {
                    comboBox1.Items.Add(schema.Name);
                }

                // Проверяем, есть ли элементы в ComboBox
                if (comboBox1.Items.Count > 0)
                {
                    comboBox1.SelectedIndex = 0;
                    comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
                }
                else
                {
                    MessageBox.Show("Не удалось загрузить алгоритмы шифрования");
                }

                // Цвет заголовка — синий
                SetTitleBarColor(this.Handle, unchecked((int)0xFFFFFFFF));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        // Стиль верхней плашки
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        private void SetTitleBarColor(IntPtr hwnd, int color)
        {
            // COLORREF = 0x00BBGGRR
            int bgr = (color & 0xFF) << 16 | (color & 0xFF00) | (color >> 16 & 0xFF);
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref bgr, sizeof(int));

            int textColor = unchecked((int)0xFFFFFFFF); // чёрный текст
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                filePath = dialog.FileName;
                label5.Text = "Выбран файл: " + Path.GetFileName(filePath);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int selectedIndex = comboBox1.SelectedIndex;
            
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    FileConverter converter = new FileConverter();
                    converter.ConvertMedia(fileBytes, Path.GetFileName(filePath), schemas[selectedIndex]);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("При загрузке файла произошла ошибка!", "Ошибка загрузки файла");
                }
            }
            else
            {
                MessageBox.Show("Файл не выбран!", "Ошибка загрузки файла");
            }
            
        }
    }
}
