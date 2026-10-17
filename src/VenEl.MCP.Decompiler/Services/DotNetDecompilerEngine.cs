using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using VenEl.MCP.Decompiler.Models;

namespace VenEl.MCP.Decompiler.Services;

public static class DotNetDecompilerEngine
{
    public static bool IsSupported(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".dll" or ".exe" or ".winmd";
    }

    public static Task<DecompileResult> DecompileAsync(string filePath, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var result = new DecompileResult
            {
                Path = filePath,
                FileName = Path.GetFileName(filePath),
                Format = ".NET Assembly (C#)"
            };

            try
            {
                if (!File.Exists(filePath))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"File not found: {filePath}";
                    return result;
                }

                // Verify it's a valid PE file and .NET assembly
                using (var stream = File.OpenRead(filePath))
                using (var peReader = new PEReader(stream))
                {
                    if (!peReader.HasMetadata)
                    {
                        result.IsSuccess = false;
                        result.Format = "Native PE Binary (Non-.NET)";
                        result.ErrorMessage = "File is a native Windows PE binary, not a managed .NET assembly.";
                        return result;
                    }

                    var headers = peReader.PEHeaders;
                    result.Architecture = headers.CoffHeader.Machine switch
                    {
                        Machine.I386 => "x86 (32-bit)",
                        Machine.Amd64 => "x64 (64-bit)",
                        Machine.Arm64 => "ARM64",
                        _ => headers.CoffHeader.Machine.ToString()
                    };

                    var metadata = peReader.GetMetadataReader();
                    if (metadata.IsAssembly)
                    {
                        var asmDef = metadata.GetAssemblyDefinition();
                        result.Version = SafeVersionString(asmDef.Version);
                        
                        // Extract target framework attribute if present
                        foreach (var attrHandle in asmDef.GetCustomAttributes())
                        {
                            var attr = metadata.GetCustomAttribute(attrHandle);
                            var attrType = attr.Constructor;
                            // Check attribute string
                            try
                            {
                                var value = attr.DecodeValue(new DummyStringProvider());
                                if (value.FixedArguments.Length > 0 && 
                                    value.FixedArguments[0].Value is string s && 
                                    s.StartsWith(".NET", StringComparison.OrdinalIgnoreCase))
                                {
                                    result.TargetFramework = s;
                                    break;
                                }
                            }
                            catch {}
                        }

                        // Collect referenced assemblies
                        foreach (var asmRefHandle in metadata.AssemblyReferences)
                        {
                            var asmRef = metadata.GetAssemblyReference(asmRefHandle);
                            var name = metadata.GetString(asmRef.Name);
                            var ver = SafeVersionString(asmRef.Version);
                            result.Dependencies.Add($"{name} (v{ver})");
                        }
                    }
                }

                // Decompile with ICSharpCode.Decompiler
                var settings = new DecompilerSettings(LanguageVersion.Latest)
                {
                    ThrowOnAssemblyResolveErrors = false,
                    ShowXmlDocumentation = true
                };

                var decompiler = new CSharpDecompiler(filePath, settings);

                // Build type outline
                var typeSystem = decompiler.TypeSystem;
                var rootModule = typeSystem.MainModule;

                var namespaceGroups = rootModule.TopLevelTypeDefinitions
                    .Where(t => !t.Name.StartsWith("<")) // exclude compiler-generated types
                    .GroupBy(t => string.IsNullOrEmpty(t.Namespace) ? "<Global>" : t.Namespace)
                    .OrderBy(g => g.Key);

                var totalTypesCount = 0;
                var totalMethodsCount = 0;

                foreach (var nsGroup in namespaceGroups)
                {
                    var nsNode = new TypeOutlineNode
                    {
                        Name = nsGroup.Key,
                        FullName = nsGroup.Key,
                        Kind = "namespace"
                    };

                    foreach (var typeDef in nsGroup.OrderBy(t => t.Name))
                    {
                        totalTypesCount++;
                        var typeNode = new TypeOutlineNode
                        {
                            Name = typeDef.Name,
                            FullName = typeDef.FullName,
                            Kind = typeDef.Kind switch
                            {
                                TypeKind.Interface => "interface",
                                TypeKind.Struct => "struct",
                                TypeKind.Enum => "enum",
                                _ => "class"
                            }
                        };

                        // Methods (skip accessors and compiler generated)
                        foreach (var method in typeDef.Methods.Where(m => !m.Name.StartsWith("<") && !m.IsAccessor).Take(50))
                        {
                            totalMethodsCount++;
                            typeNode.Children.Add(new TypeOutlineNode
                            {
                                Name = method.Name,
                                FullName = $"{typeDef.Name}.{method.Name}",
                                Kind = "method",
                                Signature = $"{method.ReturnType.Name} {method.Name}({string.Join(", ", method.Parameters.Select(p => $"{p.Type.Name} {p.Name}"))})"
                            });
                        }

                        // Properties
                        foreach (var prop in typeDef.Properties.Take(30))
                        {
                            typeNode.Children.Add(new TypeOutlineNode
                            {
                                Name = prop.Name,
                                FullName = $"{typeDef.Name}.{prop.Name}",
                                Kind = "property",
                                Signature = $"{prop.ReturnType.Name} {prop.Name} {{ get; set; }}"
                            });
                        }

                        nsNode.Children.Add(typeNode);
                    }

                    result.Outline.Add(nsNode);
                }

                // Decompile whole assembly/module to clean C# code
                try
                {
                    result.DecompiledCode = decompiler.DecompileWholeModuleAsString();
                }
                catch (Exception ex)
                {
                    result.DecompiledCode = $"// Decompilation notice: Full assembly view limited: {ex.Message}\n// Outline and type definitions were successfully extracted above.";
                }

                // Disassemble Intermediate Language (MSIL / CIL)
                try
                {
                    var ilOutput = new PlainTextOutput();
                    var rd = new ReflectionDisassembler(ilOutput, ct);
                    using var peStream = File.OpenRead(filePath);
                    using var peR = new PEReader(peStream);
                    var peFile = new PEFile(filePath, peR);
                    rd.WriteAssemblyHeader(peFile);
                    rd.WriteModuleContents(peFile);
                    result.IntermediateLanguageCode = ilOutput.ToString();
                }
                catch (Exception ex)
                {
                    result.IntermediateLanguageCode = $"// IL Disassembly notice: {ex.Message}";
                }

                result.CanExportProject = true;

                // Generate Deterministic Summary
                var tfSummary = !string.IsNullOrEmpty(result.TargetFramework) ? result.TargetFramework : "Managed .NET";
                var depSummary = result.Dependencies.Count > 0 
                    ? $"{result.Dependencies.Count} external reference(s): {string.Join(", ", result.Dependencies.Take(4).Select(d => d.Split(' ')[0]))}{(result.Dependencies.Count > 4 ? "..." : "")}"
                    : "No external assembly references";

                result.DeterministicSummary = $"{result.FileName} (v{result.Version ?? "1.0"}) targets {tfSummary} [{result.Architecture ?? "Any CPU"}]. Contains {totalTypesCount} type(s) and ~{totalMethodsCount} member(s) across {result.Outline.Count} namespace(s). {depSummary}.";

                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Decompilation error: {ex.Message}";
            }

