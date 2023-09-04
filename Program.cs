using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using MetaBrainz.MusicBrainz.DiscId;

namespace CdStacks
{
    class Program
    {
        static bool repeat = true;
        static string device;
        static TocRecord tocRecord;
        static ICollection<TocRecord> savedTocRecords;
        static DiscReadFeature features = TableOfContents.AvailableFeatures;
        private static readonly TimeSpan TwoSeconds = new TimeSpan(0, 0, 2);

        protected class TocRecord
        {
            public TableOfContents Toc;
            public string Performer;
            public string Title;
            public string Disc;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            device = TableOfContents.DefaultDevice;
            if (device == null)
                throw new Exception("No device's available");

            // Prompt for Disc Insertion
            device = CheckDiscDrivePresence();
            
            do
            {
                try
                {
                    // Ask for Ready Disc
                    PromptReadyDisc();

                    // Read Disc
                    tocRecord = new TocRecord() { Toc = ReadDiscInformation() };

                    // Print Information
                    PrintDiscInformation(tocRecord.Toc);

                    // Check Artist-Album Text
                    PromptTitleDetails();

                    // Save TOC?
                    PromptToSave();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Issue reading with the CD drive. Aborting...", ex);
                }

                // Repeat with New Disc if Accepted
                PromptToRepeat();
            } while (repeat);

            SaveFile();
        }

        static string CheckDiscDrivePresence()
        {
            var defaultDevice = TableOfContents.DefaultDevice;

            Console.WriteLine("Available Devices:");
            foreach (string availableDevice in TableOfContents.AvailableDevices)
            {
                Console.Write($"{availableDevice}");
                if (availableDevice == defaultDevice) Console.Write($"\t(Default Device)");
                Console.WriteLine();
            }

            return defaultDevice;
        }

