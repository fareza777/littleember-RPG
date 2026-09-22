using System;
using System.IO;
using UnityEngine;

namespace LittleEmber.Core
{
    /// <summary>
    /// Captures every Unity error/exception to persistentDataPath/crashlog.txt
    /// (last ~64 KB, newest first). Pulled via `run-as` for field debugging and
    /// doubles as lightweight crash reporting for testers.
    /// </summary>
    public static class CrashLog
    {
        static string PathFile => Path.Combine(Application.persistentDataPath, "crashlog.txt");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Hook()
        {
            Application.logMessageReceivedThreaded += OnLog;
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            try
            {
                string entry = $"[{DateTime.Now:HH:mm:ss}] {type}: {condition}\n{stackTrace}\n---\n";
                string old = "";
                if (File.Exists(PathFile)) old = File.ReadAllText(PathFile);
                if (old.Length > 65536) old = old.Substring(0, 65536);
                File.WriteAllText(PathFile, entry + old);
            }
            catch { /* never throw from the logger */ }
        }
    }
}
