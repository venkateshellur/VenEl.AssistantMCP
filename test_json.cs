using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

class Program {
    class NugetIndex {
        public string[] Versions { get; set; }
    }
    static async Task Main() {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "VenEl");
        var resp = await client.GetFromJsonAsync<NugetIndex>("https://api.nuget.org/v3-flatcontainer/venel.assistantmcp/index.json");
        if (resp != null && resp.Versions != null) {
            Console.WriteLine("Found versions: " + resp.Versions.Length);
        } else {
            Console.WriteLine("Versions is NULL!");
        }
    }
}
