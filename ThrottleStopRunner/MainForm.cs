namespace ThrottleStopRunner
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using NLog;


    public partial class MainForm : Form
    {
        #region Private fields

        TimeSpan _Interval;

        CancellationTokenSource _WorkerCrs;
        bool _IsWork;

        bool closeForm = true;



        #region Path program
        
        string nameProgram;
        string pathProgram;
         
        #endregion
        

        static readonly object locker = new object();

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        
        #endregion


        /// <summary>
        /// Конструктор.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            this.Interval = TimeSpan.FromMinutes(5);
            this._WorkerCrs = new CancellationTokenSource();
            this._IsWork = false;
            this.Text = Application.ProductName;

            
            this.nameProgram = "ThrottleStop";
            this.pathProgram = @"C:\ThrottleStop";
            
            //this.nameProgram = "MeteoSSC";
            //this.pathProgram = @"C:\ThrottleStop";
            

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
        string NameProgram
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
                    _WorkerCrs.Cancel();

                    button1.Text = "Запустить";
                }
                // Запуск работы
                else
                {
                    if (_WorkerCrs.IsCancellationRequested)
                        _WorkerCrs = new CancellationTokenSource();

                    StartWorker();

                    button1.Text = "Остановить";
                }

                _IsWork = !_IsWork;
            }
            catch (Exception ex)
            {
                BeginInvoke((MethodInvoker)(() => {    
                    MessageBox.Show($"Ошибка при запуске работы: {ex.Message}", 
                        "Внимание", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                }));

                return;
            }
        }



        /// <summary>
        /// Метод запуска работы основного потока.
        /// </summary>
        async void StartWorker()
        {
            try
            {
                await Task.Run(() => RoundWorker(), _WorkerCrs.Token);
            }
            catch (TaskCanceledException ex)
            {
            }
            catch (Exception ex2)
            {
                BeginInvoke((MethodInvoker)(() => {    
                    MessageBox.Show(
                        "Внимание!", 
                        $"{ex2.Message}");
                }));
            }
        }


        /// <summary>
        /// Метод запуска throttle stop.
        /// </summary>
        async void RoundWorker()
        {
            bool workingProgram = true;

            while (!_WorkerCrs.IsCancellationRequested)
            {
                try
                {
                    var pnames = Process.GetProcessesByName(NameProgram);

                    if (pnames.Length == 0)
                        workingProgram = false;

                    
                    // Если программа закрылась, то она запускается
                    if (!workingProgram)
                    {
#if !DEBUG
                         await Task.Delay(TimeSpan.FromSeconds(60));
#endif 
                        
                        // Запуск updater.
                        using (Process proc = new Process())
                        {
                            proc.StartInfo.FileName = $"{pathProgram}\\{NameProgram}.exe";
                            proc.StartInfo.CreateNoWindow = true;
                            proc.StartInfo.UseShellExecute = false;
                            proc.Start();
                        }

                        workingProgram = true;
                    }


                    await Task.Delay(Interval, _WorkerCrs.Token);
                }
                catch (TaskCanceledException ex)
                {
                    logger.Info($"Задача проверки Throttle stop остановлена  ex: {ex.Message}");
                }
                catch (Exception ex2)
                {
                    BeginInvoke((MethodInvoker)(() => {    
                        MessageBox.Show(
                            "Внимание!",
                            $"{ex2.Message}",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error); 
                    }));
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


        #region Event main form

        /// <summary>
        /// Открытие главной формы.
        /// </summary>
        void OpenMainForm()
        {
            this.ShowInTaskbar = true;
            this.WindowState = FormWindowState.Normal;
            this.Show();
        }
        
        /// <summary>
        /// Событие во время загрузки программы.
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
        }

        /// <summary>
        /// Событие при закрытии программы.
        /// </summary>
        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.WindowsShutDown)
            {
                _WorkerCrs.Cancel();
                Application.Exit();
            }
            else
            {
                e.Cancel = CloseForm;
                this.Hide();
            }
        }

        #endregion
    }
}