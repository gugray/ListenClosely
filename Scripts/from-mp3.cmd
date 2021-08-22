@echo off
setlocal

rem SET "FFMPEG=..\_tools\ffmpeg-n4.4-79-gde1132a891-win64-gpl-4.4\bin\ffmpeg.exe"
SET "FFMPEG=..\_tools\ffmpeg-2021-01-05-git-66deab3a26-full_build\bin\ffmpeg.exe"


IF "%1"=="" ECHO Please set the audio file name && GOTO END
IF "%2"=="" ECHO Please set the audio file format (MP3 or WAV) && GOTO END
IF NOT "%3"=="" (
    IF "%3"=="-y" ( SET OVERWRITE=-y ) ELSE ( SET ATEMPO=%3 )
)

IF NOT "%4"=="" (
    IF "%ATEMPO%"=="" ( SET ATEMPO=%4 ) 
)


SET "EXT=%2"
IF NOT "%ATEMPO%"=="" ( SET ATEMPO=-filter:a "atempo=%ATEMPO%" )

SET "INPUT_PATH=..\_audio"
SET "OUTPUT_PATH=..\ProsePlayer\public\media"

IF NOT EXIST %INPUT_PATH%\%1.%EXT% ECHO File not found: '%INPUT_PATH%\%1.%EXT%' && GOTO END

:WEBM
echo *** Convert to WEBM
%FFMPEG% -i %INPUT_PATH%\%1.%EXT% %ATEMPO% -vn -dash 1 %INPUT_PATH%\%1.webm %OVERWRITE%
:M4A
echo *** Convert to M4A
%FFMPEG% -i %INPUT_PATH%\%1.%EXT% %ATEMPO% -vn -codec:a aac %INPUT_PATH%\%1.m4a %OVERWRITE%
:FLAC
echo *** Convert to FLAC
%FFMPEG% -i %INPUT_PATH%\%1.%EXT% -af aformat=s16:16000 %ATEMPO% -ac 1 -start_at_zero -copytb 1 %INPUT_PATH%\%1.flac  %OVERWRITE%

COPY %INPUT_PATH%\%1.flac %OUTPUT_PATH%
COPY %INPUT_PATH%\%1.m4a %OUTPUT_PATH%
COPY %INPUT_PATH%\%1.webm %OUTPUT_PATH%

:END