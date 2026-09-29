import re
path = "Tools/AtlassianDispatcherTool.cs"
with open(path, "r") as f: content = f.read()

replacement = """    public async Task<string> DispatchAtlassianCommandAsync(
        [Description("The arguments for the Atlassian command")] AtlassianCommandArgs args,
        CancellationToken ct)
    {
        try {
            System.Console.Error.WriteLine("[AtlassianDispatcherTool] Entering DispatchAtlassianCommandAsync");
            return await DispatchAsync(args, ct);
        } catch (System.Exception ex) {
            System.Console.Error.WriteLine("[AtlassianDispatcherTool] Exception: " + ex.ToString());
            return "Fatal Error: " + ex.Message;
        }
    }"""

content = re.sub(r"public Task<string> DispatchAtlassianCommandAsync[\s\S]*?{\s*return DispatchAsync\(args, ct\);\s*}", replacement, content)

with open(path, "w") as f: f.write(content)
