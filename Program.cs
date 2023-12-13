using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{
    private static string strBearer = "";
    private static string strLicUpdateIP = "";
    private static string strBase_url = "";

    public class Config
    {
        public string? Bearer { get; set; }
        public string? LicUpdateIP { get; set; }
        public string? Base_url { get; set; }
    }

    static async Task Main()
    {
        var json = File.ReadAllText("LuncherConfig.json");
        var config = JsonConvert.DeserializeObject<Config>(json);
        strBearer =  config.Bearer;
        strBase_url = config.Base_url;
        strLicUpdateIP = config.LicUpdateIP;

        await RunPandora(true);
        using (HttpClient client = new HttpClient())
        {
            Timer timer = new Timer(CheckIP, null, 0, 60000);
            Timer Token_timer = new Timer(CheckandUpdateToken, null, 0, 24 * 60 * 60 * 1000);
            await Task.Run(() => Console.ReadLine());
        }
    }
    static async Task RunPandora(bool isrun = false)
    {
        string newIP = await GetPublicIP();
        bool isupdateip = false;
        if (strLicUpdateIP != newIP)
        {
            isupdateip = true;
            Console.WriteLine($"IP changed to {newIP}. Downloading file...");
            await DownloadFile();

            strLicUpdateIP = newIP;
            var json = File.ReadAllText("LuncherConfig.json");
            JObject jsonObject = JObject.Parse(json);
            jsonObject["LicUpdateIP"] = strLicUpdateIP;
            File.WriteAllText("LuncherConfig.json", jsonObject.ToString());

        }
        if (isupdateip || isrun)
            RestartProcess("PandoraNext.exe");

    }
    static async void CheckIP(object? state)
    {
        await RunPandora();
    }
    static async void CheckandUpdateToken(object? state)
    {
        await Task.Delay(20 * 1000);
        var strtokensjson = File.ReadAllText("tokens.json");
        JObject jsonObject = JObject.Parse(strtokensjson);
        bool isupdate = false;
        foreach (var account in jsonObject)
        {
            string accountName = account.Key;
            string username = "";
            try
            {
                username = account.Value["accuser"].ToString();
            }
            catch { }

            if (username == "")
                continue;

            string accpsw = account.Value["accpsw"].ToString();
            long timestamp = (long)account.Value["updatetimestamp"];

            if (IsTimestampOlderThan7Days(timestamp))
            {
                try
                {
                    string newtoken = await GetAccessTokenAsync(strBase_url, username, accpsw);
                    string sharetoken = await RegisterTokenAsync(username, newtoken);
                    account.Value["token"] = newtoken;
                    account.Value["sharetoken"] = sharetoken;
                    account.Value["updatetimestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    isupdate = true;
                    //Console.WriteLine($"Update Account: {accountName}, Token: {newtoken}");
                }
                catch
                {
                    Console.WriteLine($"Update Account False: {accountName}");
                }

            }

        }
        if (isupdate)
        {

            File.WriteAllText($"tokens_{DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")}.json", strtokensjson);
            File.WriteAllText("tokens.json", jsonObject.ToString());
            Console.WriteLine($"Update Account token info successful, restart Pandora ....");
            RestartProcess("PandoraNext.exe");
        }

    }
    static async Task<string> GetAccessTokenAsync(string base_url, string username, string password)
    {
        string login_url = $"{base_url}/api/auth/login";
        var data = new System.Collections.Generic.Dictionary<string, string>
        {
            { "username", username },
            { "password", password }
        };

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.PostAsync(login_url, new FormUrlEncodedContent(data));

            if (response.IsSuccessStatusCode)
            {
                string jsonResult = await response.Content.ReadAsStringAsync();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonResult).access_token;
            }
            else
            {
                throw new Exception(username + "Failed to log in and get access token!");
            }
        }
    }
    static async Task<string> RegisterTokenAsync(string username, string accesstoken)
    {
        using (HttpClient client = new HttpClient())
        {
            string url = "https://ai.fakeopen.com/token/register";

            var content = new StringContent(
                $"unique_name={username}" +
                $"&access_token={accesstoken}" +
                "&site_limit=" +
                "&expires_in=0" +
                "&show_conversations=true" +
                "&show_userinfo=true",
                Encoding.UTF8,
                "application/x-www-form-urlencoded");

            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();                
                return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(result).token_key;
            }
            else
            {

                throw new Exception($"Error: {response.StatusCode}");
            }
        }
    }

    static bool IsTimestampOlderThan7Days(long timestamp)
    {
        // 获取当前时间的 Unix 时间戳
        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // 判断是否超过4天
        return (currentTimestamp - timestamp) > (4 * 24 * 60 * 60);
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