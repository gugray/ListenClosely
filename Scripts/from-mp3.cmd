@echo off

SET "FFMPEG=..\_tools\ffmpeg-2021-01-05-git-66deab3a26-full_build\bin\ffmpeg.exe"




IF [%1]==[] ECHO Please set the audio file name && GOTO END
IF [%2]==[] ECHO Please set the audio file format (MP3 or WAV) && GOTO END
SET "EXT=%2"

SET "INPUT_PATH=..\_audio"
SET "OUTPUT_PATH=..\ProsePlayer\public\media"

IF NOT EXIST %INPUT_PATH%\%1.%EXT% ECHO File not found: '%INPUT_PATH%\%1.%EXT%' && GOTO END

%FFMPEG% -i %INPUT_PATH%\%1.%EXT% -vn -dash 1 %INPUT_PATH%\%1.webm
%FFMPEG% -i %INPUT_PATH%\%1.%EXT% -vn -codec:a aac %INPUT_PATH%\%1.m4a
%FFMPEG% -i %INPUT_PATH%\%1.%EXT% -af aformat=s16:16000 -ac 1 %INPUT_PATH%\%1.flac
COPY %INPUT_PATH%\%1.flac %OUTPUT_PATH%
COPY %INPUT_PATH%\%1.m4a %OUTPUT_PATH%
COPY %INPUT_PATH%\%1.webm %OUTPUT_PATH%

:END