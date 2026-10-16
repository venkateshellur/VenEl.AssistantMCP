using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VenEl.MCP.Decompiler.Models;

namespace VenEl.MCP.Decompiler.Services;

public static class JavaDecompilerEngine
{
    public static bool IsSupported(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".class" or ".jar" or ".war";
    }

    public static async Task<DecompileResult> DecompileAsync(string filePath, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext == ".jar" || ext == ".war")
        {
            return await DecompileJarAsync(filePath, ct);
        }

        return await DecompileClassAsync(filePath, ct);
    }

    private static async Task<DecompileResult> DecompileClassAsync(string filePath, CancellationToken ct)
    {
        var result = new DecompileResult
        {
            Path = filePath,
            FileName = Path.GetFileName(filePath),
            Format = "Java Class (.class)"
        };

        try
        {
            if (!File.Exists(filePath))
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"File not found: {filePath}";
                return result;
            }

            var bytes = await File.ReadAllBytesAsync(filePath, ct);
            if (bytes.Length < 10)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "File is too small to be a valid Java class.";
                return result;
            }

            // Check Java Magic: 0xCAFEBABE (big endian)
            if (bytes[0] != 0xCA || bytes[1] != 0xFE || bytes[2] != 0xBA || bytes[3] != 0xBE)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Invalid Java class magic header (expected 0xCAFEBABE).";
                return result;
            }

            var minorVersion = (bytes[4] << 8) | bytes[5];
            var majorVersion = (bytes[6] << 8) | bytes[7];
            result.TargetFramework = MapJavaVersion(majorVersion);
            result.Architecture = "JVM Bytecode";

            // Parse Constant Pool
            var cpResult = ParseConstantPool(bytes);
            var className = cpResult.ClassName.Replace('/', '.');
            var superName = cpResult.SuperClassName.Replace('/', '.');
            var packageName = className.Contains('.') ? className.Substring(0, className.LastIndexOf('.')) : "<default>";
            var simpleClassName = className.Contains('.') ? className.Substring(className.LastIndexOf('.') + 1) : className;

            var nsNode = new TypeOutlineNode
            {
                Name = packageName,
                FullName = packageName,
                Kind = "namespace"
            };

            var classNode = new TypeOutlineNode
            {
                Name = simpleClassName,
                FullName = className,
                Kind = cpResult.IsInterface ? "interface" : (cpResult.IsEnum ? "enum" : "class"),
                Signature = $"public {simpleClassName} extends {superName}"
            };

            foreach (var field in cpResult.Fields)
            {
                classNode.Children.Add(new TypeOutlineNode
                {
                    Name = field.Name,
                    FullName = $"{simpleClassName}.{field.Name}",
                    Kind = "field",
                    Signature = $"{DecodeDescriptor(field.Descriptor)} {field.Name}"
                });
            }

            foreach (var method in cpResult.Methods)
            {
                var sig = DecodeMethodDescriptor(method.Name, method.Descriptor);
                classNode.Children.Add(new TypeOutlineNode
                {
                    Name = method.Name,
                    FullName = $"{simpleClassName}.{method.Name}",
                    Kind = "method",
                    Signature = sig
                });
            }

            nsNode.Children.Add(classNode);
            result.Outline.Add(nsNode);

            // Attempt decompilation / disassembly with 'javap' if available on system
            var javapOutput = await TryRunJavapAsync(filePath, ct);
            if (!string.IsNullOrWhiteSpace(javapOutput))
            {
                result.DecompiledCode = javapOutput;
            }
            else
            {
                // Generate clean Java structural pseudo-code from parsed metadata
                var sb = new StringBuilder();
                sb.AppendLine($"// Decompiled structure from {result.FileName}");
                sb.AppendLine($"// Compiled with Java version: {result.TargetFramework} (class version {majorVersion}.{minorVersion})");
                sb.AppendLine($"package {packageName};");
                sb.AppendLine();
                sb.AppendLine($"public class {simpleClassName} extends {superName} {{");
                foreach (var field in cpResult.Fields)
                {
                    sb.AppendLine($"    public {DecodeDescriptor(field.Descriptor)} {field.Name};");
                }
                sb.AppendLine();
                foreach (var method in cpResult.Methods)
                {
                    sb.AppendLine($"    public {DecodeMethodDescriptor(method.Name, method.Descriptor)} {{ /* compiled code */ }}");
                }
                sb.AppendLine("}");
                result.DecompiledCode = sb.ToString();
            }

            result.DeterministicSummary = $"{result.FileName} compiled for {result.TargetFramework}. Class '{className}' defines {cpResult.Fields.Count} field(s) and {cpResult.Methods.Count} method(s).";
            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"Error decompiling Java class: {ex.Message}";
        }

        return result;
    }

    private static async Task<DecompileResult> DecompileJarAsync(string filePath, CancellationToken ct)
    {
        var result = new DecompileResult
        {
            Path = filePath,
            FileName = Path.GetFileName(filePath),
            Format = "Java Archive (.jar)"
        };

        try
        {
            if (!File.Exists(filePath))
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"File not found: {filePath}";
                return result;
            }

            using var archive = ZipFile.OpenRead(filePath);
            var manifestEntry = archive.GetEntry("META-INF/MANIFEST.MF");
            var manifestProps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (manifestEntry != null)
            {
                using var reader = new StreamReader(manifestEntry.Open(), Encoding.UTF8);
                string? line;
                while ((line = await reader.ReadLineAsync(ct)) != null)
                {
                    var idx = line.IndexOf(':');
                    if (idx > 0)
                    {
                        var key = line.Substring(0, idx).Trim();
                        var val = line.Substring(idx + 1).Trim();
                        manifestProps[key] = val;
                    }
                }
            }

            if (manifestProps.TryGetValue("Main-Class", out var mainClass))
            {
                result.Version = $"Main-Class: {mainClass}";
            }

            if (manifestProps.TryGetValue("Build-Jdk", out var buildJdk))
            {
                result.TargetFramework = $"Java {buildJdk}";
            }
            else if (manifestProps.TryGetValue("Created-By", out var createdBy))
            {
                result.TargetFramework = createdBy;
            }
            else
            {
                result.TargetFramework = "Java Archive";
            }

            // Enumerate classes in JAR
            var classEntries = archive.Entries
                .Where(e => e.FullName.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var packages = new Dictionary<string, List<string>>();
            foreach (var entry in classEntries)
            {
                var full = entry.FullName.Substring(0, entry.FullName.Length - 6).Replace('/', '.');
                var pkg = full.Contains('.') ? full.Substring(0, full.LastIndexOf('.')) : "<default>";
                var cls = full.Contains('.') ? full.Substring(full.LastIndexOf('.') + 1) : full;

                if (!packages.ContainsKey(pkg)) packages[pkg] = new List<string>();
                packages[pkg].Add(cls);
            }

            foreach (var kvp in packages.OrderBy(p => p.Key).Take(100))
            {
                var nsNode = new TypeOutlineNode
                {
                    Name = kvp.Key,
                    FullName = kvp.Key,
                    Kind = "namespace"
                };

                foreach (var cls in kvp.Value.Take(50))
                {
                    nsNode.Children.Add(new TypeOutlineNode
                    {
                        Name = cls,
                        FullName = $"{kvp.Key}.{cls}",
                        Kind = "class"
                    });
                }

                result.Outline.Add(nsNode);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"// Java Archive (JAR) Inspection: {result.FileName}");
            sb.AppendLine($"// Total classes contained: {classEntries.Count}");
            sb.AppendLine($"// Total packages: {packages.Count}");
            if (manifestProps.Count > 0)
            {
                sb.AppendLine("// --- Manifest Attributes ---");
                foreach (var kvp in manifestProps)
                {
                    sb.AppendLine($"// {kvp.Key}: {kvp.Value}");
                }
            }
            sb.AppendLine();
            sb.AppendLine("// Packages and Types:");
            foreach (var kvp in packages.Take(30))
            {
                sb.AppendLine($"package {kvp.Key} {{");
                foreach (var cls in kvp.Value.Take(15))
                {
                    sb.AppendLine($"    class {cls};");
                }
                if (kvp.Value.Count > 15) sb.AppendLine($"    // ... +{kvp.Value.Count - 15} more classes");
                sb.AppendLine("}");
            }

            result.DecompiledCode = sb.ToString();
            result.DeterministicSummary = $"JAR package '{result.FileName}' containing {classEntries.Count} class(es) across {packages.Count} package(s). {(manifestProps.TryGetValue("Main-Class", out var mc) ? $"Entry point: {mc}." : "Library package.")}";
            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"Error inspecting JAR: {ex.Message}";
        }

        return result;
    }

    private static string MapJavaVersion(int majorVersion) => majorVersion switch
    {
        45 => "Java 1.1",
        46 => "Java 1.2",
        47 => "Java 1.3",
        48 => "Java 1.4",
        49 => "Java 5",
        50 => "Java 6",
        51 => "Java 7",
        52 => "Java 8",
        53 => "Java 9",
        54 => "Java 10",
        55 => "Java 11 (LTS)",
        56 => "Java 12",
        57 => "Java 13",
        58 => "Java 14",
        59 => "Java 15",
        60 => "Java 16",
        61 => "Java 17 (LTS)",
        62 => "Java 18",
        63 => "Java 19",
        64 => "Java 20",
        65 => "Java 21 (LTS)",
        66 => "Java 22",
        67 => "Java 23",
        68 => "Java 24",
        _ => $"Java (Class ver {majorVersion})"
    };

    private record ParsedMember(string Name, string Descriptor);
    private class ConstantPoolResult
    {
        public string ClassName { get; set; } = "Unknown";
        public string SuperClassName { get; set; } = "java.lang.Object";
        public bool IsInterface { get; set; }
        public bool IsEnum { get; set; }
        public List<ParsedMember> Fields { get; set; } = new();
        public List<ParsedMember> Methods { get; set; } = new();
    }

    private static ConstantPoolResult ParseConstantPool(byte[] bytes)
    {
        var result = new ConstantPoolResult();
        int offset = 8;
        if (offset + 2 > bytes.Length) return result;

        int cpCount = (bytes[offset++] << 8) | bytes[offset++];
        var strings = new Dictionary<int, string>();
        var classIndices = new Dictionary<int, int>(); // cpIndex -> nameIndex

        for (int i = 1; i < cpCount; i++)
        {
            if (offset >= bytes.Length) break;
            int tag = bytes[offset++];
            switch (tag)
            {
                case 1: // Utf8
                    int len = (bytes[offset++] << 8) | bytes[offset++];
                    if (offset + len <= bytes.Length)
                    {
                        strings[i] = Encoding.UTF8.GetString(bytes, offset, len);
                        offset += len;
                    }
                    break;
                case 7: // Class
                    classIndices[i] = (bytes[offset++] << 8) | bytes[offset++];
                    break;
                case 3: // Integer
                case 4: // Float
                case 9: // Fieldref
                case 10: // Methodref
                case 11: // InterfaceMethodref
                case 12: // NameAndType
                case 18: // InvokeDynamic
                    offset += 4;
                    break;
                case 5: // Long
                case 6: // Double
                    offset += 8;
                    i++; // Takes 2 entries
                    break;
                case 8: // String
                case 16: // MethodType
                case 19: // Module
                case 20: // Package
                    offset += 2;
                    break;
                case 15: // MethodHandle
                    offset += 3;
                    break;
                default:
                    return result; // Unknown tag, return best effort
            }
        }

        if (offset + 6 <= bytes.Length)
        {
            int accessFlags = (bytes[offset++] << 8) | bytes[offset++];
            result.IsInterface = (accessFlags & 0x0200) != 0;
            result.IsEnum = (accessFlags & 0x4000) != 0;

            int thisClassIdx = (bytes[offset++] << 8) | bytes[offset++];
            if (classIndices.TryGetValue(thisClassIdx, out var nameIdx) && strings.TryGetValue(nameIdx, out var clsName))
            {
                result.ClassName = clsName;
            }

            int superClassIdx = (bytes[offset++] << 8) | bytes[offset++];
            if (classIndices.TryGetValue(superClassIdx, out var superNameIdx) && strings.TryGetValue(superNameIdx, out var sName))
            {
                result.SuperClassName = sName;
            }

            // Interfaces count
            if (offset + 2 <= bytes.Length)
            {
                int interfacesCount = (bytes[offset++] << 8) | bytes[offset++];
                offset += interfacesCount * 2;
            }

            // Fields count
            if (offset + 2 <= bytes.Length)
            {
                int fieldsCount = (bytes[offset++] << 8) | bytes[offset++];
                for (int f = 0; f < fieldsCount && offset + 8 <= bytes.Length; f++)
                {
                    offset += 2; // access flags
                    int nameId = (bytes[offset++] << 8) | bytes[offset++];
                    int descId = (bytes[offset++] << 8) | bytes[offset++];
                    int attrCount = (bytes[offset++] << 8) | bytes[offset++];

                    var fName = strings.TryGetValue(nameId, out var fn) ? fn : $"field_{f}";
                    var fDesc = strings.TryGetValue(descId, out var fd) ? fd : "Ljava/lang/Object;";
                    result.Fields.Add(new ParsedMember(fName, fDesc));

                    for (int a = 0; a < attrCount && offset + 6 <= bytes.Length; a++)
                    {
                        offset += 2;
                        int aLen = (bytes[offset++] << 24) | (bytes[offset++] << 16) | (bytes[offset++] << 8) | bytes[offset++];
                        offset += aLen;
                    }
                }
            }

            // Methods count
            if (offset + 2 <= bytes.Length)
            {
                int methodsCount = (bytes[offset++] << 8) | bytes[offset++];
                for (int m = 0; m < methodsCount && offset + 8 <= bytes.Length; m++)
                {
                    offset += 2; // access flags
                    int nameId = (bytes[offset++] << 8) | bytes[offset++];
                    int descId = (bytes[offset++] << 8) | bytes[offset++];
                    int attrCount = (bytes[offset++] << 8) | bytes[offset++];

                    var mName = strings.TryGetValue(nameId, out var mn) ? mn : $"method_{m}";
                    var mDesc = strings.TryGetValue(descId, out var md) ? md : "()V";
                    result.Methods.Add(new ParsedMember(mName, mDesc));

                    for (int a = 0; a < attrCount && offset + 6 <= bytes.Length; a++)
                    {
                        offset += 2;
                        int aLen = (bytes[offset++] << 24) | (bytes[offset++] << 16) | (bytes[offset++] << 8) | bytes[offset++];
                        offset += aLen;
                    }
                }
            }
        }

        return result;
    }

    private static string DecodeDescriptor(string desc)
    {
        if (string.IsNullOrEmpty(desc)) return "void";
        if (desc.StartsWith("[")) return DecodeDescriptor(desc.Substring(1)) + "[]";
        return desc switch
        {
            "Z" => "boolean",
            "B" => "byte",
            "C" => "char",
            "S" => "short",
            "I" => "int",
            "J" => "long",
            "F" => "float",
            "D" => "double",
            "V" => "void",
            _ when desc.StartsWith("L") && desc.EndsWith(";") => desc.Substring(1, desc.Length - 2).Replace('/', '.'),
            _ => desc
        };
    }

    private static string DecodeMethodDescriptor(string methodName, string desc)
    {
        if (string.IsNullOrEmpty(desc) || !desc.StartsWith("(")) return $"{methodName}()";
        int closeParen = desc.IndexOf(')');
        if (closeParen < 0) return $"{methodName}()";

        string paramsStr = desc.Substring(1, closeParen - 1);
        string returnTypeStr = desc.Substring(closeParen + 1);
        string retType = DecodeDescriptor(returnTypeStr);

        var paramTypes = new List<string>();
        int i = 0;
        while (i < paramsStr.Length)
        {
            int start = i;
            while (i < paramsStr.Length && paramsStr[i] == '[') i++;
            if (i < paramsStr.Length)
            {
                if (paramsStr[i] == 'L')
                {
                    int semi = paramsStr.IndexOf(';', i);
                    if (semi >= 0) i = semi + 1;
                    else i = paramsStr.Length;
                }
                else
                {
                    i++;
                }
            }
            paramTypes.Add(DecodeDescriptor(paramsStr.Substring(start, i - start)));
        }

        return $"{retType} {methodName}({string.Join(", ", paramTypes.Select((p, idx) => $"{p} arg{idx}"))})";
    }

    private static async Task<string?> TryRunJavapAsync(string classFilePath, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "javap",
                Arguments = $"-p -c \"{classFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return null;

            var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
            var waitTask = proc.WaitForExitAsync(ct);

            if (await Task.WhenAny(waitTask, Task.Delay(2000, ct)) == waitTask && proc.ExitCode == 0)
            {
                return await stdoutTask;
            }

            try { proc.Kill(); } catch {}
            return null;
        }
        catch
        {
            return null;
        }
    }
}
