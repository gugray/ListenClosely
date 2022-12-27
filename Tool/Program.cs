using Microsoft.Extensions.Configuration.Ini;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using File = System.IO.File;

namespace Tool
{
    public class Program
    {
        private const string APP_VERSION = "1.0.2";
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

        public const string OVERWRITE = "OVERWRITE";
        public const string SKIP = "SKIP";
        public const string BACKUP = "BACKUP";
        public const string BREAK = "BREAK";
        public const string PROCESS = "PROCESS";

        private const string GOOGLE_API = "G";
        private const string MS_API = "M";

        public const string AUDIO_MP3 = "MP3";
        public const string AUDIO_WAV = "WAV";

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
        public const string ARG_KEY_ABBREVIATION_LONG = "--abbreviation:";
        public const string ARG_KEY_ABBREVIATION_SHORT = "-a:";

        public const string ARG_KEY_CUSTOM_DIC_LONG = "--customDicPath:";
        public const string ARG_KEY_CUSTOM_DIC_SHORT = "-d:";

        public const string ARG_KEY_SPEECH_API_OFOS_LONG = "--speechApiOutFileOverrideStrategy:";
        public const string ARG_KEY_SPEECH_API_OFOS_SHORT = "-sos:";

        public const string ARG_KEY_LEMMATIZING_OFOS_LONG = "--lemmatizingOutFileOverrideStrategy:";
        public const string ARG_KEY_LEMMATIZING_OFOS_SHORT = "-los:";

        public const string ARG_KEY_FFMPEG_OFOS_LONG = "--ffmpegOutFileOverrideStrategy:";
        public const string ARG_KEY_FFMPEG_OFOS_SHORT = "-fos:";

        public const string ARG_KEY_SEGMENTS_OFOS_LONG = "--segmentsOutFileOverrideStrategy:";
        public const string ARG_KEY_SEGMENTS_OFOS_SHORT = "-soos:";

        public const string ARG_KEY_POST_LEMMATIZING_OFOS_LONG = "--postLemmatizingStrategy:";
        public const string ARG_KEY_POST_LEMMATIZING_OFOS_SHORT = "-plos:";

        public const string ARG_KEY_TITLE_LONG = "--title:";
        public const string ARG_KEY_TITLE_SHORT = "-t:";

        public const string ARG_KEY_AUDIO_FORMAT_LONG = "--audioFormat:";
        public const string ARG_KEY_AUDIO_FORMAT_SHORT = "-af:";

        public const string ARG_KEY_SHIFT_TITLE_LINES_LONG = "--shiftTitleLines:";
        public const string ARG_KEY_SHIFT_TITLE_LINES_SHORT = "-stl:";

        public const string ARG_KEY_TEMPO_CORRECTION_LONG = "--tempoCorrection:";
        public const string ARG_KEY_TEMPO_CORRECTION__SHORT = "-tc:";

        public const string ARG_KEY_SHIFT_LONG = "--shift:";
        public const string ARG_KEY_SHIFT_SHORT = "-sh:";

        public const string ARG_KEY_VERSES_LONG = "--verses:";
        public const string ARG_KEY_VERSES_SHORT = "-v:";

        public const string ARG_KEY_PROPERTIES_FILE_LONG = "--propertiesFilePath:";
        public const string ARG_KEY_PROPERTIES_FILE_SHORT = "-p:";

        public const string ARG_KEY_HELP_LONG = "--help";
        public const string ARG_KEY_HELP_SHORT = "-h";
        public const string ARG_KEY_HELP_WIN = "/?";

        // Which entries from RuWiki have to be read
        private  string[] s_ruWikiLanguages = new string[] { "it", "es", "fr", "de" };

        // internal set
        private string OrigFilePath;
        private string PlainFilePath;
        private string LemsFilePath;
        private string AddParFilePath;
        private string GoogleJsonFilePath;
        private string GooleTransJsonFilePath;
        private string MsTransJsonFilePath;
        private string MsConvJsonFilePath;
        private string SegsFilePath;
        private string FlacFilePath;
        private string WebmFilePath;
        private string M4aFilePath;
        private string AudioInFilePath;

        private string SpeechApi; // MS/Google

        // set by INI
        private string GoogleApiKeyPath;
        private string GooleApiProjectId;
        private string GoogleApiBucketName;
        private string FfmpegPath;
        private string PythonPath;


