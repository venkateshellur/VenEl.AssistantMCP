using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

class Program {
    static async Task Main() {
        var client = new HttpClient();
        var response = await client.GetFromJsonAsync<NugetIndex>("https://api.nuget.org/v3-flatcontainer/venel.assistantmcp/index.json");
        Console.WriteLine($"Versions count: {response?.Versions?.Length ?? 0}");
        if (response?.Versions?.Length > 0) {
            Console.WriteLine($"Latest: {response.Versions[^1]}");
            var v = Version.Parse(response.Versions[^1]);
            Console.WriteLine($"Parsed latest: {v}");
        }
    }
}
class NugetIndex {
    public string[] Versions { get; set; }
}
