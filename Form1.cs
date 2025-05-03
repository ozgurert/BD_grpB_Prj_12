using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Newtonsoft.Json;
using CsvHelper;
using YamlDotNet.Serialization;

namespace RequirementsConverter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
            ofd.Title = "Bir JSON dosyası seçin";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = ofd.FileName;
            }
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtFilePath.Text))
                {
                    MessageBox.Show("Lütfen bir JSON dosyası seçin!");
                    return;
                }

                if (cmbFormats.SelectedItem == null)
                {
                    MessageBox.Show("Lütfen bir çıktı formatı seçin!");
                    return;
                }

                string selectedFormat = cmbFormats.SelectedItem.ToString();
                string jsonContent = File.ReadAllText(txtFilePath.Text);

                // İlk olarak jsonContent içinden requirements.items dizisini alalım
                dynamic root = JsonConvert.DeserializeObject<dynamic>(jsonContent);
                var items = root.data.requirements.items;

                List<Requirement> requirements = new List<Requirement>();

                foreach (var item in items)
                {
                    requirements.Add(new Requirement
                    {
                        Id = item.uuid,
                        Title = item.name,
                        Description = item.desc != null ? item.desc.ToString() : ""
                    });
                }

                string outputPath = Path.GetDirectoryName(txtFilePath.Text);

                if (selectedFormat == "XML")
                {
                    XElement rootElement = new XElement("requirements");

                    foreach (var req in requirements)
                    {
                        XElement reqElement = new XElement("requirement",
                            new XAttribute("id", req.Id),
                            new XElement("title", req.Title),
                            new XElement("description", req.Description)
                        );
                        rootElement.Add(reqElement);
                    }

                    rootElement.Save(Path.Combine(outputPath, "requirements.xml"));
                    lblResult.Text = "XML dosyası başarıyla oluşturuldu!";
                }
                else if (selectedFormat == "YAML")
                {
                    var serializer = new SerializerBuilder().Build();
                    var yaml = serializer.Serialize(requirements);
                    File.WriteAllText(Path.Combine(outputPath, "requirements.yaml"), yaml);
                    lblResult.Text = "YAML dosyası başarıyla oluşturuldu!";
                }
                else if (selectedFormat == "CSV")
                {
                    using (var writer = new StreamWriter(Path.Combine(outputPath, "requirements.csv")))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(requirements);
                    }
                    lblResult.Text = "CSV dosyası başarıyla oluşturuldu!";
                }
            }
            catch (Exception ex)
            {
                lblResult.Text = "Bir hata oluştu: " + ex.Message;
            }
        }

        // Ayrıca Requirement sınıfını eklememiz gerekiyor
        public class Requirement
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
        }

    }
}
