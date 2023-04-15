## Dev & Prod: environment setup

### Python 3 instalation
* Download Python 3 installer from https://www.python.org/downloads/windows/
* Process the Python 3 installation. During the installation, approve the installation of pip tool. Also, approve adding python to Windows system environment variables.

### Alternative: Embedded Python 3 (portable) instalation
* Download Embedded Python file python-3.10.0-embed-amd64.zip from<br>
    https://www.python.org/ftp/python/3.10.0/python-3.10.0-embed-amd64.zip
* Unpack the Zip, e.g. under ./_tools/python-3.10.0-embed-amd64
* In Windows Explorer, navigate to the installation folder of Embedded Python and open the file `python310._pth` for edit. 
Find the line "#import site". Remove the comment mark ("#"), save and close the file.
* Download the file `get-pip.py` from<br>
    https://bootstrap.pypa.io/get-pip.py<br>
and save it in the same folder as Python
* Open the command line, navigate to the installation folder of mbedded Python and call:<br>
    `.\python get-pip.py --no-warn-script-location`

### pymystem3: Russian lemmatizer by Yandex, see https://pypi.org/project/pymystem3/<br>
* By use the standard Python, start the command line and call:<br>
  `pip install pymystem3`
* By use the Embedded Python, start the command line, navigate to the Embedded Python installation path  and call:<br>
  `.\python -m pip install pymystem3`

### FFMPEG: Command-line tool for audio conversion <br>
* Download executable (as zip archive) for Windows from<br>
    https://ffmpeg.org/download.html
* Unpack into a local directory
    
### The [OpenRussian](https://de.openrussian.org/) dictionary as a CSV files <br>
* Note for prod: this is a delivery part. 
* Note for dev: this ais a project part <br>
  - Specifically, the following files are needed in `_materials\openrussian`:<br>
    - `words.csv`
    - `translations.csv`
  - For download of 'words':
    * Navigate to:
      - https://app.togetherdb.com/db/fwoedz5fvtwvq03v/russian3/words
      - https://app.togetherdb.com/db/fwoedz5fvtwvq03v/russian3/translations
    * The data is represented as many tables, each one in a separate tab
    * In the tab head, klick on the symbol "⁝" (right of "🔒") and select "Export table". Apply the tabulation as separator
    * Rename the files locally to "words.csv" and "translations.csv"
    
### The [RuWiktionary](https://ru.wiktionary.org/) dictionary as txt file <br>
* Note for prod: this is a delivery part. 
* Note for dev: this ais a project part <br>
    - TODO: download description for https://dumps.wikimedia.org/ruwiktionary/


## Dev: Setup MS Visual Studio

### Clone the project into a local repository

### Control / adjust the paths in project configuration:
* In the Solution Explorer (Projektmappen Explorer) select the project "Tool". 
    Open the menu "Debug" ("Debuggen") -> Tool: Debug properties (Debugeigenschaften) -> Correct the path of work directory
* Correct the value of workingDirectory in the files "launchsettings.json" in projects: WiktionaryParser, MSTranscriber, Tool
    
### Control the build settings for all projects
* In Solution Explorer (Projektmappen Explorer) select the main Solution (Projektmappe)
* Right mouse click on it -> select Configuration Manager (Konfigurations-Manager)
* Settings for all projects: 
  - Configuration=Release
  - Platform=Any CPU
  - Build for the "Tool" project = yes, else "no"
        
### Create a build for project Tool
* In Solution Explorer (Projektmappen Explorer), select the project "Tool"
* Right mouse click -> Publish (Erstellen) -> Publish (Auswahl veröffentlichen) -> Add a publish profile (Veröffentlichungsprofil hinhufügen)
 - Target (Ziel) => Folder (Ordner) -> Next
 - Specific target (Bestimmtes Ziel) => Folder (Ordner) -> Next
 - Location (Speicherort) => keep default -> Finish
 - Edit the just created puublishing profile with "Show all settings" 
    ("Alle Einstellungen anzeigen") for set:
     - Deployment mode (Bereitstellungsmodus) => Self-contained (Eigenständif)
     - Target runtime (Zielruntime) => win-x86
 - Click on "File publish options" ("Dateiveröffentlichungsoptionen") and set:
     - Produce single file (Einzelne Datei erstellen) = yes
     - Enable ReadyToRun compilation (ReadyToRun-Komprimierung aktivieren) = yes

