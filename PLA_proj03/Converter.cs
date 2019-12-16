using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;

namespace PLA_proj03
{
    public partial class MainWindow
    {
        private class Converter
        {
            private readonly BackgroundWorker backgroundWorker;
            private readonly ConcurrentQueue<string> files = new ConcurrentQueue<string>();
            private readonly string outputPath;
            private readonly Action<string, string> report;
            private readonly Action<string> abortReport;
            private readonly SingleWorker1 singleWorker;

            public Converter(string outputPath, Func<SingleWorker1> getWorker, Action<string, string> report, Action<string> abortReport)
            {
                //run bg worker
                backgroundWorker = new BackgroundWorker();
                backgroundWorker.WorkerReportsProgress = true;
                backgroundWorker.WorkerSupportsCancellation = true;
                backgroundWorker.DoWork += backgroundWorker_DoWork;
                backgroundWorker.RunWorkerAsync();
                this.outputPath = outputPath;
                this.report = report;
                this.abortReport = abortReport;
                singleWorker = getWorker();
            }

            private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
            {
                BackgroundWorker worker = sender as BackgroundWorker;
                while (true)
                {
                    if (files.TryDequeue(out string file))
                    {
                        var fileName = Path.GetFileName(file);
                        try
                        {
                            var aborted = singleWorker.Work(file, (p) =>
                            {
                                report("Working on " + fileName + ": " + p + "%", fileName);
                            }, () => worker.CancellationPending);
                            if (aborted)
                            {
                                files.Enqueue(file);
                                abortReport(file);
                                e.Cancel = true;
                                break;
                            }
                        }
                        catch (AggregateException ex)
                        {
                            report(ex.InnerExceptions.Select(ex => ex.Message).Aggregate("", (s1, s2) => s1 + " " + s2), null);
                        }
                    }
                    else
                        Thread.Sleep(100);
                    if (worker.CancellationPending == true)
                    {
                        e.Cancel = true;
                        break;
                    }
                }
            }

            public void StartStop()
            {
                if (backgroundWorker.IsBusy)
                    backgroundWorker.CancelAsync();
                else
                    backgroundWorker.RunWorkerAsync();
            }

            public bool IsBusy()
            {
                return backgroundWorker.IsBusy;
            }

            public void AddFiles(IEnumerable<string> files)
            {
                foreach (var file in files)
                {
                    this.files.Enqueue(file);
                }
            }
        }
    }
}