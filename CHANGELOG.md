### Version 2.6 (21/05/2023)

- Added a new window to search for files using unordered keywords (Shortcut: Shift+Alt+I)
- The fonts are now grabbed automatically from Visual Studio 
- Added options to change the fonts and their size
- Added options to change the results row heights
- Added shortcut for "Group By" (Alt+G)
- Added shortcut for expanding/collapsing the result groups (Alt+X)
- The search errors thrown from qgrep are now shown
- Fixed issues with the config selection combo box
- Moved the windows menu items to the "View" menu

### Version 2.7 (28/05/2023)

- Added an experimental option to update the index automatically that tries its best to optimize index time if only a few files are updated
- The word under the caret is now grabbed automatically when opening the tool window using the command
- Added file icons to any file search (from the dialog or from the "include files" field)
- The file search dialog now automatically selects the best candidate
- Added two options to the file search dialog to customize the path style and to limit the scope of the search
- Added an option to limit the search scope of the "filter results" field
- All resizable windows now remember their size
- Fixed the old search in files from the "include files" field to work as before the new window
- Fixed issue where tree view items collapsed/expanded by the user were modified by a search that was still appending to the results
- Fixed issue with escaped backslashes ("\\") in filters that were not being recognized by qgrep as a valid path separator

### Version 2.8 (02/06/2023)

- Fixed occasional crash when opening the tool window
- Localized all strings and added some machine translations (Chinese Simplified, Chinese Traditional, Czech, French, German, Italian, Japanese, Korean, Polish, Romanian, Spanish, Portuguese and Turkish)
- Improved the UI responsiveness for searches with lots of results
- Migrated to using Visual Studio's ThreadHelper.JoinableTaskFactory for a more responsive UI
- Fixed the style of textboxes' contextual menus
- Fixed issue with right click not selecting the tree view item before opening the contextual menu
- Search file dialog's input textbox and results listbox are now both focused at the same time
- Added support for UTF-8 encoded paths
- Fixed issue where pressing Delete would delete the Config/Group while editing the name
- Fixed issue where underscores ('_') weren't showing up in the search summary info
- Fixed issue with backslashes ('\\') not being recognized as a valid path separator in all the fields that expect a path (file search, include files and exclude files)

### Version 2.9 (04/06/2023)

- Added an experimental option to automatically include the selected file as a C++ include statement in the currently active document
- The history is now a circular buffer of 50 items that is preserved between sessions
- Made the shortcuts of the contextual menu items visible
- Fixed several issues with the search file window

### Version 2.10 (07/06/2023)

- Improved performance significantly when loading the results to the UI
- Implemented a crash handler to record unhandled exceptions and their callstacks in log files, with an additional option to forward them to a Google form
- Added automated stress tests for the UI and resolved all identified issues

### Version 2.11 (08/06/2023)

- Added a stop button to allow forcible interruption of the index updates

### Version 2.12 (09/06/2023)

- Added shortcuts to toggle individual search configurations and also for exclusive selection of a single configuration
- Resolved an issue where the automatic gathering of folders added duplicates
- Fixed several issues with the automatic C++ header inclusion
- Fixed issue with the shortcuts context missing its name in Visual Studio