# Translation

The client has a built-in support for translations. The translation system is made to allow non-programmers to easily translate mods and games based on XNA CnCNet client to the languages of their choice.

The translation system supports the following:
- translating client's built-in text strings;
- translating INI-defined text values without modifying the respective INI files themselves;
- adjusting INI-defined size and position values for client controls per translation;
- providing custom client asset overrides (including both generic and theme-specific) in translations (for instance, translated buttons with text on them, or fonts for different CJK variatons);
- auto-detecting the initial language of the client based on the system's language settings (if provided; happens on first start of the client);
- configurable set of files to copy to the game directory (for ingame translations);
- an ability to generate a translation template/stub file for easy translation.

## Translation structure

The translation system reads folders from the `Resources/Translations` directory by default. Each folder found in that directory is considered a translation and can contain the main translation INI (contains some translation metadata and the translated values), generic assets (they take priority over what's found in `Resources` folder under the same relative path), theme-specific translation INIs and theme-specific assets (overrides for `Resources/[theme name]`) placed in the folders with the same names as the main theme folders that they are supposed to override.

For example:

```md
- Resources
  - Some Theme Folder
    * someThemeAsset.png
    * ...
  - Translations
    - ru
      - Some Theme Folder
        * Translation.ini
        * someThemeAsset.png
        * ...
      * Translation.ini
      * someAsset.png
      * ...
    - uk
      * ...
    - zh-Hans
      * ...
    - zh-Hant
      * ...
  * someAsset.png
  * ...
```

### Folder naming and automatic language detection

The translation folder name is used to match it to the system locale code (as defined by BCP-47), so it is advised to name the translation folders according to that (for example, see how [the locales Windows uses](https://learn.microsoft.com/ru-ru/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c) are coded). That allows the client to choose the appropriate translation based on the system locale and also automatically fetch the name of the translation.

> [!NOTE]
> Unless you're aiming for making a translation for a specific country (e.g. `en-US` and `en-GB`), it's advised to use simply a [language code](http://www.loc.gov/standards/iso639-2/php/code_list.php) (for example, `ru`, `de`, `en`, `zh-Hans`, `zh-Hant` etc.)

The folder name doesn't explicitly need to match the existing locale code. However, in that case you would want to provide an explicit name in the translation INI, and the translation won't be automatically picked in any case.

> [!NOTE]
> The hardcoded client strings can be overridden using an `en` translation. Because the built-in `en` strings are always available, so it English client language. Even if the client doesn't have any translations, English will still be picked by default. If for some reason you need to override hardcoded strings in your client distribution, you can create a `Resources/Translations/en/Translation.ini` file and override needed values there.

### Translation INI format

```ini
[General]          ; translation metadata
Name=Some Language ; string, used instead of a system-provided name if set
Author=Someone     ; string
MapEncoding=UTF-8  ; string, defines the name of the map encoding to be used to load the map files to the spawnmap.ini file. The 'Auto' option means that the client will try to guess the encoding. Please either omit this line or specify 'UTF-8'. Only specify 'Auto' or an encoding different from 'UTF-8' if you really know what you are doing.

[Values]             ; the key-values for translation
Some:Key=Some Value  ; string, see below for explanation
```

#### Translation values key format

Examples:
```ini
INI:HotkeyCategories:Interface=Интерфейс  ; Interface
INI:Hotkeys:AllToCheer:Description=Приказать вашей пехоте ликовать.  ; Make all of your infantry units cheer.
INI:Hotkeys:AllToCheer:UIName=Ликовать  ; Cheer
INI:Controls:CheaterScreen:lblCheater:Text=Обнаружены изменения!  ; Modifications Detected!
Client:DTAConfig:ForceUpdate=Принудительное обновление  ; Force Update
INI:Controls:UpdaterOptionsPanel:btnForceUpdate:Location=320,213
INI:Controls:UpdaterOptionsPanel:btnForceUpdate:Size=220,23
```

Each key in the `[Values]` section is composed of a few elements, joined using `:`, that have different semantic meaning. The structure can be described like this (with list level denoting the position).
- `Client` - the client's built-in text strings.
  - The 2nd and 3rd parts usually denote the string's "namespace" or category and the string's name, respectively, and are chosen arbitrarily by the developers.
- `INI` - the INI-defined values.
  - `Controls` - denotes all INI-defined control values.
    - `[parent control name]` - the name of the parent control of the control that the value is defined for. Specifying `Global` instead of the parent name allows to specify identical translated value for all instances of the control regardless of the parent (parent-specific definition overrides this still though)
      - `[control name]` - the name of the control that the value is defined for.
        - `[attribute name]` - the name of the attribute that is being translated. Currently supported:
          - `Text`, `Size`, `Width`, `Height`, `Location`, `X`, `Y`, `DistanceFromRightBorder`, `DistanceFromBottomBorder` for every control;
          - `ToolTip` for controls with tooltip;
          - `Suggestion` for suggestion text boxes;
          - `URL`, `UnixURL` for link buttons;
          - `ItemX` (where X) for setting/game options dropdowns;
          - `OptionName` for game option dropdowns;
          - `$X`, `$Y`, `$Width`, `$Height` for INItializable window system.
  - `Sides` - subcategory for the game's/mod's side names.
  - `Colors` - subcategory for the game's/mod's color names.
  - `Themes` - subcategory for the game's/mod's theme names.
  - `GameModes` - subcategory for the game's/mod's game modes.
    - `[name]` - uniquely identifies the game mode.
      - `[attribute name]` - the name of the attribute that is being translated. Only `UIName` is supported.
  - `Maps` - subcategory for the game's/mod's maps (custom maps are not supported).
    - `[map path]` - uniquely identifies the map.
      - `[attribute name]` - the name of the attribute that is being translated. Only `Description` (map name) and `Briefing` are supported.
  - `Missions` - subcategory for the game's/mod's singleplayer missions.
    - `[mission section name]` - uniquely identifies the map (taken from `Battle*.ini`).
      - `[attribute name]` - the name of the attribute that is being translated. Only `Description` (mission name) and `LongDescription` (actual description) are supported.
  - `CustomComponents` - subcategory for the game's/mod's custom components.
    - `[custom component INI name]` - uniquely identifies the custom component.
      - `[attribute name]` - the name of the attribute that is being translated. Only `UIName` is supported.
  - `UpdateMirrors` - subcategory for the game's/mod's update download mirrors.
    - `[mirror name]` - uniquely identifies the mirror.
      - `[attribute name]` - the name of the attribute that is being translated. Only `Name` and `Location` are supported.
  - `Hotkeys` - subcategory for the game's/mod's hotkeys.
    - `[INI name]` - uniquely identifies the hotkey.
      - `[attribute name]` - the name of the attribute that is being translated. Only `UIName` and `Description` are supported.
  - `HotkeyCategories` - subcategory for the game's/mod's hotkey categories.
  - `ClientDefinitions` - self explanatory.
    - `WindowTitle` - self explanatory, only works if set in `ClientDefinitions.ini`

> [!WARNING]
> You can only translate an INI value if it was used in the INI in the first place! That means that defining a translated value for a control's attribute (example: translating `X` and `Y` when `Location` is defined) that is not present in the INI **will not have any effect**.

> [!IMPORTANT]
> If the button has an `IdleTexture` key, be sure to place this key as the first key in the button's section, otherwise you will not be able to resize it from `Translation.ini`, because `IdleTexture` changes the size of the button.

## Ingame translation setup

The translation system's ingame translation support requires the mod/game author(s) to specify the files which translators can provide in order to translate the game. The files are specified in the the syntax is `GameFileX=path/to/source.file,path/to/destination.file[,checked]` INI key in the `[Translations]` section of `ClientDefinitions.ini` (X is any text you want to add to the key to help sort files), with comma-separated parts of the value meaning the following:
1) the path to the source file relative to currently selected translation directory;
2) the destination to copy to, relative to the game root folder;
3) (optional) `checked` for the file to be checked by file integrity checks (should be on if this file can be used to cheat), if not specified - this file is not checked.

> [!IMPORTANT]
> When processing the translation game files, by default, the translation system will attempt to create destination files as [hard links](https://learn.microsoft.com/en-us/windows/win32/fileio/hard-links-and-junctions). If creating a hard link is unsuccessful, the system will instead make copies of the files.
>
> Translators are advised to always work on files located in the source folder and avoid editing the copies in the destination folder. This is important because when a language is deselected, the client will automatically delete the files in the destination folder. Be aware that even if a source file and the corresponding destination file are hard-linked, editing either file in a text editor might cause one of these two consequences: either both files will be concurrently updated, or the hard link might be broken, causing only the file being edited to receive the updates. This is why it is recommended to always work on the source files.
>
> To see links in Windows Explorer, you can install [this extension](https://schinagl.priv.at/nt/hardlinkshellext/linkshellextension.html).

> [!WARNING]
> If you include checked files in your ingame translation files, that means users won't be able to do custom translations if they include those files and you won't be able to use custom components with those files **without triggering the modified files / cheater warning**. This mechanism is made for those games and mods where it's impossible to provide a mechanism to provide translations in a cheat-safe way, so please use it only if you have no other choice, otherwise don't specify this parameter.

Example configuration in `ClientDefinitions.ini`:
```ini
[Translations]
GameFileTranslationMix=translation.mix,expandmo98.mix
GameFile_GDI01=Missions/g0.map,Maps/Missions/g0.map
GameFile_NOD01=Missions/n0.map,Maps/Missions/n0.map
GameFile_DLL_SD=Resources/language_800x600.dll, Resources/language_800x600.dll
GameFile_DLL_HD=Resources/language_1024x720.dll,Resources/language_1024x720.dll
```

This will make the `translation.mix` file from current translation folder (say, `Resources/Translations/ru`) copied to game root as `expandmo98.mix` on game start.

> [!WARNING]
> This feature is needed only for *game* files, not *client* files like INIs, theme assets etc.!

## Suggested translation workflow

0. In the mod's settings INI file (for example: `SUN.INI`, `RA2MD.INI`) append `GenerateTranslationStub=true` in `[Options]` section. This will make the client generate a `Translation.ini` file in `Client` folder with all (almost; read caveat below) translatable text values, sorted alphabetically by key. Values with no translations will be commented out; if some translation was already loaded - then the present values and metadata will be carried over to the stub ini.
   - You can also specify `GenerateOnlyNewValuesInTranslationStub=true` in the same place to only output missing values instead of everything in the translation stub, which may be more convenient depending on your workflow.
   - Non-text values (for instance, size and position) are not written to the stub INI, but you can still write them manually if needed.
1. Create a folder in `Resources/Translations` that uses the desired language code as name (see above) and place `Translation.ini` from `Client` folder there, and start translating the strings and uncommenting the translated ones.
   - Hardcoded strings are shared between same client binaries and are independent of mods, so you could reuse all the strings with `Client` prefix that you or someone else made for the language you're translating the client to. Or use `[INISystem]->BasedOn=  ; INI name` in the main `Translation.ini` to include a separate file (for instance, `ClientTranslation.ini`) with all the `Client`-prefixed strings placed in the same section.
   - **Caveat:** hardcoded control size/position values are not read from the translation file at all; as a workaround ask the mod author to specify the size/position values that you will adjust using INI definition for that control, so that it can be adjusted using translation system
   - To speed up the workflow it's advised to use an editor with multi-selection, like [Visual Studio Code](https://code.visualstudio.com), so that you can select values in batches. Select the `=` on the first untranslated line, press `Ctrl+D` as many times as needed to select the remaining `=` on untranslated lines, press `→`, then `Shift+End`. That will select all untranslated values for the lines you marked, so copy them and go to [DeepL](https://www.deepl.com) (recommended) or any other translator, paste the text, correct the translation, copy it back and paste in the same position. VSCode automatically splits the lines back so you don't need to input them one by one.
     - DeepL also adds it's "translated with" line too, so you might need to paste the text in some intermediate file/window/tab, remove that line, and copy it again.
2. For every translated asset, including theme-specific ones, you must replicate the exact path relative to the `Resources` folder for the original asset in your translation folder. The assets should also be named the same as the original ones. They will automatically override the non-translated ones.
3. In case you need theme-specific translated values - create `Translation.ini` in the theme subfolder of your translation folder and put the needed key-value overrides in `[Values]` section (metadata won't be read from this file; also it won't be read at all if the main `Translation.ini` doesn't exist).
4. (optional) Look up the game/mod-specific ingame translation files that are specified in `ClientDefinitions.ini`->`[Translations]`->`GameFileX` and/or consult the game/mod author(s) for a list of files for ingame translation. Make and arrange your ingame translation into the files with specified names (first part of the value) and place them in your translation folder.
   - If the game/mod has integrity-checked translation files - contact the game/mod author to include your translation with the game/mod package so the ingame translation won't make your or your users' installations trigger a modified files warning online.

Happy translating!

## Miscellanous

- Discord presence, game broadcasting, stats etc. use untranslated names so that other players can see the more universal English names, and to not be locked onto a translation in case it changes.
- When translated, original map names still display in a tooltip and can be copied via context menu.
- Where applicable, both translated and untranslated names are used to search (map and lobby searches).
