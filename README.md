# ImGui Widget Collection for Hexa.NET.ImGui

This repository contains a variety of custom widgets and utilities designed to extend the capabilities of the [Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui) library. Whether you're developing tools, applications, or games, these widgets will help you create more interactive and user-friendly interfaces.

## Table of Contents

- [Installation](#installation)
- [Widgets](#widgets)
- [Contributing](#contributing)
- [License](#license)

## Installation

To use the ImGui Widget Collection in your project, install the NuGet package:

(Not published yet)
```sh
dotnet add package Hexa.NET.ImGui.Widgets
```

## Widgets
- ImGuiBufferingBar (a simple buffering bar)
- ImGuiButton
  - Toggle Button (a normal button, but when toggled it gets a circle around it to indicate that it' active)
  - Transparent Button (a normal button but without a background when not hovered)
- ImGuiSpinner (a simple buffering spinner)
- ImGuiTreeNode (a special tree node with the ability to add a icon with a color)
- ImGuiWidgetFlameGraph (a widget used for drawing a flame graph)
- MessageBox (a message box framework for simple yes no cancel ok dialogs)
- DialogMessageBox (similar to MessageBox, but with the difference that it blocks other windows)
- OpenFileDialog (used for users to select files or folders, multi selection supported)
- RenameFileDialog (used for renaming files and folders)
- SaveFileDialog (used for selecting a path for a file/folder to save to.)

## Helper classes and types
- ComboHelper (a small helper for using enums with combo boxes)
- ImageHelper (a small helper for aligning a image)
- TextHelper (a helper for text alignment)
- TooltipHelper (a helper for tooltips)
- ImGuiName (a struct that provides a unique name useful for cases where multiple things are named the same way)

## Widgets Extra (Requires my math lib)
- ImGuiBezierWidget (a widget for visually modifying bezier curves)
- ImGuiCurveEditor (a curve editor commonly used in color grading)

## Contributing

We welcome contributions to the ImGui Widget Collection! If you have an idea for a new widget or an improvement to an existing one, please submit a pull request.

## License

This project is licensed under the MIT License. See the LICENSE file for details.
