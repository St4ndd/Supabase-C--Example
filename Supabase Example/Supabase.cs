using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Supabase;
using Supabase.Storage;
using StorageClient = Supabase.Storage.Client;

namespace Supabase_Example
{
    public partial class Supabase : Form
    {
        private global::Supabase.Client supabase;
        string BucketName = "files";
        string downloadPath;

        public Supabase()
        {
            InitializeComponent();

            // check if config is availible
            CheckConfig();
        }



        private async void resetListFiles()
        {
            await Task.Delay(3000);
            lblStatus.Text = "";
        }


        private async void CheckConfig()
        {
            // Bestimme den Pfad der Konfigurationsdatei (im selben Verzeichnis wie die EXE)
            string exePath = Application.StartupPath; // Verzeichnis der EXE-Datei
            string configFilePath = Path.Combine(exePath, "config.txt"); // Der Pfad der Konfigurationsdatei
            // Überprüfe, ob die Konfigurationsdatei existiert
            if (!File.Exists(configFilePath))
            {
                MessageBox.Show("No Config found!");
                return;
            }
            // Lese die Konfiguration aus der Datei
            try
            {
                using (StreamReader reader = new StreamReader(configFilePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length != 2) continue;
                        string key = parts[0];
                        string value = parts[1];
                        switch (key)
                        {
                            case "apiUrl":
                                txtapiUrl.Text = value;
                                break;
                            case "apiKey":
                                txtapiKey.Text = value;
                                break;
                            case "bucket":
                                txtbucket.Text = value;
                                break;
                        }
                    }

                    if (string.IsNullOrEmpty(txtapiUrl.Text) || string.IsNullOrEmpty(txtapiKey.Text) || string.IsNullOrEmpty(txtbucket.Text))
                    {
                        MessageBox.Show("Config file is missing values!");
                        return;
                    }
                }
                mainInfoLabel.Text = "Configuration loaded successfully.";

                BucketName = txtbucket.Text;

                // initialize supabase
                supabase = new global::Supabase.Client(txtapiUrl.Text, txtapiKey.Text);
                await supabase.InitializeAsync();
                mainInfoLabel.Text = "Supabase initialized!";
            }
            catch
            {
                mainInfoLabel.Text = "Couldn`t read config file!.";
            }
        }



        private async void btnListFiles_Click_1(object sender, EventArgs e)
        {
            try
            {
                lblStatus.Text = "Fetching file list...";

                var storage = supabase.Storage.From(BucketName);
                var files = await storage.List();

                // Überprüfen, ob Dateien zurückgegeben wurden
                if (files.Count == 0)
                {
                    lblStatus.Text = "No files found.";
                    resetListFiles();
                    return;
                }

                // Die Listbox zurücksetzen und die Dateien hinzufügen
                lstFiles.Items.Clear();
                foreach (var file in files)
                {
                    lblStatus.Text = $"File found: {file.Name}"; // Datei für Debugging
                    lstFiles.Items.Add(file.Name); // Dateien zur Listbox hinzufügen
                }

                lblStatus.Text = "File list updated.";
                resetListFiles();
            }
            catch (Exception ex)
            {
                // Fehlerbehandlung
                lblStatus.Text = "Error fetching files: {ex.Message}";
                resetListFiles();
            }
        }

        private async void btnDelete_Click_1(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItem == null) return;
            string fileName = lstFiles.SelectedItem.ToString();
            lblStatus.Text = $"Deleting file: {fileName}";

            var storage = supabase.Storage.From(BucketName);
            await storage.Remove(new List<string> { fileName });

