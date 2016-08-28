using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;

namespace DevZH.RuntimesInternalCopier
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication(false)
            {
                Name = "dotnet copy-runtimes",
                FullName = ".NET Core Native Runtimes Copier",
                Description = "A tool to copy the native runtimes",
            };
            app.HelpOption("-h|--help");

            var prefixOption = app.Option("-p|--prefix", "prefix of the internal packages", CommandOptionType.SingleValue);

            var enablePackOption = app.Option("--enable-pack",
                "after finishing copy libraries, pack the package using Release configuration",
                CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                var prefix = prefixOption.Value();

                var enablePack = enablePackOption.HasValue();

                if (string.IsNullOrEmpty(prefix))
                {
                    app.ShowHelp();
                    return 2;
                }

                Console.WriteLine("Copy the runtimes directory of internal package to current project");

                var exitCode = new PrefixCommand(prefix, enablePack, app.RemainingArguments).Run();

                Console.WriteLine("Completed successfully.");

                return exitCode;
            });

            try
            {
                return app.Execute(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.ToString());
            }

            return 1;
        }

        
    }
}
