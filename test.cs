using System;
using System.Reflection;
class Program {
  static void Main() {
    var entry = Assembly.LoadFrom("publish/VenEl.MCP.Server.dll");
    var attr = entry.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
    Console.WriteLine("Version: " + attr?.InformationalVersion);
  }
}
