using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using src;

internal class Launcher
{
    private static void Main(string[] args){

        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        //Check if the user provided a config file
        if (args.Length != 1)
        {
            System.Console.WriteLine("Usage: dotnet run <config file>");
            return;
        }

        Console.WriteLine("Starting launcher application, reading from config file: " + args[0]);

        string configPath, ClientPath, TransactionManagerPath, LeaseManagerPath;
        ConfigReader configReader;

        //paths will be different depending on OS
        if(isWindows){
            configPath = "..\\..\\..\\config\\" + args[0];

            //Path for executables
            ClientPath = Directory.GetCurrentDirectory();
            TransactionManagerPath = Directory.GetCurrentDirectory();
            LeaseManagerPath = Directory.GetCurrentDirectory();

            ClientPath.Replace("Launcher", "Client");
            TransactionManagerPath.Replace("Launcher", "TransactionManager");
            LeaseManagerPath.Replace("Launcher", "LeaseManager");

            ClientPath += @"\Client.exe";
            TransactionManagerPath += @"\TransactionManager.exe";
            LeaseManagerPath += @"\LeaseManager.exe";

            string[] processesPaths = new string[3];
            processesPaths[0] = ClientPath; processesPaths[1] = TransactionManagerPath; processesPaths[2] = LeaseManagerPath;

            configReader = new ConfigReader(configPath, processesPaths);
        }
        else{
            configPath = "config/" + args[0];

            // Path to client, transaction manager and lease manager
            ClientPath = LauncherPaths.ClientPath;
            TransactionManagerPath = LauncherPaths.TransactionManagerPath;
            LeaseManagerPath = LauncherPaths.LeaseManagerPath;

            configReader = new ConfigReader(configPath);
        }

        //Read config, store arguments in ConfigReader class
        configReader.ReadConfig();

        if (isWindows)
        {
            StartProcessWindows(configReader.Processes);
        }
        else
        {
            //TODO: implement for linux
        }
       

    }

    private static void StartProcessLinux(string projectPath)
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

    private static void StartProcessWindows(List<ProcessStartInfo> processStartInfoList)
    {
        foreach (ProcessStartInfo processStartInfo in processStartInfoList)
        {
            Process p = new Process();
            p.StartInfo = processStartInfo;
            p.Start();
        }
    }
}