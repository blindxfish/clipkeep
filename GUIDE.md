# ClipForge — C# Implementation Specification

## Product Summary

**ClipForge** is a Windows clipboard manager that stores, organizes, and searches everything the user copies.

It is **local-first**, **offline**, **fast**, and **privacy-focused**.

No AI.
No cloud.
No account.
No telemetry.

The goal:

> Never lose anything you copied again.

---

# Recommended Tech Stack

## Application

* C#
* .NET 8 or .NET 9
* WPF desktop application

## Database

* SQLite
* SQLite FTS5 for full-text search

## UI

* WPF
* MVVM pattern
* CommunityToolkit.Mvvm

## Tray Icon

* Hardcodet.NotifyIcon.Wpf
  or
* WinForms NotifyIcon

## Clipboard Access

* Windows clipboard APIs
* WPF Clipboard class
* Native clipboard listener via Win32 `AddClipboardFormatListener`

## Global Hotkeys

* Win32 `RegisterHotKey`

## Image Processing

* System.Drawing or ImageSharp
* Store images on disk
* Store metadata in SQLite

## OCR

Optional future feature:

* Tesseract OCR

## Installer

* Inno Setup
* MSIX
* or WiX Toolset

---

# Core Features

## 1. Tray Application

ClipForge must run primarily from the Windows system tray.

### Tray Menu

The tray icon should provide:

* Open ClipForge
* Search Clipboard
* Quick Paste
* Pause Monitoring
* Resume Monitoring
* Favorites
* Settings
* Exit

### Tray States

The tray icon should visually indicate:

* Normal monitoring
* Paused monitoring
* Error state

---

# 2. Main Window

The main window should contain:

## Left Sidebar

* All Clips
* Text
* URLs
* Images
* Code
* Files
* Favorites
* Settings

## Top Bar

* Search input
* Filter dropdown
* Sort dropdown

## Center Panel

List of clipboard entries.

Each item should show:

* Preview
* Type icon
* Source application
* Created time
* Favorite indicator

## Right Details Panel

When an item is selected, show:

* Full content
* Metadata
* Copy button
* Delete button
* Favorite toggle
* Source application
* Window title
* Copy count
* First copied date
* Last copied date

---

# 3. Clipboard Monitoring

ClipForge must listen for clipboard changes globally.

Use native Win32 clipboard listener:

```csharp
AddClipboardFormatListener(windowHandle);
```

Handle:

```csharp
WM_CLIPBOARDUPDATE
```

When clipboard changes:

1. Read clipboard content.
2. Determine content type.
3. Generate content hash.
4. Check if content already exists.
5. If duplicate, update copy count and last copied timestamp.
6. If new, store content and metadata.
7. Update search index.

Supported clipboard types:

* Plain text
* URLs
* Emails
* Phone numbers
* Code snippets
* File paths
* Copied files
* Images
* HTML/rich text

---

# 4. Content Classification

Classification should be deterministic and rule-based.

No AI.

## Text Types

Detect using regex and heuristics:

### URL

Examples:

```text
https://example.com
www.example.com
```

Store separately in URL table.

### Email

Example:

```text
john@example.com
```

### Phone Number

Example:

```text
+31 6 12345678
```

### File Path

Examples:

```text
C:\Projects\ClipForge
\\server\share\file.pdf
```

### Color

Examples:

```text
#FFAA22
rgb(255, 0, 0)
```

### Code

Detect likely programming language using patterns.

Languages to support initially:

* C#
* JavaScript
* TypeScript
* SQL
* Python
* JSON
* XML
* HTML
* CSS
* YAML
* PowerShell

### Long Text

Text longer than 200 characters should be marked as long text/document.

---

# 5. SQLite Database Design

## clipboard_entries

```sql
CREATE TABLE clipboard_entries (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    type TEXT NOT NULL,
    content TEXT,
    content_hash TEXT NOT NULL UNIQUE,
    source_app TEXT,
    source_process TEXT,
    window_title TEXT,
    favorite INTEGER NOT NULL DEFAULT 0,
    copy_count INTEGER NOT NULL DEFAULT 1,
    first_copied_at TEXT NOT NULL,
    last_copied_at TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
```

