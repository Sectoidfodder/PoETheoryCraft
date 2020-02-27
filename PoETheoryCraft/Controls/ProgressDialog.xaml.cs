using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PoETheoryCraft.Controls
{
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : Window
    {
        public delegate void WorkStep();
        public WorkStep Increment { get; set; } = () => { };
        public int Steps { get; set; } = 0;
        public int ReportStep { get; set; } = 1;
        private BackgroundWorker Worker;
        public ProgressDialog()
        {
            InitializeComponent();
        }
        private void Do_Task(object sender, EventArgs e)
        {
            Worker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            Worker.DoWork += DoIncrements;
            Worker.ProgressChanged += UpdateStatus;
            Worker.RunWorkerAsync();
            Worker.RunWorkerCompleted += CloseDialog;
        }
        private void Stop_Task(object sender, EventArgs e)
        {
            if (Worker != null)
                Worker.CancelAsync();
        }
        private void CloseDialog(object sender, EventArgs e)
        {
            if (DialogResult == null)
                DialogResult = true;
        }
        private void DoIncrements(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < Steps; i++)
            {
                if (Worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                Increment();
                if (i % ReportStep == 0)
                    (sender as BackgroundWorker).ReportProgress(i * 100 / Steps);
            }
            (sender as BackgroundWorker).ReportProgress(100);
        }
        private void UpdateStatus(object sender, ProgressChangedEventArgs e)
        {
            Status.Value = e.ProgressPercentage;
            StatusText.Text = e.ProgressPercentage * Steps / 100 + " / " + Steps;
        }
    }
}
