using Aspose.Cells;
using DataBaseParser.DTO;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataBaseParser.MVVM.Model
{
    class DatabaseModel
    {
        private HttpClient? httpClient;

        public async Task<ObservableCollection<Vulnerability>?> UpdateByInstallationLink(string address, string searchText)
        {
            if (searchText == string.Empty)
                throw new ArgumentException("Строка для поиска пуста");

            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "FSTEC.xlsx")))
                await DownloadFileFromLink(address);

            try
            {
                ObservableCollection<Vulnerability> vulnerabilitys = new();

                using Workbook workbook = new(Path.Combine(Environment.CurrentDirectory, "FSTEC.xlsx"));
                using Worksheet worksheet = workbook.Worksheets[0];

                var numberRows = worksheet.Cells.MaxDataRow;
                var numberColumn = worksheet.Cells.MaxDataColumn;

                for (int rowIterator = 3; rowIterator < numberRows; rowIterator++)
                {
                    for (int columnIterator = 0; columnIterator < numberColumn; columnIterator++)
                    {
                        string currentString = Convert.ToString(worksheet.Cells[rowIterator, columnIterator].Value);

                        if (currentString.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        {
                            Vulnerability vulnerability = new() { ParameterAndDescription = new() };

                            for (int lineIterator = 0; lineIterator < numberColumn; lineIterator++)
                            {
                                if (lineIterator is 18)
                                    continue;
                                vulnerability.ParameterAndDescription.Add((string)worksheet.Cells[2, lineIterator].Value, Convert.ToString(worksheet.Cells[rowIterator, lineIterator].Value));
                            }
                            vulnerability.Identifier = Convert.ToString(worksheet.Cells[rowIterator, 0].Value);
                            vulnerabilitys.Add(vulnerability);
                            break;
                        }
                    }
                }
                return vulnerabilitys;
            }
            catch (HttpRequestException ex)
            {
                //проблемы с сервером
                return null;
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    //истекло время запроса
                }
                else
                {
                    //другая ошибка
                }
                return null;
            }
            catch (Exception ex)
            {
                //другие ошибки
                return null;
            }
            finally
            {
                httpClient.Dispose();
            }
        }

        private async Task DownloadFileFromLink(string address)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
                (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };

            try
            {
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
                }
                else
                {
                    throw new Exception(GetExceptionMessage(response.StatusCode));
                }
            }
            catch (HttpRequestException ex)
            {
                //проблемы с сервером
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    //истекло время запроса
                }
                else
                {
                    //другая ошибка
                }
            }
            catch (Exception ex)
            {
                //другие ошибки
            }
            finally
            {
                httpClient.Dispose();
            }
        }

        private async Task<string?> GetJsonStringByApi(string apiKey, string address)
        {
            try
            {
                httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(30) };
                using HttpRequestMessage request = new(HttpMethod.Get, address);
                request.Headers.Add("User-Agent", $"{apiKey}");
                using HttpResponseMessage response = httpClient.Send(request);

                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync();
                else
                    throw new Exception(GetExceptionMessage(response.StatusCode));
            }
            catch (HttpRequestException ex)
            {
                //проблемы с сервером
                return null;
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    //истекло время запроса
                }
                else
                {
                    //другая ошибка
                }
                return null;
            }
            catch (Exception ex)
            {
                //другие ошибки
                return null;
            }
            finally
            {
                httpClient.Dispose();
            }
        }

        public ObservableCollection<Vulnerability> UpdateByApiRequest(string apiKey, string address)
        {
            string responseString = GetJsonStringByApi(apiKey, address).Result ?? throw new ArgumentException("Строка поиска пуста");

            var json = JToken.Parse(responseString);
            var trips = json["vulnerabilities"];
            ObservableCollection<Vulnerability> vulnerabilitys = new();

            foreach (JToken trip in trips)
            {
                List<string> referencesList = new();
                Vulnerability vulnerability = new();

                var cve = trip["cve"];
                var id = cve["id"];
                var references = cve["references"];
                var metrics = cve["metrics"];
                var cvssData = (metrics["cvssMetricV2"]?.First["cvssData"] ?? metrics["cvssMetricV31"]?.First["cvssData"]) ?? null;

                if (cvssData is not null)
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
                                    { "Последнее изменение", (string)cve["lastModified"] },
                                    { "Описание", (string)cve["descriptions"].First["value"] },
                                    { "Идентификатор источника", (string)cve["sourceIdentifier"] },
                                    { "Базовый балл" , (string)baseScore},
                                    {"Версия метрики", (string)version },
                                    { "Вектор", (string)vectorString },
                                    {"Дата опубликоваия", (string) cve["published"]},
                                    {"Статус" , (string)cve["vulnStatus"]},
                                    {"Сложность доступа", (string )accessComplexity},
                                    {"Воздействие на конфиденциальность", (string )confidentialityImpact},
                                    {"Воздействие на целостность", (string)integrityImpact},
                                    {"Воздействие на доступность", (string)availabilityImpact},
                                };
                }

                foreach (var reference in references)
                    referencesList.Add((string)reference["url"]);
                vulnerability.Reference = referencesList;

                vulnerability.Identifier = (string)id;
                vulnerabilitys.Add(vulnerability);
            }
            return vulnerabilitys;
        }

        public ObservableCollection<Vulnerability>? UpdateByPageParsing(string searchText) 
        {
            HtmlWeb web = new();
            List<string> refVul = new();
            ObservableCollection<Vulnerability> vulnerabilitys = new();
            try
            {
                var htmlDoc = web.Load(searchText);
                var numberVul = htmlDoc.DocumentNode.SelectNodes("//table[@class='result_class']/tr").Count;

                for (int i = 2; i <= numberVul; i++)
                {
                    var refer = htmlDoc.DocumentNode.SelectSingleNode($"//table[@class='result_class']/tr[{i}]/td[1]/a[1]").Attributes["href"].Value;
                    refVul.Add("https://jvndb.jvn.jp" + refer);
                }
                for (int i = 0; i < refVul.Count; i++)
                {                                                                                                 
                    Vulnerability vulnerability = new();

                    var htmlDocument = web.Load(refVul[i]);
                    var nodes = htmlDocument.DocumentNode.SelectNodes("//div[@id='news-list']/table[1]/tr");

                    vulnerability.Identifier = htmlDocument.DocumentNode.SelectSingleNode("//div[@id='news-list']/table[1]/tr[2]").InnerText.Replace("\n", string.Empty);
                    vulnerability.ParameterAndDescription = new()
                    {
                        { "Описание", htmlDocument.DocumentNode.SelectSingleNode("//div[@id='news-list']/table[1]/tr[5]").InnerText.Replace("\n", string.Empty) },
                    };
                    vulnerabilitys.Add(vulnerability);
                }
                return vulnerabilitys;
            }
            catch (JsonException ex)
            {
                // Проблемы с сериализацией объектов
                return null;
            }
            catch (Exception ex)
            {
                // Непредвиденная ошибка
                return null;
            }
        }

        private string GetExceptionMessage(HttpStatusCode statusCode)
        {
            if (statusCode is HttpStatusCode.Forbidden)
                return ("Сервер отказывается выполнить запрос");
            else if (statusCode is HttpStatusCode.Gone)
                return ("Ресурс больше недоступен");
            else if (statusCode is HttpStatusCode.InternalServerError)
                return ("На сервере произошла общая ошибка");
            else if (statusCode is HttpStatusCode.ServiceUnavailable)
                return ("Сервер временно недосутпен");
            else
                return ("Непредвиденная ошибка");
        }
    }
}