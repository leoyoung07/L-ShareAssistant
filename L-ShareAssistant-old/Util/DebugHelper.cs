using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace L_ShareAssistant.Util
{
    class DebugHelper
    {
        public enum DebugType
        {
            Log,
            Console,
            DebugConsole
        };

        private static Dictionary<string, string> _logMap = new Dictionary<string, string>();

        private static string _logRoot;
        public static string LogRoot
        {
            get
            {
                if (_logRoot == null)
                {
                    _logRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                }
                return _logRoot;
            }
            set
            {
                _logRoot = value;
            }
        }

        public static void MethodDebug(string message = "", DebugType debugType = DebugType.Log, [CallerMemberName] string callerName = "")
        {
            string debugMessage = string.Format("Method [{0}], message [{1}]", callerName, message);
            switch (debugType)
            {
                case DebugType.Log:
                    _writeLog(debugMessage);
                    break;
                case DebugType.Console:
                    Console.WriteLine(string.Format("[{0}]{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), debugMessage));
                    break;
                case DebugType.DebugConsole:
                    Debug.WriteLine(string.Format("[{0}]{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), debugMessage));
                    break;
                default:
                    _writeLog(debugMessage);
                    break;
            }
        }

        private static void _writeLog(string message)
        {
            DateTime now = DateTime.Now;
            string logDateStr = now.ToString("yyyy-MM-dd");
            string logTimeStr = now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logPath = Path.Combine(LogRoot, logDateStr + ".log");
            if (!Directory.Exists(LogRoot))
            {
                Directory.CreateDirectory(LogRoot);
            }
            if(!_logMap.ContainsKey(logPath))
            {
                _logMap.Add(logPath, logPath);
            }
            Thread logThread = new Thread(() =>
            {
                lock (_logMap[logPath])
                {
                    using (StreamWriter sw = new StreamWriter(logPath, true, Encoding.UTF8))
                    {
                        sw.WriteLine(string.Format("[{0}]{1}", logTimeStr, message));
                    }
                }
            });
            logThread.Start();
        }
    }
}
