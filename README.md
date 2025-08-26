# DownloadOrganizer
A lightweight WinForms system tray application that automatically organizes your **Downloads** folder into categorized subfolders on demand.

## Installation

1. Download the latest release from the Releases page.

2. Extract the .zip and run autodownloadsorter.exe.

3. The app will appear in your system tray.

## Configuration

- Customizable folder name & file extension rules via a `> Settings` menu.
- Included defaults for common file types

| File Type                           | Destination Folder |
| ----------------------------------- | ------------------ |
| `.pdf`, `.docx`, `.pptx`, `.drawio` | Documents          |
| `.xlsx`, `.csv`                     | Spreadsheets       |
| `.exe`, `.msi`, `.iso`              | Installers         |
| `.zip`                              | ZIP Files          |
| `.jpg`, `.jpeg`, `.png`             | Images             |
| `.gif`                              | GIFs               |
| `.mp4`                              | Videos             |
| `.mp3`, `.wav`, `.m4a`              | Audio              |
| `.html`, `.htm`, `.json`            | WebDownloads       |
| `.3mf`                              | BambuStudio        |

The app reads sorting rules from `sortdownloads_rules.json` in its working directory.
Edit this file to add custom file extensions or change target folders.

Example:

```json
{
  ".psd": "Design",
  ".blend": "3D"
}
```