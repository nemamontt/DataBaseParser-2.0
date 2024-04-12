using DataBaseParser.Core;
using DataBaseParser.DTO;
using DataBaseParser.MVVM.Model;
using Microsoft.Win32;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DataBaseParser.MVVM.ViewModel
{
    class MainWindowViewModel : ObservedObject
    {
        private readonly DatabaseModel model;

        private List<string> _listDatabases;
        public List<string> ListDatabases
        {
            get => _listDatabases; 
            set { _listDatabases = value; }
        }

        private ObservableCollection<Vulnerability>? _resaultSearch;
        public ObservableCollection<Vulnerability>? ResaultSearch
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
                    var collectionVul = await model.UpdateByInstallationLink("https://bdu.fstec.ru/files/documents/vullist.xlsx", searchText);
                    if (collectionVul is not null)
                        foreach (var vul in collectionVul)
                        {
                            ResaultSearch.Add(vul);
                        }
                    break;

                case "JVN":
                 /*   foreach (var vul in model.)
                    {
                        ResaultSearch.Add(vul);
                    }*/
                    break;
            }
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
    }
}