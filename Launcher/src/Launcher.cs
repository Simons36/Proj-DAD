using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using src;
using src.util;
using src.ConfigReader;

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
            configPath = @"..\..\..\config\" + args[0];

            //Path for executables
            ClientPath = Directory.GetCurrentDirectory();
            TransactionManagerPath = Directory.GetCurrentDirectory();
            LeaseManagerPath = Directory.GetCurrentDirectory();

            ClientPath = ClientPath.Replace("Launcher", "Client");
            TransactionManagerPath = TransactionManagerPath.Replace("Launcher", "TransactionManager");
            LeaseManagerPath = LeaseManagerPath.Replace("Launcher", "LeaseManager");

            ClientPath += @"\Client.exe";
            TransactionManagerPath += @"\TransactionManager.exe";
            LeaseManagerPath += @"\LeaseManager.exe";

            string[] processesPaths = new string[3];
            processesPaths[0] = ClientPath; processesPaths[1] = TransactionManagerPath; processesPaths[2] = LeaseManagerPath;


            //configReader = new ConfigReaderLinux(configPath, processesPaths);
            configReader = new ConfigReader(configPath, processesPaths);
        }
        else{
            configPath = "config/" + args[0];

            configReader = new ConfigReader(configPath);
        }

        configReader.ReadConfig();
        //Read config, store arguments in ConfigReader class

        if (isWindows)
        {
            src.util.ProcessStart.StartProcessWindows(configReader.Processes);
        }
        else
        {
            src.util.ProcessStart.StartProcessLinux(configReader.Processes);
        }
       

    }

    
}