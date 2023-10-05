using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SilentStarter
{
    internal class Program
    {
        enum LauncherType
        {
            PYTHON,
            BATCH,
            POWERSHELL,
            UNKNOWN
        }
        static void Main(string[] args)
        {
            string logFile = Process.GetCurrentProcess().MainModule.FileName.Replace(".exe", ".log");
            bool createLog = false;
            int delaySeconds = 0;
            string[] scriptParams = null;
            string scriptCommand;
            int scriptParamCount;
            if (args[0].StartsWith("/"))
            {
                string ssParams = args[0].ToUpper();
                if (ssParams.Contains("L")) createLog = true;
                if (ssParams.Contains("D"))
                {
                    try
                    {
                        delaySeconds = Int32.Parse(ssParams.Substring(ssParams.IndexOf("D") + 1));
                    }
                    catch (FormatException)
                    {
                        File.AppendAllText(logFile, $"ERROR: Unable to parse delay parameter: {delaySeconds}{Environment.NewLine}");
                        return;
                    }
                }
                scriptCommand = args[1].ToUpper();
                scriptParamCount = args.Length - 2;

                if (scriptParamCount > 0)
                {
                    scriptParams = new string[scriptParamCount];
                    for (int i = args.Length - scriptParamCount, j = 0; i < args.Length; i++)
                    {
                        scriptParams[j] = args[i];
                        j++;
                    }
                }
            }
            else
            {
                scriptCommand = args[0].ToUpper();
                scriptParamCount = args.Length - 1;

                if (scriptParamCount > 0)
                {
                    scriptParams = new string[scriptParamCount];
                    for (int i = args.Length - scriptParamCount, j = 0; i < args.Length; i++)
                    {
                        scriptParams[j] = args[i];
                        j++;
                    }
                }
            }

            if (!scriptCommand.StartsWith("\"")) scriptCommand = "\"" + scriptCommand;
            if (!scriptCommand.EndsWith("\"")) scriptCommand += "\"";

            LauncherType shellType = LauncherType.UNKNOWN;
            if (scriptCommand.EndsWith(".PY") || scriptCommand.EndsWith(".PY\"")) shellType = LauncherType.PYTHON;
            else if (scriptCommand.EndsWith(".CMD") || scriptCommand.EndsWith(".CMD\"") || scriptCommand.EndsWith(".BAT") || scriptCommand.EndsWith(".BAT\"")) shellType = LauncherType.BATCH;
            else if (scriptCommand.EndsWith(".PS1") || scriptCommand.EndsWith(".PS1\"")) shellType = LauncherType.POWERSHELL;
            if (shellType == LauncherType.UNKNOWN)
            {
                File.AppendAllText(logFile, $"ERROR! Could not find the launcher for this file type: {scriptCommand}{Environment.NewLine}{Environment.NewLine}");
                return;
            }

            string workDir;
            if (scriptCommand.StartsWith("\"") && scriptCommand.EndsWith("\""))
                workDir = Path.GetDirectoryName(scriptCommand.Substring(1, scriptCommand.Length - 2));
            else
                workDir = Path.GetDirectoryName(scriptCommand);

            string arg = "", launcher;// = "";
            if (shellType == LauncherType.PYTHON) arg = "/c where python";
            else if (shellType == LauncherType.POWERSHELL) arg = "/c where powershell";
            if (shellType != LauncherType.BATCH)
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = arg,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                proc.Start();
                launcher = proc.StandardOutput.ReadLine();
                if (!File.Exists(launcher))
                {
                    File.AppendAllText(logFile, $"ERROR! Could not find the launcher for this script type: {scriptCommand}{Environment.NewLine}{Environment.NewLine}");
                    return;
                }
            }
            else launcher = scriptCommand;

            Thread.Sleep(delaySeconds * 1000);

            string debugText;

            using (var process = new Process())
            {
                string quotedParams = "";
                if (shellType != LauncherType.BATCH)
                    quotedParams = scriptCommand + " ";
                if (shellType == LauncherType.POWERSHELL)
                    quotedParams = "-nologo -executionpolicy bypass -File " + scriptCommand + " ";

                if (scriptParams != null)
                     if (scriptParams.Length > 0)
                        for (int i = 0; i<scriptParams.Length; i++)
                        {
                            if (scriptParams[i].Contains(" ")) scriptParams[i] = "\"" + scriptParams[i] + "\"";
                            quotedParams += scriptParams[i] + " ";
                        }
                quotedParams = quotedParams.Trim();
                process.StartInfo = new ProcessStartInfo(launcher, quotedParams)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workDir
                };
                debugText =
                    $"Timestamp : {DateTime.Now}{Environment.NewLine}" +
                    $"Shell     : {launcher}{Environment.NewLine}" +
                    $"Arguments : {process.StartInfo.Arguments}{Environment.NewLine}" +
                    $"Directory : {process.StartInfo.WorkingDirectory}";
                var timer = new Stopwatch();
                timer.Start();
                process.Start();
                process.WaitForExit();
                timer.Stop();
                TimeSpan timeTaken = timer.Elapsed;
                string elapsed = timeTaken.ToString(@"m\:ss\.fff");
                debugText += $"{Environment.NewLine}Exit code : {process.ExitCode}";
                debugText += $"{Environment.NewLine}Elapsed   : {elapsed}";
                debugText += $"{Environment.NewLine}{Environment.NewLine}";
            }
            if (createLog)
                File.AppendAllText(logFile, debugText);
        }
    }
}