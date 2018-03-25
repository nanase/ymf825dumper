using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CommandLine;
using MidiUtils.Sequencer;
using Newtonsoft.Json;
using Ymf825;

namespace Ymf825Dumper
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);

            if (string.IsNullOrWhiteSpace(result.Value.InputFile))
            {
                Console.WriteLine("InputFile is not specified.");
                return;
            }

            Run(result.Value);
        }

        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        private static void Run(Options options)
        {
            const int resolution = 1000;
            const double tickSeconds = 1.0 / resolution;
            const double tickMilliSeconds = tickSeconds * 1000;

            var savePath = GetSavePath(options);

            if (File.Exists(savePath))
                File.Delete(savePath);

            using (var zipArchive = ZipFile.Open(savePath, ZipArchiveMode.Create))
            {
                using (var fileStream = zipArchive.CreateEntry("config.json", CompressionLevel.Optimal).Open())
                using (var sw = new StreamWriter(fileStream))
                {
                    var config = new Dictionary<string, object>
                    {
                        { "version", 0 },
                        { "resolution", resolution }
                    };
                    sw.Write(JsonConvert.SerializeObject(config));
                }

                using (var fileStream = zipArchive.CreateEntry("dump", CompressionLevel.Optimal).Open())
                using (var dumpWriter = new DumpWriter(fileStream))
                {
                    var sequence = new Sequence(options.InputFile);
                    var sequencer = new Sequencer(sequence);
                    var ymf825DumpChip = new Ymf825DumpChip(dumpWriter);
                    var ymf825Driver = new Ymf825Driver(ymf825DumpChip);
                    var project = LoadProject(options);
                    var driver = new MidiDriver(project.Tones.ToArray(), project.Equalizers.ToArray(), ymf825Driver);
                    var stopped = false;

                    ymf825Driver.EnableSectionMode();
                    ymf825Driver.SleepAction += i =>
                    {
                        var tick = (int)Math.Ceiling(i / tickMilliSeconds);

                        if (dumpWriter.Disposed)
                            return;

                        dumpWriter.RealtimeWait((ushort)tick);
                    };
                    driver.Start();

                    sequencer.OnTrackEvent += (sender, args) =>
                    {
                        foreach (var argsEvent in args.Events)
                            driver.ProcessMidiEvent(argsEvent);
                    };
                    sequencer.SequenceEnd += (sender, args) => stopped = true;
                    sequencer.Tick = 0L;

                    while (!stopped)
                    {
                        sequencer.Progress(tickSeconds);
                        dumpWriter.Wait(1);
                    }
                }
            }
        }

        private static string GetSavePath(Options options)
        {
            return string.IsNullOrWhiteSpace(options.Path) ?
                Path.ChangeExtension(options.InputFile, ".825") :
                options.Path;
        }

        private static Project LoadProject(Options options)
        {
            var path = string.IsNullOrWhiteSpace(options.ProjectFile) ?
                Path.ChangeExtension(options.InputFile, "json") :
                options.ProjectFile;

            var serializeText = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Project>(serializeText);
        }
    }

    internal class Ymf825DumpChip : IYmf825
    {
        private readonly DumpWriter dumpWriter;


        public bool AutoFlush { get; set; } = true;

        public TargetChip CurrentTargetChip { get; } = TargetChip.Board0 | TargetChip.Board1;


        public Ymf825DumpChip(DumpWriter dumpWriter)
        {
            this.dumpWriter = dumpWriter;
        }

        public void Dispose()
        {
            // thru
        }

        public void Flush()
        {
            dumpWriter.Flush();
        }

        public void Write(byte address, byte data)
        {
            dumpWriter.Write(address, data);
        }

        public void BurstWrite(byte address, byte[] data, int offset, int count)
        {
            dumpWriter.BurstWrite(address, data, offset, count);
        }

        public byte Read(byte address)
        {
            throw new NotSupportedException();
        }

        public void ResetHardware()
        {
            dumpWriter.ResetHardware();
        }

        public void ChangeTargetDevice(TargetChip target)
        {
            dumpWriter.ChangeTarget((byte)target);
        }
    }

    internal class Options
    {
        [Value(0, Required = true)]
        public string InputFile { get; set; }

        [Option('p', "project", HelpText = "プロジェクトJSONファイルのパスです。")]
        public string ProjectFile { get; set; }

        [Option('o', "output", HelpText = "出力先のファイルパスです。")]
        public string Path { get; set; }
    }
}
