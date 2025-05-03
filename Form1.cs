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


namespace RequirementsConverter
{
    public partial class Form1 : Form
    {
        // Dummy configuration for demonstration.
        // In a real app, load this from a file based on user selection or input file type.
        private ConversionConfig currentConfig;

        public Form1()
        {
            InitializeComponent();
            // cmbFormats ComboBox'ı artık LoadConfiguration metodunda doldurulacak
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Form ilk yüklendiğinde ComboBox'ı boş bırakabiliriz,
            // ya da tüm olası formatları ekleyip dosya seçildiğinde filtreleyebiliriz.
            // Dinamik yükleme yapacağımız için burayı boş bırakabiliriz veya temizleyebiliriz.
            cmbFormats.Items.Clear();
            lblResult.Text = "Please select an input file.";
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            // Filter for supported input files
            ofd.Filter = "JSON Files (*.json)|*.json|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
            ofd.Title = "Select a JSON or CSV input file";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                btnSelectFile.Text = ofd.FileName;
                lblResult.Text = "File selected: " + Path.GetFileName(btnSelectFile.Text); // Info message

                // Attempt to load a configuration based on the selected file extension
                // This method also populates the output format ComboBox
                LoadConfiguration(ofd.FileName);
            }
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            lblResult.Text = ""; // Clear previous result
            try
            {
                if (string.IsNullOrEmpty(btnSelectFile.Text))
                {
                    MessageBox.Show("Lütfen bir giriş dosyası seçin!");
                    return;
                }

                if (cmbFormats.SelectedItem == null)
                {
                    MessageBox.Show("Lütfen bir çıktı formatı seçin!");
                    return;
                }

                if (currentConfig == null)
                {
                    MessageBox.Show("Configuration could not be loaded for the selected file type.");
                    return;
                }

                string selectedFormat = cmbFormats.SelectedItem.ToString();
                string filePath = btnSelectFile.Text;
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8); // Read with UTF-8 encoding
                string outputPath = Path.GetDirectoryName(filePath);
                string outputFileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

                List<Requirement> requirements = null;

