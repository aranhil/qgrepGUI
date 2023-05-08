# qgrep Search Tool

*qgrep Search Tool* is a GUI wrapper and Visual Studio extension based on qgrep made by Arseny Kapoulkine. It provides a powerful and user-friendly interface for searching through files and directories within your projects, improving your development experience and productivity.

## Features

- :mag: Advanced search functionality in Visual Studio
- :gear: Easy customization and configuration
- :bookmark_tabs: Colorful and easy-to-read output
- :zap: Fast indexing and searching

## Installation

Install the Visual Studio extension and follow the setup instructions below.

## Setting up in Visual Studio

1. After installing the extension, the toolbar can be opened from **View >> Other Windows >> qgrep Search Tool**.
2. You can also set up a shortcut for this from here:

   ![image](https://user-images.githubusercontent.com/755601/236953452-f5cb9be3-ffca-4431-befb-aba9d22c65f4.png)
   This will also grab the selected text in your current document before opening.

3. After this, you have to set up the folders that will be indexed. The window to set can be opened up from the lower left corner of the toolbar:

   ![image](https://user-images.githubusercontent.com/755601/236954367-7722217e-73b9-40c6-8002-28f8a6eba032.png)

4. From here, you can add the folders and file filters. You can also use the "Gather from solution" button to automatically grab all the folders inside your solution.

   ![image](https://user-images.githubusercontent.com/755601/236954626-9786eb9a-189f-4e0d-99be-f42ebe1f29b8.png)

5. After closing this window, indexing will begin automatically, and you can start searching.

## Interface overview

![image](https://user-images.githubusercontent.com/755601/236955206-2bd9c6ae-639a-43a0-9063-1402d3d6794a.png)

### Search input

The search input has three toggles that can also be found in Visual Studio: Case sensitive, Whole word, and Regular expressions.

### Search results

The search results list can be interacted with using the keyboard/mouse without the search input losing the focus. There is also a contextual menu with more options.

   ![image](https://user-images.githubusercontent.com/755601/236957474-e479308b-ddc6-4829-adb3-80ea27ba28e4.png)

### Customize colors

Here you can switch between the Auto, Dark, and Light color schemes. If you're in Visual Studio, the Auto color scheme grabs the IDE's colors automatically, so you can use any custom theme you want. You also have the option to override any color used in the currently selected color scheme.

### Advanced options

- **Include files**: Toggles the visibility of an input textbox that you can use to match the files that will be searched.
- **Exclude files**: Like the previous one, but this one excludes the matched files from the search.
- **Filter results**: Like the previous two, can be used to further filter the results, for both the path and the text
- **Show history**: This toggles the visibility of an icon on the input search box that shows all your recent searches.
- **Search while typing**: This will be on by default, but if you have performance issues, you can turn it off. Note that turning this off will no longer always keep the focus of the input textbox.
- **Group by**: File - groups the results by file; None - shows the results on a single line
- **Group expand**:If the previous option is set to File, this chooses how group expansion will be handled. Auto collapses all the results if there are too many.
- **Path style**: This changes how the paths will be shown in the results, it does NOT affect searching.
- **Trim spaces on copy**: This will trim any spaces and tabs surrounding the text when it's being copied.

### Search configurations

Advanced mode opens up the possibility to toggle between multiple search configs. After setting them up, they can then be switched on or off from the lower left corner of the toolbar.

   ![image](https://user-images.githubusercontent.com/755601/236958126-d6fbd8c7-e20c-4ff5-a46a-4733f9d5481a.png)

### Update cache

This button updates the index.

### Clean and update cache

If there is an error with the indexing, this button cleans everything and indexes from scratch.

## License

*qgrep Search Tool* is licensed under the [MIT License](LICENSE).

## Support

If you have any questions or need help, please open an issue in the [GitHub repository](https://github.com/aranhil/qgrepSearchTool/issues).

## Acknowledgements

I would like to thank Arseny Kapoulkine for creating qgrep and all the contributors who have helped make *qgrep Search Tool* an excellent search tool for Visual Studio.
