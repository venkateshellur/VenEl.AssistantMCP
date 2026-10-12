# 🚀 VenEl.MCP.FileManager

**Bring your local file system natively into your AI IDEs with zero-click embedded UIs, fast search, and rich previews.**

`VenEl.MCP.FileManager` is an incredibly powerful, AI-first file management module built on the **Model Context Protocol (MCP)**. Instead of forcing AI agents to blindly read code or ripping developers out of their IDEs, this package provides a seamless, stunning, and fully integrated File Explorer UI right where you work.

## ✨ Why this is a Game Changer

Most MCP integrations just execute basic JSON RPC commands in the background. We wanted to take it to the next level. 

With this package, when you type _"Open File Manager"_ to your AI assistant, you get a beautiful, interactive web interface magically rendered **inside your IDE** (or OS browser if you prefer!). 

*   **🪄 Zero-Click AI Integration**: The AI can dynamically open the Explorer UI exactly where you need it.
*   **🧭 Editable Dropdown Address Bar**: Navigate directly like Windows Explorer or Mac Finder, with 1-click access to system drives, user roots, folder hierarchy, and subdirectories.
*   **🖼️ Rich Previews**: Instantly view your `.png`, `.jpg`, `.cs`, `.json`, and text files directly within a responsive side-panel!
*   **⚡ Blazing Fast Search**: Filter files and directories instantly as you type.
*   **🎨 Stunning Design**: Enjoy a premium aesthetic with deep dark mode, smooth hover animations, and an intuitive grid/list toggle.
*   **🧠 Intelligent Fallbacks**: The agent knows if it's running inside a supported IDE (like VS Code) or terminal, smartly asking you whether to open the UI locally or natively.

## 🛠️ How it works under the hood

This isn't just a basic tool. It leverages a embedded, lightweight **C# Kestrel Web Server** running inside the `VenEl.MCP` host. The backend efficiently queries your local system, handles file reads, and serves an optimized Javascript frontend dynamically. 

Because of our **Shared Engine Architecture**, the heavy lifting stays in the C# background process while the frontend remains lightning fast.

### 📦 Quick Start

Integrating this into your VenEl Modular MCP host is just one line of code:

```csharp
using VenEl.MCP.FileManager;

// Add this to your Program.cs or Startup
builder.Services.AddFileManagerFeature(builder.Configuration);
```

### ⚡ Supported AI Actions

The AI agent automatically gains access to these powerful context tools:
*   `list_dir`: Traverse paths programmatically.
*   `read_file`: Fetch file contents.
*   `write_file`: Generate and overwrite files on the disk.
*   `delete_file` / `move_file`: Easily manipulate directories.
*   `open_ui`: **The Magic Command** — Renders the beautiful UI dashboard!

## 🧩 VS Code Extension Ready

This package is completely compatible with the upcoming `venel-mcp-extension` for Visual Studio Code, seamlessly embedding the Kestrel-served UI straight into a `WebviewPanel` tab for an unmatched developer experience.

---
*Built with ❤️ for the VenEl ecosystem.*
