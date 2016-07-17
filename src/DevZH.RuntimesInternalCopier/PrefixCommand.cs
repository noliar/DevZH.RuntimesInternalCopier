using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DevZH.RuntimesInternalCopier
{
    public class PrefixCommand
    {
        public string Prefix { get; }

        public PrefixCommand(string prefix)
        {
            Prefix = prefix;
        }

        public int Run()
        {
            try
            {
                var packagesFolder = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

                if (string.IsNullOrEmpty(packagesFolder))
                {
                    packagesFolder = Path.Combine(GetHome(), ".nuget", "packages");
                }

                packagesFolder = Environment.ExpandEnvironmentVariables(packagesFolder);

                var lockJson = JObject.Parse(File.ReadAllText("project.lock.json"));

                foreach (var libuvLib in lockJson["libraries"].OfType<JProperty>().Where(
                    p => p.Name.StartsWith(Prefix, StringComparison.Ordinal)))
                {
                    foreach (var filePath in libuvLib.Value["files"].Select(v => v.Value<string>()))
                    {
                        if (filePath.ToString().StartsWith("runtimes/", StringComparison.Ordinal))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                            File.Copy(Path.Combine(packagesFolder, libuvLib.Name, filePath), filePath, overwrite: true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
            return 0;
        }

        // Copied from DNX's DnuEnvironment.cs
        private static string GetHome()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.GetEnvironmentVariable("USERPROFILE") ??
                    Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH");
            }
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME");

                if (string.IsNullOrEmpty(home))
                {
                    throw new Exception("Home directory not found. The HOME environment variable is not set.");
                }

                return home;
            }
        }
    }
}
