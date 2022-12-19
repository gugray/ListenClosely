using Microsoft.Extensions.Configuration.Ini;
using Microsoft.Extensions.FileProviders;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using File = System.IO.File;

namespace Tool
{
    class Program
    {

        private const string APP_VERSION = "1.0.1";
        private const string APP_NAME = "ListenClosely";

        // Installation parts
        private const string WORK_DIR_PATH = "./_work";
        private const string AUDIO_DIR_PATH = "./_audio";
        private const string OUT_DIR_PATH = "./_out";
        private const string SCRIPTS_DIR_PATH = "./_scripts";
        private const string RULEM_PY_PATH = SCRIPTS_DIR_PATH + "/rulem_mod.py";
        private const string MATERIALS_OPENR_WORDS_PATH = "./_materials/openrussian/words.csv";
        private const string MATERIALS_OPENR_TRANSL_PATH = "./_materials/openrussian/translations.csv";
        private const string MATERIALS_RUWIKI_PATH = "./_materials/ruwiktionary/ruwiktionary.txt";
        private const string INI_FILE = "ListenClosely.ini";

        private const string OVERWRITE = "OVERWRITE";
        private const string SKIP = "SKIP";
        private const string BACKUP = "BACKUP";
        private const string BREAK = "BREAK";
        private const string PROCESS = "PROCESS";

        private const string GOOGLE_API = "G";
        private const string MS_API = "M";

        private const string AUDIO_MP3 = "MP3";
        private const string AUDIO_WAV = "WAV";

        // Which entries from RuWiki have to be read
        private static string[] RU_WIKI_LANGUAGES = new string[] { "it", "es", "fr", "de" };

        // internal set
        private static string ORIG_FILE_PATH;
        private static string PLAIN_FILE_PATH;
        private static string LEMS_FILE_PATH;
        private static string ADD_PAR_FILE_PATH;
        private static string GOOGLE_JSON_FILE_PATH;
        private static string GOOGLE_TRANS_JSON_FILE_PATH;
        private static string MS_TRANS_JSON_FILE_PATH;
        private static string MS_CONV_JSON_FILE_PATH;
        private static string SEGS_FILE_PATH;
        private static string FLAC_FILE_PATH;
        private static string WEBM_FILE_PATH;
        private static string M4A_FILE_PATH;
        private static string AUDIO_IN_FILE_PATH;

        private static string SPEECH_API; // MS/Google

        // set by INI
        private static string GOOGLE_API_KEY_PATH;
        private static string GOOGLE_API_PROJECT_ID;
        private static string GOOGLE_API_BUCKET_NAME;
        private static string FFMPEG_PATH;
        private static string PYTHON_PATH;


        // set by properties file
        // Abbreviated name of the work data, mandatory
        private static string ABBREVIATION;
        // The path to the custom dictionary (nullable)
        private static string CUSTOM_DIC_PATH;
        // The work title, which will be displayed on the page, mandatory
        private static string TITLE;
        // The audio input file format; currently supported only WAV and MP3
        private static string AUDIO_FORMAT;
        // The first X lines which will be marked as title lines of the text
        // default 0
        private static int SHIFT_TITLE_LINES; 
        // The value to shift the segments timestamps
        // default 0.0
        private static decimal SHIFT; 
        // The value for tempo correction(0 if not required)
        // default 0.0
        private static double TEMPO_CORRECTION; 
        // The flag for mark main text as verses lines
        // default false
        private static bool VERSES; 
        // if a speech recognition file found as created by previous run:
        // break / skip [default] / backup / overwrite
        private static string SPEECH_API_OUT_FILE_OVERRIDE_STRATEGY;
        // if a lemmatization file found as created by previous run:
        // break / skip [default] / backup / overwrite
        private static string LEMMATIZING_OUT_FILE_OVERRIDE_STRATEGY;
        // if a FLAC file found as created by previous run:
        // break / skip [default] / backup / overwrite
        private static string FFMPEG_OUT_FILE_OVERRIDE_STRATEGY;
        // if a ready segments output file found as created by previous run:
        // break / skip / backup / overwrite [default]
        private static string SEGMENTS_OUT_FILE_OVERRIDE_STRATEGY; 
        // what to do after the lemmatizator did the work:
        // break [default] / process 
        private static string POST_LEMMATIZING_STRATEGY;

        private static string BASE_URL;


