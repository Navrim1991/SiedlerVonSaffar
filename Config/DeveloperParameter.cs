using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

namespace SiedlerVonSaffar.Configuration
{
    public class DeveloperParameter
    {
        private const string CONFIG_FILE_PATH = "config.txt";
        public static bool PrintDebugInfoToConsole { get; internal set; }
        public static bool DebuggerIsAttached { get; internal set; }
        public static bool TcpEcho { get; internal set; }
        public static int ParamaterCount { get; private set; }

        private static ConfigFileWatcher configFileWatcher;

        public static string CONFIG_FILE
        {
            get
            {
                return CONFIG_FILE_PATH;
            }
        }

        public static void init()
        {
            PrintDebugInfoToConsole = System.Diagnostics.Debugger.IsAttached ? true : false;
            DebuggerIsAttached = System.Diagnostics.Debugger.IsAttached;
            TcpEcho = true;

            Type developerParameterType = typeof(DeveloperParameter);
            PropertyInfo[] propertyInfoFields = developerParameterType.GetProperties(BindingFlags.Public | BindingFlags.Static);
            ParamaterCount = propertyInfoFields.Length;

            if (!DebuggerIsAttached)
            {
                if (!File.Exists(@CONFIG_FILE_PATH))
                {
                    writeConfigFile(propertyInfoFields);
                }
            }
            else
            {
                writeConfigFile(propertyInfoFields);
            }

            configFileWatcher = new ConfigFileWatcher();

            if (!DebuggerIsAttached)
                configFileWatcher.ReadFileTextWithEncoding(CONFIG_FILE_PATH);
        }

        private static void writeConfigFile(PropertyInfo[] propertyInfoFields)
        {
            using (StreamWriter streamWriter = File.CreateText(@CONFIG_FILE_PATH))
            {
                foreach (PropertyInfo element in propertyInfoFields)
                {
                    if (checkElementForReflection(element.Name))
                        continue;

                    streamWriter.WriteLine(element.Name + "=" + element.GetValue(null) + ";");
                }
            }

            PrintDebug("Config-File wrote");
        }

        public static bool checkElementForReflection(string name)
        {
            if (name == "CONFIG_FILE" || name == "ParamaterCount")
                return true;

            return false;
        }

        public static void PrintDebug(string message)
        {
            if (DebuggerIsAttached || PrintDebugInfoToConsole)
            {
                ConsoleColor oldColor = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("DEBUG: ");

                Console.ForegroundColor = oldColor;
                Console.WriteLine("\t" + message);
            }

        }
    }
}
