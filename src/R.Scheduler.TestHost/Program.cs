﻿using R.Scheduler.Interfaces;

namespace R.Scheduler.TestHost
{
    class Program
    {
        static void Main(string[] args)
        {
            R.Scheduler.Scheduler.Initialize(config =>
            {
                config.EnableWebApiSelfHost = true;
                config.PersistenceStoreType = PersistenceStoreType.Postgre;
                config.ConnectionString = "Server=127.0.0.1;Port=5432;Database=Scheduler;User Id=postgres;Password=postgres;";
                //config.CustomFtpLibraryAssemblyName = "R.Scheduler.FakeFtpLib";
                config.CustomAuthorizationAssemblyName = "R.Scheduler.TestCustomAuthorizationImp";
                config.CustomWebAppSettingsAssemblyName = "R.Scheduler.TestCustomAuthorizationImp";
            });
        }
    }
}