        /**
         * Initialize the static variables; read the INI file and the properties; check main pre-conditions
         */
        private static void setUp(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            checkHelpRequest(args);

            checkEnvironments();

            readRunProperties(args);

            ORIG_FILE_PATH = toAbsolutePath(WORK_DIR_PATH + "/" + ABBREVIATION + "-orig.txt");
            PLAIN_FILE_PATH = toAbsolutePath(WORK_DIR_PATH + "/" + ABBREVIATION + "-plain.txt");
            LEMS_FILE_PATH = toAbsolutePath(WORK_DIR_PATH + "/" + ABBREVIATION + "-lem.txt");
            ADD_PAR_FILE_PATH = toAbsolutePath(WORK_DIR_PATH + "/" + ABBREVIATION + "-addpar.txt");
            GOOGLE_JSON_FILE_PATH = toAbsolutePath(WORK_DIR_PATH + "/" + ABBREVIATION + "-conv-goog.json");
            GOOGLE_TRANS_JSON_FILE_PATH = toAbsolutePath(WORK_DIR_PATH + "/" + ABBREVIATION + "-goog.json");
            MS_TRANS_JSON_FILE_PATH = toAbsolutePath(WORK_DIR_PATH + "/" + ABBREVIATION + "-ms.json");
            MS_CONV_JSON_FILE_PATH = toAbsolutePath(WORK_DIR_PATH + "/" + ABBREVIATION + "-conv-ms.json");
            SEGS_FILE_PATH = toAbsolutePath(WORK_DIR_PATH + "/" + ABBREVIATION + "-segs.json");

            FLAC_FILE_PATH = toAbsolutePath(AUDIO_DIR_PATH + "/" + ABBREVIATION + ".flac");
            WEBM_FILE_PATH = toAbsolutePath(AUDIO_DIR_PATH + "/" + ABBREVIATION + ".webm");
            M4A_FILE_PATH = toAbsolutePath(AUDIO_DIR_PATH + "/" + ABBREVIATION + ".m4a");

            readIni();

            checkPreconditions();
        }

        private static string toAbsolutePath(string filePath)
        {
            return new FileInfo(filePath).FullName;
        }

        private static string getBackupName(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            string extension = fi.Extension;
            string name = fi.FullName;
            name = name.Substring(0, name.Length - extension.Length);
            String now = DateTime.Now.ToString("yyyyMMddHHmmss");
            name += "_" + now + extension;
            return name;
        }
        private static string getOutName(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            string name = fi.Name;
            fi = new FileInfo(OUT_DIR_PATH + "/" + name);
            return fi.FullName;
        }

        /**
         * Read and validate the run settings from the properties file passed as run argument
         */
        private static void readRunProperties(string[] args)
        {
            Console.WriteLine("Analyze the call arguments...");

            // At the moment, only the supported processing is to pass a path to properties
            // file into the application as a single argument

            if (args.Length == 0)
            {
                // Print the elp line and finish
                Console.WriteLine("Please provide a path to the run configuration file");
                System.Environment.Exit(1);
            }

            string path = args[0];

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("File not found: '" + toAbsolutePath(path) + "'");
            }

            SPEECH_API_OUT_FILE_OVERRIDE_STRATEGY = SKIP;
            LEMMATIZING_OUT_FILE_OVERRIDE_STRATEGY = SKIP;
            FFMPEG_OUT_FILE_OVERRIDE_STRATEGY = SKIP;
            SEGMENTS_OUT_FILE_OVERRIDE_STRATEGY = OVERWRITE;
            POST_LEMMATIZING_STRATEGY = BREAK;
            SHIFT_TITLE_LINES = 0;
            SHIFT = 0;
            TEMPO_CORRECTION = 0.0;
            VERSES = false;

            ABBREVIATION = null;
            CUSTOM_DIC_PATH = null;
            TITLE = null;
            AUDIO_FORMAT = null;

            string line;
            using (StreamReader sr = new StreamReader(path))
            {
                // only single-line property entries are supported!
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("#")) continue;
                    if (!line.Contains("=")) continue;
                    string[] split = line.Split("=");
                    if (split.Length < 2) continue;
                    string key = split[0].Trim();
                    if (key.Length == 0) continue;
                    string[] valueArr = new string[split.Length - 1];
                    Array.Copy(split, 1, valueArr, 0, valueArr.Length);
                    string value = String.Join("", valueArr).Trim();


                    if (value.Length > 0)
                    {
                        switch (key)
                        {
                            case "abbreviation":
                                ABBREVIATION = value;
                                break;
                            case "customDicPath":
                                CUSTOM_DIC_PATH = value;
                                break;
                            case "speechApiOutFileOverrideStrategy":
                                SPEECH_API_OUT_FILE_OVERRIDE_STRATEGY = value.ToUpper();
                                checkAllowedValuesBSBO("speechApiOutFileOverrideStrategy", SPEECH_API_OUT_FILE_OVERRIDE_STRATEGY);
                                break;
                            case "lemmatizingOutFileOverrideStrategy":
                                LEMMATIZING_OUT_FILE_OVERRIDE_STRATEGY = value.ToUpper();
                                checkAllowedValuesBSBO("lemmatizingOutFileOverrideStrategy", LEMMATIZING_OUT_FILE_OVERRIDE_STRATEGY);
                                break;
                            case "ffmpegOutFileOverrideStrategy":
                                FFMPEG_OUT_FILE_OVERRIDE_STRATEGY = value.ToUpper();
                                checkAllowedValuesBSBO("ffmpegOutFileOverrideStrategy", FFMPEG_OUT_FILE_OVERRIDE_STRATEGY);
                                break;
                            case "segmentsOutFileOverrideStrategy":
                                SEGMENTS_OUT_FILE_OVERRIDE_STRATEGY = value.ToUpper();
                                checkAllowedValuesBSBO("segmentsOutFileOverrideStrategy", SEGMENTS_OUT_FILE_OVERRIDE_STRATEGY);
                                break;
                            case "postLemmatizingStrategy":
                                POST_LEMMATIZING_STRATEGY = value.ToUpper();
                                checkAllowedValuesBP("postLemmatizingStrategy", POST_LEMMATIZING_STRATEGY);
                                break;
                            case "title":
                                TITLE = value;
                                break;
                            case "audioFormat":
                                AUDIO_FORMAT = value.ToUpper();
                                break;
                            case "shiftTitleLines":
                                try
                                {
                                    SHIFT_TITLE_LINES = int.Parse(value);
                                    if (SHIFT_TITLE_LINES < 0)
                                    {
                                        throw new InvalidDataException("Cannot read argument 'shiftTitleLines' value '" + value + "'. Invalid value");
                                    }
                                }
                                catch (Exception e)
                                {
                                    throw new InvalidDataException("Cannot read argument 'shiftTitleLines' value '" + value + "' as integer", e);
                                }
                                break;
                            case "tempoCorrection":
                                try
                                {
                                    TEMPO_CORRECTION = double.Parse(value);
                                }
                                catch (Exception e)
                                {
                                    throw new InvalidDataException("Cannot read argument 'tempoCorrection' value '" + value + "' as double", e);
                                }
                                break;
                            case "shift":
                                try
                                {
                                    SHIFT = decimal.Parse(value);
                                }
                                catch (Exception e)
                                {
                                    throw new InvalidDataException("Cannot read argument 'shift' value '" + value + "' as decimal", e);
                                }
                                break;
                            case "verses":
                                try
                                {
                                    VERSES = bool.Parse(value.ToLower());
                                }
                                catch (Exception e)
                                {
                                    throw new InvalidDataException("Cannot read argument 'verses' value '" + value + "' as boolean", e);
                                }
                                break;
                        }
                    }
                }

