
# SMAPI Game Loader

you can run Stardew Valley Clone with custom Assets, Dlls without any game patch 


## How it work?
    1. custom load game assets at Android/data/packagename/files/Stardew Assets
    2. custom load dlls at Android/data/packagename/files/**.dll
    3. game assets & dlls it clone from base.apk & split_content.apk
    4. hook a method MonoGame.Framework.dll Load Assets
        and redirect the loading path from internal assets to external paths

## For Developer
1. need install package with cmd -> dotnet add package Microsoft.NETCore.App.Runtime.Mono.android-arm64 --version 8.0.x