                // --- Parsing Phase (driven by Configuration) ---
                try
                {
                    if (currentConfig.InputType.ToLower() == "json")
                    {
                        if (currentConfig.JsonConfig == null) throw new InvalidOperationException("JSON configuration is missing.");
                        requirements = DataParser.ParseJson(fileContent, currentConfig.JsonConfig);
                    }
                    else if (currentConfig.InputType.ToLower() == "csv")
                    {
                        if (currentConfig.CsvConfig == null) throw new InvalidOperationException("CSV configuration is missing.");
                        requirements = DataParser.ParseCsv(fileContent, currentConfig.CsvConfig);
                    }
                    else
                    {
                        MessageBox.Show($"Unsupported input type specified in configuration: {currentConfig.InputType}");
                        return;
                    }

                    if (requirements == null || requirements.Count == 0)
                    {
                        lblResult.Text = "Parsing resulted in no requirements or encountered an error, or file is empty.";
                        return;
                    }

                }
                catch (Exception parseEx)
                {
                    lblResult.Text = "Parsing Error: " + parseEx.Message;
                    MessageBox.Show("Parsing Error Details:\n\n" + parseEx.ToString(), "Parsing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    System.Diagnostics.Debug.WriteLine("Parsing Error Details:");
                    System.Diagnostics.Debug.WriteLine(parseEx.ToString());
                    return; // Stop conversion on parsing error
                }


                // --- Conversion Phase (driven by Configuration and Output Format) ---
                string outputContent = "";
                string outputFileExtension = "";

                try
                {
                    switch (selectedFormat)
                    {
                        case "XML":
                            outputContent = DataConverter.ConvertRequirementsToXml(requirements);
                            outputFileExtension = ".xml";
                            break;
                        case "YAML":
                            outputContent = DataConverter.ConvertRequirementsToYaml(requirements);
                            outputFileExtension = ".yaml";
                            break;
                        case "CSV":
                            // For CSV output, we might need field mapping from the Requirement object
                            // The DataConverter would handle this based on a potential output configuration
                            outputContent = DataConverter.ConvertRequirementsToCsv(requirements); // This would need refinement based on desired CSV output structure
                            outputFileExtension = ".csv";
                            break;
                        case "JSON":
                            outputContent = DataConverter.ConvertRequirementsToJson(requirements);
                            outputFileExtension = ".json";
                            break;
                        case "OSLC_RM_RDF_XML":
                            // This is where the complex OSLC RM 2.0 RDF/XML generation logic goes
                            outputContent = DataConverter.ConvertRequirementsToOslcRmXml(requirements); // Placeholder method
                            outputFileExtension = ".rdf"; // Standard extension for RDF/XML
                            break;
                        default:
                            MessageBox.Show("Unsupported output format selected.");
                            return;
                    }
                }
                catch (Exception convertEx)
                {
                    lblResult.Text = "Conversion Error: " + convertEx.Message;
                    MessageBox.Show("Conversion Error Details:\n\n" + convertEx.ToString(), "Conversion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    System.Diagnostics.Debug.WriteLine("Conversion Error Details:");
                    System.Diagnostics.Debug.WriteLine(convertEx.ToString());
                    return; // Stop on conversion error
                }


                // --- Writing Output File ---
                string outputFilePath = Path.Combine(outputPath, $"{outputFileNameWithoutExtension}_converted{outputFileExtension}");
                File.WriteAllText(outputFilePath, outputContent, Encoding.UTF8);

                lblResult.Text = $"{selectedFormat} file successfully created: {Path.GetFileName(outputFilePath)}";
            }
            catch (Exception ex)
            {
                // Catch any unexpected errors
                lblResult.Text = "An unexpected error occurred: " + ex.Message;
                MessageBox.Show("Unexpected Error Details:\n\n" + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine("Unexpected Error Details:");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        // --- Configuration Loading Logic (Updated for Specific Examples and Dynamic ComboBox) ---
        private void LoadConfiguration(string filePath)
        {
            string fileExtension = Path.GetExtension(filePath).ToLower();

            currentConfig = new ConversionConfig();

            cmbFormats.Items.Clear();
            currentConfig.JsonConfig = null;
            currentConfig.CsvConfig = null;

            if (fileExtension == ".json")
            {
                currentConfig.InputType = "json";
                currentConfig.JsonConfig = new JsonInputConfig
                {
                    RootPath = "data.requirements.items",
                    FieldMappings = new List<FieldMapping>
            {
                new FieldMapping { InputFieldName = "uuid", OutputPropertyName = "Id", DataType = "string" },
                new FieldMapping { InputFieldName = "name", OutputPropertyName = "Title", DataType = "string" },
                new FieldMapping { InputFieldName = "desc", OutputPropertyName = "Description", DataType = "string" },
                // --- Yeni JSON Alan Eşlemeleri (Eğer JSON'da varsa) ---
                // JSON'da 'stakeholders' bir dizi ise, DataParser'ın bunu List<string>'e ayrıştırması gerekir.
                // Bu FieldMapping sadece alanın adını belirtir. Ayrıştırma logic'i DataParser'da olmalıdır.
                new FieldMapping { InputFieldName = "stakeholders", OutputPropertyName = "Stakeholders", DataType = "list<string>" }, // DataType sadece bilgi amaçlı olabilir
                new FieldMapping { InputFieldName = "products", OutputPropertyName = "Products", DataType = "list<string>" },
                new FieldMapping { InputFieldName = "tags", OutputPropertyName = "Tags", DataType = "list<string>" },
                new FieldMapping { InputFieldName = "workPackages", OutputPropertyName = "WorkPackages", DataType = "list<string>" },
            }
                };
                cmbFormats.Items.AddRange(new string[] { "CSV", "XML", "YAML", "OSLC_RM_RDF_XML" });
            }
            else if (fileExtension == ".csv")
            {
                currentConfig.InputType = "csv";
                currentConfig.CsvConfig = new CsvInputConfig
                {
                    HasHeaderRow = true,
                    Delimiter = ",",
                    FieldMappings = new List<FieldMapping>
            {
                new FieldMapping { InputFieldName = "id", OutputPropertyName = "Id", DataType = "string" },
                new FieldMapping { InputFieldName = "name", OutputPropertyName = "Title", DataType = "string" },
                new FieldMapping { InputFieldName = "description", OutputPropertyName = "Description", DataType = "string" },
                
                new FieldMapping { InputFieldName = "stakeholders", OutputPropertyName = "Stakeholders", DataType = "list<string>" },
                new FieldMapping { InputFieldName = "products", OutputPropertyName = "Products", DataType = "list<string>" },
                new FieldMapping { InputFieldName = "tags", OutputPropertyName = "Tags", DataType = "list<string>" },
                new FieldMapping { InputFieldName = "workPackages", OutputPropertyName = "WorkPackages", DataType = "list<string>" },
            }
                };
                cmbFormats.Items.AddRange(new string[] { "XML", "YAML", "JSON", "OSLC_RM_RDF_XML" });
            }
            else
            {
                currentConfig = null;
            }

            // ... (UI güncelleme kısmı aynı kalacak) ...
            if (currentConfig != null)
            {
                lblResult.Text += $" Configuration loaded for {currentConfig.InputType.ToUpper()} input.";
                if (cmbFormats.Items.Count > 0)
                {
                    cmbFormats.SelectedIndex = 0;
                }
                else
                {
                    lblResult.Text += " No output formats available for this input type.";
                }
            }
            else
            {
                lblResult.Text += " No specific configuration found or supported for this file type.";
                cmbFormats.Items.Clear();
            }
        }


        // Placeholders for UI event handlers that might exist in Designer.cs
        private void label1_Click(object sender, EventArgs e) { } // Example from your original code
        private void txtFilePath_TextChanged(object sender, EventArgs e) { }
        private void cmbFormats_SelectedIndexChanged(object sender, EventArgs e) { }
    }
}