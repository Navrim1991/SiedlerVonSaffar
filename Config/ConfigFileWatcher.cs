using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace SiedlerVonSaffar.Configuration
{
    internal class ConfigFileWatcher : FileSystemWatcher
    {
        private readonly string CONFIG_FILE_PATH;
        private readonly int ERROR_SHARING_VIOLATION = 32;
        private readonly int ERROR_LOCK_VIOLATION = 33;
        public ConfigFileWatcher()
        {
            CONFIG_FILE_PATH = DeveloperParameter.CONFIG_FILE;
            this.Path = Environment.CurrentDirectory;
            this.Filter = "*.txt";

            this.Changed += new FileSystemEventHandler(ConfigFileWatcher_Changed);
            this.Created += new FileSystemEventHandler(ConfigFileWatcher_Created);
            this.Deleted += new FileSystemEventHandler(ConfigFileWatcher_Deleted);
            this.Renamed += new RenamedEventHandler(ConfigFileWatcher_Renamed);

            this.EnableRaisingEvents = true;

            DeveloperParameter.PrintDebug("FileWatcher started");
        }

        void ConfigFileWatcher_Renamed(object sender, RenamedEventArgs e)
        {

        }

        void ConfigFileWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.Contains(@CONFIG_FILE_PATH))
            {
                DeveloperParameter.PrintDebug("Config File was deleted. FileWatcher stopped");

                this.EnableRaisingEvents = false;
            }


        }

        void ConfigFileWatcher_Created(object sender, FileSystemEventArgs e)
        {

        }

        void ConfigFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.Contains(@CONFIG_FILE_PATH))
            {
                DeveloperParameter.PrintDebug("Config file changed");

                ReadFileTextWithEncoding(e.FullPath);
            }

        }

        private bool IsFileLocked(Exception exception)
        {
            int errorCode = Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION;
        }

        internal void ReadFileTextWithEncoding(object filePath)
        {
            string fileContents = "";
            StringBuilder stringBuilder = new StringBuilder();
            byte[] buffer;
            bool fileIsLocked = false;

            do
            {
                try
                {
                    using (FileStream fileStream = File.Open(filePath.ToString(), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        int length = (int)fileStream.Length;  // get file length
                        buffer = new byte[length];            // create buffer
                        int count;                            // actual number of bytes read
                        int sum = 0;                          // total number of bytes read

                        // read until Read method returns 0 (end of the stream has been reached)
                        while ((count = fileStream.Read(buffer, sum, length - sum)) > 0)
                        {
                            sum += count;  // sum is a buffer offset for next reading
                        }

                        fileStream.Close(); //Again - this is not needed, just me being paranoid and explicitly releasing resources ASAP

                        //Depending on the encoding you wish to use - I'll leave that up to you
                        fileContents = System.Text.Encoding.Default.GetString(buffer);

                        fileIsLocked = false;
                    }
                }
                catch (IOException ex)
                {
                    //THE FUNKY MAGIC - TO SEE IF THIS FILE REALLY IS LOCKED!!!
                    if (IsFileLocked(ex))
                    {
                        fileIsLocked = true;

                        DeveloperParameter.PrintDebug("config-File is locked\n\r" + ex.Message + "\n\r" + ex.StackTrace);

                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    DeveloperParameter.PrintDebug(ex.Message + "\n\r" + ex.StackTrace);
                }
                finally
                { }
            } while (fileIsLocked);

            changeDeveloperParamter(fileContents);
        }

        private void changeDeveloperParamter(string fileContents)
        {
            Type developerParameterType = typeof(DeveloperParameter);
            PropertyInfo[] propertyInfoFields = developerParameterType.GetProperties(BindingFlags.Public | BindingFlags.Static);

            using (StringReader reader = new StringReader(fileContents))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Do something with the line
                }
            }

            foreach (PropertyInfo element in propertyInfoFields)
            {

                if (DeveloperParameter.checkElementForReflection(element.Name))
                    continue;

                //streamWriter.WriteLine(i + " " + element.Name + "=" + element.GetValue(null) + ";");
                int valueStartIndex = fileContents.IndexOf(element.Name); //plus 1 wegen dem "=" und dem darauffolgenden Wert

                if (valueStartIndex < 0)
                {
                    DeveloperParameter.PrintDebug("ConfigParamerter: " + element.Name + "not find in config-File");

                    continue;
                }

                valueStartIndex += +element.Name.Length + 1;
                int valueEndIndex = fileContents.IndexOf(";", valueStartIndex);

                string value = fileContents.Substring(valueStartIndex, valueEndIndex - valueStartIndex);

                if (value != element.GetValue(null).ToString())
                {
                    element.SetValue(null, Convert.ChangeType(value, element.PropertyType), null);

                    DeveloperParameter.PrintDebug("ConfigParamerter: " + element.Name + " changed to " + element.GetValue(null).ToString());
                }

            }
        }
    }
}