        // set by properties file
        // Abbreviated name of the work data, mandatory
        private string Abbreviation;
        // The path to the custom dictionary (nullable)
        private string CustomDicPath;
        // The work title, which will be displayed on the page, mandatory
        private  string Title;
        // The audio input file format; currently supported only WAV and MP3
        private string AudioFormat;
        // The first X lines which will be marked as title lines of the text
        // default 0
        private int ShiftTitleLines;
        // The value to shift the segments timestamps
        // default 0.0
        private decimal Shift;
        // The value for tempo correction(0 if not required)
        // default 0.0
        private double TempoCorrection;
        // The flag for mark main text as verses lines
        // default false
        private bool Verses;
        // if a speech recognition file found as created by previous run:
        // break / skip [default] / backup / overwrite
        private string SpeechApiOutFileOverrideStrategy;
        // if a lemmatization file found as created by previous run:
        // break / skip [default] / backup / overwrite
        private string LemmatizingOutFileIOverrideStrategy;
        // if a FLAC file found as created by previous run:
        // break / skip [default] / backup / overwrite
        private string FfmpegOutFileOverrideStrategy;
        // if a ready segments output file found as created by previous run:
        // break / skip / backup / overwrite [default]
        private string SegmentsOutFileOverrideStrategy;
        // what to do after the lemmatizator did the work:
        // break [default] / process 
        private string PostLemmatizingStrategy;

        private static string OUT_DIR;

        private string BaseUrl;

        private bool StaticRun;

        private System.Windows.Controls.TextBlock GuiOutField;

        public Program()
        {
            this.StaticRun = false;
            checkEnvironment();
        }

       private Program(bool staticRun)
       {
           this.StaticRun = staticRun;
        }


