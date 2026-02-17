using LibGit2Sharp;

namespace DLSH.Core;

public class ShellGlobals
{
    internal static AliasService? AliasServiceProxy;
    internal static CommandDispatcher? DispatcherProxy;

    public Action<string>? OnInput { get; set; }
    public Action? Repeat { get; set; }

    public ShellGlobals(CommandDispatcher dispatcher, AliasService aliasService)
    {
        DispatcherProxy = dispatcher;
        AliasServiceProxy = aliasService;
    }

    public ShellGlobals() { }

    public static string GetUser() => Environment.UserName;
    public static string GetCurrentDir() => Directory.GetCurrentDirectory();
    public static string? GetVar(string name) => Environment.GetEnvironmentVariable(name);
    public static void SetVar(string name, string value) => Environment.SetEnvironmentVariable(name, value);
    public static void Print(object obj) => Console.WriteLine(obj);
    public static string Color(string text, int r, int g, int b) => $"\u001b[38;2;{r};{g};{b}m{text}\u001b[0m";
    public static string Bold(string text) => $"\u001b[1m{text}\u001b[0m";
    public static void AliasAdd(string name, string command) => AliasServiceProxy?.Add(name, command);
    public static void AliasRemove(string name) => AliasServiceProxy?.Remove(name);
    public static string? AliasGet(string name) => AliasServiceProxy?.Get(name);
    public static void AliasClear() => AliasServiceProxy?.Clear();
    public static void AliasList()
    {
        if (AliasServiceProxy == null) return;
        foreach (var alias in AliasServiceProxy.List())
            Console.WriteLine($"{alias.Key}={alias.Value}");
    }

    public static void Run(string command) => DispatcherProxy?.Dispatch(command);
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