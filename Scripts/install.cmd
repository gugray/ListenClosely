@echo off

SET CURDIR=%cd%

SET PYTHON_ZIP_FILENAME=python-3.10.0-embed-amd64.zip
SET PIP_FILENAME=get-pip.py
SET FFMPEG_ZIP_FILENAME=ffmpeg-master-latest-win64-gpl-shared.zip
SET PYTHON_DOWNLOAD_URL=https://www.python.org/ftp/python/3.10.0/%PYTHON_ZIP_FILENAME%
SET PIP_DOWNLOAD_URL=https://bootstrap.pypa.io/%PIP_FILENAME%
SET FFMPEG_DOWNLOAD_URL=https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/%FFMPEG_ZIP_FILENAME%

echo Install the ListenClosely Tool into %CURDIR%...

rem pre-condition: %CURDIR% esists, containing the unpacked delivery
if not exist "%CURDIR%\_materials" (
    rem prepare the _materials directory
    echo Install the directory %CURDIR%\_materials...
    mkdir "%CURDIR%\_materials"
)
if not exist "%CURDIR%\_work" (
    rem prepare the _work directory
    echo Install the directory %CURDIR%\_work...
    mkdir "%CURDIR%\_work"
)
if not exist "%CURDIR%\_runs" (
    rem prepare the _runs directory
    echo Install the directory %CURDIR%\_runs...
    mkdir "%CURDIR%\_runs"
)

if not exist "%CURDIR%\_tmp" (
    rem prepare the temporary directory
    mkdir "%CURDIR%\_tmp"
)

cd /d "%CURDIR%\_tmp"

if not exist "%CURDIR%\_tmp\%PYTHON_ZIP_FILENAME%" (
    rem download Portable Python
    echo Download Python...
    curl "%PYTHON_DOWNLOAD_URL%" -o .\%PYTHON_ZIP_FILENAME%
    if not exist "%CURDIR%\_tmp\%PYTHON_ZIP_FILENAME%" (
        echo Failed Portable Python download from %PYTHON_DOWNLOAD_URL%
        goto ERR
    )
)

if not exist "%CURDIR%\_tmp\%PIP_FILENAME%" (
    rem download PIP
    echo Download PIP...
    curl -L "%PIP_DOWNLOAD_URL%" -o .\%PIP_FILENAME%
    if not exist "%CURDIR%\_tmp\%PIP_FILENAME%" (
        echo Failed PIP download from %PIP_DOWNLOAD_URL%
        goto ERR
    )
)

if not exist "%CURDIR%\_tools" (
    rem prepare the _tools directory
    echo Install the directory %CURDIR%\_tools...
    mkdir "%CURDIR%\_tools"
)

rem prepare the Python directory
if exist "%CURDIR%\_tools\python" (
    echo Delete the directory %CURDIR%\_tools\python...
    rmdir /S /Q "%CURDIR%\_tools\python"
) 
echo Install the directory %CURDIR%\_tools\python...
mkdir "%CURDIR%\_tools\python"

rem unzip Python
echo Install Python into %CURDIR%\_tools\python...
powershell Expand-Archive ^
        -Path %CURDIR%\_tmp\%PYTHON_ZIP_FILENAME% ^
        -DestinationPath %CURDIR%\_tools\python ^
        -Force

if not exist "%CURDIR%\_tools\python\python.exe" (
    echo Failed Python installation
    goto ERR
)

rem edit the original Python file python310._pth for uncomment the line #import site
powershell ^
    "(gc '%CURDIR%\_tools\python\python310._pth') -replace '#import site', 'import site' | Out-File -encoding ASCII %CURDIR%\_tools\python\python310._pth"

cd /d "%CURDIR%\_tools\python\"

rem install pip
echo Install PIP...
copy "%CURDIR%\_tmp\%PIP_FILENAME%" .
    .\python %PIP_FILENAME% --no-warn-script-location 

