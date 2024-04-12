using Aspose.Cells;
using DataBaseParser.DTO;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace DataBaseParser.MVVM.Model
{
    class DatabaseModel
    {
        private HttpClient httpClient;

        public async Task<ObservableCollection<Vulnerability>> UpdateByInstallationLink(string address, string searchText)
        {
            if(!File.Exists(Path.Combine(Environment.CurrentDirectory, "FSTEC.xlsx"))) 
                await DownloadFileFromLink(address);

            try
            {
                ObservableCollection<Vulnerability> vulnerabilitys = new();

                using Workbook workbook = new(Path.Combine(Environment.CurrentDirectory, "FSTEC.xlsx"));
                using Worksheet worksheet = workbook.Worksheets[0];

                var rows = worksheet.Cells.MaxDataRow;
                var cols = worksheet.Cells.MaxDataColumn;

                for (int i = 3; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        string currentString = Convert.ToString(worksheet.Cells[i, j].Value);
                        if (currentString.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        {
                            Vulnerability vulnerability = new() { ParameterAndDescription = new() };

                            for (int k = 0; k < cols; k++)
                            {
                                if (k is 18)
                                    continue;
                                vulnerability.ParameterAndDescription.Add((string)worksheet.Cells[2, k].Value, Convert.ToString(worksheet.Cells[i, k].Value));
                            }
                            vulnerability.CVEidentifier = Convert.ToString(worksheet.Cells[i, 0].Value); //(string)worksheet.Cells[i, 18].Value ?? 
                            vulnerabilitys.Add(vulnerability);
                            break;
                        }                    
                    }
                    
                }
                return vulnerabilitys;
            }
            catch { return null; }
            finally { }
                 
        }

        private async Task DownloadFileFromLink(string address)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
                (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };

            httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            using HttpRequestMessage request = new(HttpMethod.Get, address);
            var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                using Stream stream = await response.Content.ReadAsStreamAsync();

                Process[] processList;
                processList = Process.GetProcessesByName("EXCEL");
                foreach (Process proc in processList) { proc.Kill(); }

                await stream.CopyToAsync(new FileStream(Path.Combine(Environment.CurrentDirectory, "FSTEC.xlsx"), FileMode.Create));
                httpClient.Dispose();
            }
        }

        private async Task<string> GetJsonStringByApi(string apiKey, string address)
        {
            try
            {
                httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(30) };
                using HttpRequestMessage request = new(HttpMethod.Get, address);
                request.Headers.Add("User-Agent", $"{apiKey}");
                using HttpResponseMessage response = httpClient.Send(request);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    return content;
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (TimeoutException ex)
            {
                return string.Empty;
            }
            finally
            {
                httpClient.Dispose();
            }
        }

        public ObservableCollection<Vulnerability> UpdateByApiRequest(string apiKey, string address)
        {
            string threatLines = GetJsonStringByApi(apiKey, address).Result;

            try
            {
                var json = JToken.Parse(threatLines);
                var trips = json["vulnerabilities"];
                ObservableCollection<Vulnerability> vulnerabilitys = new();

                foreach (JToken trip in trips)
                {
                    List<string> referencesList = new();
                    Vulnerability vulnerability = new();

                    var cve = trip["cve"];
                    var id = cve["id"];
                    var sourceIdentifier = cve["sourceIdentifier"];
                    var description = cve["descriptions"].First["value"];
                    var lastModified = cve["lastModified"];
                    var published = cve["published"];
                    var vulnStatus = cve["vulnStatus"];
                    var references = cve["references"];

                    var metrics = cve["metrics"];
                    var cvssData = (metrics["cvssMetricV2"]?.First["cvssData"] ?? metrics["cvssMetricV31"]?.First["cvssData"]) ?? null;
                    if(cvssData is not null)
                    {
                        var vectorString = cvssData["vectorString"] ?? "Парметр не задан";
                        var baseScore = cvssData["baseScore"] ?? "Парметр не задан";
                        var version = cvssData["version"] ?? "Парметр не задан";
                        var accessComplexity = cvssData["accessComplexity"] ?? "Парметр не задан";
                        var confidentialityImpact = cvssData["confidentialityImpact"] ?? "Парметр не задан";
                        var integrityImpact = cvssData["integrityImpact"] ?? "Парметр не задан";
                        var availabilityImpact = cvssData["availabilityImpact"] ?? "Парметр не задан";

                        vulnerability.ParameterAndDescription = new()
                                {
                                    { "Последнее изменение", (string)lastModified },
                                    { "Описание", (string)description },
                                    { "Идентификатор источника", (string)sourceIdentifier },
                                    { "Базовый балл" , (string)baseScore},
                                    {"Версия метрики", (string)version },
                                    { "Вектор", (string)vectorString },
                                    {"Дата опубликоваия", (string)published},
                                    {"Статус" , (string)vulnStatus},
                                    {"Сложность доступа", (string )accessComplexity},
                                    {"Воздействие на конфиденциальность", (string )confidentialityImpact},
                                    {"Воздействие на целостность", (string)integrityImpact},
                                    {"Воздействие на доступность", (string)availabilityImpact},
                                };
                    }

                    foreach (var reference in references)
                        referencesList.Add((string)reference["url"]);
                    vulnerability.Reference = referencesList;

                    vulnerability.CVEidentifier = (string)id;                 
                    vulnerabilitys.Add(vulnerability);
                }
                return vulnerabilitys;
            }
            catch
            {
                return null;
            }           
        }

        public void UpdateByPageParsing(string searchText)
        {


            // https://jvndb.jvn.jp/search/index.php?mode=_vulnerability_search_IA_VulnSearch&lang=en&keyword=HTML
        }
    }
}