            lblStatus.Text = "File deleted!";
            btnListFiles_Click_1(sender, e);
        }

        private async void btnDownload_Click(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItem == null) return;
            string fileName = lstFiles.SelectedItem.ToString();
            string targetPath;

            if (string.IsNullOrEmpty(downloadPath))
            {
                targetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
            }
            else
            {
                targetPath = Path.Combine(downloadPath, fileName);
            }

            lblStatus.Text = $"Downloading file: {fileName} to {targetPath}";

            try
            {
                var storage = supabase.Storage.From(BucketName);
                var url = await storage.CreateSignedUrl(fileName, 60);
                using var client = new System.Net.WebClient();
                client.DownloadFile(url, targetPath);

                lblStatus.Text = "Download successful!";
            }
            catch (UnauthorizedAccessException ex)
            {
                lblStatus.Text = $"Access to path denied: {ex.Message}";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error downloading the file: {ex.Message}";
            }
        }


        private async void btnUpload_Click(object sender, EventArgs e)
        {
            using OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true // Ermöglicht die Auswahl mehrerer Dateien
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var storage = supabase.Storage.From(BucketName);

                foreach (string filePath in openFileDialog.FileNames)
                {
                    string fileName = Path.GetFileName(filePath);

                    // UI-Update auf dem Hauptthread
                    lblStatus.Invoke((Action)(() => lblStatus.Text = $"Uploading file: {filePath} as {fileName}"));

                    try
                    {
                        // Versuchen, die Datei hochzuladen
                        await storage.Upload(filePath, fileName);
                        lblStatus.Invoke((Action)(() => lblStatus.Text = $"Upload successful: {fileName}"));
                    }
                    catch (global::Supabase.Storage.Exceptions.SupabaseStorageException ex) when (ex.Message.Contains("The resource already exists"))
                    {
                        // Datei existiert bereits, daher überschreiben
                        await storage.Upload(filePath, fileName, new global::Supabase.Storage.FileOptions { Upsert = true });
                        lblStatus.Invoke((Action)(() => lblStatus.Text = $"Upload successful (overwritten): {fileName}"));
                    }
                    catch (Exception ex)
                    {
                        lblStatus.Invoke((Action)(() => lblStatus.Text = $"Error uploading file {fileName}: {ex.Message}"));
                    }

                    btnListFiles_Click_1(sender, e);
                }
            }
        }

                                                                                                                                                                                                                                                                                                                                                   
                                                                                                                                                                                                     
        //                                                                                                                                                                                             
        //                                                                                                                                                                                             
        //                                   ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑                                    
        //                                  ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑                                   
        //                                  ↑↑↑                                                                                                                  ↑↑↑                                   
        //                                  ↑↑↑                                                                                                                  ↑↑↑                                   
        //                                  ↑↑↑                                                                                                                  ↑↑↑                                   
        //                                  ↑↑↑                                                                                                                  ↑↑↑                                   
        //                                  ↑↑↑            ↑↑↑↑↑↑↑     ↑↑↑↑↑↑↑↑↑↑↑↑↑        ↑↑↑↑↑↑    ↑↑↑↑↑     ↑↑↑↑↑   ↑↑↑↑↑↑↑↑↑↑       ↑↑↑↑↑↑↑↑↑↑              ↑↑↑                                   
        //                                  ↑↑↑          ↑↑↑↑↑↑↑↑↑↑↑   ↑↑↑↑↑↑↑↑↑↑↑↑↑       ↑↑↑↑↑↑↑    ↑↑↑↑↑↑    ↑↑↑↑↑   ↑↑↑↑↑↑↑↑↑↑↑↑↑    ↑↑↑↑↑↑↑↑↑↑↑↑↑           ↑↑↑                                   
        //                                  ↑↑↑          ↑↑↑↑   ↑↑↑↑↑      ↑↑↑↑          ↑↑↑↑↑↑↑↑↑    ↑↑↑↑↑↑↑   ↑↑↑↑↑   ↑↑↑↑    ↑↑↑↑↑↑   ↑↑↑↑↑   ↑↑↑↑↑↑          ↑↑↑                                   
        //                                  ↑↑↑          ↑↑↑↑↑↑            ↑↑↑↑         ↑↑↑↑↑↑↑↑↑↑    ↑↑↑↑↑↑↑↑  ↑↑↑↑↑   ↑↑↑↑      ↑↑↑↑   ↑↑↑↑↑     ↑↑↑↑↑         ↑↑↑                                   
        //                                  ↑↑↑           ↑↑↑↑↑↑↑↑↑↑       ↑↑↑↑        ↑↑↑↑  ↑↑↑↑↑    ↑↑↑↑ ↑↑↑↑↑↑↑↑↑↑   ↑↑↑↑      ↑↑↑↑↑  ↑↑↑↑↑     ↑↑↑↑↑         ↑↑↑                                   
        //                                  ↑↑↑               ↑↑↑↑↑↑↑      ↑↑↑↑       ↑↑↑↑↑↑↑↑↑↑↑↑↑   ↑↑↑↑  ↑↑↑↑↑↑↑↑↑   ↑↑↑↑      ↑↑↑↑   ↑↑↑↑↑     ↑↑↑↑↑         ↑↑↑                                   
        //                                  ↑↑↑          ↑↑↑↑   ↑↑↑↑↑      ↑↑↑↑      ↑↑↑↑↑↑↑↑↑↑↑↑↑↑   ↑↑↑↑   ↑↑↑↑↑↑↑↑   ↑↑↑↑     ↑↑↑↑↑   ↑↑↑↑↑    ↑↑↑↑↑          ↑↑↑                                   
        //                                  ↑↑↑          ↑↑↑↑↑↑↑↑↑↑↑↑      ↑↑↑↑              ↑↑↑↑↑    ↑↑↑↑     ↑↑↑↑↑↑   ↑↑↑↑↑↑↑↑↑↑↑↑↑    ↑↑↑↑↑↑↑↑↑↑↑↑↑           ↑↑↑                                   
        //                                  ↑↑↑            ↑↑↑↑↑↑↑↑        ↑↑↑↑              ↑↑↑↑↑    ↑↑↑↑      ↑↑↑↑↑   ↑↑↑↑↑↑↑↑↑↑↑      ↑↑↑↑↑↑↑↑↑↑↑             ↑↑↑                                   
        //                                  ↑↑↑                                                                                                                  ↑↑↑                                   
        //                                  ↑↑↑                                                                                                                  ↑↑↑                                   
        //                                  ↑↑↑                                                                                                                  ↑↑↑                                   
        //                                  ↑↑↑                                                                                                                  ↑↑↑                                   
        //                                  ↑↑↑                                             ↑↑↑↑ ↑ ↑↑↑ ↑  ↑ ↑  ↑ ↑↑↑                                             ↑↑↑                                   
        //                                  ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑              ↑   ↑ ↑  ↑  ↑↑↑↑↑↑  ↑ ↑↑↑↑              ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑                                   
        //                                   ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑              ↑↑  ↑↑↑  ↑  ↑  ↑ ↑  ↑ ↑  ↑              ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑                                    
        //                                                                                                                                                                                             
                                                                                                                                                                                                     
                                                                                                                                                                                       


        // save config
        private void btnSaveConfig_Click(object sender, EventArgs e)
        {
            // Hole die Werte aus den Textboxen
            string apiUrl = txtapiUrl.Text;
            string apiKey = txtapiKey.Text;
            string bucket = txtbucket.Text;

            // Bestimme den Pfad der Konfigurationsdatei (im selben Verzeichnis wie die EXE)
            string exePath = Application.StartupPath; // Verzeichnis der EXE-Datei
            string configFilePath = Path.Combine(exePath, "config.txt"); // Der Pfad der Konfigurationsdatei

            // Erstelle und schreibe die Konfiguration in die Datei
            try
            {
                using (StreamWriter writer = new StreamWriter(configFilePath))
                {
                    writer.WriteLine("apiUrl=" + apiUrl);
                    writer.WriteLine("apiKey=" + apiKey);
                    writer.WriteLine("bucket=" + bucket);
                }

                MessageBox.Show("Configuration saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving the configuration: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2ToggleSwitch1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void switchCustomDownloadPath_CheckedChanged(object sender, EventArgs e)
        {
            if (switchCustomDownloadPath.Checked)
            {
                using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
                {
                    if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    {
                        downloadPath = folderBrowserDialog.SelectedPath;
                    }
                }
            }
            else
            {
                downloadPath = "";
            }

        }
    }
}