rem short additional check: at least the sub orders Scripts and Lib must be created
if not exist "%CURDIR%\_tools\python\Lib" (
    echo Failed Python PIP installation
    goto ERR
)
if not exist "%CURDIR%\_tools\python\Scripts" (
    echo Failed Python PIP installation
    goto ERR
)

rem install pymystem3
echo Install PYMYSTEM3...
.\python -m pip install pymystem3 --no-warn-script-location 

cd /d "%CURDIR%\_tmp"

if not exist "%CURDIR%\_tmp\%FFMPEG_ZIP_FILENAME%" (
    rem download Portable Python
    echo Download FFmpeg...
    curl -L "%FFMPEG_DOWNLOAD_URL%" -o .\%FFMPEG_ZIP_FILENAME%
    if not exist "%CURDIR%\_tmp\%FFMPEG_ZIP_FILENAME%" (
        echo Failed FFmpeg download from %FFMPEG_DOWNLOAD_URL%
        goto ERR
    )
)

rem prepare the Python directory
if exist "%CURDIR%\_tools\ffmpeg" (
    echo Delete the directory %CURDIR%\_tools\ffmpeg...
    rmdir /S /Q "%CURDIR%\_tools\ffmpeg"
) 
echo Install the directory %CURDIR%\_tools\ffmpeg...
mkdir "%CURDIR%\_tools\ffmpeg"

rem unzip FFMpeg
echo Install FFmpeg into %CURDIR%\_tools\ffmpeg...
powershell Expand-Archive ^
        -Path %CURDIR%\_tmp\%FFMPEG_ZIP_FILENAME% ^
        -DestinationPath %CURDIR%\_tools\ffmpeg ^
        -Force

if not exist "%CURDIR%\_tools\ffmpeg\ffmpeg-master-latest-win64-gpl-shared\bin\ffmpeg.exe" (
    echo Failed FFMpeg installation
    goto ERR
)

rem copy samples
echo Install ru-custom.txt.sample into %CURDIR%\_materials...
copy "%CURDIR%\_samples\ru-custom.txt.sample" "%CURDIR%\_materials\"

echo Install run_configuration.sample into %CURDIR%\_runs...
copy "%CURDIR%\_samples\run_configuration.sample" "%CURDIR%\_runs\"

echo Install ListenClosely.ini.sample into %CURDIR%\ListenClosely.ini...
copy "%CURDIR%\_samples\ListenClosely.ini.sample" "%CURDIR%\ListenClosely.ini"

rem append paths to the ini file
echo Enhance installation paths in %CURDIR%\ListenClosely.ini...
powershell -Command ^
    "(gc '%CURDIR%\ListenClosely.ini') -replace 'FFmpegPath =', 'FFmpegPath = %CURDIR%\_tools\ffmpeg\ffmpeg-master-latest-win64-gpl-shared\bin\ffmpeg.exe' | Out-File -encoding ASCII %CURDIR%\ListenClosely.ini"
powershell -Command ^
    "(gc '%CURDIR%\ListenClosely.ini') -replace 'PythonPath =', 'PythonPath = %CURDIR%\_tools\python\python.exe' | Out-File -encoding ASCII %CURDIR%\ListenClosely.ini"
powershell -Command ^
    "(gc '%CURDIR%\ListenClosely.ini') -replace 'GoogleAppiProjectId =', 'GoogleAppiProjectId = listenclosely' | Out-File -encoding ASCII %CURDIR%\ListenClosely.ini"
powershell -Command ^
    "(gc '%CURDIR%\ListenClosely.ini') -replace 'GoogleAppiBucketName =', 'GoogleAppiBucketName = rulit' | Out-File -encoding ASCII %CURDIR%\ListenClosely.ini"

rem move from _tmp for be able to delete it
cd /D "%CURDIR%"

echo Delete the directory %CURDIR%\_tmp...
rmdir /S /Q "%CURDIR%\_tmp"

:ERR
rem always move to the installation path
cd /D "%CURDIR%"