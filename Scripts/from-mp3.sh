#!/bin/sh

AUDIO_PATH="../_audio"

echo "*** Convert to WEBM"
ffmpeg -i $AUDIO_PATH/$1.mp3 -vn -dash 1 $AUDIO_PATH/$1.webm

echo "*** Convert to M4A"
ffmpeg -i $AUDIO_PATH/$1.mp3 -vn -codec:a aac $AUDIO_PATH/$1.m4a

echo "*** Convert to FLAC"
ffmpeg -i $AUDIO_PATH/$1.mp3 -af aformat=s16:16000 -ac 1 -start_at_zero -copytb 1 $AUDIO_PATH/$1.flac

echo "*** Convert to WAV"
ffmpeg -i $AUDIO_PATH/$1.mp3 -acodec pcm_s16le -ac 1 -ar 16000 $AUDIO_PATH/$1.wav

#COPY %INPUT_PATH%\%1.m4a %OUTPUT_PATH%
#COPY %INPUT_PATH%\%1.webm %OUTPUT_PATH%
