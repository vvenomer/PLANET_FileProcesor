using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace PLA_proj03
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string path;
        private string outputPath = "Output";
        private Converter converter;
        private FileSystemWatcher watcher;
        private ObservableCollection<string> filesQueue = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            QuedFiles.ItemsSource = filesQueue;
        }

        private void OnFileSystemWatcherChanged(object source, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() => filesQueue.Add(e.Name));
            converter.AddFiles(new List<string>() { e.FullPath });
        }

        private void StartProcessing()
        {
            SetWatch();
            converter = new Converter(outputPath, () =>
            {
                var from = ChangeFrom.Text[0];
                var to = ChangeTo.Text[0];
                return new SingleWorker1(from, to, outputPath);
            }, (str, file) =>
            {
                Dispatcher.Invoke(() =>
                {
                    CurrentProgress.Content = str;
                    if (file != null)
                        filesQueue.Remove(Path.GetFileName(file));
                });
            }, (file) =>
            {
                Dispatcher.Invoke(() =>
                {
                    filesQueue.Add(Path.GetFileName(file));
                });
            });
            var files = Directory.GetFiles(path);
            converter.AddFiles(files);
            foreach (var file in files)
            {
                filesQueue.Add(Path.GetFileName(file));
            }
            Task.Run(() =>
            {
                while (true)
                {
                    SetWorkerStatus();
                    Thread.Sleep(100);
                }
            });
        }

        private void SetWatch()
        {
            watcher = new FileSystemWatcher();
            watcher.Path = path;
            watcher.Created += OnFileSystemWatcherChanged;
            watcher.EnableRaisingEvents = true;
        }

        private void SetPath_Click(object sender, RoutedEventArgs e)
        {
            string path = DirPath.Text;
            if (!Directory.Exists(path))
            {
                MessageBox.Show("Can't find given directory");
                return;
            }
            else if (this.path == path)
            {
                MessageBox.Show("Chosen directory is the same as the last one");
                return;
            }
            onOff.IsEnabled = true;
            this.path = path;
            StartProcessing();
            MessageBox.Show("Monitored dir set to: " + path);
        }

        private void onOff_Click(object sender, RoutedEventArgs e)
        {
            converter.StartStop();
        }

        private void SetWorkerStatus()
        {
            SolidColorBrush fill;
            if (converter.IsBusy())
                fill = new SolidColorBrush(Colors.Green);
            else
                fill = new SolidColorBrush(Colors.Red);
            fill.Freeze();
            Dispatcher.Invoke(() => WorkerStatus.Fill = fill);
        }
    }
}