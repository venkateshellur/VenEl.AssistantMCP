using System;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

var asm = typeof(McpServerBuilderExtensions).Assembly;
foreach(var m in asm.GetTypes().SelectMany(t => t.GetMethods()).Where(m => m.Name == "WithResources" || m.Name == "WithResource")) {
    var p = m.IsGenericMethod ? string.Join(", ", m.GetGenericArguments().Select(a => a.Name)) : "non-generic";
    Console.WriteLine(m.Name + " <" + p + "> => " + string.Join(", ", m.GetParameters().Select(param => param.ParameterType.Name)));
}
