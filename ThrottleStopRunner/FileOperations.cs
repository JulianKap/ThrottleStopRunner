namespace ThrottleStopRunner
{
    using System;
    using System.Linq;
    using System.Windows.Forms;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Diagnostics;
    using System.Net;
    using System.Net.NetworkInformation;
    using Microsoft.Win32;
    using System.Xml.Linq;
    using System.Threading;
    using System.Management;
    using System.Security.Cryptography;
    using System.Globalization;
    using System.Data;


    static class FileOperations
    {

        #region Автозагрузка

        /// <summary>
        /// Автозапуск в реестре.
        /// </summary>
        /// <param name="autorun">True - включить, false - отключить.</param>
        public static bool SetAutorun()
        {
            try
            {
                string name = Application.ProductName;

                var ExePath = Application.ExecutablePath;


                var pathautorun = $"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}\\autorun.xml";
                XDocument xDoc = XDocument.Load(pathautorun);

                var xel = xDoc.Root.Element("{http://schemas.microsoft.com/windows/2004/02/mit/task}Actions");

                xel.Element("{http://schemas.microsoft.com/windows/2004/02/mit/task}Exec")
                    .Element("{http://schemas.microsoft.com/windows/2004/02/mit/task}Command").Value =
                    $"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}\\{name}.exe";

                var xel2 = xDoc.Root.Element("{http://schemas.microsoft.com/windows/2004/02/mit/task}Principals");

                xel2.Element("{http://schemas.microsoft.com/windows/2004/02/mit/task}Principal")
                    .Element("{http://schemas.microsoft.com/windows/2004/02/mit/task}UserId").Value =
                    $"{System.Security.Principal.WindowsIdentity.GetCurrent().User.Value}";

                xDoc.Save(pathautorun);


                // Запуск процесса для добавления задачи автозапуска в планировщик.
                using (var p = new Process())
                {
                    p.StartInfo = new ProcessStartInfo("schtasks", $"/create /xml \"{pathautorun}\" /tn \"Runner\"")
                    {
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Verb = "runas"
                    };
                    p.Start();
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
