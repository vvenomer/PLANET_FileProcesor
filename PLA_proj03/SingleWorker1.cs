using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Path = System.IO.Path;

namespace PLA_proj03
{
    public partial class MainWindow
    {
        private class SingleWorker1
        {
            private readonly char changeFrom;
            private readonly char changeTo;
            private string outputPath;
            private int lastPercentage = 0;

            public SingleWorker1(char changeFrom, char changeTo, string outputPath)
            {
                this.changeFrom = changeFrom;
                this.changeTo = changeTo;
                this.outputPath = outputPath;
            }

            public bool Work(string path, Action<int> report, Func<bool> shouldAbort)
            {
                int nrOfLines = File.ReadAllLines(path).Length;
                var outputFilePath = Path.Combine(outputPath, Path.GetFileName(path));
                using var reader = new StreamReader(path);
                using var writer = new StreamWriter(outputFilePath);
                var random = new Random();
                string line;
                int currentLine = 1;
                ConcurrentQueue<(string line, int i)> newLines = new ConcurrentQueue<(string line, int i)>();
                ConcurrentQueue<Exception> exceptions = new ConcurrentQueue<Exception>();
                File.ReadAllLines(path).Select((line, i) => (line, i)).AsParallel().ForAll(lineWithIndex =>
                {
                    var newLine = lineWithIndex.line.Replace(changeFrom, changeTo);
                    try
                    {
                        if (random.Next(0, 30) == 0)
                            throw new Exception("too hard!");
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                    Thread.Sleep(random.Next(500, 1500));
                    Report(currentLine, nrOfLines, report);
                    Interlocked.Increment(ref currentLine);
                    if (shouldAbort())
                        return;
                    newLines.Enqueue((newLine, lineWithIndex.i));
                });
                if (exceptions.Count > 0)
                {
                    throw new AggregateException(exceptions);
                }
                if (shouldAbort())
                    return true;
                var ordered = newLines.ToList().OrderBy(lineWithIndex => lineWithIndex.i).Select(lineWithIndex => lineWithIndex.line);
                foreach (var newLine in ordered)
                {
                    writer.WriteLine(newLine);
                }
                return false;
            }

            private void Report(int currentLine, int nrOfLines, Action<int> report)
            {
                var newPercentage = 100 * currentLine / nrOfLines;
                if (newPercentage != lastPercentage)
                {
                    lastPercentage = newPercentage;
                    report(100 * currentLine / nrOfLines);
                }
            }
        }
    }
}