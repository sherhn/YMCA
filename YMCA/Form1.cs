using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace YMCA
{
    public partial class Form1 : Form
    {
        // Путь до файла
        string filePath;
        string mediaPath;
        List<EncryptionSchema> schemas;

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

            // Делаем panel1 рабочей дропзоной
            panel1.AllowDrop = true;

            panel1.DragEnter += (s, ev) =>
            {
                if (ev.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    ev.Effect = DragDropEffects.Copy;
                    panel1.BackColor = Color.LightBlue; // Визуальная обратная связь
                }
            };

            panel1.DragLeave += (s, ev) =>
            {
                panel1.BackColor = SystemColors.Control;
            };

            panel1.DragDrop += (s, ev) =>
            {
                panel1.BackColor = SystemColors.Control;

                if (ev.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])ev.Data.GetData(DataFormats.FileDrop);
                    if (files.Length > 0)
                    {
                        filePath = files[0];

                        StringBuilder labelText = new StringBuilder();
                        labelText.Append("Выбран файл: ");
                        labelText.Append(Path.GetFileName(filePath));
                        label5.Text = labelText.ToString();

                        // Обновляем текст на панели
                        dropZoneLabel1.Text = Path.GetFileName(filePath);
                        dropZoneLabel1.Font = new Font(dropZoneLabel1.Font, FontStyle.Regular);
                        dropZoneLabel1.ForeColor = SystemColors.ControlText;
                    }
                }
            };

            // Для второй панели
            Label dropZoneLabel2 = new Label();
            dropZoneLabel2.Text = "Перетащите файл сюда";
            dropZoneLabel2.Dock = DockStyle.Fill;
            dropZoneLabel2.TextAlign = ContentAlignment.MiddleCenter;
            dropZoneLabel2.Font = new Font(dropZoneLabel2.Font, FontStyle.Italic);
            dropZoneLabel2.ForeColor = Color.Gray;

            // Делаем panel2 рабочей дропзоной
            panel2.AllowDrop = true;

            panel2.DragEnter += (s, ev) =>
            {
                if (ev.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    ev.Effect = DragDropEffects.Copy;
                    panel2.BackColor = Color.LightBlue; // Визуальная обратная связь
                }
            };

            panel2.DragLeave += (s, ev) =>
            {
                panel2.BackColor = SystemColors.Control;
            };

            panel2.DragDrop += (s, ev) =>
            {
                panel2.BackColor = SystemColors.Control;

                if (ev.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])ev.Data.GetData(DataFormats.FileDrop);
                    if (files.Length > 0)
                    {
                        mediaPath = files[0];

                        StringBuilder labelText = new StringBuilder();
                        labelText.Append("Выбран файл: ");
                        labelText.Append(Path.GetFileName(mediaPath));
                        label7.Text = labelText.ToString();

                        // Обновляем текст на панели
                        dropZoneLabel2.Text = Path.GetFileName(mediaPath);
                        dropZoneLabel2.Font = new Font(dropZoneLabel2.Font, FontStyle.Regular);
                        dropZoneLabel2.ForeColor = SystemColors.ControlText;
                    }
                }
            };

            // Добавляем в соответствующие панели
            panel1.Controls.Add(dropZoneLabel1);
            panel2.Controls.Add(dropZoneLabel2);

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // Инициализируем поле класса schemas
                schemas = EncryptionSchema.LoadSchemasFromJson(@"Resources/algorithms.json");

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

                StringBuilder labelText = new StringBuilder();
                labelText.Append("Выбран файл: ");
                labelText.Append(Path.GetFileName(filePath));
                label5.Text = labelText.ToString();
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            int selectedIndex = comboBox1.SelectedIndex;

            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    // Отключаем кнопку во время обработки
                    button1.Enabled = false;
                    progressBar1.Value = 0;
                    label4.Text = "0.0%";

                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    FileConverter converter = new FileConverter();

                    // Используем асинхронную версию
                    await converter.ConvertFileAsync(fileBytes, Path.GetFileName(filePath),
                        schemas[selectedIndex], progressBar1, label4);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"При загрузке файла произошла ошибка: {ex.Message}",
                        "Ошибка загрузки файла");
                }
                finally
                {
                    // Включаем кнопку обратно
                    button1.Enabled = true;
                }
            }
            else
            {
                MessageBox.Show("Файл не выбран!", "Ошибка загрузки файла");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                mediaPath = dialog.FileName;

                StringBuilder labelText = new StringBuilder();
                labelText.Append("Выбран файл: ");
                labelText.Append(Path.GetFileName(mediaPath));
                label7.Text = labelText.ToString();
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(mediaPath))
            {
                try
                {
                    // Отключаем кнопку во время обработки
                    button3.Enabled = false;
                    progressBar2.Value = 0;
                    label9.Text = "0.0%";

                    MediaConverter converter = new MediaConverter();

                    // Используем асинхронную версию
                    await converter.ConvertMediaAsync(mediaPath, Path.GetFileName(mediaPath),
                        schemas, progressBar2, label9);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"При загрузке файла произошла ошибка: {ex.Message}",
                        "Ошибка загрузки файла");
                }
                finally
                {
                    // Включаем кнопку обратно
                    button3.Enabled = true;
                }
            }
            else
            {
                MessageBox.Show("Файл не выбран!", "Ошибка загрузки файла");
            }
        }
    }
}