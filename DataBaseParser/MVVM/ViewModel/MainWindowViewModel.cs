using DataBaseParser.Core;
using DataBaseParser.DTO;
using DataBaseParser.MVVM.Model;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DataBaseParser.MVVM.ViewModel
{
    class MainWindowViewModel : ObservedObject
    {
        private readonly DatabaseModel model;

        private ObservableCollection<string> _listDatabases;
        public ObservableCollection<string> ListDatabases
        {
            get => _listDatabases; 
            set
            {
                _listDatabases = value; 
                OnPropertyChanged(nameof(ListDatabases));
            }
        }

        private ObservableCollection<Vulnerability> _resaultSearch;
        public ObservableCollection<Vulnerability> ResaultSearch
        {
            get => _resaultSearch;
            set { _resaultSearch = value; }
        }

        public MainWindowViewModel()
        {
            model = new();
            ResaultSearch = new();
            ListDatabases = new()
            {
                "NVD",
                "FSTEC",
                "JVN"
            };
        }

        public async void VulnerabilitySearch(string searchText, string selectedDataBase)
        {
            try
            {
                switch (selectedDataBase)
                {
                    case "NVD":
                        foreach (var vul in model.UpdateByApiRequest("6cbe6e35-f52e-410f-a627-444352adf9c3",
                       $"https://services.nvd.nist.gov/rest/json/cves/2.0?keywordSearch={searchText}&keywordExactMatch"))
                        {
                            ResaultSearch.Add(vul);
                        }
                        break;

                    case "FSTEC":
                        foreach (var vul in await model.UpdateByInstallationLink("https://bdu.fstec.ru/files/documents/vullist.xlsx", searchText))
                            ResaultSearch.Add(vul);
                        break;

                    case "JVN":
                        foreach (var vul in model.UpdateByPageParsing($"https://jvndb.jvn.jp/search/index.php?mode=_vulnerability_search_IA_VulnSearch&lang=en&keyword={searchText}"))
                            ResaultSearch.Add(vul);
                        break;
                }
            }
            catch (NullReferenceException ex)
            {
                
            }
            catch(Exception ex) {}
        }

        public void SaveFile(IEnumerable objects)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(objects, options);

            SaveFileDialog saveFileDialog = new() { Filter = "Json files (*.json)|*.json|All files (*.*)|*.*" };
            if (saveFileDialog.ShowDialog() == true)
                File.WriteAllText(saveFileDialog.FileName, json);
        }

        public void ImportFile()
        {
            OpenFileDialog openFileDialog = new() { Filter = "Json files (*.json)|*.json" };

            if (openFileDialog.ShowDialog() == true)
            {
                var filePath = openFileDialog.FileName;
                string jsonString = File.ReadAllText(filePath);
                var vulnerabilitys = JsonSerializer.Deserialize<ObservableCollection<Vulnerability>>(jsonString);

                ResaultSearch.Clear();
                foreach (var vul in vulnerabilitys)
                    ResaultSearch.Add(vul);
            }
        }
    }
}