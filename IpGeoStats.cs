using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

class Program
{
    static async Task Main()
    {
        string[] ips;
        string exeDir = AppContext.BaseDirectory;
        string filePath = Path.Combine(exeDir, "ips.txt");
        try
        {
            ips = File.ReadAllLines("ips.txt")
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            if (ips.Length == 0)
                throw new Exception("Файл пустой");
        }
        catch
        {
            Console.WriteLine("Не удалось прочитать ips.txt -> используется встроенный список");

            ips = new[]
            {
                "5.18.233.41",    "46.147.120.9",     "91.203.56.188",
                "89.163.214.77",  "138.201.95.12",   "54.182.91.203",
                "51.158.102.66",  "133.242.187.19",  "3.120.54.98",
                "177.72.34.90",   "203.12.89.45"
            };
        }
        
        var results = await Task.WhenAll(
            ips.Select(IpInfoService.GetIpDataAsync)
        );
        
        var countryGroups = results
            .Where(x => !string.IsNullOrWhiteSpace(x.Country))
            .GroupBy(x => x.Country)
            .OrderByDescending(g => g.Count())
            .ToList();
    
        Console.WriteLine("Статистика по странам (количество городов):");
        foreach (var group in countryGroups)
        {
            var cityCount = group
                .Where(x => !string.IsNullOrWhiteSpace(x.City))
                .Select(x => x.City)
                .Distinct()
                .Count();
            
            Console.WriteLine($"{group.Key} - {cityCount}");
        }

        var topCountryGroup = countryGroups.First();
        Console.WriteLine($"\nСтрана с наибольшим числом IP: {topCountryGroup.Key}");
        Console.WriteLine("Города:");

        foreach (var city in topCountryGroup
            .Where(x => !string.IsNullOrWhiteSpace(x.City))
            .Select(x => x.City)
            .Distinct())
        {
            Console.WriteLine(city);
        }
    }
}

public static class IpInfoService
{
    private static readonly HttpClient _httpClient = new HttpClient();

    public static async Task<IpData> GetIpDataAsync(string ip)
    {
        var url = $"https://ipinfo.io/{ip}/json";
        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<IpData>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        );
    }
}

public class IpData
{
    public string Ip { get; set; }
    public string City { get; set; }
    public string Region { get; set; }
    public string Country { get; set; }
    public string Loc { get; set; }
    public string Org { get; set; }
    public string Timezone { get; set; }
}