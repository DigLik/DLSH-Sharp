using DLSH.Core;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

var aliasService = new AliasService();
var dispatcher = new CommandDispatcher(aliasService);

var globals = new ShellGlobals(dispatcher, aliasService);

var home = Environment.GetEnvironmentVariable("HOME")
           ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var configPath = Path.Combine(home, ".dlshrc.csx");
var historyPath = Path.Combine(home, ".dlsh_history");

var scriptOptions = ScriptOptions.Default
    .WithImports("System", "System.IO", "System.Linq", "DLSH.Core")
    .WithReferences(typeof(ShellGlobals).Assembly);

if (File.Exists(configPath))
{
    try
    {
        var code = File.ReadAllText(configPath);
        await CSharpScript.EvaluateAsync(code, scriptOptions, globals: globals);
    }
    catch (CompilationErrorException e)
    {
        Console.Error.WriteLine($"Error in .dlshrc.csx: {string.Join(Environment.NewLine, e.Diagnostics)}");
    }
}
else
{
    Environment.SetEnvironmentVariable("PS1", "DLSH >> ");
}

if (File.Exists(historyPath))
{
    ReadLine.ClearHistory();
    ReadLine.AddHistory(File.ReadAllLines(historyPath));
}

Console.WriteLine("Welcome to DLSH-Sharp (Modular Edition)");

while (true)
{
    try { globals.Repeat?.Invoke(); }
    catch (Exception ex) { Console.Error.WriteLine($"Error in Repeat hook: {ex.Message}"); }

    var ps1 = Environment.GetEnvironmentVariable("PS1") ?? "$ ";
    string input = ReadLine.Read(ps1);

    if (!string.IsNullOrWhiteSpace(input))
    {
        var history = ReadLine.GetHistory();
        if (history.Count == 0 || history[^1] != input)
            ReadLine.AddHistory(input);
    }

    bool handledByHook = false;
    if (globals.OnInput != null)
    {
        try
        {
            globals.OnInput.Invoke(input);
            handledByHook = true;
        }
        catch (Exception ex) { Console.Error.WriteLine($"Error in OnInput hook: {ex.Message}"); }
    }

    if (!handledByHook)
        dispatcher.Dispatch(input);

    File.WriteAllLines(historyPath, ReadLine.GetHistory());
}