                if (ABBREVIATION == null) throw new InvalidDataException("The argument 'abbreviation' is mandatory");
                if (TITLE == null) throw new InvalidDataException("The argument 'title' is mandatory");
                if (CUSTOM_DIC_PATH != null )
                {
                    CUSTOM_DIC_PATH = toAbsolutePath(CUSTOM_DIC_PATH);
                    if(!File.Exists(CUSTOM_DIC_PATH))
                    {
                        throw new FileNotFoundException("File not found: '" + CUSTOM_DIC_PATH + "'");
                    }
                }
            }
        }

        private static void checkAllowedValuesBSBO(string key, string value)
        {
            switch (value)
            {
                case BREAK:
                case SKIP:
                case BACKUP:
                case OVERWRITE:
                    return;
                default:
                    throw new InvalidDataException("Unexepcted value '" + value + "'" +
                        " for argument '" + key + "'. Only " +
                        "'" + BREAK + "'/" +
                        "'" + SKIP + "'/" +
                        "'" + BACKUP + "' and " +
                        "'" + OVERWRITE +
                        "' are allowed");
            }
        }
        private static void checkAllowedValuesBP(string key, string value)
        {
            switch (value)
            {
                case BREAK:
                case PROCESS:
                    return;
                default:
                    throw new InvalidDataException("Unexepcted value '" + value + "'" +
                        " for argument '" + key + "'. Only " +
                        "'" + BREAK + "' and " +
                        "'" + PROCESS +
                        "' are allowed");
            }
        }

        private static void checkAllowedValuesMW(string key, string value)
        {
            switch (value)
            {
                case AUDIO_MP3:
                case AUDIO_WAV:
                    return;
                default:
                    throw new InvalidDataException("Unexepcted value '" + value + "'" +
                        " for argument '" + key + "'. Only " +
                        "'" + AUDIO_MP3 + "' and " +
                        "'" + AUDIO_WAV +
                        "' are allowed");
            }
        }


        /**
         * Read and validate the main application settings from the program INI file 'ListenClosely.ini'
         * which is expected in the program installation root
         */
        private static void readIni()
        {
            Console.WriteLine("Read the INI file...");

            GOOGLE_API_KEY_PATH = null;
            GOOGLE_API_PROJECT_ID = null;
            GOOGLE_API_BUCKET_NAME = null;
            FFMPEG_PATH = null;
            PYTHON_PATH = null;

            string dirPath = toAbsolutePath(".");
            try
            {
                var configProvider = new IniConfigurationProvider(
                new IniConfigurationSource()
                {
                    Path = INI_FILE,
                    FileProvider = new PhysicalFileProvider(dirPath)
                }
                );
                configProvider.Load();

                configProvider.TryGet("Tool:FFmpegPath", out FFMPEG_PATH);
                configProvider.TryGet("Tool:PythonPath", out PYTHON_PATH);

                configProvider.TryGet("Google:GoogleAppiKeyPath", out GOOGLE_API_KEY_PATH);
                configProvider.TryGet("Google:GoogleAppiProjectId", out GOOGLE_API_PROJECT_ID);
                configProvider.TryGet("Google:GoogleAppiBucketName", out GOOGLE_API_BUCKET_NAME);
            }
            catch (Exception e)
            {
                throw new InvalidProgramException("Cannot read settings from the file'" + dirPath + "\\" + INI_FILE + "'", e);
            }

            // Hard coded at the mionemt
            SPEECH_API = GOOGLE_API;
        }


        private static void checkHelpRequest(string[] args)
        {
            if(args.Length == 0 || "/?" == args[0] || "-h" == args[0].ToLower() || "--help" == args[0].ToLower())
            {
                Console.WriteLine(APP_NAME + " : v. " + APP_VERSION);
                Console.WriteLine("Call:");
                Console.WriteLine("ListenClosely.exe <path to the run file>");
                System.Environment.Exit(0);
            }
        }

        private static void checkEnvironments()
        {
            Console.WriteLine("Check the environment...");

            // Pre-check the main directoris are here
            string abs = toAbsolutePath(WORK_DIR_PATH);
            if (!Directory.Exists(abs))
            {
                // missing directory
                throw new FileNotFoundException("Directory not found: '" + abs + "'");
            }
            abs = toAbsolutePath(AUDIO_DIR_PATH);
            if (!Directory.Exists(abs))
            {
                // missing directory
                throw new FileNotFoundException("Directory not found: '" + abs + "'");
            }
            abs = toAbsolutePath(OUT_DIR_PATH);
            if (!Directory.Exists(abs))
            {
                // create the missing directory
                try
                {
                    Console.WriteLine("Create directory: '" + abs + "'...");
                    Directory.CreateDirectory(OUT_DIR_PATH);
                }
                catch(Exception e)
                {
                    throw new FileNotFoundException("Cannot create the directory '" + abs + "': " + e.Message, e);
                }
            }
        }

        private static void checkPreconditions()
        {
            Console.WriteLine("Check the settings...");

            // Pre-check if the ServiceAccountKey.json is installed (except: the Google file is already provided and will be reused)
            if ((SPEECH_API == GOOGLE_API && !File.Exists(GOOGLE_JSON_FILE_PATH)) ||
               (SPEECH_API_OUT_FILE_OVERRIDE_STRATEGY == BACKUP || SPEECH_API_OUT_FILE_OVERRIDE_STRATEGY == OVERWRITE))
            {
                if (GOOGLE_API_KEY_PATH == null)
                {
                    throw new InvalidDataException("The mandatory configuration for Google API Key 'GoogleAppiKeyPath' is missing in the file'" + INI_FILE + "'");
                }
                else if (!File.Exists(GOOGLE_API_KEY_PATH))
                {
                    throw new FileNotFoundException("File not found: '" + toAbsolutePath(GOOGLE_API_KEY_PATH) + "'");
                }
                else if (GOOGLE_API_PROJECT_ID == null)
                {
                    throw new InvalidProgramException("The mandatory configuration for GoogleAppiProjectId is missing in the file'" + INI_FILE + "'");
                }
                else if (GOOGLE_API_BUCKET_NAME == null)
                {
                    throw new InvalidProgramException("The mandatory configuration for GoogleAppiBucketName is missing in the file'" + INI_FILE + "'");
                }
            }
            // Pre-check for MS API ... - ?
            else if ((SPEECH_API == MS_API && !File.Exists(MS_CONV_JSON_FILE_PATH)) ||
                (SPEECH_API_OUT_FILE_OVERRIDE_STRATEGY == BACKUP || SPEECH_API_OUT_FILE_OVERRIDE_STRATEGY == OVERWRITE))
            {
                // TODO - check something is required?
            }

            // Pre-check if the FFMpeg installation script exists (except: the audio files are already provided and will be reused
            if (!File.Exists(FLAC_FILE_PATH) || (FFMPEG_OUT_FILE_OVERRIDE_STRATEGY == BACKUP || FFMPEG_OUT_FILE_OVERRIDE_STRATEGY == OVERWRITE))
            {
                if (FFMPEG_PATH == null)
                {
                    throw new InvalidDataException("The mandatory configuration for FFmpeg tool path 'FFmpegPath' is missing in the file'" + INI_FILE + "'");
                }
                else 
                {
                    FFMPEG_PATH = toAbsolutePath(FFMPEG_PATH);
                    if (!File.Exists(FFMPEG_PATH))
                    {
                        throw new FileNotFoundException("File not found: '" + FFMPEG_PATH + "'");
                    }
                }

                if (AUDIO_FORMAT == null) throw new InvalidDataException("The argument 'audioFormat' is mandatory");
                checkAllowedValuesMW("audioFormat", AUDIO_FORMAT);

                // Pre-check the input audio file exists
                // Only the formats WAV and MP3 are supported, yet
                AUDIO_IN_FILE_PATH = AUDIO_DIR_PATH + "/" + ABBREVIATION + ".";
                if (AUDIO_FORMAT == AUDIO_WAV) AUDIO_IN_FILE_PATH += "wav";
                else AUDIO_IN_FILE_PATH += "mp3";
                AUDIO_IN_FILE_PATH = toAbsolutePath(AUDIO_IN_FILE_PATH);
                if (!File.Exists(AUDIO_IN_FILE_PATH))
                {
                    throw new FileNotFoundException("File not found: '" + AUDIO_IN_FILE_PATH + "'");
                }
            }

            if(!File.Exists(ORIG_FILE_PATH))
            {
                throw new FileNotFoundException("File not found: '" + ORIG_FILE_PATH + "'");
            }
        }

        /**
         * Create the temporary Python script direct from the code
         */
        private static void createPythonScript()
        {
            Console.WriteLine("Create temporary file '" + RULEM_PY_PATH + "'...");

            string abs = toAbsolutePath(SCRIPTS_DIR_PATH);
            if(!Directory.Exists(abs))
            try
            {
                Directory.CreateDirectory(abs);
            }
            catch (Exception e)
            {
                throw new FileNotFoundException("Cannot create the directory '" + abs + "': " + e.Message, e);
            }

            StreamWriter sw = null;
            try
            {
                using (sw = new StreamWriter(RULEM_PY_PATH))
                {
                    sw.WriteLine("from pymystem3 import Mystem");
                    sw.WriteLine("from io import open");
                    sw.WriteLine("import sys");
                    sw.WriteLine("import os");
                    sw.WriteLine("m = Mystem()");
                    sw.WriteLine("with open(\"" + PLAIN_FILE_PATH + "\", \"r\", encoding=\"utf8\") as f:");
                    sw.WriteLine("  with open(\"" + LEMS_FILE_PATH + "\", \"w\", encoding=\"utf8\") as g:");
                    sw.WriteLine("    for line in f:");
                    sw.WriteLine("      lemmas = m.lemmatize(line)");
                    sw.WriteLine("      lemmasStr = ''.join(lemmas)");
                    sw.WriteLine("      g.write(lemmasStr)");
                    sw.WriteLine("sys.exit()");
                    sw.Flush();
                }
            }
            catch(Exception e)
            {
                throw new FileNotFoundException("Cannot prepare the Python script: '" + RULEM_PY_PATH + "'");
            }
            finally
            {
                if(sw != null)
                {
                    sw.Close();
                }
            }
        }

        /**
         * After the pytoh call, delete the script
         */
        private static void deletePythonScript()
        {
            if (File.Exists(RULEM_PY_PATH))
            {
                Console.WriteLine("Delete temporary file '" + RULEM_PY_PATH + "'...");
                try
                {
                    File.Delete(RULEM_PY_PATH);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Cannot delete temporary file '" + RULEM_PY_PATH + "'. Please handle manually: " + e.Message);
                }
            }
        }

        private static void shiftSegments(Material mat)
        {
            foreach (var segm in mat.Segments)
            {
                if (segm.StartSec > 0) segm.StartSec += SHIFT;
            }
        }


        /**
         * If the FLAC audio file was converted using the atempo filter 
         * which may be helpful for avoid the speech recognition errors on the Google side,
         * then revert the duration and the start time to the initial values
         */
        private static void changeSegmentsForTempo(Material mat)
        {
            foreach (var segm in mat.Segments)
            {
                segm.StartSec = (decimal)((double)segm.StartSec * TEMPO_CORRECTION);
                segm.LengthSec = (decimal)((double)segm.LengthSec * TEMPO_CORRECTION);
                foreach (var wd in segm.Words)
                {
                    wd.StartSec = (decimal)((double)segm.StartSec * TEMPO_CORRECTION);
                    wd.LengthSec = (decimal)((double)wd.LengthSec * TEMPO_CORRECTION);
                }
            }
        }

        /**
         * This method will add an empty segment on the given position
         * 
         */
        private static void shiftSegments(Material mat, int position, bool markAsTitle)
        {
            shiftSegments(mat, position, null, markAsTitle);
        }

        /**
         * This method will add an empty segment on the given position and add the "hidden" text into the new line
         */
        private static void shiftSegments(Material mat, int position, string hiddenText, bool markAsTitle)
        {
            int i = mat.Segments.Count;

            // requested line position >= the end position - ? ...
            if (position >= mat.Segments[i - 1].ParaIx)
            {
                return;
            }

            i = mat.Segments.Count - 1;
            for (; i > 0; i--)
            {
                if (mat.Segments[i].ParaIx < position)
                {
                    break;
                }
            }
            i++;

            mat.Segments.Insert(i, new Segment());

            // clean the segment on the current position for have no data
            // zero duration
            mat.Segments[i].LengthSec = 0;
            // start point the same as end point of previous segment
            mat.Segments[i].StartSec = mat.Segments[i - 1].StartSec + mat.Segments[i - 1].LengthSec;
            // the paragraph idx 
            mat.Segments[i].ParaIx = mat.Segments[i - 1].ParaIx + 1;
            mat.Segments[i].IsEmptyLine = true;
            // list of words must contain one empty word
            mat.Segments[i].Words.Add(new Word());
            mat.Segments[i].Words[0].Text = "";
            if(!string.IsNullOrEmpty(hiddenText))
            {
                mat.Segments[i].Words[0].Text = hiddenText;
                mat.Segments[i].IsHiddenTextLine = true;
            }
            mat.Segments[i].Words[0].Lemma = "";
            mat.Segments[i].Words[0].Lead = "";
            mat.Segments[i].Words[0].Trail = "";
            // zero duration
            mat.Segments[i].Words[0].LengthSec = 0;
            // start point the same as start point of this segment
            mat.Segments[i].Words[0].StartSec = mat.Segments[i].StartSec;

            i++;
            for (; i < mat.Segments.Count; i++)
            {
                ++mat.Segments[i].ParaIx;
            }

            // also, mark the title lines
            if (markAsTitle)
            {
                i = 0;
                for (; i < position; i++)
                {
                    mat.Segments[i].IsTitleLine = true;
                }
            }
        }

        /**
         * This method will take first N lines and mark they as a separate paragraph. 
         * To be used for mark the title lines in the verses.
         */
        private static void shiftTitleSegments(Material mat)
        {
            shiftSegments(mat, SHIFT_TITLE_LINES, true);
        }

        /**
         * Set the attribute "IsVerse" to true
         */
        private static void markVerses(Material mat)
        {
            foreach (Segment seg in mat.Segments)
            {
                if (!seg.IsTitleLine)
                {
                    seg.IsVerse = true;
                }
            }
        }

        /**
         *  This method will search for the 'addpar' file and parfe the content a a list of numbers, 
         *  then, for each number, it will add an empoty segment
         */
        private static void shiftAdditionalParas(Material mat)
        {
            if (!File.Exists(ADD_PAR_FILE_PATH))
            {
                return;
            }

            AdditionalLines addPars = new AdditionalLines();

            String line;
            using (StreamReader sr = new StreamReader(ADD_PAR_FILE_PATH))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Trim().Length == 0) return;
                    addPars.addLine(line);
                }
            }

            if(addPars.Lines.Count == 0)
            {
                return;
            }


            // add empty segments
            for (int i = 0; i < addPars.Lines.Count; i++)
            {
                AdditionalLines.AdditionalLine al = addPars.Lines[i];
                int addPar = al.Idx;
                if (SHIFT_TITLE_LINES > 0) addPar++;
                addPar += i;

                shiftSegments(mat, addPar, al.HiddenText, false);
            }
        }

        /**
         * The special character (U + FE19) will be recognized as a ellipsis mark and replaced by "<...>"
         * */
        private static void transformEllipsisCharacter(Material mat)
        {
            foreach (Segment seg in mat.Segments)
            {
                foreach (Word word in seg.Words)
                {
                    if (word.Lead.Contains("\uFE19"))
                    {
                        word.Lead = word.Lead.Replace("\uFE19", "<...>");
                    }
                    else if (word.Text.Contains("\uFE19"))
                    {
                        word.Text = word.Text.Replace("\uFE19", "<...>");
                        word.Lemma = word.Lemma.Replace("\uFE19", "");
                    }
                    else if (word.Trail.Contains("\uFE19"))
                    {
                        word.Trail = word.Trail.Replace("\uFE19", "<...>");
                    }
                }
            }
        }


        /**
        * Save the segments file
        */
        private static void saveSegmentsFile(Material mOrig)
        {
            if (saveByStrategy(SEGS_FILE_PATH, SEGMENTS_OUT_FILE_OVERRIDE_STRATEGY))
            {
                Console.WriteLine("Save the segments file '" + SEGS_FILE_PATH + "'...");
                mOrig.SaveJson(SEGS_FILE_PATH);
            }
        }

        /**
         * Save the 3 work files into the output directory
         */
        private static void distibuteFiles()
        {
            copyFile(WEBM_FILE_PATH, false, true);
            copyFile(M4A_FILE_PATH, false, true);
            copyFile(SEGS_FILE_PATH, false, true);
        }

        private static void printInfoForPublish()
        {
            Console.WriteLine("<!-- DOWNLOAD FILE ENTRIES FOR: " + TITLE + " -->");

            Console.WriteLine("<b>" + TITLE + "</b>");
            Console.WriteLine("<a href = \"" + BASE_URL + "/media/" + ABBREVIATION + "-segs.json\">JSON</a>");
            Console.WriteLine("<a href = \"" + BASE_URL + "/media/" + ABBREVIATION + ".m4a\">M4A</a>");
            Console.WriteLine("<a href = \"" + BASE_URL + "/media/" + ABBREVIATION + ".webm\">WEBM</a>");

            Console.WriteLine("<!-- INDEX ENTRY FOR: " + TITLE + " -->");

            Console.WriteLine("<li class=\"title\">");
            Console.WriteLine("<a href=\"" + BASE_URL + "/prose/player.html?ep=" + ABBREVIATION + "\">" + TITLE + "</a>");
            Console.WriteLine("</li>");
        }


        /**
         * Call Python based Yandex lemmatizer as system process
         */
        private static void callLemmatizer()
        {

            if (PYTHON_PATH != null)
            {
                PYTHON_PATH = toAbsolutePath(PYTHON_PATH);
                if (!File.Exists(PYTHON_PATH))
                {
                    throw new FileNotFoundException("File not found: '" + PYTHON_PATH + "'");
                }
            }
            else
            {
                PYTHON_PATH = "python";
            }

            // Pre-check if the input file exists
            if (!File.Exists(PLAIN_FILE_PATH))
            {
                throw new FileNotFoundException("File not found: '" + PLAIN_FILE_PATH + "'");
            }

            // process only if required
            if (!saveByStrategy(LEMS_FILE_PATH, LEMMATIZING_OUT_FILE_OVERRIDE_STRATEGY)) return;

            // create temporary Python script for call the lemmatizer
            createPythonScript();
            try
            {
                // Option overwrite
                callProcess("Python based Yandex lemmatizer", "\"" + PYTHON_PATH + "\"", 
                    "\"" + RULEM_PY_PATH + "\"", false);
            }
            finally
            {
                // delete temporary Python script
                deletePythonScript();
            }
        }

        /**
         * Call Google or MS Speech API and handle the output file
         * Returns the path to API file;
         */
        private static string callSpeechApi()
        {
            string transJson = null;
            string convertedJson = null;
            if (SPEECH_API == GOOGLE_API)
            {
                transJson = GOOGLE_TRANS_JSON_FILE_PATH;
                convertedJson = GOOGLE_JSON_FILE_PATH;
            }
            else
            {
                transJson = MS_TRANS_JSON_FILE_PATH;
                convertedJson = MS_CONV_JSON_FILE_PATH;
            }

            if (!saveByStrategy(convertedJson, SPEECH_API_OUT_FILE_OVERRIDE_STRATEGY)) return transJson;

            Material trans = null;

            // Using Google?
            if (SPEECH_API == GOOGLE_API)
            {
                Console.WriteLine("Call the Google speech API...");

                // If transcription is missing, get it now
                // Transcribe text with Google engine
                GoogleTranscriber.GoogleTranscriber gt = new GoogleTranscriber.GoogleTranscriber(GOOGLE_API_KEY_PATH, GOOGLE_API_PROJECT_ID, GOOGLE_API_BUCKET_NAME);
                gt.Transcribe(FLAC_FILE_PATH, "ru", GOOGLE_JSON_FILE_PATH); // ? "../_work/" + abbreviation + "-conv-goog.json"

                // Set title, serialize
                trans = Material.fromGoogle(GOOGLE_JSON_FILE_PATH);
            }
            // Using MS?
            else
            {
                Console.WriteLine("Call the Microsoft speech API...");

                // -conv-ms.json is the direct output of the MS service
                // It is nout our own Material class serialized
                trans = Material.FromMS(MS_CONV_JSON_FILE_PATH);
            }

            // We have a new transcription: save it
            if (trans != null && transJson != null)
            {
                // Set title, serialize
                trans.Title = TITLE;
                trans.SaveJson(transJson);
                return transJson;
            }
            else
            {
                throw new InvalidProgramException("Broken speech recognition");
            }
        }

        /**
         *  Call FFMgeg tool for convert the original filer (WAV/MP3) into FLAC, WEBM and M4A
         */
        private static void callFfmpeg()
        {
            if (!saveByStrategy(FLAC_FILE_PATH, FFMPEG_OUT_FILE_OVERRIDE_STRATEGY)) return;

            // FLAC
            Console.WriteLine("Create a FLAC file '" + FLAC_FILE_PATH + "'...");
            string arguments = " -i \"" + AUDIO_IN_FILE_PATH + "\""
                + " -af aformat=s16:16000 -ac 1 -start_at_zero -copytb 1 "
                + "\"" + FLAC_FILE_PATH + "\" -y";
            callProcess("convert audio to FLAC", FFMPEG_PATH, arguments, true);
            Console.WriteLine("The FLAC file stored in '" + FLAC_FILE_PATH + "'");

            try
            {
                // WEBM
                Console.WriteLine("Create a WEBM file '" + WEBM_FILE_PATH + "'...");
                arguments = " -i \"" + AUDIO_IN_FILE_PATH + "\""
                    + " -vn -dash 1 \"" + WEBM_FILE_PATH + "\" -y";
                callProcess("convert audio to WEBM", FFMPEG_PATH, arguments, true);
                Console.WriteLine("The WEBM file stored in '" + WEBM_FILE_PATH + "'");
            }
            catch (Exception e)
            {
                // ignore the errors, WEBM and M4A are optional for this processing
                Console.WriteLine("Error by saving WEBM file in '" + WEBM_FILE_PATH + "': " + e.Message);
            }
            try
            {
                // M4A
                Console.WriteLine("Create a M4A file '" + M4A_FILE_PATH + "'...");
                arguments = " -i \"" + AUDIO_IN_FILE_PATH + "\""
                    + " -vn -codec:a aac \"" + M4A_FILE_PATH + "\" -y";
                callProcess("convert audio to M4A", FFMPEG_PATH, arguments, true);
                Console.WriteLine("The M4A file stored in '" + M4A_FILE_PATH + "'");
            }
            catch (Exception e)
            {
                // ignore the errors, WEBM and M4A are optional for this processing
                Console.WriteLine("Error by saving M4A file in '" + M4A_FILE_PATH + "': " + e.Message);
            }
        }

        private static void callProcess(string processName, string fileName, string args, bool skipStdErr)
        {
            Console.WriteLine("Execute the " + processName + " by call: '" + fileName + " " + string.Join(" ", args) + "'");

            ProcessStartInfo start = new ProcessStartInfo();
            // start.EnvironmentVariables.Add("pymystem3.constants.MYSTEM_BIN", toAbsolutePath(SCRIPTS_DIR_PATH));
            start.FileName = fileName;
            start.Arguments = args;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;

            try
            {
                using (Process process = Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string stderr = process.StandardError.ReadToEnd(); // Here are the exceptions from program
                        string result = reader.ReadToEnd(); // Here is the result of StdOut

                        if (!skipStdErr && stderr != null && stderr.Length > 0)
                        {
                            throw new InvalidProgramException("Errors occured by run '" + processName + "': " + stderr);
                        }
                    }
                }
            }
            catch (Win32Exception e)
            {
                throw new InvalidProgramException("Cannot run '" + processName + "': " + e.Message, e);
            }
        }


        private static bool saveByStrategy(string filePath, string strategy)
        {
            if (File.Exists(filePath))
            {
                // Option skip
                if (strategy == SKIP)
                {
                    return false;
                }
                // Option break
                else if (strategy == BREAK)
                {
                    throw new InvalidProgramException("The file " + filePath + " exists already. Break processing.");
                }
                // Option backup
                else if (strategy == BACKUP)
                {
                    copyFile(filePath, true, true);
                }
            }

            // Option overwrite [default]
            return true;
        }


        /**
         * Backup the given file with the timestamp into the same path
         */
        private static bool copyFile(string filePath, bool isBackup, bool ignoreErrors)
        {
            try
            {
                string newFilePath = isBackup ? getBackupName(filePath) : getOutName(filePath);
                string msg = isBackup ? 
                    "Backup file '" + filePath + "' as '" + newFilePath + "'..." : 
                    "Copy file '" + filePath + "' as '" + newFilePath + "'...";
                Console.WriteLine(msg);
                File.Copy(filePath, newFilePath, true);
            }
            catch(Exception e)
            {
                string errorMsg = isBackup ? "Error by backup: " : "Error by copy: " + e.Message;
                if (!ignoreErrors)
                {
                    throw new InvalidProgramException(errorMsg + e.Message, e);
                }
                Console.WriteLine(errorMsg);
                return false;
            }
            return true;
        }

        /**
         * Process the main data preparation flow
         */
        private static void doOrigAlignRus()
        {
            // Prepare the flac file (at least)
            callFfmpeg();

            // this one will be initialized within callSpeechApi
            // Call the speach API
            string transJson = callSpeechApi();

            // Re-load - just to make it easier to uncomment part above independently
            var mTrans = Material.LoadJson(transJson);
            mTrans.Title = TITLE;

            // Read original text and segment paragraphs
            Console.WriteLine("Read original text and segment paragraphs...");
            var mOrig = Material.FromPlainText(ABBREVIATION, true);
            mOrig.Title = TITLE;

            // Save as plain text, for lemmatization
            Console.WriteLine("Save as plain text, for lemmatization...");
            mOrig.SavePlain(PLAIN_FILE_PATH);

            // Align, and infuse timestamps
            TimeFuser fs = new TimeFuser(mTrans, mOrig);
            fs.Fuse();

            // Call the lemmatization
            callLemmatizer();

            // Shift all segment timestamps... Don't ask why
            shiftSegments(mOrig);

            // Read lemmas to JSON from lemmatized text file
            mOrig.AddLemmasRu(LEMS_FILE_PATH);

            Console.WriteLine("Prepare translations based on Open Ruissian dictionary...");

            // Read translations from OpenRussian dictionary
            Dict dict = Dict.FromOpenRussian(MATERIALS_OPENR_WORDS_PATH, MATERIALS_OPENR_TRANSL_PATH);

            Console.WriteLine("Prepare translations based on RuWiki dictionary...");

            // Additionally, read from RuWiki dictionary
            dict.UpdateFromRuWiktionary(MATERIALS_RUWIKI_PATH, false, RU_WIKI_LANGUAGES);

            // Finally, read fom customer dictionary if any
            if (CUSTOM_DIC_PATH != null)
            {
                Console.WriteLine("Prepare translations based on customer dictionary '" + CUSTOM_DIC_PATH + "'...");

                // Extend/override the dictionary by additional customized dictionary
                dict.UpdateFromCustomList(CUSTOM_DIC_PATH);
            }

            // Sort the dictionary entries by language, for each header
            dict.SortByLang();

            // 
            dict.indexDisplayedHeaders();

            // Compose the lemmas-based translations
            dict.FillDict(mOrig);

            // Workaround for mark the title lines if required
            if(SHIFT_TITLE_LINES > 0)
            {
                shiftTitleSegments(mOrig);
            }
            
            // Workaround for mark the empty lines between the strophes of verses if required
            shiftAdditionalParas(mOrig);

            // set the IsVerse attribute to true (may be used iun JavaScript for markup purposes)
            if (VERSES)
            {
                markVerses(mOrig);
            }

            // if tempo correction requested
            if (TEMPO_CORRECTION != 0.0)
            {
                changeSegmentsForTempo(mOrig);
            }

            // change the \uFE19 character to "<...>"; it is actually an optional step,
            // because the "<...>" notation in the original text seems to work, too
            transformEllipsisCharacter(mOrig);

            // save the segments file
            saveSegmentsFile(mOrig);

            distibuteFiles();

            // print info for update the HTML files
            printInfoForPublish();

            // try
            // {
            //     var rg = new ReviewGenerator();
            //     rg.Print(mOrig, "_work/" + abbreviation + "-annot.html");
            // }
            // catch(Exception e)
            // {
            //     Console.WriteLine("Please correct the review generator part!!!");
            // }
        }
        
        static void Main(string[] args)
        {
            try
            {
                setUp(args);
                doOrigAlignRus();
            }
            catch(Exception e)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine(e.Message);
                System.Environment.Exit(1);
            }

            Console.WriteLine("READY");
            System.Environment.Exit(0);
        }
    }
}
