using System;
using System.Collections.Generic;

namespace VenEl.MCP.FileManager.Sftp;

public record SftpConnectRequest(
    string Host,
    int Port = 22,
    string Username = "",
    string AuthType = "password", // "password" | "key"
    string? Password = null,
    string? PrivateKeyContent = null,
    string? PrivateKeyPassphrase = null,
    string? InitialPath = null,
    string? ConnectionName = null
);

public record SftpSessionInfo(
    string ConnectionId,
    string Name,
    string Host,
    int Port,
    string Username,
    string InitialPath,
    DateTime ConnectedAt
);

public record SftpConnectResult(
    bool Success,
    string? ConnectionId,
    string? ErrorMessage,
    string? CurrentPath,
    List<SftpFileItem>? Items,
    string? Name
);

public record SftpTestResult(
    bool Success,
    string? ErrorMessage,
    int? LatencyMs,
    string? ServerVersion
);

public record SftpFileItem(
    string Name,
    string Type, // "Directory" | "File"
    long Size,
    string Modified,
    bool IsSftp = true,
    string? RemotePath = null,
    string? Permissions = null
);

public record SftpDirectoryResult(
    string CurrentPath,
    List<SftpFileItem> Items
);

public record SftpSaveFileRequest(
    string ConnectionId,
    string RemotePath,
    string Content
);

public record SftpNewFolderRequest(
    string ConnectionId,
    string CurrentPath,
    string FolderName
);

public record SftpNewFileRequest(
    string ConnectionId,
    string CurrentPath,
    string FileName,
    string? Content
);

public record SftpRenameRequest(
    string ConnectionId,
    string OldPath,
    string NewName
);

public record SftpDeleteRequest(
    string ConnectionId,
    string TargetPath,
    bool IsDirectory = false
);
