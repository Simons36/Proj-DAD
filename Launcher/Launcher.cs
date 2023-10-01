using System.Diagnostics;
using src;

internal class Launcher
{
    private static void Main(string[] args){

        // Check if the user provided a config file
        if(args.Length != 1){
            System.Console.WriteLine("Usage: dotnet run <config file>");
            return;
        }

        Console.WriteLine("Starting launcher application, reading from config file: " + args[0]);

        String configPath = "config/" + args[0];

        // Path to client, transaction manager and lease manager
        string ClientPath = LauncherPaths.ClientPath;
        string TransactionManagerPath = LauncherPaths.TransactionManagerPath;
        string LeaseManagerPath = LauncherPaths.LeaseManagerPath;
        
        ConfigReader configReader = new ConfigReader(configPath);
        configReader.ReadConfig();
    }

    private static void StartProcess(string projectPath)
    {
        Process p = new Process(){

            StartInfo = new ProcessStartInfo{
                FileName = "gnome-terminal",
                Arguments = $"-- dotnet run --project {projectPath}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }

        };

        p.Start();

        p.WaitForExit();
    }
}