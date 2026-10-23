using System;
using System.Collections.Generic;

namespace VenEl.MCP.FileManager.Cloud;

public record CloudConnectRequest(
    string Provider, // "s3" | "azure_blob"
    string? Name = null,
    string? Endpoint = null, // Custom endpoint URL for MinIO, R2, Wasabi
    string? Region = "us-east-1",
    string? BucketName = null, // S3 bucket or Azure container name
    string? AccessKey = null,
    string? SecretKey = null,
    string? ConnectionString = null, // Azure Blob Storage Connection String or SAS URL
    bool ForcePathStyle = false, // true for MinIO/Localstack
    string? InitialPrefix = null
);

public record CloudSessionInfo(
    string ConnectionId,
    string Name,
    string Provider,
    string BucketOrContainer,
    string? Endpoint,
    string? Region,
    DateTime ConnectedAt
);

public record CloudConnectResult(
    bool Success,
    string? ConnectionId,
    string? ErrorMessage,
    string? CurrentPath,
    List<CloudFileItem>? Items,
    string? Name,
    string? Provider
);

public record CloudTestResult(
    bool Success,
    string? ErrorMessage,
    int? LatencyMs = null,
    int? ItemCount = null
);

public record CloudFileItem(
    string Name,
    string Type, // "Directory" | "File"
    long Size,
    string Modified,
    bool IsCloud = true,
    string Provider = "s3",
    string? Key = null,
    string? StorageClass = null,
    string? ETag = null
);

public record CloudDirectoryResult(
    string ConnectionId,
    string Provider,
    string BucketOrContainer,
    string CurrentPrefix,
    List<CloudFileItem> Items
);

public record CloudDisconnectRequest(string ConnectionId);

public record CloudSaveFileRequest(
    string ConnectionId,
    string Key,
    string Content
);

public record CloudCreateFolderRequest(
    string ConnectionId,
    string CurrentPrefix,
    string FolderName
);

public record CloudDeleteRequest(
    string ConnectionId,
    string Key,
    bool IsDirectory = false
);
