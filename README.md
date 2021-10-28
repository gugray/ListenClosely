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
* A Google service account key for access to the Speech API <br>
https://console.cloud.google.com/apis/library/speech.googleapis.com <br>
https://console.cloud.google.com/apis/credentials <br>
   * Place `ServiceAccountKey.json` into solution root
   * Edit values at top of GoogleTranscriber.cs (projectId and bucketName) to match what you set up in API console.

The code in `Tool` itself takes care of uploading the FLAC file, initiating the transcription, polling for progress, and retrieving
the transcribed text.

### Transcribing with MS Speech-To-Text

Prerequisite:
* A subscription key for the speech-to-text service in Azure App Services<br>
https://portal.azure.com/#blade/Microsoft_Azure_ProjectOxford/CognitiveServicesHub/SpeechServices

How to:
* Transcription is performed by a dedicated tool in the repository, `MSTranscriber`.
* Use from-mp3.cmd to creat a WAV file with the required parameters. Upload the file to a publicly available URL.
* Create a `TranslationConfig.json` in the solution root (you can use the sample file).
Enter your subscription key, and fill in the URL, language, and output file name
* Build and run MSTranscriber. Working directory must be the solution root.

## Processing a file

To prepare, open the Properties of the `Tool` project in Visual Studio (right-click in Solution Explorer),
and under Debug set the solution root as the working directory.

* Convert mp3 file to other formats: `Scripts\from-mp3.bat SAMPLE`
* Prepare cleaned-up plain text file of text, one line per paragraph. This will live as `_work\SAMPLE-orig.txt`
* In the Tool project, edit line in Main() to match current conversion: `doOrigAlignRus("SAMPLE", (decimal)0.35, "Чехов: Анна на шее");`
* Edit the Method `doOrigAlignRus` for uncomment the `return`.
* Run the Tool.
* Edit `Scripts\rulem.py` to work on SAMPLE, and execute: python3 rulem.py
* Edit the Method `doOrigAlignRus` for comment the `return`.
* Run the Tool again. Second run skips transcription, and finishes annotation because `SAMPLE-lem.txt` is now available.

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


