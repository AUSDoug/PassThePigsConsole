using System;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;

//File:   GameLog.cs
//Central logging setup (Serilog). Two channels:
//  - default channel: game flow, to the console and logs/passthepigs-*.log
//  - "AI" channel (GameLog.Ai): the rulesets' reasoning. Always written to
//    logs/ai-commentary-*.log, and echoed to the console only in verbose mode.
//
//Verbosity is one switch (SetVerbose), driven by the "Log Mode" setting - it
//replaces the old per-call `if (logMode) Trace.WriteLine(...)` pattern.

namespace PassThePigsConsole
{
    internal static class GameLog
    {
        private const string ChannelProperty = "Channel";
        private const string AiChannel = "AI";

        private const string ConsoleTemplate = "{Message:lj}{NewLine}{Exception}";
        private const string FileTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        //Gates the console + main log; SetVerbose raises or lowers it once the
        //"Log Mode" setting is known.
        private static readonly LoggingLevelSwitch MainLevel =
            new LoggingLevelSwitch(LogEventLevel.Information);

        //The AI reasoning channel. No-op until Configure() runs (e.g. in bench mode).
        public static ILogger Ai { get; private set; } = Logger.None;

        //Builds Log.Logger. Call once at start-up.
        public static void Configure(string logDirectory)
        {
            Directory.CreateDirectory(logDirectory);

            //So non-ASCII in messages (arrows, dashes) render on the console.
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { /* no console */ }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()   // let both channels through; each sink gates itself
                .Enrich.FromLogContext()
                // game flow -> console + main log, at the chosen verbosity
                .WriteTo.Logger(flow => flow
                    .MinimumLevel.ControlledBy(MainLevel)
                    .Filter.ByExcluding(Matching.WithProperty<string>(ChannelProperty, c => c == AiChannel))
                    .WriteTo.Console(outputTemplate: ConsoleTemplate)
                    .WriteTo.File(Path.Combine(logDirectory, "passthepigs-.log"),
                        rollingInterval: RollingInterval.Day, outputTemplate: FileTemplate))
                // AI reasoning -> its own file always; console echo follows verbosity
                .WriteTo.Logger(reasoning => reasoning
                    .Filter.ByIncludingOnly(Matching.WithProperty<string>(ChannelProperty, c => c == AiChannel))
                    .WriteTo.File(Path.Combine(logDirectory, "ai-commentary-.log"),
                        rollingInterval: RollingInterval.Day, outputTemplate: FileTemplate)
                    .WriteTo.Console(outputTemplate: ConsoleTemplate, levelSwitch: MainLevel))
                .CreateLogger();

            Ai = Log.ForContext(ChannelProperty, AiChannel);

            AppDomain.CurrentDomain.ProcessExit += (s, e) => Shutdown();
        }

        //Normal shows game flow only; verbose adds start-up detail and the AI channel.
        public static void SetVerbose(bool verbose)
        {
            MainLevel.MinimumLevel = verbose ? LogEventLevel.Debug : LogEventLevel.Information;
        }

        public static void Shutdown()
        {
            Log.CloseAndFlush();
        }
    }
}
