﻿# Hyba Video Compressor | Славянский сжиматель видео
Эта штука там короче при помощи там вот ну вот это вот ffmpeg там типо ffprobe
чё то там сжимает вообщем....

# Как собрать | Что использовать
Что бы собрать эту не слишком полезную штуку нужно:
* .NET Framework 4.8
* Visual Studio 2022 (Забавно, я это всё в Rider сделал)
* Поддержка WPF в Visual Studio (Опять же, Rider тоже можно)

Для запуска же нужно что бы в папке с программой дополнительно были:
* ffmpeg.exe | для сжатия видео
* ffprobe.exe (он докачается сам) | для вытаскивания некоторой информации из видео

# Какие ОС поддерживаются
Поддерживается только Windows, так как проект сделан на WPF.

# Скриншоты