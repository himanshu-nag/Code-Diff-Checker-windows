# Professional Diff Tool

A professional Windows application for comparing files and folders with GitLab-style UI and colored differences, built with WPF and DiffPlex.

## Features

- **GitLab-Style UI**: Modern interface with colored diff display
- **Colored Differences**: 
  - 🟢 Green for additions
  - 🔴 Red for deletions  
  - 🟡 Yellow for modifications
- **Advanced Diff Engine**: Uses DiffPlex for accurate text comparisons
- **File & Folder Comparison**: Support for both individual files and directory trees
- **Professional UI**: Menu bar, status bar, and themed interface
- **Save and Copy**: Export diff results to file or clipboard

## UI Theme
- Light background (#f8f9fa) similar to GitLab
- Blue buttons (#007bff) with hover effects
- Color-coded diff lines matching GitLab conventions

## Usage

1. Run the application (`dotnet run --project DiffTool.csproj`).
2. Choose comparison mode: Files or Folders.
3. Select two paths using buttons or menu.
4. Click "Compare" to view colored differences.
5. Use menu options to save, copy, or clear results.

## Menu Options

- **File**: Open files/folders, save diff, exit
- **Edit**: Copy diff, clear all
- **View**: Switch between file and folder modes
- **Help**: About dialog

## Building

Use `dotnet build` to build the project.

Use `dotnet run --project DiffTool.csproj` to run the application.