## urls

```sql
CREATE TABLE urls (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entry_id INTEGER NOT NULL,
    url TEXT NOT NULL,
    domain TEXT,
    protocol TEXT,
    path TEXT,
    query TEXT,
    FOREIGN KEY(entry_id) REFERENCES clipboard_entries(id) ON DELETE CASCADE
);
```

## images

```sql
CREATE TABLE images (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entry_id INTEGER NOT NULL,
    file_path TEXT NOT NULL,
    thumbnail_path TEXT,
    width INTEGER,
    height INTEGER,
    file_size INTEGER,
    ocr_text TEXT,
    FOREIGN KEY(entry_id) REFERENCES clipboard_entries(id) ON DELETE CASCADE
);
```

## files

```sql
CREATE TABLE files (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entry_id INTEGER NOT NULL,
    file_path TEXT NOT NULL,
    file_name TEXT,
    extension TEXT,
    exists_on_disk INTEGER,
    FOREIGN KEY(entry_id) REFERENCES clipboard_entries(id) ON DELETE CASCADE
);
```

## tags

```sql
CREATE TABLE tags (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE
);
```

## entry_tags

```sql
CREATE TABLE entry_tags (
    entry_id INTEGER NOT NULL,
    tag_id INTEGER NOT NULL,
    PRIMARY KEY(entry_id, tag_id),
    FOREIGN KEY(entry_id) REFERENCES clipboard_entries(id) ON DELETE CASCADE,
    FOREIGN KEY(tag_id) REFERENCES tags(id) ON DELETE CASCADE
);
```

## FTS Search Table

```sql
CREATE VIRTUAL TABLE clipboard_fts USING fts5(
    content,
    source_app,
    window_title,
    tags,
    ocr_text,
    content='clipboard_entries',
    content_rowid='id'
);
```

---

# 6. Duplicate Detection

Every clipboard item should be hashed.

For text:

```csharp
SHA256(normalizedText)
```

For images:

```csharp
SHA256(imageBytes)
```

For files:

```csharp
SHA256(filePath + lastModifiedTime + fileSize)
```

If a copied item already exists:

Do not create a new record.

Instead update:

* `copy_count`
* `last_copied_at`
* `updated_at`

---

# 7. Image Storage

Images should not be stored as BLOBs in SQLite.

Store image files on disk.

Recommended folder structure:

```text
%AppData%\ClipForge\
  Data\
    clipforge.db
  Images\
    2026\
      07\
        image_000001.png
  Thumbnails\
    2026\
      07\
        image_000001_thumb.jpg
```

Database stores only:

* Image path
* Thumbnail path
* Width
* Height
* File size
* OCR text, if enabled

---

# 8. Search

Search must be instant.

Use SQLite FTS5.

Search should include:

* Clipboard content
* URL domains
* Source application
* Window title
* Tags
* OCR text

Search examples:

```text
cognex
```

```text
PORD004758
```

```text
tgw-group
```

```text
docker compose
```

---

# 9. Quick Paste Window

Global hotkey:

```text
Ctrl + Shift + V
```

Behavior:

1. Small popup appears near cursor.
2. Shows recent clipboard entries.
3. Search input is focused.
4. User types to filter.
5. Arrow keys navigate.
6. Enter copies selected item to clipboard.
7. Optional: automatically paste into active app using simulated Ctrl+V.

The quick paste window should be lightweight and fast.

---

# 10. Settings Screen

Settings should include:

## General

* Launch on Windows startup
* Start minimized to tray
* Show notifications
* Confirm before deleting items

## Clipboard Monitoring

* Enable monitoring
* Monitor text
* Monitor images
* Monitor files
* Monitor HTML/rich text

## Privacy

* Ignore sensitive content
* Encrypt sensitive content
* Save sensitive content normally
* Excluded applications list

Default excluded apps:

```text
Bitwarden
1Password
KeePass
KeePassXC
NordPass
Dashlane
Browser password manager windows
```

