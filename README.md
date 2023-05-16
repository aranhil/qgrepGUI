# qgrep GUI

*qgrep GUI* is a Visual Studio extension and GUI wrapper for [qgrep](https://github.com/zeux/qgrep) by Arseny Kapoulkine. As an indexing search tool, it's well-suited for large codebases, providing a user-friendly and responsive interface equipped with all standard search tool features. Additionally, it offers fully customizable color themes for a more personalized user experience.

![Animation](https://github.com/aranhil/qgrepGUI/assets/755601/05eecff9-d7eb-4beb-95fa-1cf7a0b8c2ea)

## Installation

Install the Visual Studio extension and follow the setup instructions below.

## Setting up in Visual Studio

1. After installing the extension, the toolbar can be opened from **View >> Other Windows >> qgrep Search Tool**. It also comes with a shorcut already assigned (Alt+Shift+F), but if it's already assigned or you want to change it, you can do that from **Tools >> Options >> Environment >> Keyboard**, the command name is "View.qgrepSearchTool".

2. After opening the tool window, you have to set up the folders that will be indexed:

   <gif with how to set up the indexed folders>
   If your projects have a format that can be parsed by MSBuild, you can use the **Gather from solution** button to automatically grab all the folders and file extensions from the solution. 

## Features overview

![image](https://user-images.githubusercontent.com/755601/236962874-6614cf8c-dcf8-4029-8dce-fc8f323409f3.png)

### Search Input
   The search input has the same three toggles that can also be found in Visual Studio: Case sensitive, Whole word, and Regular expressions.
   <gif of toggling them>
   
### Search Results
   The search results can be shown as list or grouped by file, there is also a contextual menu.
   <gif with toggle between grouping type and contextual menu>
   
### Keyboard navigation
   You can cycle with the **Tab** key between the input textboxes and the search results. The **Down** key focuses on the results list. Here is a complete list with all the shortcuts and their default key combination:
- Toggle case sensitive (Alt + C)
- Toggle whole word (Alt + W)
- Toggle regular expressions (Alt + R)
- Toggle include files (Alt + I)
- Toggle exclude files (Alt + E)
- Toggle filter results (Alt + F)
- Open history (Alt + H)
      
### History
   The history keeps all of your recent searches but it can also show all of your recently opened files from inside the tool. 
      <gif with example>
      
### Customize colors
   There are three color schemes available: Auto, Dark, and Light. the Auto color scheme grabs the IDE's colors automatically, so you can use any custom theme you want:
         <gif with theme changing>
    You also have the option to override any color used in the currently selected color scheme.
            <gif with that>

### Advanced search configurations
    You can set up multiple search configurations and toggle between them:
   <gif with example>

## License

*qgrep Search Tool* is licensed under the [MIT License](LICENSE).

## Support

If you have any questions or need help, please open an issue in the [GitHub repository](https://github.com/aranhil/qgrepSearchTool/issues).

## Acknowledgements

This project has benefited from the use of the following open-source projects:

- [qgrep](https://github.com/zeux/qgrep): This project is licensed under the [MIT License](./LICENSE-qgrep.md).
- [Extended WPF Toolkit™](https://github.com/xceedsoftware/wpftoolkit): This project has been used for the color picker and the CheckComboBox. The version used (3.8.2) is licensed under the [MS-PL license](./LICENSE-Extended-WPF-Toolkit.md).
- [ControlzEx](https://github.com/ControlzEx/ControlzEx): This project has been used to replace the standard Windows title bar. It is licensed under the [MIT License](./LICENSE-ControlzEx.md).

This project also makes use of icons from [Visual Studio Code - Icons](https://github.com/microsoft/vscode-icons), which are used under the terms of the [Creative Commons Attribution 4.0 International License](./LICENSE-vscode-icons.md).