### Setup Transcribing with Google

* A Google service account key for access to the Google Speech API <br>
https://console.cloud.google.com/apis/library/speech.googleapis.com <br>
https://console.cloud.google.com/apis/credentials <br>
   * Place `ServiceAccountKey.json` into solution root
   * Edit values at top of GoogleTranscriber.cs (projectId and bucketName) to match what you set up in API console.<br>

`For switch to Google API, set the boolean variable useMs in the Main method of Tool Program to false.<br>`

The code in `Tool` itself takes care of uploading the FLAC file, initiating the transcription, polling for progress, and retrieving
the transcribed text.<br>

### Setup Transcribing with MS Speech-To-Text (Development in process; not supported in prod.)

* A subscription key for the speech-to-text service in Azure App Services<br>
https://portal.azure.com/#blade/Microsoft_Azure_ProjectOxford/CognitiveServicesHub/SpeechServices

`For switch to Microsoft API, set the boolean variable useMs in the Main method of Tool Program to true.<br>`

Notes:
* Transcription is performed by a dedicated tool in the repository, `MSTranscriber`.
* Use from-mp3.cmd to creat a WAV file with the required parameters. Upload the file to a publicly available URL.
* Create a `TranslationConfig.json` in the solution root (you can use the sample file).
Enter your subscription key, and fill in the URL, language, and output file name
* Build and run MSTranscriber. Working directory must be the solution root.
* To prepare, open the Properties of the `Tool` project in Visual Studio (right-click in Solution Explorer),
and under Debug set the solution root as the working directory.

## Dev: Compiling the player

### Prerequisites