## Storage

* Database location
* Image storage location
* Maximum database size
* Retention period

Retention options:

* Forever
* 30 days
* 90 days
* 1 year

## Hotkeys

* Open ClipForge
* Quick Paste
* Pause monitoring

---

# 11. Sensitive Content Detection

ClipForge should detect sensitive content using regex.

Initial detections:

* Credit card numbers
* IBANs
* JWT tokens
* API keys
* Private keys
* Password-like strings

Default behavior:

```text
Do not save sensitive content.
```

User can change this in settings.

---

# 12. Application Exclusion

The user must be able to exclude applications from clipboard capture.

Example:

```text
bitwarden.exe
keepass.exe
1password.exe
```

If clipboard content comes from an excluded application, ClipForge should ignore it.

---

# 13. Source Application Tracking

For every clipboard event, store:

* Active process name
* Application name
* Active window title

Use Win32 APIs:

* `GetForegroundWindow`
* `GetWindowText`
* `GetWindowThreadProcessId`

This allows the user to search later by app or context.

---

# 14. Favorites

Each entry can be marked as favorite.

Favorites:

* Appear in Favorites view
* Are never automatically deleted by retention cleanup
* Are prioritized in quick paste search

---

# 15. Deletion and Cleanup

User can delete:

* Single clipboard entry
* All non-favorites
* Entries older than selected retention period
* All image files with missing database references

Deleting an image entry must also delete:

* Original image file
* Thumbnail file
* OCR data

---

# 16. Statistics Dashboard

Show useful local statistics:

* Total clipboard entries
* Total text entries
* Total images
* Total URLs
* Total code snippets
* Database size
* Image storage size
* Most copied entries
* Most common source applications
* Clipboard activity by day

---

# 17. Project Structure

Recommended solution structure:

```text
ClipForge.sln

src/
  ClipForge.App/
    App.xaml
    MainWindow.xaml
    Views/
    ViewModels/
    Controls/

  ClipForge.Core/
    Models/
    Services/
    Classification/
    Search/
    Security/

  ClipForge.Infrastructure/
    Database/
    Repositories/
    Clipboard/
    Storage/
    WindowsApi/

  ClipForge.Tests/
    ClassificationTests/
    DatabaseTests/
    ClipboardTests/
```

---

# 18. Important Services

## ClipboardMonitorService

Responsibilities:

* Listen for clipboard updates
* Read clipboard safely
* Prevent self-trigger loops
* Forward content to classification/storage pipeline

## ClipboardClassificationService

Responsibilities:

* Determine type
* Extract metadata
* Generate tags

## ClipboardStorageService

Responsibilities:

* Save entries
* Detect duplicates
* Save images to disk
* Update FTS index

## SearchService

Responsibilities:

* Query SQLite FTS5
* Return ranked results
* Filter by type/date/favorite

## SettingsService

Responsibilities:

* Load settings
* Save settings
* Provide defaults

## HotkeyService

Responsibilities:

* Register global hotkeys
* Handle conflicts
* Open quick paste window

## TrayService

Responsibilities:

* Create tray icon
* Build tray menu
* Handle pause/resume/exit

---

# 19. MVP Scope

The first version should include:

* Tray icon
* Main window
* Settings window
* Text clipboard monitoring
* URL/email/phone/file path/code detection
* SQLite storage
* Duplicate detection
* Search
* Favorites
* Quick paste popup
* Basic sensitive content exclusion
* Application blacklist

Images and OCR can be added after the core text system works.

---

# 20. Future Features

Possible version 2 features:

* Image clipboard history
* OCR search
* Collections
* Tags
* Export/import
* Backup
* Portable mode
* Windows Explorer integration
* Browser extension
* VS Code extension
* Clipboard timeline
* Session grouping
* Theme customization

---

# Product Identity

## Name

**ClipForge**

## Tagline

Forge your clipboard into a searchable memory.

## Positioning

ClipForge is not just clipboard history.
It is a local archive for everything useful you copied.

Fast like a utility.
Organized like a database.
Private like an offline tool.