        /**
         * Initialize the  variables; read the INI file and the properties; check main pre-conditions
         */
        private void setUp(string[] args)
        {
            checkHelpRequest(args);

            checkEnvironment();

            readRunArgs(args);

            OrigFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + Abbreviation + "-orig.txt");
            PlainFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + Abbreviation + "-plain.txt");
            LemsFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + Abbreviation + "-lem.txt");
            AddParFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + Abbreviation + "-addpar.txt");
            GoogleJsonFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + Abbreviation + "-conv-goog.json");
            GooleTransJsonFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + Abbreviation + "-goog.json");
            MsTransJsonFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + Abbreviation + "-ms.json");
            MsConvJsonFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + Abbreviation + "-conv-ms.json");
            SegsFilePath = toAbsolutePath(WORK_DIR_PATH + "/" + Abbreviation + "-segs.json");

            FlacFilePath = toAbsolutePath(AUDIO_DIR_PATH + "/" + Abbreviation + ".flac");
            WebmFilePath = toAbsolutePath(AUDIO_DIR_PATH + "/" + Abbreviation + ".webm");
            M4aFilePath = toAbsolutePath(AUDIO_DIR_PATH + "/" + Abbreviation + ".m4a");

            readIni();

            checkPreconditions();
        }

        /**
         * For the given abbreviation, try to detect the text and audio files in the environment.
         * Return a string array: 
         * [0][0]=expected file path for original file, [0][1]='1' if found else '0'
         * [1][0]=expected file path for mp3 file, [1][1]='1' if found else '0'
         * [2][0]=expected file path for wav file, [2][1]='1' if found else '0'
         */
        public static string[][] checkInputFilesByAbbreviation(string abbreviation)
        {
            string[][] ret = new string[3][];
            ret[0] = new string[2];
            ret[1] = new string[2];
            ret[2] = new string[2];
            ret[0][0] = toAbsolutePath(WORK_DIR_PATH + "/" + abbreviation + "-orig.txt");
            ret[1][0] = toAbsolutePath(AUDIO_DIR_PATH + "/" + abbreviation + "." + AUDIO_MP3);
            ret[2][0] = toAbsolutePath(AUDIO_DIR_PATH + "/" + abbreviation + "." + AUDIO_WAV);
            ret[0][1] = File.Exists(ret[0][0]) ? "1" : "0";
            ret[1][1] = File.Exists(ret[1][0]) ? "1" : "0";
            ret[2][1] = File.Exists(ret[2][0]) ? "1" : "0";
            return ret;
        }

        /**
         * Read and validate the main application settings from the program INI file 'ListenClosely.ini'
         * which is expected in the program installation root
         */
        private void readIni()
        {
            WriteLine("Read the INI file...");

            GoogleApiKeyPath = null;
            GooleApiProjectId = null;
            GoogleApiBucketName = null;
            FfmpegPath = null;
            PythonPath = null;

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

                configProvider.TryGet("Tool:FFmpegPath", out FfmpegPath);
                configProvider.TryGet("Tool:PythonPath", out PythonPath);

                configProvider.TryGet("Google:GoogleAppiKeyPath", out GoogleApiKeyPath);
                configProvider.TryGet("Google:GoogleAppiProjectId", out GooleApiProjectId);
                configProvider.TryGet("Google:GoogleAppiBucketName", out GoogleApiBucketName);
            }
            catch (Exception e)
            {
                throw new InvalidProgramException("Cannot read settings from the file'" + dirPath + "\\" + INI_FILE + "'", e);
            }

            // Hard coded at the mionemt
            SpeechApi = GOOGLE_API;
        }


        private void checkPreconditions()
        {
            WriteLine("Check the settings...");

            // Pre-check if the ServiceAccountKey.json is installed (except: the Google file is already provided and will be reused)
            if ((SpeechApi == GOOGLE_API && !File.Exists(GoogleJsonFilePath)) ||
               (SpeechApiOutFileOverrideStrategy == BACKUP || SpeechApiOutFileOverrideStrategy == OVERWRITE))
            {
                if (GoogleApiKeyPath == null)
                {
                    throw new InvalidDataException("The mandatory configuration for Google API Key 'GoogleAppiKeyPath' is missing in the file'" + INI_FILE + "'");
                }
                else if (!File.Exists(GoogleApiKeyPath))
                {
                    throw new FileNotFoundException("File not found: '" + toAbsolutePath(GoogleApiKeyPath) + "'");
                }
                else if (GooleApiProjectId == null)
                {
                    throw new InvalidProgramException("The mandatory configuration for GoogleAppiProjectId is missing in the file'" + INI_FILE + "'");
                }
                else if (GoogleApiBucketName == null)
                {
                    throw new InvalidProgramException("The mandatory configuration for GoogleAppiBucketName is missing in the file'" + INI_FILE + "'");
                }
            }
            // Pre-check for MS API ... - ?
            else if ((SpeechApi == MS_API && !File.Exists(MsConvJsonFilePath)) ||
                (SpeechApiOutFileOverrideStrategy == BACKUP || SpeechApiOutFileOverrideStrategy == OVERWRITE))
            {
                // TODO - check something is required?
            }

            // Pre-check if the FFMpeg installation script exists (except: the audio files are already provided and will be reused
            if (!File.Exists(FlacFilePath) || (FfmpegOutFileOverrideStrategy == BACKUP || FfmpegOutFileOverrideStrategy == OVERWRITE))
            {
                if (FfmpegPath == null)
                {
                    throw new InvalidDataException("The mandatory configuration for FFmpeg tool path 'FFmpegPath' is missing in the file'" + INI_FILE + "'");
                }
                else
                {
                    FfmpegPath = toAbsolutePath(FfmpegPath);
                    if (!File.Exists(FfmpegPath))
                    {
                        throw new FileNotFoundException("File not found: '" + FfmpegPath + "'");
                    }
                }

                if (AudioFormat == null) throw new InvalidDataException("The argument 'audioFormat' is mandatory");
                checkAllowedValuesMW("audioFormat", AudioFormat);

                // Pre-check the input audio file exists
                // Only the formats WAV and MP3 are supported, yet
                AudioInFilePath = AUDIO_DIR_PATH + "/" + Abbreviation + ".";
                if (AudioFormat == AUDIO_WAV) AudioInFilePath += "wav";
                else AudioInFilePath += "mp3";
                AudioInFilePath = toAbsolutePath(AudioInFilePath);
                if (!File.Exists(AudioInFilePath))
                {
                    throw new FileNotFoundException("File not found: '" + AudioInFilePath + "'");
                }
            }

            if (!File.Exists(OrigFilePath))
            {
                throw new FileNotFoundException("File not found: '" + OrigFilePath + "'");
            }
        }

        /**
         * Process the main data preparation flow
         */
        private void doOrigAlignRus()
        {
            // Prepare the flac file (at least)
            callFfmpeg();

            // this one will be initialized within callSpeechApi
            // Call the speach API
            string transJson = callSpeechApi();

            // Re-load - just to make it easier to uncomment part above independently
            var mTrans = Material.LoadJson(transJson);
            mTrans.Title = Title;

            // Read original text and segment paragraphs
            WriteLine("Read original text and segment paragraphs...");
            var mOrig = Material.FromPlainText(Abbreviation, true);
            mOrig.Title = Title;

            // Save as plain text, for lemmatization
            WriteLine("Save as plain text, for lemmatization...");
            mOrig.SavePlain(PlainFilePath);

            // Align, and infuse timestamps
            TimeFuser fs = new TimeFuser(mTrans, mOrig);
            fs.Fuse();

            // Call the lemmatization
            callLemmatizer();

            // Shift all segment timestamps... Don't ask why
            shiftSegments(mOrig);

            // Read lemmas to JSON from lemmatized text file
            mOrig.AddLemmasRu(LemsFilePath);

            WriteLine("Prepare translations based on Open Ruissian dictionary...");

            // Read translations from OpenRussian dictionary
            Dict dict = Dict.FromOpenRussian(MATERIALS_OPENR_WORDS_PATH, MATERIALS_OPENR_TRANSL_PATH);

            WriteLine("Prepare translations based on RuWiki dictionary...");

            // Additionally, read from RuWiki dictionary
            dict.UpdateFromRuWiktionary(MATERIALS_RUWIKI_PATH, false, s_ruWikiLanguages);

            // Finally, read fom customer dictionary if any
            if (CustomDicPath != null)
            {
                WriteLine("Prepare translations based on customer dictionary '" + CustomDicPath + "'...");

                // Extend/override the dictionary by additional customized dictionary
                dict.UpdateFromCustomList(CustomDicPath);
            }

            // Sort the dictionary entries by language, for each header
            dict.SortByLang();

            // 
            dict.indexDisplayedHeaders();

            // Compose the lemmas-based translations
            dict.FillDict(mOrig);

            // Workaround for mark the title lines if required
            if (ShiftTitleLines > 0)
            {
                shiftTitleSegments(mOrig);
            }

            // Workaround for mark the empty lines between the strophes of verses if required
            shiftAdditionalParas(mOrig);

            // set the IsVerse attribute to true (may be used iun JavaScript for markup purposes)
            if (Verses)
            {
                markVerses(mOrig);
            }

            // if tempo correction requested
            if (TempoCorrection != 0.0)
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
        }


        public static string toAbsolutePath(string filePath)
        {
            return new FileInfo(filePath).FullName;
        }

        private string getBackupName(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            string extension = fi.Extension;
            string name = fi.FullName;
            name = name.Substring(0, name.Length - extension.Length);
            String now = DateTime.Now.ToString("yyyyMMddHHmmss");
            name += "_" + now + extension;
            return name;
        }
        private string getOutName(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            string name = fi.Name;
            fi = new FileInfo(OUT_DIR_PATH + "/" + name);
            return fi.FullName;
        }

        /**
         * Read and validate the run settings from the properties file passed as run argument
         */
        private void readRunArgs(string[] args)
        {
            WriteLine("Analyze the call arguments...");

            // set some default values
            SpeechApiOutFileOverrideStrategy = SKIP;
            LemmatizingOutFileIOverrideStrategy = SKIP;
            FfmpegOutFileOverrideStrategy = SKIP;
            SegmentsOutFileOverrideStrategy = OVERWRITE;
            PostLemmatizingStrategy = BREAK;
            ShiftTitleLines = 0;
            Shift = 0;
            TempoCorrection = 0.0;
            Verses = false;

            Abbreviation = null;
            CustomDicPath = null;
            Title = null;
            AudioFormat = null;

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
                    throw new InvalidDataException(String.Join(Environment.NewLine, errors.ToArray()));
                }
            }
        }

        /**
         * Read the run configuzration from the passed arguments in format: "-p:<PATH>"
         */
        private List<string> readRunPropertiesFromArgs(string[] args)
        {
            WriteLine("Read the run arguments from the command call...");

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
                string value = String.Join(":", valueArr).Replace("\"", "").Trim();

                key += ":";

                if (value.Length > 0)
                {
                    switch (key)
                    {
                        case ARG_KEY_ABBREVIATION_LONG:
                        case ARG_KEY_ABBREVIATION_SHORT:
                            Abbreviation = value;
                            break;
                        case ARG_KEY_CUSTOM_DIC_LONG:
                        case ARG_KEY_CUSTOM_DIC_SHORT:
                            CustomDicPath = value;
                            break;
                        case ARG_KEY_SPEECH_API_OFOS_LONG:
                        case ARG_KEY_SPEECH_API_OFOS_SHORT:
                            SpeechApiOutFileOverrideStrategy = value.ToUpper();
                            try
                            {
                                checkAllowedValuesBSBO(key, SpeechApiOutFileOverrideStrategy);
                            }
                            catch(Exception e)
                            {
                                ret.Add(e.Message);
                            }
                            break;
                        case ARG_KEY_LEMMATIZING_OFOS_LONG:
                        case ARG_KEY_LEMMATIZING_OFOS_SHORT:
                            LemmatizingOutFileIOverrideStrategy = value.ToUpper();
                            try
                            {
                                checkAllowedValuesBSBO(key, LemmatizingOutFileIOverrideStrategy);
                            }
                            catch (Exception e)
                            {
                                ret.Add(e.Message);
                            }
                            break;
                        case ARG_KEY_FFMPEG_OFOS_LONG:
                        case ARG_KEY_FFMPEG_OFOS_SHORT:
                            FfmpegOutFileOverrideStrategy = value.ToUpper();
                            try
                            {
                                checkAllowedValuesBSBO(key, FfmpegOutFileOverrideStrategy);
                            }
                            catch (Exception e)
                            {
                                ret.Add(e.Message);
                            }
                            break;
                        case ARG_KEY_SEGMENTS_OFOS_LONG:
                        case ARG_KEY_SEGMENTS_OFOS_SHORT:
                            SegmentsOutFileOverrideStrategy = value.ToUpper();
                            try
                            {
                                checkAllowedValuesBSBO(key, SegmentsOutFileOverrideStrategy);
                            }
                            catch (Exception e)
                            {
                                ret.Add(e.Message);
                            }
                            break;
                        case ARG_KEY_POST_LEMMATIZING_OFOS_LONG:
                        case ARG_KEY_POST_LEMMATIZING_OFOS_SHORT:
                            PostLemmatizingStrategy = value.ToUpper();
                            try
                            {
                                checkAllowedValuesBP(key, PostLemmatizingStrategy);
                            }
                            catch (Exception e)
                            {
                                ret.Add(e.Message);
                            }
                            break;
                        case ARG_KEY_TITLE_LONG:
                        case ARG_KEY_TITLE_SHORT:
                            Title = value;
                            break;
                        case ARG_KEY_AUDIO_FORMAT_LONG:
                        case ARG_KEY_AUDIO_FORMAT_SHORT:
                            AudioFormat = value.ToUpper();
                            break;
                        case ARG_KEY_SHIFT_TITLE_LINES_LONG:
                        case ARG_KEY_SHIFT_TITLE_LINES_SHORT:
                            try
                            {
                                ShiftTitleLines = int.Parse(value);
                                if (ShiftTitleLines < 0)
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
                                TempoCorrection = double.Parse(value);
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
                                Shift = decimal.Parse(value);
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
                                Verses = bool.Parse(value.ToLower());
                            }
                            catch (Exception e)
                            {
                                ret.Add("Cannot read argument '" + key + "' value '" + value + "' as boolean: " + e.Message);
                            }
                            break;
                    }
                }
            }

            if (Abbreviation == null) ret.Add("The argument '" + ARG_KEY_ABBREVIATION_SHORT + "' or '" + ARG_KEY_ABBREVIATION_LONG + "' is mandatory");
            if (Title == null) ret.Add("The argument '" + ARG_KEY_TITLE_SHORT + "' or '" + ARG_KEY_TITLE_LONG + "' is mandatory");
            if (CustomDicPath != null)
            {
                CustomDicPath = toAbsolutePath(CustomDicPath);
                if (!File.Exists(CustomDicPath))
                {
                    ret.Add("File not found: '" + CustomDicPath + "'");
                }
            }

            return ret;
        }

        /**
         * Read the run configuzration from a file passed as argument in format: "-p:<PATH>"
         */
        private void readRunPropertiesFromFile(string filePath)
        {
            WriteLine("Read the run arguments from the properties file...");

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
                    string value = String.Join(":", valueArr).Replace("\"", "").Trim();

                    if (value.Length > 0)
                    {
                        switch (key)
                        {
                            case PROP_KEY_ABBREVIATION:
                                Abbreviation = value;
                                break;
                            case PROP_KEY_CUSTOM_DIC:
                                CustomDicPath = value;
                                break;
                            case PROP_KEY_SPEECH_API_OFOS:
                                SpeechApiOutFileOverrideStrategy = value.ToUpper();
                                checkAllowedValuesBSBO(PROP_KEY_SPEECH_API_OFOS, SpeechApiOutFileOverrideStrategy);
                                break;
                            case PROP_KEY_LEMMATIZING_OFOS:
                                LemmatizingOutFileIOverrideStrategy = value.ToUpper();
                                checkAllowedValuesBSBO(PROP_KEY_LEMMATIZING_OFOS, LemmatizingOutFileIOverrideStrategy);
                                break;
                            case PROP_KEY_FFMPEG_OFOS:
                                FfmpegOutFileOverrideStrategy = value.ToUpper();
                                checkAllowedValuesBSBO(PROP_KEY_FFMPEG_OFOS, FfmpegOutFileOverrideStrategy);
                                break;
                            case PROP_KEY_SEGMENTS_OFOS:
                                SegmentsOutFileOverrideStrategy = value.ToUpper();
                                checkAllowedValuesBSBO(PROP_KEY_SEGMENTS_OFOS, SegmentsOutFileOverrideStrategy);
                                break;
                            case PROP_KEY_POST_LEMMATIZING_OFOS:
                                PostLemmatizingStrategy = value.ToUpper();
                                checkAllowedValuesBP(PROP_KEY_POST_LEMMATIZING_OFOS, PostLemmatizingStrategy);
                                break;
                            case PROP_KEY_TITLE:
                                Title = value;
                                break;
                            case PROP_KEY_AUDIO_FORMAT:
                                AudioFormat = value.ToUpper();
                                break;
                            case PROP_KEY_SHIFT_TITLE_LINES:
                                try
                                {
                                    ShiftTitleLines = int.Parse(value);
                                    if (ShiftTitleLines < 0)
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
                                    TempoCorrection = double.Parse(value);
                                }
                                catch (Exception e)
                                {
                                    throw new InvalidDataException("Cannot read argument '" + PROP_KEY_TEMPO_CORRECTION + "' value '" + value + "' as double", e);
                                }
                                break;
                            case PROP_KEY_SHIFT:
                                try
                                {
                                    Shift = decimal.Parse(value);
                                }
                                catch (Exception e)
                                {
                                    throw new InvalidDataException("Cannot read argument '" + PROP_KEY_SHIFT + "' value '" + value + "' as decimal", e);
                                }
                                break;
                            case PROP_KEY_VERSES:
                                try
                                {
                                    Verses = bool.Parse(value.ToLower());
                                }
                                catch (Exception e)
                                {
                                    throw new InvalidDataException("Cannot read argument '" + PROP_KEY_VERSES + "' value '" + value + "' as boolean", e);
                                }
                                break;
                        }
                    }
                }

                if (Abbreviation == null) throw new InvalidDataException("The argument '" + PROP_KEY_ABBREVIATION  + "' is mandatory");
                if (Title == null) throw new InvalidDataException("The argument '" + PROP_KEY_TITLE + "' is mandatory");
                if (CustomDicPath != null)
                {
                    CustomDicPath = toAbsolutePath(CustomDicPath);
                    if (!File.Exists(CustomDicPath))
                    {
                        throw new FileNotFoundException("File not found: '" + CustomDicPath + "'");
                    }
                }
            }
        }

        private void checkAllowedValuesBSBO(string key, string value)
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
        private void checkAllowedValuesBP(string key, string value)
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


        private void checkAllowedValuesMW(string key, string value)
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




        private void checkHelpRequest(string[] args)
        {
            // default
            bool helpRequested = true;
            if(args.Length > 0)
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
            if(helpRequested && StaticRun)
            {
                Console.WriteLine(APP_NAME + " : v. " + APP_VERSION);
                Console.WriteLine("Call:");
                Console.WriteLine("[1] ListenClosely.exe " + ARG_KEY_PROPERTIES_FILE_SHORT + "<path to the run file> or: " + ARG_KEY_PROPERTIES_FILE_LONG + "<path to the run file>");
                Console.WriteLine("[2] ListenClosely.exe [argument]:<value>. Arguments:");
                Console.WriteLine(ARG_KEY_ABBREVIATION_SHORT + "<abbreviation> or: " + ARG_KEY_ABBREVIATION_LONG + "<abbreviation>");
                Console.WriteLine(ARG_KEY_TITLE_SHORT + "<title> or: " + ARG_KEY_TITLE_LONG + "<title>");
                Console.WriteLine(ARG_KEY_AUDIO_FORMAT_SHORT + "<audio format: WAV|MP3> or: " + ARG_KEY_AUDIO_FORMAT_LONG + "<audio format: WAV|MP3>");
                Console.WriteLine(ARG_KEY_CUSTOM_DIC_SHORT + "<path to custom dictionary> or: " + ARG_KEY_CUSTOM_DIC_LONG + "<path to custom dictionary>");
                Console.WriteLine(ARG_KEY_SHIFT_TITLE_LINES_SHORT + "<number of title lines> or: " + ARG_KEY_SHIFT_TITLE_LINES_LONG + "<number of title lines>");
                Console.WriteLine(ARG_KEY_VERSES_SHORT + "<mark as verses: true|false> or: " + ARG_KEY_VERSES_LONG + "<mark as verses: true|false>");
                Console.WriteLine(ARG_KEY_POST_LEMMATIZING_OFOS_SHORT + "<post lemmatizing strategy: BREAK|PROCESS> or: " + ARG_KEY_POST_LEMMATIZING_OFOS_LONG + "<post lemmatizing strategy: BREAK|PROCESS>");
                Console.WriteLine(ARG_KEY_FFMPEG_OFOS_SHORT + "<converted audio output files overwrite strategy: SKIP|OVERWRITE|BACKUP|BREAK> or: " + ARG_KEY_FFMPEG_OFOS_LONG + "<converted audio output files overwrite strategy: SKIP|OVERWRITE|BACKUP|BREAK>");
                Console.WriteLine(ARG_KEY_SPEECH_API_OFOS_SHORT + "<speech API output file overwrite strategy: SKIP|OVERWRITE|BACKUP|BREAK> or: " + ARG_KEY_SPEECH_API_OFOS_LONG + "<speech API output file overwrite strategy: SKIP|OVERWRITE|BACKUP|BREAK>");
                Console.WriteLine(ARG_KEY_LEMMATIZING_OFOS_SHORT + "<lemmatizing output file overwrite strategy: SKIP|OVERWRITE|BACKUP|BREAK> or: " + ARG_KEY_LEMMATIZING_OFOS_LONG + "<lemmatizing output file overwrite strategy: SKIP|OVERWRITE|BACKUP|BREAK>");

                System.Environment.Exit(0);
            }
        }

        private void checkEnvironment()
        {
            WriteLine("Check the environment...");

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
                    WriteLine("Create directory: '" + abs + "'...");
                    Directory.CreateDirectory(OUT_DIR_PATH);
                }
                catch(Exception e)
                {
                    throw new FileNotFoundException("Cannot create the directory '" + abs + "': " + e.Message, e);
                }
            }
        }

        /**
         * Create the temporary Python script direct from the code
         */
        private void createPythonScript()
        {
            WriteLine("Create temporary file '" + RULEM_PY_PATH + "'...");

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
                    sw.WriteLine("with open(\"" + PlainFilePath + "\", \"r\", encoding=\"utf8\") as f:");
                    sw.WriteLine("  with open(\"" + LemsFilePath + "\", \"w\", encoding=\"utf8\") as g:");
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
        private void deletePythonScript()
        {
            if (File.Exists(RULEM_PY_PATH))
            {
                WriteLine("Delete temporary file '" + RULEM_PY_PATH + "'...");
                try
                {
                    File.Delete(RULEM_PY_PATH);
                }
                catch(Exception e)
                {
                    WriteLine("Cannot delete temporary file '" + RULEM_PY_PATH + "'. Please handle manually: " + e.Message);
                }
            }
        }

        private void shiftSegments(Material mat)
        {
            foreach (var segm in mat.Segments)
            {
                if (segm.StartSec > 0) segm.StartSec += Shift;
            }
        }


        /**
         * If the FLAC audio file was converted using the atempo filter 
         * which may be helpful for avoid the speech recognition errors on the Google side,
         * then revert the duration and the start time to the initial values
         */
        private void changeSegmentsForTempo(Material mat)
        {
            foreach (var segm in mat.Segments)
            {
                segm.StartSec = (decimal)((double)segm.StartSec * TempoCorrection);
                segm.LengthSec = (decimal)((double)segm.LengthSec * TempoCorrection);
                foreach (var wd in segm.Words)
                {
                    wd.StartSec = (decimal)((double)segm.StartSec * TempoCorrection);
                    wd.LengthSec = (decimal)((double)wd.LengthSec * TempoCorrection);
                }
            }
        }

        /**
         * This method will add an empty segment on the given position
         * 
         */
        private void shiftSegments(Material mat, int position, bool markAsTitle)
        {
            shiftSegments(mat, position, null, markAsTitle);
        }

        /**
         * This method will add an empty segment on the given position and add the "hidden" text into the new line
         */
        private void shiftSegments(Material mat, int position, string hiddenText, bool markAsTitle)
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
        private void shiftTitleSegments(Material mat)
        {
            shiftSegments(mat, ShiftTitleLines, true);
        }

        /**
         * Set the attribute "IsVerse" to true
         */
        private void markVerses(Material mat)
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
        private void shiftAdditionalParas(Material mat)
        {
            if (!File.Exists(AddParFilePath))
            {
                return;
            }

            AdditionalLines addPars = new AdditionalLines();

            String line;
            using (StreamReader sr = new StreamReader(AddParFilePath))
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
                if (ShiftTitleLines > 0) addPar++;
                addPar += i;

                shiftSegments(mat, addPar, al.HiddenText, false);
            }
        }

        /**
         * The special character (U + FE19) will be recognized as a ellipsis mark and replaced by "<...>"
         * */
        private void transformEllipsisCharacter(Material mat)
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
        private void saveSegmentsFile(Material mOrig)
        {
            if (saveByStrategy(SegsFilePath, SegmentsOutFileOverrideStrategy))
            {
                WriteLine("Save the segments file '" + SegsFilePath + "'...");
                mOrig.SaveJson(SegsFilePath);
            }
        }

        /**
         * Save the 3 work files into the output directory
         */
        private void distibuteFiles()
        {
            copyFile(WebmFilePath, false, true);
            copyFile(M4aFilePath, false, true);
            copyFile(SegsFilePath, false, true);
        }

        private void printInfoForPublish()
        {
            if (StaticRun) 
            {
                Console.WriteLine("<!-- DOWNLOAD FILE ENTRIES FOR: " + Title + " -->");

                Console.WriteLine("<b>" + Title + "</b>");
                Console.WriteLine("<a href = \"" + BaseUrl + "/media/" + Abbreviation + "-segs.json\">JSON</a>");
                Console.WriteLine("<a href = \"" + BaseUrl + "/media/" + Abbreviation + ".m4a\">M4A</a>");
                Console.WriteLine("<a href = \"" + BaseUrl + "/media/" + Abbreviation + ".webm\">WEBM</a>");

                Console.WriteLine("<!-- INDEX ENTRY FOR: " + Title + " -->");

                Console.WriteLine("<li class=\"title\">");
                Console.WriteLine("<a href=\"" + BaseUrl + "/prose/player.html?ep=" + Abbreviation + "\">" + Title + "</a>");
                Console.WriteLine("</li>");
            }
        }


        /**
         * Call Python based Yandex lemmatizer as system process
         */
        private void callLemmatizer()
        {

            if (PythonPath != null)
            {
                PythonPath = toAbsolutePath(PythonPath);
                if (!File.Exists(PythonPath))
                {
                    throw new FileNotFoundException("File not found: '" + PythonPath + "'");
                }
            }
            else
            {
                PythonPath = "python";
            }

            // Pre-check if the input file exists
            if (!File.Exists(PlainFilePath))
            {
                throw new FileNotFoundException("File not found: '" + PlainFilePath + "'");
            }

            // process only if required
            if (!saveByStrategy(LemsFilePath, LemmatizingOutFileIOverrideStrategy)) return;

            // create temporary Python script for call the lemmatizer
            createPythonScript();
            try
            {
                // Option overwrite
                callProcess("Python based Yandex lemmatizer", "\"" + PythonPath + "\"", 
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
        private string callSpeechApi()
        {
            string transJson = null;
            string convertedJson = null;
            if (SpeechApi == GOOGLE_API)
            {
                transJson = GooleTransJsonFilePath;
                convertedJson = GoogleJsonFilePath;
            }
            else
            {
                transJson = MsTransJsonFilePath;
                convertedJson = MsConvJsonFilePath;
            }

            if (!saveByStrategy(convertedJson, SpeechApiOutFileOverrideStrategy)) return transJson;

            Material trans = null;

            // Using Google?
            if (SpeechApi == GOOGLE_API)
            {
                WriteLine("Call the Google speech API...");

                // If transcription is missing, get it now
                // Transcribe text with Google engine
                GoogleTranscriber.GoogleTranscriber gt = new GoogleTranscriber.GoogleTranscriber(GoogleApiKeyPath, GooleApiProjectId, GoogleApiBucketName);
                gt.Transcribe(FlacFilePath, "ru", GoogleJsonFilePath); // ? "../_work/" + abbreviation + "-conv-goog.json"

                // Set title, serialize
                trans = Material.fromGoogle(GoogleJsonFilePath);
            }
            // Using MS?
            else
            {
                WriteLine("Call the Microsoft speech API...");

                // -conv-ms.json is the direct output of the MS service
                // It is nout our own Material class serialized
                trans = Material.FromMS(MsConvJsonFilePath);
            }

            // We have a new transcription: save it
            if (trans != null && transJson != null)
            {
                // Set title, serialize
                trans.Title = Title;
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
        private void callFfmpeg()
        {
            if (!saveByStrategy(FlacFilePath, FfmpegOutFileOverrideStrategy)) return;

            // FLAC
            WriteLine("Create a FLAC file '" + FlacFilePath + "'...");
            string arguments = " -i \"" + AudioInFilePath + "\""
                + " -af aformat=s16:16000 -ac 1 -start_at_zero -copytb 1 "
                + "\"" + FlacFilePath + "\" -y";
            callProcess("convert audio to FLAC", FfmpegPath, arguments, true);
            WriteLine("The FLAC file stored in '" + FlacFilePath + "'");

            try
            {
                // WEBM
                WriteLine("Create a WEBM file '" + WebmFilePath + "'...");
                arguments = " -i \"" + AudioInFilePath + "\""
                    + " -vn -dash 1 \"" + WebmFilePath + "\" -y";
                callProcess("convert audio to WEBM", FfmpegPath, arguments, true);
                WriteLine("The WEBM file stored in '" + WebmFilePath + "'");
            }
            catch (Exception e)
            {
                // ignore the errors, WEBM and M4A are optional for this processing
                WriteLine("Error by saving WEBM file in '" + WebmFilePath + "': " + e.Message);
            }
            try
            {
                // M4A
                WriteLine("Create a M4A file '" + M4aFilePath + "'...");
                arguments = " -i \"" + AudioInFilePath + "\""
                    + " -vn -codec:a aac \"" + M4aFilePath + "\" -y";
                callProcess("convert audio to M4A", FfmpegPath, arguments, true);
                WriteLine("The M4A file stored in '" + M4aFilePath + "'");
            }
            catch (Exception e)
            {
                // ignore the errors, WEBM and M4A are optional for this processing
                WriteLine("Error by saving M4A file in '" + M4aFilePath + "': " + e.Message);
            }
        }

        private void callProcess(string processName, string fileName, string args, bool skipStdErr)
        {
            WriteLine("Execute the " + processName + " by call: '" + fileName + " " + string.Join(" ", args) + "'");

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

        private bool saveByStrategy(string filePath, string strategy)
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
        private bool copyFile(string filePath, bool isBackup, bool ignoreErrors)
        {
            try
            {
                string newFilePath = isBackup ? getBackupName(filePath) : getOutName(filePath);
                string msg = isBackup ? 
                    "Backup file '" + filePath + "' as '" + newFilePath + "'..." : 
                    "Copy file '" + filePath + "' as '" + newFilePath + "'...";
                WriteLine(msg);
                File.Copy(filePath, newFilePath, true);
            }
            catch(Exception e)
            {
                string errorMsg = isBackup ? "Error by backup: " : "Error by copy: " + e.Message;
                if (!ignoreErrors)
                {
                    throw new InvalidProgramException(errorMsg + e.Message, e);
                }
                WriteLine(errorMsg);
                return false;
            }
            return true;
        }

        public static string getOutDirPath()
        {
            if (OUT_DIR == null) OUT_DIR = toAbsolutePath(OUT_DIR_PATH);
            return OUT_DIR;
        }

        private void WriteLine(string line)
        {
            if (StaticRun) Console.WriteLine(line);
            else if (GuiOutField != null)
            {
                GuiOutField.Text += (Environment.NewLine + line);
                GuiOutField.UpdateLayout();

            }
        }

        public void setGuiOutField(System.Windows.Controls.TextBlock outField)
        {
            this.GuiOutField = outField;
        }

        public void start(string[] args)
        {
            WriteLine("START...");
            setUp(args);
            doOrigAlignRus();
            WriteLine("READY");
        }

        
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Program p = new Program(true);
            try
            {
                p.start(args);
            }
            catch(Exception ex)
            {
                Console.WriteLine("ERROR");
                Console.Error.WriteLine(ex.Message);
                System.Environment.Exit(1);
            }

            Console.WriteLine("READY");
            System.Environment.Exit(0);

        }
    }
}
