// See https://aka.ms/new-console-template for more information

using DigitalWorkstation.Launcher.Utils;

Logger.Information("Launcher", $"Launcher startup.");
AssemblyLoader.Initialize();

DigitalWorkstation.Common.AssemblyLoader.Initialize(@".\Config\AssemblyPath.json");