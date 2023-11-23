using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

class Program
{
    private static string oldIP = "";
    private static string strBearer = "";

    public class Config
    {
        public string Bearer { get; set; }
    }

    static async Task Main()
    {
        var json = File.ReadAllText("LuncherConfig.json");
        var config = JsonConvert.DeserializeObject<Config>(json);
        strBearer = config.Bearer;
        using (HttpClient client = new HttpClient())
        {
            Timer timer = new Timer(CheckIP, null, 0, 60000);
            await Task.Run(() => Console.ReadLine());
        }
    }

    static async void CheckIP(object? state)
    {
        string newIP = await GetPublicIP();
        if (oldIP != newIP)
        {
            oldIP = newIP;
            Console.WriteLine($"IP changed to {newIP}. Downloading file...");
            await DownloadFile();
            RestartProcess("PandoraNext.exe");
        }
    }

    static async Task<string> GetPublicIP()
    {
        string url = "https://myip.ipip.net";
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);
            string data = await response.Content.ReadAsStringAsync();
            Regex regex = new Regex(@"(\d+\.\d+\.\d+\.\d+)");
            Match match = regex.Match(data);
            return match.Success ? match.Value : string.Empty;
        }
    }

    static async Task DownloadFile()
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", strBearer);
            var response = await client.GetAsync("https://dash.pandoranext.com/data/license.jwt");
            var content = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync("license.jwt", content);
        }
    }

    static void RestartProcess(string processName)
    {
        foreach (var process in System.Diagnostics.Process.GetProcessesByName("PandoraNext"))
        {
            process.Kill();
            process.WaitForExit();
        }
        System.Diagnostics.Process.Start(processName);
    }

}