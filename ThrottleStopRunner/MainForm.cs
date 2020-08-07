namespace ThrottleStopRunner
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;


    public partial class MainForm : Form
    {
        #region Private fields

        TimeSpan _Interval;

        CancellationTokenSource workerCrs;
        bool _IsWork;

        bool closeForm = true;

        static string nameProgram = "UpdaterMyTraffics.exe";

        static readonly object locker = new object();

        #endregion


        /// <summary>
        /// Конструктор.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            Interval = TimeSpan.FromMinutes(1);

            workerCrs = new CancellationTokenSource();

            _IsWork = false;

            this.Text = Application.ProductName;


            button1_Click(null, null);

            FileOperations.SetAutorun();
        }

        #region Properties

        /// <summary>
        /// Интервал проверки запуска программы.
        /// </summary>
        public TimeSpan Interval
        {
            get
            {
                lock (locker)
                {
                    return _Interval;
                }
            }
            set
            {
                lock (locker)
                {
                    _Interval = value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static string NameProgram
        {
            get
            {
                lock (locker)
                    return nameProgram;
            }
            set
            {
                lock (locker)
                    nameProgram = value;
            }
        }

        /// <summary>
        /// Булевое выражение, означающее что форма закрыта.
        /// </summary>
        bool CloseForm
        {
            get
            {
                lock (locker)
                    return closeForm;
            }
            set
            {
                lock (locker)
                    closeForm = value;
            }
        }

        #endregion



        /// <summary>
        /// Обработка нажатия кнопки запуска работы.
        /// </summary>
        void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var text = Convert.ToInt32(textBoxInterval.Text);

                if (text <= 0)
                {
                    MessageBox.Show($"Интервал не может быть отрицательным или равным нулю!",
                        "Внимание", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
                    return;
                }

                _Interval = TimeSpan.FromSeconds(text);

                // Остановка работы
                if (_IsWork)
                {
                    workerCrs.Cancel();

                    button1.Text = "Запустить";
                }
                // Запуск работы
                else
                {
                    if (workerCrs.IsCancellationRequested)
                        workerCrs = new CancellationTokenSource();

                    StartWorker();

                    button1.Text = "Остановить";
                }

                _IsWork = !_IsWork;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске работы: {ex.Message}", 
                    "Внимание", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);

                return;
            }
        }



        async void StartWorker()
        {
            try
            {
                await Task.Run(() => RoundWorker(), workerCrs.Token);
            }
            catch (TaskCanceledException ex)
            {

            }
            catch (Exception ex2)
            {

            }
        }


        async void RoundWorker()
        {
            bool workingProgram = true;

            while (!workerCrs.IsCancellationRequested)
            {

                try
                {
                    var pnames = Process.GetProcessesByName("ThrottleStop");

                    if (pnames.Length == 0)
                        workingProgram = false;

                    
                    // Если программа закрылась, то она запускается

                    if (!workingProgram)
                    {
                        // Запуск updater.
                        using (Process proc = new Process())
                        {
                            proc.StartInfo.FileName = @"C:\ThrottleStop\ThrottleStop.exe";
                            proc.StartInfo.CreateNoWindow = true;
                            proc.StartInfo.UseShellExecute = false;
                            proc.Start();
                        }

                        workingProgram = true;
                    }


                    await Task.Delay(Interval, workerCrs.Token);
                }
                catch (TaskCanceledException ex)
                {

                }
                catch (Exception ex2)
                {

                }
            }
        }

        #region Уведомление

       

        /// <summary>
        /// Показать окно программы.
        /// </summary>
        void toolStripMenuItemView_Click(object sender, EventArgs e)
        {
            OpenMainForm();
        }

        /// <summary>
        /// Выход.
        /// </summary>
        void toolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            MainForm_FormClosing(null, new FormClosingEventArgs(CloseReason.WindowsShutDown, true));

            BeginInvoke((MethodInvoker)(() =>
            {
                notifyIconWorker.Visible = false;
            }));
        }

        #endregion


        /// <summary>
        /// Открытие главной формы.
        /// </summary>
        void OpenMainForm()
        {
            this.ShowInTaskbar = true;
            this.WindowState = FormWindowState.Normal;
            this.Show();
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.WindowsShutDown)
            {
                Application.Exit();
            }
            else
            {
                e.Cancel = CloseForm;
                this.Hide();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false;

            this.WindowState = FormWindowState.Normal;
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
        }
    }
}
