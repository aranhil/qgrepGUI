# qgrep GUI
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/aranhil/qgrepGUI/msbuild.yml)
![Visual Studio Marketplace Installs](https://img.shields.io/visual-studio-marketplace/i/Stefan-IulianChivu.qgrepSearchTool-x64)
![Visual Studio Marketplace Downloads](https://img.shields.io/visual-studio-marketplace/d/Stefan-IulianChivu.qgrepSearchTool-x64)
![GitHub issues](https://img.shields.io/github/issues/aranhil/qgrepGUI)
![GitHub](https://img.shields.io/github/license/aranhil/qgrepGUI)


*qgrep GUI* is a Visual Studio extension and GUI wrapper for [qgrep](https://github.com/zeux/qgrep) by Arseny Kapoulkine. As an indexing search tool, it's well-suited for large codebases, providing a user-friendly and responsive interface equipped with all the standard search tool features. Additionally, it offers fully customizable color themes for a more personalized user experience.

![Intro](https://github.com/aranhil/qgrepGUI/assets/755601/1fd9db78-6186-49c3-ba15-bc6ba38ec6f7)

## Installation

Install the Visual Studio extension from [here](https://marketplace.visualstudio.com/items?itemName=Stefan-IulianChivu.qgrepSearchTool-x64) (for Visual Studio 2022) or from [here](https://marketplace.visualstudio.com/items?itemName=Stefan-IulianChivu.qgrepSearchTool-x86) (for Visual Studio 2019) and follow the setup instructions below.

## Setting up in Visual Studio

1. After installing the extension, the tool window can be opened from **View >> qgrep Search Tool**. It also comes with a shorcut already assigned (Alt+Shift+F), but if it's already assigned or you want to change it, you can do that from **Tools >> Options >> Environment >> Keyboard**, the command name is **View.qgrepSearchTool**.

2. After opening the tool window, you have to set up the folders that will be indexed:

![Setup](https://github.com/aranhil/qgrepGUI/assets/755601/7b3a9acc-1ec9-4617-ad16-eafe1bf971fe)

   If your projects have a format that can be parsed by MSBuild, you can use the **Gather from solution** button to automatically grab all the folders and file extensions from the solution.
   
   If you make changes to your files and want the changes to be visible in the search results, you have to reindex manually using the buttons from the lower right corner of the tool window.

## Features overview

### Search Results
   The search results can be shown as list or grouped by file, there is also a contextual menu.

![Results](https://github.com/aranhil/qgrepGUI/assets/755601/489b926a-e7d8-40da-85f4-8ef07b4c4886)
      
### History
   The history keeps all of your recent searches but it can also show all of your recently opened files from inside the tool. 

![History](https://github.com/aranhil/qgrepGUI/assets/755601/9d683c0e-6389-4052-85f5-27641dc09321)

### Advanced search configurations
   You can set up multiple search configurations and toggle between them:

![Configs](https://github.com/aranhil/qgrepGUI/assets/755601/08dce467-3ac5-4120-8703-21a157ce97ed)
      
### Open file
   You can also search for files using unordered keywords, using a different window that can be opened from **View >> qgrep Open File** or using the shortcut (Alt+Shift+I). You can change this shortcut as well, the command name is **View.qgrepSearchFile**.
   
   ![OpenFile](https://github.com/aranhil/qgrepGUI/assets/755601/3c8974e7-e93f-4c45-9732-64626ec6e9e0)
      
### Customize colors
   There are three color schemes available: **Auto**, **Dark**, and **Light**. The **Auto** color scheme grabs the IDE's colors automatically, so you can use any custom theme you want. You also have the option to override any color used in the currently selected color scheme:

![Colors](https://github.com/aranhil/qgrepGUI/assets/755601/92359355-4932-419c-ac32-d0fb56d93ed3)
   
### Keyboard navigation
   You can cycle with the **Tab** key between the input TextBoxes and the search results. The **Down** key focuses on the results list. Here is a complete list with all the shortcuts and their default key combination:
- Toggle case sensitive (Alt + C)
- Toggle whole word (Alt + W)
- Toggle regular expressions (Alt + R)
- Toggle include files (Alt + I)
- Toggle exclude files (Alt + E)
- Toggle filter results (Alt + F)
- Toggle group by (Alt + G)
- Toggle group expansion (Alt + X)
- Open history (Alt + H)

You can change these as well, from **Tools >> Options >> Environment >> Keyboard**, all of the commands are prefixed with **qgrep.** and make sure to keep the same **qgrep Tool Window** context so that they only work when the tool window is focused.

## Changelog

### Version 2.11 (08/06/2023)
- Added a stop button to allow forcible interruption of the index updates

### Version 2.10 (07/06/2023)

- Improved performance significantly when loading the results to the UI
- Implemented a crash handler to record unhandled exceptions and their callstacks in log files, with an additional option to forward them to a Google form
- Added automated stress tests for the UI and resolved all identified issues

### Version 2.9 (04/06/2023)

- Added an experimental option to automatically include the selected file as a C++ include statement in the currently active document
- The history is now a circular buffer of 50 items that is preserved between sessions
- Made the shortcuts of the contextual menu items visible
- Fixed several issues with the search file window

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
- Fixed issue with escaped backslashes ("\\\\") in filters that were not being recognized by qgrep as a valid path separator

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

## License

*qgrep GUI* is licensed under the [MIT License](LICENSE).

## Support

If you encounter a bug or want to suggest a feature, please open an issue in the [GitHub repository](https://github.com/aranhil/qgrepGUI/issues).

## Acknowledgements

This project has benefited from the use of the following open-source projects:

- [qgrep](https://github.com/zeux/qgrep): This project is licensed under the [MIT License](./LICENSE-qgrep.md).
- [Extended WPF Toolkit™](https://github.com/xceedsoftware/wpftoolkit): This project has been used for the color picker and the CheckComboBox. The version used (3.8.2) is licensed under the [MS-PL license](./LICENSE-Extended-WPF-Toolkit.md).
- [ControlzEx](https://github.com/ControlzEx/ControlzEx): This project has been used to replace the standard Windows title bar. It is licensed under the [MIT License](./LICENSE-ControlzEx.md).

This project also makes use of icons from [Visual Studio Code - Icons](https://github.com/microsoft/vscode-icons), which are used under the terms of the [Creative Commons Attribution 4.0 International License](./LICENSE-vscode-icons.md).
