using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace DLSH.Core;

public class AliasService
{
    private readonly Dictionary<string, string> _aliases = [];

    public void Add(string name, string command) => _aliases[name] = command;
    public void Remove(string name) => _aliases.Remove(name);
    public string? Get(string name) => _aliases.GetValueOrDefault(name);
    public Dictionary<string, string> List() => new(_aliases);
    public void Clear() => _aliases.Clear();
}

public static class VariableService
{
    private static readonly ScriptOptions MathOptions = ScriptOptions.Default
        .WithImports("System", "System.Math");

    public static string? Get(string name) => Environment.GetEnvironmentVariable(name);

    public static void Set(string name, string value) => Environment.SetEnvironmentVariable(name, value ?? "");

    public static void HandleVarCommand(string[] args)
    {
        if (args.Length == 0)
        {
            foreach (System.Collections.DictionaryEntry env in Environment.GetEnvironmentVariables())
                Console.WriteLine($"{env.Key}={env.Value}");
            return;
        }

        if (args[0] == "del" && args.Length >= 2)
        {
            Environment.SetEnvironmentVariable(args[1], null);
            return;
        }

        if (args[0] == "match" && args.Length >= 4 && args[2] == "=")
        {
            var varName = args[1];
            var expression = string.Join(" ", args.Skip(3)).Trim();

            while (expression.EndsWith(';'))
                expression = expression[..^1].Trim();

            try
            {
                var result = CSharpScript.EvaluateAsync(expression, MathOptions).GetAwaiter().GetResult();
                Set(varName, result?.ToString() ?? "");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Math error in '{expression}': {e.Message}");
            }
        }
        else
        {
            var full = string.Join(" ", args);
            var parts = full.Split('=', 2);
            if (parts.Length == 2)
                Set(parts[0].Trim(), parts[1].Trim());
        }
    }
}

public static class FileSystemService
{
    public static void ChangeDirectory(string[] args)
    {
        var target = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("HOME") ??
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        try
        {
            target = Path.GetFullPath(target.Replace('/', Path.DirectorySeparatorChar));

            Directory.SetCurrentDirectory(target);
            Environment.SetEnvironmentVariable("PWD", target);
        }
        catch (Exception ex) { Console.Error.WriteLine($"cd: {ex.Message}"); }
    }

    public static void ListDirectory(string[] args)
    {
        var targetDir = args.FirstOrDefault(a => !a.StartsWith('-')) ?? ".";
        var di = new DirectoryInfo(targetDir);

        if (!di.Exists)
        {
            Console.Error.WriteLine($"ls: {targetDir}: No such directory");
            return;
        }

        bool showHidden = args.Any(a => a.Contains('a'));
        bool longFormat = args.Any(a => a.Contains('l'));

        var entries = di.GetFileSystemInfos();

        foreach (var entry in entries)
        {
            if (!showHidden && entry.Attributes.HasFlag(FileAttributes.Hidden))
                continue;

            if (longFormat)
            {
                string type = entry is DirectoryInfo ? "d" : "-";
                string lastWrite = entry.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
                Console.WriteLine($"{type}  {lastWrite}  {entry.Name}");
            }
            else
            {
                Console.Write($"{entry.Name}  ");
            }
        }

        if (!longFormat) Console.WriteLine();
    }
}