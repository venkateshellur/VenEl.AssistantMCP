# Enterprise Data Migration Architecture

This architectural topology represents a true N-Tier enterprise design. It explicitly models the **Shared Core Infrastructure Library** that standardizes data access, logging, and file operations across all application services, ensuring clean abstraction between the compute tier and the persistence tier.

```mermaid
flowchart TB
    %% Enterprise Styling
    classDef database fill:#fcc419,stroke:#e67700,stroke-width:1.5px,color:#212529,shape:cylinder
    classDef appNode fill:#339af0,stroke:#1864ab,stroke-width:1.5px,color:#fff,shape:rect
    classDef coreLib fill:#be4bdb,stroke:#862e9c,stroke-width:1.5px,color:#fff,shape:rect
    classDef storage fill:#868e96,stroke:#495057,stroke-width:1.5px,color:#fff,shape:rect
    classDef azure fill:#22b8cf,stroke:#0b7285,stroke-width:1.5px,color:#fff,shape:cloud
    classDef vendorNode fill:#51cf66,stroke:#2b8a3e,stroke-width:1.5px,color:#fff,shape:rect

    subgraph OnPrem [Corporate On-Premises Data Center]
        
        %% LAYER 1: Application Services (Top)
        subgraph Compute [Application & Compute Services Tier]
            direction LR
            ETL["ETL Bootstrap Service\n(One-time Data Prep)"]:::appNode
            App["Manifesting Worker Cluster\n(n-Nodes, Distributed)"]:::appNode
            Uploader["Dedicated Egress Service\n(Azure Uploader)"]:::appNode
        end

        %% LAYER 2: Shared Library (Middle)
        subgraph Core [Shared Infrastructure Framework / Core Library]
            direction LR
            CoreSQL["⚙️ SQL Operations Module"]:::coreLib
            CoreFile["⚙️ File I/O Module"]:::coreLib
            CoreLog["⚙️ Telemetry & Logging Module"]:::coreLib
        end

        %% LAYER 3: Persistence (Bottom)
        subgraph Persistence [Data Persistence & Storage Tier]
            direction LR
            DB_Norm[("Normalized\nMaster DB")]:::database
            DB_Denorm[("Denormalized\nTarget DB")]:::database
            DB_Track[("Tracking\nOrchestrator DB")]:::database
            
            NAS[/"NAS Storage\n(Legacy Documents)"/]:::storage
            SAN[/"Staging SAN\n(Bundle Folders)"/]:::storage
            CSV[/"Exceptions Sink\n(CSV Logs)"/]:::storage
        end

    end

    %% CLOUD & EXTERNAL
    subgraph External [Cloud & External Vendor Boundaries]
        direction LR
        Blob(("Azure Blob Storage\n(Container)")):::azure
        Gateway["Vendor Ingestion Gateway"]:::vendorNode
        VendorDB[("Vendor Core DB")]:::database
    end

    %% --- Internal Dependencies (Compute -> Core) ---
    ETL ===>|References & Uses| CoreSQL
    ETL ===>|References & Uses| CoreLog
    
    App ===>|References & Uses| CoreSQL
    App ===>|References & Uses| CoreFile
    App ===>|References & Uses| CoreLog

    Uploader ===>|References & Uses| CoreSQL
    Uploader ===>|References & Uses| CoreFile
    Uploader ===>|References & Uses| CoreLog

    %% --- Data Access (Core -> Persistence) ---
    CoreSQL -.->|Execute Read/Write| DB_Norm
    CoreSQL -.->|Execute Read/Write| DB_Denorm
    CoreSQL -.->|Manage State / Locks| DB_Track
    
    CoreFile -.->|Read Physical Files| NAS
    CoreFile -.->|Write XML & Bundles| SAN
    CoreFile -.->|Write Exceptions| CSV
    CoreLog -.->|Append Logs| CSV

    %% --- External Egress (Compute -> Cloud) ---
    Uploader ===>|HTTPS / TLS Egress| Blob
    Blob -.->|Event-Driven Pull| Gateway
    Gateway ===>|Data Ingestion| VendorDB
```

## Architectural Design Decisions

1. **N-Tier Architecture with Shared Core:** 
   - A dedicated **Shared Infrastructure Framework** encapsulates all low-level SQL execution, File I/O, and Telemetry/Logging. 
   - This ensures the `Compute Tier` (ETL, Workers, and Uploader) remains purely focused on business logic (flattening data, validating rules, generating XML manifests, uploading). 
   - Code duplication is eliminated; if a database connection string or file path strategy changes, it only changes in the Core Library.

2. **Clean Separation of Storage vs Compute:** 
   - Compute nodes do not talk directly to the databases or NAS. They route all requests through the Core framework, representing a robust Enterprise Data Access Layer (DAL).

3. **State Management & Concurrency:**
   - The `Tracking Orchestrator DB` acts as a distributed lock manager. The `Manifesting Cluster` independently leases batches of work through the `CoreSQL` module, preventing race conditions across the network.

4. **Exception Handling & Auditing:**
   - Empty (0kb) files or corrupt records trigger the `CoreFile` and `CoreLog` modules to immediately route errors to a local CSV exception sink, maintaining a clean cloud egress pipeline.

5. **Strict Network Boundaries:**
   - The architecture explicitly defines trust boundaries: the Corporate On-Premises Data Center, the Microsoft Azure Cloud, and the external Vendor Network. Handoff occurs securely via Azure Blob Storage.