        static void PromptReadyDisc()
        {
            Console.Write("Ready? (E to Eject)");
            bool awaitValid = true;
            do
            {
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                }
                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.Enter:
                        awaitValid = false;
                        break;
                    case ConsoleKey.E:            
                        OpenCdDoor();
                        break;
                }
            } while (awaitValid);

            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop-1);
        }

        static TableOfContents ReadDiscInformation()
        {
            return TableOfContents.ReadDisc(device, TableOfContents.AvailableFeatures);
        }

        static void PrintDiscInformation(TableOfContents toc)
        {
            //PrintToConsole(toc);
        }

        static void PromptToRepeat()
        {
            if (savedTocRecords?.Count > 0)
                Console.WriteLine($"Saved Disc Records: {savedTocRecords.Count}");
                
            Console.Write("Scan another Disc? (Enter to repeat, Esc or N to quit)");
            //bool awaitValid = true;
            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }
            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.Escape:
                case ConsoleKey.N:
                    repeat = false;
                    //awaitValid = false;
                    break;
                case ConsoleKey.Enter:
                    //awaitValid = false;
                    break;
            }
            
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop-1);
        }

        static void PromptTitleDetails()
        {
            if (tocRecord.Toc?.TextInfo == null || string.IsNullOrWhiteSpace(tocRecord.Toc?.TextInfo[0]?.Title))
            {
                Console.WriteLine("Disc TOC is missing an Album Title. Please provide one...");
                Console.Write("> ");
                tocRecord.Title = Console.ReadLine() + " (User Input)";
            }
            else
                tocRecord.Title = tocRecord.Toc?.TextInfo[0]?.Title;

            if (tocRecord.Toc?.TextInfo == null || string.IsNullOrWhiteSpace(tocRecord.Toc?.TextInfo[0]?.Performer))
            {
                Console.WriteLine("Disc TOC is missing a Performer. Please provide one...");
                Console.Write("> ");
                tocRecord.Performer = Console.ReadLine() + " (User Input)";
            }
            else
                tocRecord.Performer = tocRecord.Toc?.TextInfo[0]?.Performer;

            Console.WriteLine("If multiple disc album, enter Disc Number");
            Console.Write("> ");
            string input = Console.ReadLine();
            tocRecord.Disc = string.IsNullOrWhiteSpace(input) ? "1" : input;
        }

        static void PromptToSave()
        {
            Console.Write("Save Disc Information? (Y or [Enter]");
            //bool awaitValid = true;
            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }
            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.Enter:
                case ConsoleKey.Y:
                    if (savedTocRecords == null) savedTocRecords = new List<TocRecord>();
                    savedTocRecords.Add(tocRecord);
                    break;
            }
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop-1);
        }

        static void PrintToConsole(TableOfContents toc)
        {
            //Console.WriteLine($"CD Device Used      : {toc.DeviceName}");
            //Console.WriteLine($"Features Requested  : {features}");
            //Console.WriteLine();
            if ((features & DiscReadFeature.MediaCatalogNumber) != 0)
                Console.WriteLine($"Media Catalog Number: {toc.MediaCatalogNumber ?? "* not set *"}");
            Console.WriteLine($"MusicBrainz Disc ID : {toc.DiscId}");
            Console.WriteLine($"FreeDB Disc ID      : {toc.FreeDbId}");
            Console.WriteLine($"Submission URL      : {toc.SubmissionUrl}");
            Console.WriteLine();
            
            var languages = toc.TextLanguages;
            if (languages?.Count > 0) {
                var text = toc.TextInfo;
                if (text?.Count > 0) {
                    Console.WriteLine("CD-TEXT Information:");
                    var idx = 0;
                    foreach (var l in languages) {
                        Console.WriteLine($"- Language: {l}");
                        var ti = text[idx++];
                        if (ti.Genre.HasValue) {
                            if (ti.GenreDescription != null)
                                Console.WriteLine($"  - Genre           : {ti.Genre.Value} ({ti.GenreDescription})");
                            else
                                Console.WriteLine($"  - Genre           : {ti.Genre.Value}");
                        }

                        if (ti.Identification != null) Console.WriteLine($"  - Identification  : {ti.Identification}");
                        if (ti.ProductCode    != null) Console.WriteLine($"  - UPC/EAN         : {ti.ProductCode}");
                        if (ti.Title          != null) Console.WriteLine($"  - Title           : {ti.Title}");
                        if (ti.Performer      != null) Console.WriteLine($"  - Performer       : {ti.Performer}");
                        if (ti.Lyricist       != null) Console.WriteLine($"  - Lyricist        : {ti.Lyricist}");
                        if (ti.Composer       != null) Console.WriteLine($"  - Composer        : {ti.Composer}");
                        if (ti.Arranger       != null) Console.WriteLine($"  - Arranger        : {ti.Arranger}");
                        if (ti.Message        != null) Console.WriteLine($"  - Message         : {ti.Message}");
                    }
                    Console.WriteLine();
                } 
            }
            
            Console.WriteLine("Tracks:");
            { // Check for a "hidden" pre-gap track
                var t = toc.Tracks[toc.FirstTrack];
                if (t.StartTime > Program.TwoSeconds)
                    Console.WriteLine($" --- Offset: {150,6} ({Program.TwoSeconds,-16}) Length: {t.Offset - 150,6} ({t.StartTime.Subtract(Program.TwoSeconds),-16})");
            }
        
            foreach (var t in toc.Tracks) {
                Console.Write($" {t.Number,2}. Offset: {t.Offset,6} ({t.StartTime,-16}) Length: {t.Length,6} ({t.Duration,-16})");
                if ((features & DiscReadFeature.TrackIsrc) != 0)
                    Console.Write($" ISRC: {t.Isrc ?? "* not set *"}");
                Console.WriteLine();
                
                if (languages?.Count > 0) {
                    var text = t.TextInfo;
                    if (text?.Count > 0) {
                        Console.WriteLine("     CD-TEXT Information:");
                        var idx = 0;
                        foreach (var l in languages) {
                            Console.WriteLine($"     - Language: {l}");
                            var ti = text[idx++];
                            if (ti.Title          != null) Console.WriteLine($"       - Title     : {ti.Title}");
                            if (ti.Performer      != null) Console.WriteLine($"       - Performer : {ti.Performer}");
                            if (ti.Lyricist       != null) Console.WriteLine($"       - Lyricist  : {ti.Lyricist}");
                            if (ti.Composer       != null) Console.WriteLine($"       - Composer  : {ti.Composer}");
                            if (ti.Arranger       != null) Console.WriteLine($"       - Arranger  : {ti.Arranger}");
                            if (ti.Message        != null) Console.WriteLine($"       - Message   : {ti.Message}");
                            if (ti.Isrc           != null) Console.WriteLine($"       - ISRC      : {ti.Isrc}");
                        }
                    }
                }
            }
        }

        private static string PrintDiscHeaders(TocRecord tocRecord)
        {
            return string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\"",
                tocRecord.Toc?.DiscId,
                tocRecord.Toc?.FreeDbId,
                (tocRecord.Toc?.TextInfo != null) ? tocRecord.Toc?.TextInfo[0]?.Identification : "",
                (tocRecord.Toc?.TextInfo != null) ? tocRecord.Toc?.TextInfo[0]?.ProductCode : "",
                (tocRecord.Toc?.TextInfo != null) ? tocRecord.Toc?.TextInfo[0]?.Title : tocRecord.Title,
                (tocRecord.Toc?.TextInfo != null) ? tocRecord.Toc?.TextInfo[0]?.Performer : tocRecord.Performer,
                (tocRecord.Toc?.TextInfo != null) ? tocRecord.Toc?.TextInfo[0]?.Lyricist : "",
                (tocRecord.Toc?.TextInfo != null) ? tocRecord.Toc?.TextInfo[0]?.Composer : "",
                (tocRecord.Toc?.TextInfo != null) ? tocRecord.Toc?.TextInfo[0]?.Arranger : "",
                (tocRecord.Toc?.TextInfo != null) ? tocRecord.Toc?.TextInfo[0]?.Message : "",
                tocRecord.Disc
            );
        }

        private static void SaveFile()
        {
            if (savedTocRecords == null) return;

            StringBuilder csv = new StringBuilder();

            string path = "CdStacks.csv";
            if (!File.Exists(path))
            {
                csv.AppendLine("\"MB DiscID\",\"FreeDbId\",\"Identification\",\"Product Code\",\"Title\",\"Performer\",\"Lyricist\",\"Composer\",\"Arranger\",\"Message\",\"Disc\"");
            }
            
            foreach (TocRecord record in savedTocRecords)
            {
                csv.AppendLine(PrintDiscHeaders(record));
            }

            File.AppendAllText(path, csv.ToString());
        }

        #region CD Interation
        [DllImport("winmm.dll", EntryPoint = "mciSendString")]
        private static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);

        private static void OpenCdDoor()
        {
            int result = mciSendString("set cdaudio door open", null, 0, 0);
        }
        #endregion
    }
}
