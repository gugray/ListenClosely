## Prerequisites, environment

* **Python 3**
* **pymystem3**: Russian lemmatizer by Yandex
https://pypi.org/project/pymystem3/ <br>
`pip install pymystem3`
* **FFMPEG**: Command-line tool for audio conversion <br>
https://ffmpeg.org/download.html
* The [OpenRussian](https://de.openrussian.org/dictionary) dictionary downloaded as a CSV file.
* Specifically, the following files are needed in `_materials\openrussian`:
   * words.csv
   * translations.csv

### Transcribing with Google

Prerequisite:
* A Google service account key for access to the Google Speech API <br>
https://console.cloud.google.com/apis/library/speech.googleapis.com <br>
https://console.cloud.google.com/apis/credentials <br>
   * Place `ServiceAccountKey.json` into solution root
   * Edit values at top of GoogleTranscriber.cs (projectId and bucketName) to match what you set up in API console.<br>

For switch to Google API, set the boolean variable useMs in the Main method of Tool Program to false.<br>

The code in `Tool` itself takes care of uploading the FLAC file, initiating the transcription, polling for progress, and retrieving
the transcribed text.<br>

### Transcribing with MS Speech-To-Text

Prerequisite:
* A subscription key for the speech-to-text service in Azure App Services<br>
https://portal.azure.com/#blade/Microsoft_Azure_ProjectOxford/CognitiveServicesHub/SpeechServices

For switch to Microsoft API, set the boolean variable useMs in the Main method of Tool Program to true.<br>

How to:
* Transcription is performed by a dedicated tool in the repository, `MSTranscriber`.
* Use from-mp3.cmd to creat a WAV file with the required parameters. Upload the file to a publicly available URL.
* Create a `TranslationConfig.json` in the solution root (you can use the sample file).
Enter your subscription key, and fill in the URL, language, and output file name
* Build and run MSTranscriber. Working directory must be the solution root.
* To prepare, open the Properties of the `Tool` project in Visual Studio (right-click in Solution Explorer),
and under Debug set the solution root as the working directory.

## Processing a file
<pre>
1. Prepare the installation
    - Clone the project into a local repository
    - Create sub folders:
    ./_audio/
    ./_materials/
    ./_tools/
    ./_work/

2. Prepare the input files:
    - the file containing the original text. Name convention:
        [ABBR]-orig.txt
    - the audio file (WAV or MP3). Name convention:
        [ABBR].wav / [ABBR].mp3
    Where [ABBR] is the abbreviation.
Place the audio file into ./_audio
Place the text file into ./_work/

3. Before you start, doublecheck the input text file for:
    3.1. Typos
    3.2. Special characters in the text
    3.3. Exact match between the written text and the read audio data
    3.4. If needed, provide the expected markup, e.g., for the line breaks;
    the markup description see in: ./_materials/ru-custom.txt

4. Prepare the output audio files. 
Navigate to ./Scripts and start (as per input file format):
        from-mp3.cmd [ABBR] WAV
        or
        from-mp3.cmd [ABBR] MP3
Expectede result:
    Three output files will be created:
    ./_audio/[ABBR].flac : will be used for pass to Google / Microsoft speech recognition API
    ./_audio/[ABBR].m4a  : to be uploaded to the tool
    ./_audio/[ABBR].webm : to be uploaded to the tool

5. Open the Tool Program in MS Visual Studio and start the 1st processing step.
Provide changes in the code, in Main method:
            bool breakWork = true; <- set true for this step!
            ...
Enter a new section with:
            useMs = false; <- for use Google engine; for use Microsoft engine, set true
            abbreviation = "[ABBR]"; <- use the same abbreviation as above
            title = "[The text title]"; <- Provide the title line
            shiftTitleLines = 0; <- set the number of lines in original text which reepresent the title
            verses = false; <- if the text is a prose; else set true
            doOrigAlignRus(abbreviation, (decimal)shift, tempoCorrection, customDictFileName, title, shiftTitleLines, verses, breakWork, useMs);
Start the execution. Expectede result:
The *FLAC file will be passed to the desired speech recognizing engine. Following files will be created
(example for use the Google engine):
    ./_work/[ABBR]-conv-goog.json : Google engine output file with timelines
    ./_work/[ABBR]-plain.txt      : The original text file converted to the plain text (1 sentence=1line)
                                  : This one is expected for for lemmatization as next step
    ./_work/[ABBR]-goog.json      : The merged file containing the original text and the Google time markup.

6. Process the Yandex Engine based lemmatization for the input text. Navigate to ./Scripts and start:
    rulem.cmd [ABBR]
Expectede result: the new file will be stored, as the lemmatization output:
    ./_work/[ABBR]-lem.txt

7. The manual control and correction of lemmatization can be done on this point.

8. Open the Tool Program in MS Visual Studio and start the 2nd processing step.
Provide changes in the code, in Main method:
            bool breakWork = false; <- set false for this step!
Start the execution. Expectede result:
The final output file will be created:
    ./_work/[ABBR]-segs.json : The merged file containing: the original text; 
                                 the Google time markup; the dictionaty section; 
                                 and the dictionaty related markup.

9. Upload the files to the server; they will be used in the Javascript 'prose' tool:
    ./_work/[ABBR]-segs.json
    ./_audio/[ABBR].m4a
    ./_audio/[ABBR].webm

10. Control the results in a browser. Known issues:
    10.1. Broken synchronization
        This problem requires a complex analyze and fix. The synchronization issue may be caused by the mismatch 
        between the text and audio data, but also by errors in the speech recognition (e.g., due to bad
        quality of input audio file)
        Mainly: for each JSON sub element in the "segments" session, following mathes are expected:
        - "startSec" - same as "startSec" + "lengthSec" of the previous element;
        - "lengthSec" - in the common case, is > 0 (except the empty lines)
        - the time attributes of sub elements in the sub section "words" are not in use
        - "paraIx" - is the paragraph index, starting by 0
    10.2. Markup issue (text delimiters, title lines)
            - Correct the markup in the original file [ABBR]-orig.txt and/or the call argument shiftTitleLines
            - If manual correctioins was done for lemmatization, backup the file ./_work/[ABBR]-lem.txt
            - delete files 
                ./_work/[ABBR]-conv-goog.json
                ./_work/[ABBR]-plain.txt
                ./_work/[ABBR]-goog.json markup.            
                ./_work/[ABBR]-lem.txt
                ./_work/[ABBR]-segs.json
            - process the steps from 5 and the next
            - if the backup of ./_work/[ABBR]-lem.txt was done, take over the manual correction into the new generated data
    10.3. Typos in the text
            - Correct the original file [ABBR]-orig.txt
            - see 10.2. for next processing
    10.4. Lemmatization issues
            - Manual correct the local file ./_work/[ABBR]-lem.txt
            - Delete the file /_work/[ABBR]-segs.json
            - Process steps 8 and the next
    10.5  Translation issues
            - Decide the best way to correct:
                a. correct the lemmatization file ./_work/[ABBR]-lem.txt
                b. enhance the customer dictionary file ./_materials/ru-custom.txt
                c. (not recommended, only in grave cases) enhance the main dictionary file ./_materials/ruwiktionary.txt
            - Delete the file /_work/[ABBR]-segs.json
            - Process steps 8 and the next
</pre>
## Compiling the player

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