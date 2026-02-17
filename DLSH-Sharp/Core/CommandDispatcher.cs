using System.Diagnostics;

namespace DLSH.Core;

public class CommandDispatcher(AliasService aliasService)
{
    private readonly AliasService _aliasService = aliasService;

    public void Dispatch(string line, int depth = 0)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        if (depth > 20)
        {
            Console.Error.WriteLine("Error: Alias recursion limit exceeded.");
            return;
        }

        var commands = line.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var cmdRaw in commands)
        {
            var parts = cmdRaw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            var cmd = parts[0];
            var args = parts.Skip(1).ToArray();

            if (_aliasService.Get(cmd) is string aliasVal)
            {
                var expanded = $"{aliasVal} {string.Join(" ", args)}";
                Dispatch(expanded, depth + 1);
                continue;
            }

            switch (cmd)
            {
                case "cd":
                    FileSystemService.ChangeDirectory(args);
                    break;
                case "ls":
                    FileSystemService.ListDirectory(args);
                    break;
                case "exit":
                    int code = 0;
                    if (args.Length > 0) _ = int.TryParse(args[0], out code);
                    Environment.Exit(code);
                    break;
                case "clr":
                case "clear":
                    Console.Clear();
                    break;
                case "var":
                    VariableService.HandleVarCommand(args);
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

    private static void HandlePrint(string[] args)
    {
        var output = args.Select(a => a.StartsWith('$')
            ? VariableService.Get(a[1..]) ?? "" : a);
        Console.WriteLine(string.Join(" ", output));
    }

    private void HandleAliasCLI(string[] args)
    {
        if (args.Length == 0 || args[0] == "list")
        {
            var aliases = _aliasService.List();
            if (aliases.Count == 0) Console.WriteLine("No aliases.");
            foreach (var kv in aliases) Console.WriteLine($"{kv.Key}={kv.Value}");
        }
        else if (args[0] == "add" && args.Length >= 2)
        {
            var fullDecl = string.Join(" ", args.Skip(1));
            var split = fullDecl.Split('=', 2);
            if (split.Length == 2)
            {
                var key = split[0].Trim();
                var val = split[1].Trim();
                _aliasService.Add(key, val);
                Console.WriteLine($"Alias added: '{key}' -> '{val}'");
            }
            else Console.Error.WriteLine("Usage: alias add name=value");
        }
        else if (args[0] == "del" && args.Length >= 2)
        {
            _aliasService.Remove(args[1]);
            Console.WriteLine($"Alias removed: {args[1]}");
        }
    }

    private static void ExecuteSystemCommand(string cmd, string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = cmd,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                UseShellExecute = false
            };
            foreach (var arg in args) psi.ArgumentList.Add(arg);

            using var p = Process.Start(psi);
            p?.WaitForExit();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            if (OperatingSystem.IsWindows())
            {
                var psiShell = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {cmd} {string.Join(" ", args)}",
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    UseShellExecute = false
                };
                try { Process.Start(psiShell)?.WaitForExit(); }
                catch { Console.Error.WriteLine($"Command not found: {cmd}"); }
            }
            else Console.Error.WriteLine($"Command not found: {cmd}");
        }
        catch (Exception ex) { Console.Error.WriteLine($"Error: {ex.Message}"); }
    }
}