            return result;
        }, ct);
    }

    public static Task<ExportProjectResult> ExportProjectAsync(string filePath, string? destinationDir = null, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var result = new ExportProjectResult();
            try
            {
                if (!File.Exists(filePath))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"File not found: {filePath}";
                    return result;
                }

                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                var parentDir = Path.GetDirectoryName(filePath) ?? Directory.GetCurrentDirectory();
                var targetDir = !string.IsNullOrWhiteSpace(destinationDir)
                    ? destinationDir
                    : Path.Combine(parentDir, $"{fileNameWithoutExt}_Decompiled");

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                using var peStream = File.OpenRead(filePath);
                using var peR = new PEReader(peStream);
                var peFile = new PEFile(filePath, peR);
                var targetFramework = peFile.DetectTargetFrameworkId();
                var resolver = new UniversalAssemblyResolver(filePath, false, targetFramework);
                resolver.AddSearchDirectory(parentDir);

                var decompiler = new WholeProjectDecompiler(resolver);
                decompiler.Settings.ThrowOnAssemblyResolveErrors = false;
                decompiler.DecompileProject(peFile, targetDir);

                var csprojFiles = Directory.GetFiles(targetDir, "*.csproj", SearchOption.AllDirectories);
                var allFiles = Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories);

                result.IsSuccess = true;
                result.ProjectDirectory = targetDir;
                result.ProjectFilePath = csprojFiles.FirstOrDefault();
                result.FilesWritten = allFiles.Length;
                result.GeneratedFiles = allFiles
                    .Select(f => Path.GetRelativePath(targetDir, f))
                    .OrderBy(f => f)
                    .Take(100)
                    .ToList();

                var projName = result.ProjectFilePath != null ? Path.GetFileName(result.ProjectFilePath) : $"{fileNameWithoutExt}.csproj";
                result.Summary = $"Reconstructed C# project for {Path.GetFileName(filePath)} in '{targetDir}'. Generated {allFiles.Length} file(s) including {projName}.";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Project export failed: {ex.Message}";
            }

            return result;
        }, ct);
    }

    private static string SafeVersionString(Version? ver)
    {
        if (ver == null) return "1.0";
        try
        {
            return ver.ToString();
        }
        catch
        {
            return $"{ver.Major}.{ver.Minor}";
        }
    }

    // Helper for CustomAttribute decoding
    private class DummyStringProvider : ICustomAttributeTypeProvider<string>
    {
        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
        public string GetSystemType() => "System.Type";
        public string GetSZArrayType(string elementType) => elementType + "[]";
        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => reader.GetString(reader.GetTypeDefinition(handle).Name);
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => reader.GetString(reader.GetTypeReference(handle).Name);
        public string GetTypeFromSerializedName(string name) => name;
        public PrimitiveTypeCode GetUnderlyingEnumType(string type) => PrimitiveTypeCode.Int32;
        public bool IsSystemType(string type) => type == "System.Type";
    }
}
