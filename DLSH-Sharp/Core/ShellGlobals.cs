using LibGit2Sharp;

namespace DLSH.Core;

public class ShellGlobals
{
    public Action<string>? OnInput { get; set; }
    public Action? Repeat { get; set; }

    public static string GetUser() => Environment.UserName;
    public static string GetCurrentDir() => Directory.GetCurrentDirectory();

    public static string? GetVar(string name) => Environment.GetEnvironmentVariable(name);
    public static void SetVar(string name, string value) => Environment.SetEnvironmentVariable(name, value);

    public static void Print(object obj) => Console.WriteLine(obj);
    public static string Color(string text, int r, int g, int b) => $"\u001b[38;2;{r};{g};{b}m{text}\u001b[0m";
    public static string Bold(string text) => $"\u001b[1m{text}\u001b[0m";

    public static void AliasAdd(string name, string command) => CommandRunner.AddAlias(name, command);
    public static void AliasRemove(string name) => CommandRunner.RemoveAlias(name);
    public static string? AliasGet(string name) => CommandRunner.GetAlias(name);
    public static void AliasClear() => CommandRunner.ClearAliases();
    public static void AliasList()
    {
        foreach (var alias in CommandRunner.ListAliases())
            Console.WriteLine($"{alias.Key}={alias.Value}");
    }

    public static bool IsGitRepo() => Repository.IsValid(GetCurrentDir());

    public static string GetGitBranch()
    {
        if (!IsGitRepo()) return "";
        try
        {
            using var repo = new Repository(GetCurrentDir());
            return repo.Head.FriendlyName;
        }
        catch { return ""; }
    }

    public static bool GitIsDirty()
    {
        if (!IsGitRepo()) return false;
        try
        {
            using var repo = new Repository(GetCurrentDir());
            return repo.RetrieveStatus().IsDirty;
        }
        catch { return false; }
    }

    public static (int, int) GitAheadBehind()
    {
        if (!IsGitRepo()) return (0, 0);
        try
        {
            using var repo = new Repository(GetCurrentDir());
            var head = repo.Head;
            var tracking = head.TrackingDetails;
            return (tracking.AheadBy ?? 0, tracking.BehindBy ?? 0);
        }
        catch { return (0, 0); }
    }

    public static void Run(string command) => CommandRunner.HandleCommand(command);

    public void LoadPlugin(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                var script = File.ReadAllText(path);
                Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.EvaluateAsync(
                    script,
                    Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default
                       .WithReferences(this.GetType().Assembly)
                       .WithImports("System", "DLSH.Core"),
                    globals: this
                ).Wait();
            }
            catch (Exception e) { Console.Error.WriteLine($"Plugin load error: {e.Message}"); }
        }
    }
}