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

### Version 2.8

- Fixed occasional crash when opening the tool window
- Localized all strings and added machine translations (Chinese Simplified, Chinese Traditional, French, German, Italian, Japanese, Korean, Polish, Romanian, Spanish and Portuguese)
- Improved the UI responsiveness for searches with lots of results
- Migrated to using Visual Studio's ThreadHelper.JoinableTaskFactory for a more responsive UI
- Fixed the style of textboxes' contextual menus
- Fixed issue with right click not selecting the tree view item before opening the contextual menu
- Search file dialog's input textbox and results listbox are now both focused at the same time
- Added support for UTF-8 encoded paths
- Fixed issue where pressing Delete would delete the Config/Group while editing the name
- Fixed issue where the '_' character wasn't showing up in the search summary info