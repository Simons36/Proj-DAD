using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;



    public static class LauncherPaths{

        private static string _clientPath = "../Client/Client.csproj";

        private static string _transactionManagerPath = "../TransactionManager/TransactionManager.csproj";

        private static string _leaseManagerPath = "../LeaseManager/LeaseManager.csproj";

        public static string ClientPath{
            get => _clientPath;
        }

        public static string TransactionManagerPath{
            get => _transactionManagerPath;
        }

        public static string LeaseManagerPath{
            get => _leaseManagerPath;
        }
        
    }