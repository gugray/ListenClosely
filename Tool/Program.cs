using Microsoft.Extensions.Configuration.Ini;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
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

        // keys to be read from the run properties file
        private const string PROP_KEY_ABBREVIATION = "abbreviation";
        private const string PROP_KEY_CUSTOM_DIC = "customDicPath";
        private const string PROP_KEY_SPEECH_API_OFOS = "speechApiOutFileOverrideStrategy";
        private const string PROP_KEY_LEMMATIZING_OFOS = "lemmatizingOutFileOverrideStrategy";
        private const string PROP_KEY_FFMPEG_OFOS = "ffmpegOutFileOverrideStrategy";
        private const string PROP_KEY_SEGMENTS_OFOS = "segmentsOutFileOverrideStrategy";
        private const string PROP_KEY_POST_LEMMATIZING_OFOS = "postLemmatizingStrategy";
        private const string PROP_KEY_TITLE = "title";
        private const string PROP_KEY_AUDIO_FORMAT = "audioFormat";
        private const string PROP_KEY_SHIFT_TITLE_LINES = "shiftTitleLines";
        private const string PROP_KEY_TEMPO_CORRECTION = "tempoCorrection";
        private const string PROP_KEY_SHIFT = "shift";
        private const string PROP_KEY_VERSES = "verses";

        // argument keys which can be passed into the program by call
        // in the format: <--long_key:value> or: <-short_key:value>
        private const string ARG_KEY_ABBREVIATION_LONG = "--abbreviation:";
        private const string ARG_KEY_ABBREVIATION_SHORT = "-a:";

        private const string ARG_KEY_CUSTOM_DIC_LONG = "--customDicPath:";
        private const string ARG_KEY_CUSTOM_DIC_SHORT = "-d:";

        private const string ARG_KEY_SPEECH_API_OFOS_LONG = "--speechApiOutFileOverrideStrategy:";
        private const string ARG_KEY_SPEECH_API_OFOS_SHORT = "-sos:";

        private const string ARG_KEY_LEMMATIZING_OFOS_LONG = "--lemmatizingOutFileOverrideStrategy:";
        private const string ARG_KEY_LEMMATIZING_OFOS_SHORT = "-los:";

        private const string ARG_KEY_FFMPEG_OFOS_LONG = "--ffmpegOutFileOverrideStrategy:";
        private const string ARG_KEY_FFMPEG_OFOS_SHORT = "-fos:";

        private const string ARG_KEY_SEGMENTS_OFOS_LONG = "--segmentsOutFileOverrideStrategy:";
        private const string ARG_KEY_SEGMENTS_OFOS_SHORT = "-soos:";

        private const string ARG_KEY_POST_LEMMATIZING_OFOS_LONG = "--postLemmatizingStrategy:";
        private const string ARG_KEY_POST_LEMMATIZING_OFOS_SHORT = "-plos:";

        private const string ARG_KEY_TITLE_LONG = "--title:";
        private const string ARG_KEY_TITLE_SHORT = "-t:";

        private const string ARG_KEY_AUDIO_FORMAT_LONG = "--audioFormat:";
        private const string ARG_KEY_AUDIO_FORMAT_SHORT = "-af:";

        private const string ARG_KEY_SHIFT_TITLE_LINES_LONG = "--shiftTitleLines:";
        private const string ARG_KEY_SHIFT_TITLE_LINES_SHORT = "-stl:";

        private const string ARG_KEY_TEMPO_CORRECTION_LONG = "--tempoCorrection:";
        private const string ARG_KEY_TEMPO_CORRECTION__SHORT = "-tc:";

        private const string ARG_KEY_SHIFT_LONG = "--shift:";
        private const string ARG_KEY_SHIFT_SHORT = "-sh:";

        private const string ARG_KEY_VERSES_LONG = "--verses:";
        private const string ARG_KEY_VERSES_SHORT = "-v:";

        private const string ARG_KEY_PROPERTIES_FILE_LONG = "--propertiesFilePath:";
        private const string ARG_KEY_PROPERTIES_FILE_SHORT = "-p:";

        private const string ARG_KEY_HELP_LONG = "--help";
        private const string ARG_KEY_HELP_SHORT = "-h";
        private const string ARG_KEY_HELP_WIN = "/?";

        // Which entries from RuWiki have to be read
        private static string[] s_ruWikiLanguages = new string[] { "it", "es", "fr", "de" };

        // internal set
        private static string s_origFilePath;
        private static string s_plainFilePath;
        private static string s_lemsFilePath;
        private static string s_addParFilePath;
        private static string s_googleJsonFilePath;
        private static string s_gooleTransJsonFilePath;
        private static string s_msTransJsonFilePath;
        private static string s_msConvJsonFilePath;
        private static string s_segsFilePath;
        private static string s_flacFilePath;
        private static string s_webmFilePath;
        private static string s_m4aFilePath;
        private static string s_audioInFilePath;

        private static string s_speechApi; // MS/Google

        // set by INI
        private static string s_googleApiKeyPath;
        private static string s_gooleApiProjectId;
        private static string s_googleApiBucketName;
        private static string s_ffmpegPath;
        private static string s_pythonPath;


        // set by properties file
        // Abbreviated name of the work data, mandatory
        private static string s_abbreviation;
        // The path to the custom dictionary (nullable)
        private static string s_customDicPath;
        // The work title, which will be displayed on the page, mandatory
        private static string s_title;
        // The audio input file format; currently supported only WAV and MP3
        private static string s_audioFormat;
        // The first X lines which will be marked as title lines of the text
        // default 0
        private static int s_shiftTitleLines;
        // The value to shift the segments timestamps
        // default 0.0
        private static decimal s_shift;
        // The value for tempo correction(0 if not required)
        // default 0.0
        private static double s_tempoCorrection;
        // The flag for mark main text as verses lines
        // default false
        private static bool s_verses;
        // if a speech recognition file found as created by previous run:
        // break / skip [default] / backup / overwrite
        private static string s_speechApiOutFileOverrideStrategy;
        // if a lemmatization file found as created by previous run:
        // break / skip [default] / backup / overwrite
        private static string s_lemmatizingOutFileIOverrideStrategy;
        // if a FLAC file found as created by previous run:
        // break / skip [default] / backup / overwrite
        private static string s_ffmpegOutFileOverrideStrategy;
        // if a ready segments output file found as created by previous run:
        // break / skip / backup / overwrite [default]
        private static string s_segmentsOutFileOverrideStrategy;
        // what to do after the lemmatizator did the work:
        // break [default] / process 
        private static string s_postLemmatizingStrategy;

        private static string s_baseUrl;


        /**
         * Initialize the static variables; read the INI file and the properties; check main pre-conditions
         */
        private static void setUp(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            checkHelpRequest(args);

            checkEnvironment();

            readRunArgs(args);

            s_origFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + s_abbreviation + "-orig.txt");
            s_plainFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + s_abbreviation + "-plain.txt");
            s_lemsFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + s_abbreviation + "-lem.txt");
            s_addParFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + s_abbreviation + "-addpar.txt");
            s_googleJsonFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + s_abbreviation + "-conv-goog.json");
            s_gooleTransJsonFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + s_abbreviation + "-goog.json");
            s_msTransJsonFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + s_abbreviation + "-ms.json");
            s_msConvJsonFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + s_abbreviation + "-conv-ms.json");
            s_segsFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + s_abbreviation + "-segs.json");

            s_flacFilePath = toAbsolutePath(AUDIO_DIR_PATH + "/" + s_abbreviation + ".flac");
            s_webmFilePath = toAbsolutePath(AUDIO_DIR_PATH + "/" + s_abbreviation + ".webm");
            s_m4aFilePath = toAbsolutePath(AUDIO_DIR_PATH + "/" + s_abbreviation + ".m4a");

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
        private static void readRunArgs(string[] args)
        {
            Console.WriteLine("Analyze the call arguments...");

            // set some default values
            s_speechApiOutFileOverrideStrategy = SKIP;
            s_lemmatizingOutFileIOverrideStrategy = SKIP;
            s_ffmpegOutFileOverrideStrategy = SKIP;
            s_segmentsOutFileOverrideStrategy = OVERWRITE;
            s_postLemmatizingStrategy = BREAK;
            s_shiftTitleLines = 0;
            s_shift = 0;
            s_tempoCorrection = 0.0;
            s_verses = false;

            s_abbreviation = null;
            s_customDicPath = null;
            s_title = null;
            s_audioFormat = null;

            string readFromFilePath = null;
            foreach (string a in args)
            {
                // detect at least one passed argument is pointing to the properties file
                if (a.StartsWith(ARG_KEY_PROPERTIES_FILE_LONG) ||
                    a.StartsWith(ARG_KEY_PROPERTIES_FILE_SHORT))
                {
                    readFromFilePath = a;
                    break;
                }
            }

            if (readFromFilePath != null)
            {
                // read the run properties from a file
                readRunPropertiesFromFile(readFromFilePath);
            }
            else
            {
                // read the run properties from arguments
                List<string> errors = readRunPropertiesFromArgs(args);
                if(errors.Count > 0)
                {
                    // errors detected by call
                    foreach(string errMsg in errors)
                    {
                        // Output into stderr
                        Console.Error.WriteLine(errMsg);
                    }
                    System.Environment.Exit(1);
                }
            }

        }

        /**
         * Read the run configuzration from the passed arguments in format: "-p:<PATH>"
         */
        private static List<string> readRunPropertiesFromArgs(string[] args)
        {
            Console.WriteLine("Read the run arguments from the command call...");

            // potential error messages collected for frontend
            List<string> ret = new List<string>();

            foreach (string a in args)
            {
                string[] split = a.Split(":");
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
                        case ARG_KEY_ABBREVIATION_LONG:
                        case ARG_KEY_ABBREVIATION_SHORT:
                            s_abbreviation = value;
                            break;
                        case ARG_KEY_CUSTOM_DIC_LONG:
                        case ARG_KEY_CUSTOM_DIC_SHORT:
                            s_customDicPath = value;
                            break;
                        case ARG_KEY_SPEECH_API_OFOS_LONG:
                        case ARG_KEY_SPEECH_API_OFOS_SHORT:
                            s_speechApiOutFileOverrideStrategy = value.ToUpper();
                            try
                            {
                                checkAllowedValuesBSBO(key, s_speechApiOutFileOverrideStrategy);
                            }
                            catch(Exception e)
                            {
                                ret.Add(e.Message);
                            }
                            break;
                        case ARG_KEY_LEMMATIZING_OFOS_LONG:
                        case ARG_KEY_LEMMATIZING_OFOS_SHORT:
                            s_lemmatizingOutFileIOverrideStrategy = value.ToUpper();
                            try
                            {
                                checkAllowedValuesBSBO(key, s_lemmatizingOutFileIOverrideStrategy);
                            }
                            catch (Exception e)
                            {
                                ret.Add(e.Message);
                            }
                            break;
                        case ARG_KEY_FFMPEG_OFOS_LONG:
                        case ARG_KEY_FFMPEG_OFOS_SHORT:
                            s_ffmpegOutFileOverrideStrategy = value.ToUpper();
                            try
                            {
                                checkAllowedValuesBSBO(key, s_ffmpegOutFileOverrideStrategy);
                            }
                            catch (Exception e)
                            {
                                ret.Add(e.Message);
                            }
                            break;
                        case ARG_KEY_SEGMENTS_OFOS_LONG:
                        case ARG_KEY_SEGMENTS_OFOS_SHORT:
                            s_segmentsOutFileOverrideStrategy = value.ToUpper();
                            try
                            {
                                checkAllowedValuesBSBO(key, s_segmentsOutFileOverrideStrategy);
                            }
                            catch (Exception e)
                            {
                                ret.Add(e.Message);
                            }
                            break;
                        case ARG_KEY_POST_LEMMATIZING_OFOS_LONG:
                        case ARG_KEY_POST_LEMMATIZING_OFOS_SHORT:
                            s_postLemmatizingStrategy = value.ToUpper();
                            try
                            {
                                checkAllowedValuesBP(key, s_postLemmatizingStrategy);
                            }
                            catch (Exception e)
                            {
                                ret.Add(e.Message);
                            }
                            break;
                        case ARG_KEY_TITLE_LONG:
                        case ARG_KEY_TITLE_SHORT:
                            s_title = value;
                            break;
                        case ARG_KEY_AUDIO_FORMAT_LONG:
                        case ARG_KEY_AUDIO_FORMAT_SHORT:
                            s_audioFormat = value.ToUpper();
                            break;
                        case ARG_KEY_SHIFT_TITLE_LINES_LONG:
                        case ARG_KEY_SHIFT_TITLE_LINES_SHORT:
                            try
                            {
                                s_shiftTitleLines = int.Parse(value);
                                if (s_shiftTitleLines < 0)
                                {
                                    ret.Add("Cannot read argument '" + key + "' value '" + value + "'. Invalid value");
                                }
                            }
                            catch (Exception e)
                            {
                                ret.Add("Cannot read argument '" + key + "' value '" + value + "' as integer: " + e.Message);
                            }
                            break;
                        case ARG_KEY_TEMPO_CORRECTION_LONG:
                        case ARG_KEY_TEMPO_CORRECTION__SHORT:
                            try
                            {
                                s_tempoCorrection = double.Parse(value);
                            }
                            catch (Exception e)
                            {
                                ret.Add("Cannot read argument '" + key + "' value '" + value + "' as double: " + e.Message);
                            }
                            break;
                        case ARG_KEY_SHIFT_LONG:
                        case ARG_KEY_SHIFT_SHORT:
                            try
                            {
                                s_shift = decimal.Parse(value);
                            }
                            catch (Exception e)
                            {
                                ret.Add("Cannot read argument '" + key + "' value '" + value + "' as decimal: " + e.Message);
                            }
                            break;
                        case ARG_KEY_VERSES_LONG:
                        case ARG_KEY_VERSES_SHORT:
                            try
                            {
                                s_verses = bool.Parse(value.ToLower());
                            }
                            catch (Exception e)
                            {
                                ret.Add("Cannot read argument '" + key + "' value '" + value + "' as boolean: " + e.Message);
                            }
                            break;
                    }
                }
            }

            if (s_abbreviation == null) ret.Add("The argument '" + ARG_KEY_ABBREVIATION_SHORT + "' or '" + ARG_KEY_ABBREVIATION_LONG + "' is mandatory");
            if (s_title == null) ret.Add("The argument '" + ARG_KEY_TITLE_SHORT + "' or '" + ARG_KEY_TITLE_LONG + "' is mandatory");
            if (s_customDicPath != null)
            {
                s_customDicPath = toAbsolutePath(s_customDicPath);
                if (!File.Exists(s_customDicPath))
                {
                    ret.Add("File not found: '" + s_customDicPath + "'");
                }
            }

            return ret;
        }

        /**
         * Read the run configuzration from a file passed as argument in format: "-p:<PATH>"
         */
        private static void readRunPropertiesFromFile(string filePath)
        {
            Console.WriteLine("Read the run arguments from the properties file...");

            // split for receive the file path
            string path;
            if(filePath.StartsWith(ARG_KEY_PROPERTIES_FILE_SHORT))
            {
                path = filePath.Substring(ARG_KEY_PROPERTIES_FILE_SHORT.Length);
            } 
            else
            {
                path = filePath.Substring(ARG_KEY_PROPERTIES_FILE_LONG.Length);
            }
            
            if(path.Length == 0 )
            {
                throw new FileNotFoundException("No path to the run property file defined or defined with a blank");
            }
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("File not found: '" + path + "'");
            }

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
                            case PROP_KEY_ABBREVIATION:
                                s_abbreviation = value;
                                break;
                            case PROP_KEY_CUSTOM_DIC:
                                s_customDicPath = value;
                                break;
                            case PROP_KEY_SPEECH_API_OFOS:
                                s_speechApiOutFileOverrideStrategy = value.ToUpper();
                                checkAllowedValuesBSBO(PROP_KEY_SPEECH_API_OFOS, s_speechApiOutFileOverrideStrategy);
                                break;
                            case PROP_KEY_LEMMATIZING_OFOS:
                                s_lemmatizingOutFileIOverrideStrategy = value.ToUpper();
                                checkAllowedValuesBSBO(PROP_KEY_LEMMATIZING_OFOS, s_lemmatizingOutFileIOverrideStrategy);
                                break;
                            case PROP_KEY_FFMPEG_OFOS:
                                s_ffmpegOutFileOverrideStrategy = value.ToUpper();
                                checkAllowedValuesBSBO(PROP_KEY_FFMPEG_OFOS, s_ffmpegOutFileOverrideStrategy);
                                break;
                            case PROP_KEY_SEGMENTS_OFOS:
                                s_segmentsOutFileOverrideStrategy = value.ToUpper();
                                checkAllowedValuesBSBO(PROP_KEY_SEGMENTS_OFOS, s_segmentsOutFileOverrideStrategy);
                                break;
                            case PROP_KEY_POST_LEMMATIZING_OFOS:
                                s_postLemmatizingStrategy = value.ToUpper();
                                checkAllowedValuesBP(PROP_KEY_POST_LEMMATIZING_OFOS, s_postLemmatizingStrategy);
                                break;
                            case PROP_KEY_TITLE:
                                s_title = value;
                                break;
                            case PROP_KEY_AUDIO_FORMAT:
                                s_audioFormat = value.ToUpper();
                                break;
                            case PROP_KEY_SHIFT_TITLE_LINES:
                                try
                                {
                                    s_shiftTitleLines = int.Parse(value);
                                    if (s_shiftTitleLines < 0)
                                    {
                                        throw new InvalidDataException("Cannot read argument '" + PROP_KEY_SHIFT_TITLE_LINES + "' value '" + value + "'. Invalid value");
                                    }
                                }
                                catch (Exception e)
                                {
                                    throw new InvalidDataException("Cannot read argument '" + PROP_KEY_SHIFT_TITLE_LINES + "' value '" + value + "' as integer", e);
                                }
                                break;
                            case PROP_KEY_TEMPO_CORRECTION:
                                try
                                {
                                    s_tempoCorrection = double.Parse(value);
                                }
                                catch (Exception e)
                                {
                                    throw new InvalidDataException("Cannot read argument '" + PROP_KEY_TEMPO_CORRECTION + "' value '" + value + "' as double", e);
                                }
                                break;
                            case PROP_KEY_SHIFT:
                                try
                                {
                                    s_shift = decimal.Parse(value);
                                }
                                catch (Exception e)
                                {
                                    throw new InvalidDataException("Cannot read argument '" + PROP_KEY_SHIFT + "' value '" + value + "' as decimal", e);
                                }
                                break;
                            case PROP_KEY_VERSES:
                                try
                                {
                                    s_verses = bool.Parse(value.ToLower());
                                }
                                catch (Exception e)
                                {
                                    throw new InvalidDataException("Cannot read argument '" + PROP_KEY_VERSES + "' value '" + value + "' as boolean", e);
                                }
                                break;
                        }
                    }
                }

                if (s_abbreviation == null) throw new InvalidDataException("The argument '" + PROP_KEY_ABBREVIATION  + "' is mandatory");
                if (s_title == null) throw new InvalidDataException("The argument '" + PROP_KEY_TITLE + "' is mandatory");
                if (s_customDicPath != null)
                {
                    s_customDicPath = toAbsolutePath(s_customDicPath);
                    if (!File.Exists(s_customDicPath))
                    {
                        throw new FileNotFoundException("File not found: '" + s_customDicPath + "'");
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

            s_googleApiKeyPath = null;
            s_gooleApiProjectId = null;
            s_googleApiBucketName = null;
            s_ffmpegPath = null;
            s_pythonPath = null;

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

                configProvider.TryGet("Tool:FFmpegPath", out s_ffmpegPath);
                configProvider.TryGet("Tool:PythonPath", out s_pythonPath);

                configProvider.TryGet("Google:GoogleAppiKeyPath", out s_googleApiKeyPath);
                configProvider.TryGet("Google:GoogleAppiProjectId", out s_gooleApiProjectId);
                configProvider.TryGet("Google:GoogleAppiBucketName", out s_googleApiBucketName);
            }
            catch (Exception e)
            {
                throw new InvalidProgramException("Cannot read settings from the file'" + dirPath + "\\" + INI_FILE + "'", e);
            }

            // Hard coded at the mionemt
            s_speechApi = GOOGLE_API;
        }


        private static void checkHelpRequest(string[] args)
        {
            // default
            bool helpRequested = true;
            if(args.Length >= 0)
            {
                helpRequested = false;
                foreach (string a in args)
                {
                    if (ARG_KEY_HELP_LONG == a || ARG_KEY_HELP_SHORT == a || ARG_KEY_HELP_WIN == a)
                    {
                        helpRequested = true;
                        break;
                    }
                }
            }
            // no args or the help key detected
            if(helpRequested)
            {
                Console.WriteLine(APP_NAME + " : v. " + APP_VERSION);
                Console.WriteLine("Call:");
                Console.WriteLine("ListenClosely.exe <path to the run file>");
                System.Environment.Exit(0);
            }
        }

        private static void checkEnvironment()
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
            if ((s_speechApi == GOOGLE_API && !File.Exists(s_googleJsonFilePath)) ||
               (s_speechApiOutFileOverrideStrategy == BACKUP || s_speechApiOutFileOverrideStrategy == OVERWRITE))
            {
                if (s_googleApiKeyPath == null)
                {
                    throw new InvalidDataException("The mandatory configuration for Google API Key 'GoogleAppiKeyPath' is missing in the file'" + INI_FILE + "'");
                }
                else if (!File.Exists(s_googleApiKeyPath))
                {
                    throw new FileNotFoundException("File not found: '" + toAbsolutePath(s_googleApiKeyPath) + "'");
                }
                else if (s_gooleApiProjectId == null)
                {
                    throw new InvalidProgramException("The mandatory configuration for GoogleAppiProjectId is missing in the file'" + INI_FILE + "'");
                }
                else if (s_googleApiBucketName == null)
                {
                    throw new InvalidProgramException("The mandatory configuration for GoogleAppiBucketName is missing in the file'" + INI_FILE + "'");
                }
            }
            // Pre-check for MS API ... - ?
            else if ((s_speechApi == MS_API && !File.Exists(s_msConvJsonFilePath)) ||
                (s_speechApiOutFileOverrideStrategy == BACKUP || s_speechApiOutFileOverrideStrategy == OVERWRITE))
            {
                // TODO - check something is required?
            }

            // Pre-check if the FFMpeg installation script exists (except: the audio files are already provided and will be reused
            if (!File.Exists(s_flacFilePath) || (s_ffmpegOutFileOverrideStrategy == BACKUP || s_ffmpegOutFileOverrideStrategy == OVERWRITE))
            {
                if (s_ffmpegPath == null)
                {
                    throw new InvalidDataException("The mandatory configuration for FFmpeg tool path 'FFmpegPath' is missing in the file'" + INI_FILE + "'");
                }
                else 
                {
                    s_ffmpegPath = toAbsolutePath(s_ffmpegPath);
                    if (!File.Exists(s_ffmpegPath))
                    {
                        throw new FileNotFoundException("File not found: '" + s_ffmpegPath + "'");
                    }
                }

                if (s_audioFormat == null) throw new InvalidDataException("The argument 'audioFormat' is mandatory");
                checkAllowedValuesMW("audioFormat", s_audioFormat);

                // Pre-check the input audio file exists
                // Only the formats WAV and MP3 are supported, yet
                s_audioInFilePath = AUDIO_DIR_PATH + "/" + s_abbreviation + ".";
                if (s_audioFormat == AUDIO_WAV) s_audioInFilePath += "wav";
                else s_audioInFilePath += "mp3";
                s_audioInFilePath = toAbsolutePath(s_audioInFilePath);
                if (!File.Exists(s_audioInFilePath))
                {
                    throw new FileNotFoundException("File not found: '" + s_audioInFilePath + "'");
                }
            }

            if(!File.Exists(s_origFilePath))
            {
                throw new FileNotFoundException("File not found: '" + s_origFilePath + "'");
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
                    sw.WriteLine("with open(\"" + s_plainFilePath + "\", \"r\", encoding=\"utf8\") as f:");
                    sw.WriteLine("  with open(\"" + s_lemsFilePath + "\", \"w\", encoding=\"utf8\") as g:");
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
                if (segm.StartSec > 0) segm.StartSec += s_shift;
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
                segm.StartSec = (decimal)((double)segm.StartSec * s_tempoCorrection);
                segm.LengthSec = (decimal)((double)segm.LengthSec * s_tempoCorrection);
                foreach (var wd in segm.Words)
                {
                    wd.StartSec = (decimal)((double)segm.StartSec * s_tempoCorrection);
                    wd.LengthSec = (decimal)((double)wd.LengthSec * s_tempoCorrection);
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
            shiftSegments(mat, s_shiftTitleLines, true);
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
            if (!File.Exists(s_addParFilePath))
            {
                return;
            }

            AdditionalLines addPars = new AdditionalLines();

            String line;
            using (StreamReader sr = new StreamReader(s_addParFilePath))
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
                if (s_shiftTitleLines > 0) addPar++;
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
            if (saveByStrategy(s_segsFilePath, s_segmentsOutFileOverrideStrategy))
            {
                Console.WriteLine("Save the segments file '" + s_segsFilePath + "'...");
                mOrig.SaveJson(s_segsFilePath);
            }
        }

        /**
         * Save the 3 work files into the output directory
         */
        private static void distibuteFiles()
        {
            copyFile(s_webmFilePath, false, true);
            copyFile(s_m4aFilePath, false, true);
            copyFile(s_segsFilePath, false, true);
        }

        private static void printInfoForPublish()
        {
            Console.WriteLine("<!-- DOWNLOAD FILE ENTRIES FOR: " + s_title + " -->");

            Console.WriteLine("<b>" + s_title + "</b>");
            Console.WriteLine("<a href = \"" + s_baseUrl + "/media/" + s_abbreviation + "-segs.json\">JSON</a>");
            Console.WriteLine("<a href = \"" + s_baseUrl + "/media/" + s_abbreviation + ".m4a\">M4A</a>");
            Console.WriteLine("<a href = \"" + s_baseUrl + "/media/" + s_abbreviation + ".webm\">WEBM</a>");

            Console.WriteLine("<!-- INDEX ENTRY FOR: " + s_title + " -->");

            Console.WriteLine("<li class=\"title\">");
            Console.WriteLine("<a href=\"" + s_baseUrl + "/prose/player.html?ep=" + s_abbreviation + "\">" + s_title + "</a>");
            Console.WriteLine("</li>");
        }


        /**
         * Call Python based Yandex lemmatizer as system process
         */
        private static void callLemmatizer()
        {

            if (s_pythonPath != null)
            {
                s_pythonPath = toAbsolutePath(s_pythonPath);
                if (!File.Exists(s_pythonPath))
                {
                    throw new FileNotFoundException("File not found: '" + s_pythonPath + "'");
                }
            }
            else
            {
                s_pythonPath = "python";
            }

            // Pre-check if the input file exists
            if (!File.Exists(s_plainFilePath))
            {
                throw new FileNotFoundException("File not found: '" + s_plainFilePath + "'");
            }

            // process only if required
            if (!saveByStrategy(s_lemsFilePath, s_lemmatizingOutFileIOverrideStrategy)) return;

            // create temporary Python script for call the lemmatizer
            createPythonScript();
            try
            {
                // Option overwrite
                callProcess("Python based Yandex lemmatizer", "\"" + s_pythonPath + "\"", 
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
            if (s_speechApi == GOOGLE_API)
            {
                transJson = s_gooleTransJsonFilePath;
                convertedJson = s_googleJsonFilePath;
            }
            else
            {
                transJson = s_msTransJsonFilePath;
                convertedJson = s_msConvJsonFilePath;
            }

            if (!saveByStrategy(convertedJson, s_speechApiOutFileOverrideStrategy)) return transJson;

            Material trans = null;

            // Using Google?
            if (s_speechApi == GOOGLE_API)
            {
                Console.WriteLine("Call the Google speech API...");

                // If transcription is missing, get it now
                // Transcribe text with Google engine
                GoogleTranscriber.GoogleTranscriber gt = new GoogleTranscriber.GoogleTranscriber(s_googleApiKeyPath, s_gooleApiProjectId, s_googleApiBucketName);
                gt.Transcribe(s_flacFilePath, "ru", s_googleJsonFilePath); // ? "../_work/" + abbreviation + "-conv-goog.json"

                // Set title, serialize
                trans = Material.fromGoogle(s_googleJsonFilePath);
            }
            // Using MS?
            else
            {
                Console.WriteLine("Call the Microsoft speech API...");

                // -conv-ms.json is the direct output of the MS service
                // It is nout our own Material class serialized
                trans = Material.FromMS(s_msConvJsonFilePath);
            }

            // We have a new transcription: save it
            if (trans != null && transJson != null)
            {
                // Set title, serialize
                trans.Title = s_title;
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
            if (!saveByStrategy(s_flacFilePath, s_ffmpegOutFileOverrideStrategy)) return;

            // FLAC
            Console.WriteLine("Create a FLAC file '" + s_flacFilePath + "'...");
            string arguments = " -i \"" + s_audioInFilePath + "\""
                + " -af aformat=s16:16000 -ac 1 -start_at_zero -copytb 1 "
                + "\"" + s_flacFilePath + "\" -y";
            callProcess("convert audio to FLAC", s_ffmpegPath, arguments, true);
            Console.WriteLine("The FLAC file stored in '" + s_flacFilePath + "'");

            try
            {
                // WEBM
                Console.WriteLine("Create a WEBM file '" + s_webmFilePath + "'...");
                arguments = " -i \"" + s_audioInFilePath + "\""
                    + " -vn -dash 1 \"" + s_webmFilePath + "\" -y";
                callProcess("convert audio to WEBM", s_ffmpegPath, arguments, true);
                Console.WriteLine("The WEBM file stored in '" + s_webmFilePath + "'");
            }
            catch (Exception e)
            {
                // ignore the errors, WEBM and M4A are optional for this processing
                Console.WriteLine("Error by saving WEBM file in '" + s_webmFilePath + "': " + e.Message);
            }
            try
            {
                // M4A
                Console.WriteLine("Create a M4A file '" + s_m4aFilePath + "'...");
                arguments = " -i \"" + s_audioInFilePath + "\""
                    + " -vn -codec:a aac \"" + s_m4aFilePath + "\" -y";
                callProcess("convert audio to M4A", s_ffmpegPath, arguments, true);
                Console.WriteLine("The M4A file stored in '" + s_m4aFilePath + "'");
            }
            catch (Exception e)
            {
                // ignore the errors, WEBM and M4A are optional for this processing
                Console.WriteLine("Error by saving M4A file in '" + s_m4aFilePath + "': " + e.Message);
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
            mTrans.Title = s_title;

            // Read original text and segment paragraphs
            Console.WriteLine("Read original text and segment paragraphs...");
            var mOrig = Material.FromPlainText(s_abbreviation, true);
            mOrig.Title = s_title;

            // Save as plain text, for lemmatization
            Console.WriteLine("Save as plain text, for lemmatization...");
            mOrig.SavePlain(s_plainFilePath);

            // Align, and infuse timestamps
            TimeFuser fs = new TimeFuser(mTrans, mOrig);
            fs.Fuse();

            // Call the lemmatization
            callLemmatizer();

            // Shift all segment timestamps... Don't ask why
            shiftSegments(mOrig);

            // Read lemmas to JSON from lemmatized text file
            mOrig.AddLemmasRu(s_lemsFilePath);

            Console.WriteLine("Prepare translations based on Open Ruissian dictionary...");

            // Read translations from OpenRussian dictionary
            Dict dict = Dict.FromOpenRussian(MATERIALS_OPENR_WORDS_PATH, MATERIALS_OPENR_TRANSL_PATH);

            Console.WriteLine("Prepare translations based on RuWiki dictionary...");

            // Additionally, read from RuWiki dictionary
            dict.UpdateFromRuWiktionary(MATERIALS_RUWIKI_PATH, false, s_ruWikiLanguages);

            // Finally, read fom customer dictionary if any
            if (s_customDicPath != null)
            {
                Console.WriteLine("Prepare translations based on customer dictionary '" + s_customDicPath + "'...");

                // Extend/override the dictionary by additional customized dictionary
                dict.UpdateFromCustomList(s_customDicPath);
            }

            // Sort the dictionary entries by language, for each header
            dict.SortByLang();

            // 
            dict.indexDisplayedHeaders();

            // Compose the lemmas-based translations
            dict.FillDict(mOrig);

            // Workaround for mark the title lines if required
            if(s_shiftTitleLines > 0)
            {
                shiftTitleSegments(mOrig);
            }
            
            // Workaround for mark the empty lines between the strophes of verses if required
            shiftAdditionalParas(mOrig);

            // set the IsVerse attribute to true (may be used iun JavaScript for markup purposes)
            if (s_verses)
            {
                markVerses(mOrig);
            }

            // if tempo correction requested
            if (s_tempoCorrection != 0.0)
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
