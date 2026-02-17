using System.Diagnostics;

using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace DLSH.Core;

public static class CommandRunner
{
    private static readonly Dictionary<string, string> Aliases = [];

    public static void HandleCommand(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        var commands = line.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var cmdRaw in commands)
        {
            var parts = cmdRaw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            var cmd = parts[0];
            var args = parts.Skip(1).ToArray();

            if (Aliases.TryGetValue(cmd, out var aliasVal))
            {
                HandleCommand($"{aliasVal} {string.Join(" ", args)}");
                return;
            }

            switch (cmd)
            {
                case "cd":
                    ChangeDirectory(args);
                    break;
                case "exit":
                    HandleExit(args);
                    break;
                case "clr":
                    Console.Clear();
                    break;
                case "var":
                    HandleVar(args);
                    break;
                case "print":
                    HandlePrint(args);
                    break;
                case "alias":
                    HandleAliasCLI(args);
                    break;
                default:
                    ExecuteSystemCommand(cmd, args);
                    break;
            }
        }
    }

    public static void AddAlias(string name, string command) => Aliases[name] = command;
    public static void RemoveAlias(string name) => Aliases.Remove(name);
    public static string? GetAlias(string name) => Aliases.GetValueOrDefault(name);
    public static void ClearAliases() => Aliases.Clear();
    public static Dictionary<string, string> ListAliases() => new(Aliases);

    private static void HandleExit(string[] args)
    {
        int code = 0;
        if (args.Length > 0) _ = int.TryParse(args[0], out code);
        Environment.Exit(code);
    }

    private static void ChangeDirectory(string[] args)
    {
        var target = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("HOME") ?? "/";
        try { Directory.SetCurrentDirectory(target); }
        catch (Exception ex) { Console.Error.WriteLine($"cd: {ex.Message}"); }
    }

    private static void HandleVar(string[] args)
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
            var expression = string.Join(" ", args.Skip(3));
            try
            {
                var result = CSharpScript.EvaluateAsync(expression).Result;
                Environment.SetEnvironmentVariable(varName, result?.ToString());
            }
            catch (Exception e) { Console.Error.WriteLine($"Math error: {e.Message}"); }
        }
        else
        {
            var full = string.Join(" ", args);
            var parts = full.Split('=', 2);
            if (parts.Length == 2)
                Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
        }
    }

    private static void HandlePrint(string[] args)
    {
        var output = args.Select(a => a.StartsWith('$')
            ? Environment.GetEnvironmentVariable(a[1..]) ?? ""
            : a);
        Console.WriteLine(string.Join(" ", output));
    }

    private static void HandleAliasCLI(string[] args)
    {
        if (args.Length == 0 || args[0] == "list")
        {
            foreach (var kv in Aliases) Console.WriteLine($"{kv.Key}={kv.Value}");
        }
        else if (args[0] == "add" && args.Length >= 2)
        {
            var split = args[1].Split('=', 2);
            if (split.Length == 2) AddAlias(split[0], split[1]);
        }
        else if (args[0] == "del" && args.Length >= 2)
        {
            RemoveAlias(args[1]);
        }
    }

    private static void ExecuteSystemCommand(string cmd, string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = cmd,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                RedirectStandardInput = false
            };
            foreach (var arg in args) psi.ArgumentList.Add(arg);
            using var p = Process.Start(psi);
            p?.WaitForExit();
        }
        catch (Exception) { Console.Error.WriteLine($"Command not found: {cmd}"); }
    }
}