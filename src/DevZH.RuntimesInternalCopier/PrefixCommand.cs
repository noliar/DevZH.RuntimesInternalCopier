using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;

namespace DevZH.RuntimesInternalCopier
{
    public class PrefixCommand
    {
        public string Prefix { get; }

        public bool EnablePack { get; }

        public List<string> RemainArguments { get; private set; }

        public PrefixCommand(string prefix, bool enablePack, List<string> remainArguments)
        {
            Prefix = prefix;
            EnablePack = enablePack;
            RemainArguments = remainArguments;
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

                var set = false;

                var versionSuffix = string.Empty;

                foreach (var lib in lockJson["libraries"].OfType<JProperty>().Where(
                    p => p.Name.StartsWith(Prefix, StringComparison.Ordinal)))
                {
                    foreach (var filePath in lib.Value["files"].Select(v => v.Value<string>()))
                    {
                        if (filePath.ToString().StartsWith("runtimes/", StringComparison.Ordinal))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                            File.Copy(Path.Combine(packagesFolder, lib.Name, filePath), filePath, overwrite: true);
                        }
                    }
                    if (EnablePack && !set)
                    {
                        if (!RemainArguments.Contains("--version-suffix"))
                        {
                            if (string.IsNullOrEmpty(versionSuffix))
                            {
                                var array = lib.Name.Split('/');
                                if (array.Length > 1)
                                {
                                    var version = array[1];
                                    array = version.Split(new[] { '-' }, 2);
                                    if (array.Length > 1)
                                    {
                                        versionSuffix = array[1];
                                    }
                                }
                            }
                        }
                        
                        
                        set = true;
                    }
                }
                if (EnablePack)
                {
                    Console.WriteLine("Copying the runtimes completed successfully, now you will pack the package");
                    var args = RemainArguments;
                    //IEnumerable<string> args = new []{ "--configuration", "Release" };
                    if (!string.IsNullOrEmpty(versionSuffix))
                    {
                        args.AddRange(new List<string>
                        {
                            "--version-suffix",
                            versionSuffix
                        });
                    }

                    var command = Command.CreateDotNet("pack", args, null,
                        null);
                    var result = command.Execute();
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
