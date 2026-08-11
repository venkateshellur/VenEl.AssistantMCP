using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Server;

class Program {
    static void Main() {
        var methods = typeof(IMcpServer).GetMethods();
        foreach (var m in methods) {
            Console.WriteLine($"{m.Name}({string.Join(", ", Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name))})");
        }
    }
}
