ffmpeg -i ..\_audio\%1.mp3 -vn -dash 1 ..\_audio\%1.webm
ffmpeg -i ..\_audio\%1.mp3 -vn -codec:a aac ..\_audio\%1.m4a
ffmpeg -i ..\_audio\%1.mp3 -af aformat=s16:16000 -ac 1 ..\_audio\%1.flac
copy ..\_audio\%1.mp3 ..\ProsePlayer\public\media
copy ..\_audio\%1.m4a ..\ProsePlayer\public\media
copy ..\_audio\%1.webm ..\ProsePlayer\public\media
