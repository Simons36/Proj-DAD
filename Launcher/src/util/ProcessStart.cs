using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace src.util
{
    public static class ProcessStart
    {
        public static void StartProcessLinux(List<ProcessStartInfo> processStartInfos){
            
            foreach (ProcessStartInfo processStartInfo in processStartInfos)
            {
                Console.WriteLine(processStartInfo.Arguments);
                Process p = new Process();
                p.StartInfo = processStartInfo;
                p.Start();
            }
        }

        public static void StartProcessWindows(List<ProcessStartInfo> processStartInfoList){

            foreach (ProcessStartInfo processStartInfo in processStartInfoList)
            {
                Process p = new Process();
                p.StartInfo = processStartInfo;
                p.Start();
            }
        }
    }
}