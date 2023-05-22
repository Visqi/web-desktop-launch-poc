using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;

namespace WebLoginPOC_Client
{
    public class Program
    {
        private const string app_protocol = "custompoc";
        
        static void Main(string[] args)
        {
            var exe = Assembly.GetExecutingAssembly().Location;
            exe = exe.Replace(".dll", ".exe");

            if (args.Length == 0)
            {
                using (var ident = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(ident);
                    if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = exe,
                            Arguments = string.Join(" ", args),
                            UseShellExecute = true,
                            Verb = "runas"
                        };

                        try
                        {
                            var proc = Process.Start(startInfo);
                            proc?.WaitForExit();
                            Environment.Exit(0);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("This program must be run as an administrator.");
                        }

                        Console.ReadLine();
                        return;
                    }
                }
            }

            string exePath = $"\"{exe}\" \"%1\"";


            if (args.Length == 0)
            {
                using (var key = Registry.ClassesRoot.OpenSubKey($"{app_protocol}\\shell\\open\\command"))
                {
                    if (key == null || key.GetValue(null).ToString() != exePath)
                    {
                        Registry.ClassesRoot.DeleteSubKeyTree(app_protocol, false);

                        using (var newKey = Registry.ClassesRoot.CreateSubKey(app_protocol))
                        {
                            newKey.SetValue("", "URL:Custom Web Login Protocol POC");
                            newKey.SetValue("URL Protocol", "");

                            using (var commandKey = newKey.CreateSubKey(@"shell\\open\\command"))
                            {
                                commandKey.SetValue("", exePath);
                            }

                            Console.WriteLine("Registry Created");
                        }
                    }
                }
            }

            if (args.Length > 0)
            {
                var uri = new Uri(args[0]);

                if (uri.Scheme == app_protocol && uri.Host == "launch")
                {
                    var token = uri.AbsolutePath.Trim('/');
                    Console.WriteLine($"Received POC Token: {token}");
                }
            }

            Console.ReadLine();
        }
    }
}