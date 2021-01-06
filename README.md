## Prerequisites, environment

* **Python 3**
* **pymystem3**: Russian lemmatizer by Yandex
https://pypi.org/project/pymystem3/ <br>
`pip install pymystem3`
* **FFMPEG**: Command-line tool for audio conversion <br>
https://ffmpeg.org/download.html
* A Google service account key for access to the Speech API <br>
https://console.cloud.google.com/apis/library/speech.googleapis.com <br>
https://console.cloud.google.com/apis/credentials <br>
   * Place `ServiceAccountKey.json` into solution root
   * Edit values at top of GoogleTranscriber.cs (projectId and bucketName) to match what you set up in API console.
* The [OpenRussian](https://de.openrussian.org/dictionary) dictionary downloaded as a CSV file.
* Specifically, the following files are needed in `_materials\openrussian`:
   * words.csv
   * translations.csv

## Processing a file

* Convert mp3 file to other formats: `Scripts\from-mp3.bat SAMPLE`
* Prepare cleaned-up plain text file of text, one line per paragraph. This will live as `_work\SAMPLE.txt`
* In the Tool project, edit line in Main() to match current conversion: `doOrigAlignRus("SAMPLE", (decimal)0.35, "Чехов: Анна на шее");`
* Run the Tool.
* Edit `Scripts\rulem.py` to work on SAMPLE, and execute.
* Run the Tool again. Second run skips transcription, and finishes annotation because `SAMPLE-lem.txt` is now available.