The player is is based on [Vue.js](https://vuejs.org/); it is currently being built with Node.JS v14. The prerequisites are:

* Node.js v14 ([download page](https://nodejs.org/en/download/releases/))
* Yarn ([download page](https://classic.yarnpkg.com/en/docs/install/#windows-stable))
* Vue CLI ([download page](https://cli.vuejs.org/guide/installation.html))

### To build

* In the `ProsePlayer` folder, restore Node modules:<br/>
`> yarn`
* Build the App:<br/>
`yarn build`

The outcome is in the `dist` folder, which can be published as-is. Notes:

* Before building, make sure the data for the episodes is in `public/media`. Alternatively, the same files can be copied directly
into `dist/media`, or the equivalent folder in the published tool online. The pre-processing script copies its outputs into this folder
by itself.
* To devolop the tool (run it locally, with livereload), you need:<br/>
`yarn serve`
* You only need to run `yarn` again to update the Node modules if the content of `package.json` has changed.

## Prod: Processing files
<pre>
1. Install the delivered data:
    ./_materials/openrussian/**
    ./_materials/ruwiktionary/ruwiktionary.txt
    ./ru-custom.txt.sample
    ./run_configuration.sample
    ./ListenClosely.ini.sample
    ./ListenClosely.exe
2. Meet the dev. & prod. preconditions (see above). 
   Also, ensure you have a working internet connection, 
   which is required for access the Google online service.
3. At the moment, the tool supports in prod. only the Google API. So: 
    - Receive a Google commercial account
    - Receive the the Google service account key `ServiceAccountKey.json`
    - Install it in your local environment
4. Copy the delivered file `./ListenClosely.ini.sample` 
   into the installation directory as `./ListenClosely.ini`. 
   Edit it for set the correct local paths to the files:
    - `GoogleAppiKeyPath` = [the full path to Google ServiceAccountKey.json file]
    - `FFmpegPath` = [the full path to mmpeg executable, ffmpeg.exe]
5. Create the mandatory sub folders:
    ./_audio
    ./_work
6. Prepare the input files:
    - the file containing the original text, in the plain-text format, 
    as UTF-8 without BOM. 
    Name convention:
        [ABBR]-orig.txt
    - the audio file (WAV or MP3). 
    Name convention:
        [ABBR].wav / [ABBR].mp3
    Where [ABBR] is the abbreviation.
Place the audio file into the sub directory ./_audio
Place the text file into the sub directory ./_work
7. If you plan to use a customer dictionary: create the initial file, 
   as a copy of the delivered `ru-custom.txt.sample`, and save it 
   into ./_materials/
By apply the custom changes, follow the instructions in the file.
8. Before you start, doublecheck the input text file for:
    8.1. Typos
    8.2. Special characters in the text
    8.3. Exact match between the written text and the read audio data
    8.4. If needed, provide the expected markup, e.g., for the line breaks.
    The markup description see in: `ru-custom.txt.sample`
9. Prepare the run properties file, based on `run_configuration.sample`.
    Recommended: create the sub folder `_runs` and save the file in this 
    folder as `[ABBR].run`
    Follow the configuration instructions in this file.
9. In the command line, start the program execution with pass the path 
   to just created run properties file, e.g.:
    .\ListenClosely.exe "C:\Meine Dateien\ListenClosely\_runs\TST.run"
10. The program will process following execution steps:
    10.1. Convert the given audio files into the FLAC, M4A and WEBM
          formats, as `[ABBR].flac`, `[ABBR].m4a` and `[ABBR].webm`
    10.2. Pass the FLAC file to the Google speech API
    10.3. Call the Python based lemmatizer for process the text.
        10.3.1. If the key `postLemmatizingStrategy` is not provided 
            in the run file or is set to "BREAK": 
            - stop the work. 
            The manual control and correction of lemmatization 
            can be done on this point; then, you can modify the value 
            of `postLemmatizingStrategy` to "PROCESS" and restart 
            the program.
        10.3.2. If the key `postLemmatizingStrategy` is provided in the 
            run file by set to "PROCESS", next steps will be done.
    10.4. Compose the file `[ABBR]-segs.json`
    10.5. Copy the files `[ABBR].m4a`, `[ABBR].webm` and `[ABBR]-segs.json` 
            into the directory ./_out (will be creted, if missing)
11. Upload the files to the server. The files will be used in the Javascript 
    'prose' application:
    ./_out/[ABBR]-segs.json
    ./_out/[ABBR].m4a
    ./_out/[ABBR].webm
12. Control the results in a browser. Known issues:
    12.1. Broken synchronization between the text and audio; 
          broken indication of current paragraph
        This problem requires a complex analyze and fix. The synchronization 
        issue may be caused by the mismatch between the text and audio data, 
        but also by errors in the speech recognition (e.g., due to bad
        quality of input audio file).
        For support, contact the dev team, provide the [ABBR]-segs.json file.
        Mainly, in this file: for each JSON sub element in the "segments" 
        section, following mathes are expected:
            - "startSec" - same as ("startSec" + "lengthSec") of the previous 
              element;
            - "lengthSec" - in the common case, is > 0 (except the empty lines)
            - the time attributes of sub elements in the sub section 
              "words" are not in use
            - "paraIx" - is the paragraph index, starting by 0
    12.2. Markup issues (missing or wrong placed text delimiters, title lines)
            - Correct the markup in the original file `[ABBR]-orig.txt` 
              and/or the value of `shiftTitleLines` in the run configuration
            - Delete files:
                ./_work/[ABBR]-plain.txt
                ./_work/[ABBR]-lem.txt
                ./_work/[ABBR]-segs.json
            - Restart the call
    12.3. Typos in the text
            - Correct the original file [ABBR]-orig.txt
            - See 12.2. for next processing
    12.4. Lemmatization issues
            - Manual correct the local file ./_work/[ABBR]-lem.txt
            - Delete the file /_work/[ABBR]-segs.json
            - Restart the call
    12.5  Translation issues
            - Decide the best way to correct:
                a. correct the lemmatization file ./_work/[ABBR]-lem.txt
                b. provide/enhance the customer dictionary file
                c. not recommended, only in grave cases! - if the error is 
                detected in a source dictionary file, ask dev team for 
                correct it:
                    ./_materials/openrussian/translations.csv
                    ./_materials/ruwiktionary/ruwiktionary.txt
            - See 12.4. for next processing
</pre>
