namespace ThrottleStopRunner
{
    using System;
    using System.Diagnostics;
    using System.Windows.Forms;


    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Process[] pname = Process.GetProcessesByName(Application.ProductName);

            if (pname.Length < 2)
                Application.Run(new MainForm());
            else
                Application.Exit();
        }
    }
}
