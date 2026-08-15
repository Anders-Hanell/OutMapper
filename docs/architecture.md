# OutMapper Architecture

## Scope

This document describes both the architecture currently implemented in the OutMapper solution and explicitly approved architectural direction. Sections describing future direction are labeled as such; other planned components and responsibilities should not be inferred.

For the product purpose and domain context, see [`app_overview.md`](app_overview.md).

## Solution structure

### `OutMapper`

The Uno Platform application and presentation layer. It currently owns:

- Application startup and window creation.
- Material theme and Uno Toolkit resource initialization.
- UI composition and navigation.
- Selection and local persistence of the current workspace path.
- Project folder discovery and creation.
- Dataset user interactions, scoped to the currently selected project.
- Cohort user interactions, scoped to the currently selected project, including a basic dataset-linkage picker at creation time (the picked names are recorded but not yet matched against dataset patients).
- PDF generation currently implemented on the desktop target.
- The UI-side adapter to the task messaging system, via `OutMapper.GatewayToTaskManager`.

`OutMapper` references both `TaskManager` and `Messages`.

### `TaskManager`

A .NET class library that processes background requests. It currently owns:

- An in-process, single-reader message queue.
- The current workspace path used by task operations.
- Dataset discovery under the current project's `Datasets` directory (`Projects/<project-name>/OutMapper_InternalFiles/Datasets`).
- Creation of `.omds` dataset files and same-named dataset folders (each containing an `Imported raw data` subfolder) within the owning project's `Datasets` directory, optionally copying `.csv` files from a user-selected raw data folder into `Imported raw data`.
- Parsing every `.csv` file in a dataset's `Imported raw data` folder into a `TimeSeries` via `Algorithms.Csv.ParseBytes`, persisting each successfully parsed series and a per-dataset parse-result summary (see [Persistence and workspace layout](#persistence-and-workspace-layout)), and re-reading that persisted summary on request without reparsing.
- Cohort discovery under the current project's `Cohorts` directory (`Projects/<project-name>/OutMapper_InternalFiles/Cohorts`).
- Creation of `.omch` cohort files and same-named cohort folders (each containing an `Imported raw data` subfolder) within the owning project's `Cohorts` directory, copying the user-selected `.csv` file into `Imported raw data` and recording the selected linked-dataset names.
- Parsing a cohort's single `.csv` file into a `Cohort` via `Algorithms.CohortCsv.ParseBytes`, persisting the parsed cohort and a parse-result summary, and re-reading that persisted summary on request without reparsing.
- Emission of dataset, cohort, and parse-result responses.

`TaskManager` references `Algorithms` and `Messages`, and does not reference the UI project. `DatasetParsingService` and `CohortParsingService` (both internal) hold the CSV-parsing orchestration logic for their respective entities; `TaskManagerService`'s parse-related handlers delegate to them, mirroring the existing thin-handler shape used for dataset creation.

`TaskManagerService`, `TaskManager.MessageRouter`, `DatasetParsingService`, and `CohortParsingService` are all `internal`; `TaskManager.GatewayToOutMapper` is the only public entry point, so the message-only boundary with `OutMapper` is enforced by the compiler rather than by convention alone.

### `DataStructures`

A .NET class library with no project references of its own — the dependency-free base of the solution. It owns:

- `Result<T>`: an abstract record with `Success<T>`/`Failure<T>` subtypes, used as the errors-as-values return type for any operation in `DataStructures`/`Algorithms` that can fail.
- `TimeSeries`, `CsvParseParams`, `Cohort`, and `CohortParseParams`: value types that follow a "guaranteed valid by construction" pattern — a private constructor plus a static `Create(...)` (and, for `TimeSeries`/`Cohort`, `FromByteArray`) that performs all validation and returns `Result<T>`. Once an instance exists, callers can rely on it being valid without re-checking; there is no other way to construct one. `TimeSeries.FromByteArray`/`Cohort.FromByteArray` re-run `Create` on the deserialized data for the same reason, so the guarantee also holds for data loaded back from disk. `Cohort` holds one patient ID and one outcome value per patient (`ImmutableArray<string> PatientIds`/`Outcomes`), rejecting empty or duplicate patient IDs and empty outcomes.

### `Algorithms`

A .NET class library referencing only `DataStructures`, kept dependency-free and side-effect-free (no file or network I/O) so it can be unit tested and reasoned about as pure functions. It owns:

- `Csv.ParseBytes(bytes, parseParams)`: parses raw CSV bytes into a `Result<TimeSeries>` given a `CsvParseParams`. Callers (currently only `TaskManager`) are responsible for reading the file bytes and, on success, persisting the resulting `TimeSeries`.
- `CohortCsv.ParseBytes(bytes, parseParams)`: parses raw CSV bytes into a `Result<Cohort>` given a `CohortParseParams`, locating the patient-ID and outcome columns by header name (rather than by fixed position) so column order in the source file doesn't matter.

`OutMapper` does not reference `Algorithms` or `DataStructures` directly; see [Messages](#messages) for how `CsvParseParams`/`CohortParseParams` still reach the UI.

### `Messages`

A .NET class library containing the contracts exchanged between `OutMapper` and `TaskManager`. All messages inherit from the `Message` record. Messages do not carry a sender or receiver: the message-passing channel is a fixed, two-party, single-direction-per-message-type link between `OutMapper` and `TaskManager`, so addressing information would be redundant.

`Messages` references `DataStructures`, so a message can carry a `DataStructures` value (such as `CsvParseParams`) directly instead of re-flattening its fields into primitives. This does not weaken the "`OutMapper` never references `Algorithms`/`DataStructures`" rule: `OutMapper.csproj` gains no new project reference from this — `DataStructures` types are merely transitively visible for compilation because `Messages` (which `OutMapper` already references) exposes them as part of its message API. `OutMapper` still never references `Algorithms`, and never calls `Csv.ParseBytes` or constructs a `TimeSeries` itself; it only holds and forwards an inert value that `TaskManager` produced or will consume.

The current contracts cover:

- Workspace changes (`WorkspaceChanged`).
- Dataset list requests (`DatasetListRequest`) and responses (`DatasetListResponse`).
- Dataset creation requests (`CreateDatasetRequest`) and responses (`CreateDatasetResponse`).
- Dataset parse requests (`ParseDatasetRequest`, carrying a `CsvParseParams`) and parse-result requests (`ParseResultRequest`).
- Parse-result responses (`ParseResultResponse`), shared by both request types above — it answers "what happened the last time this dataset was parsed," whether that was moments ago or is being read back from a previous session. Carries an `ImmutableArray<CsvFileParseOutcome>`, one entry per CSV file (`FileName`, `Success`, `ErrorMessage`).
- Cohort list requests (`CohortListRequest`) and responses (`CohortListResponse`).
- Cohort creation requests (`CreateCohortRequest`, also carrying the `ImmutableArray<string>` of linked dataset names picked at creation time) and responses (`CreateCohortResponse`).
- Cohort parse requests (`ParseCohortRequest`, carrying a `CohortParseParams`) and parse-result requests (`CohortParseResultRequest`).
- Cohort parse-result responses (`CohortParseResultResponse`), shared by both cohort request types above, mirroring `ParseResultResponse` but for a single outcome (`Success`, `ErrorMessage`, `PatientCount`) since a cohort has exactly one source CSV rather than many.

Message contract type names do not carry a `Msg` suffix.

### `OutMapper.Tests`

The NUnit test project. It references `OutMapper` and currently contains only the generated placeholder test; substantive behavior is not yet covered by automated tests.

## Runtime composition

`App.OnLaunched` creates a WinUI `Window` and `Frame`, navigates the frame to `MainPage`, forwards any saved workspace path to `TaskManager`, and activates the window.

In debug builds, Uno Platform Studio support is enabled through `UseStudio()`.

`MainPage` currently composes the primary navigation and content entirely in C#. The top-level areas are Settings and Projects. Dataset and cohort management are not top-level areas; both are nested inside the Projects tab, scoped to the currently selected project. Settings contains its own navigation for Usage, Workspace, Current Projects, Select Project, and Create Project. A selected dataset or cohort has its own further-nested navigation, for Parse and Result, following the same sidebar-plus-content-area shape as Settings.

Although the project enables the Uno MVUX feature, the currently implemented screens use programmatic UI construction and event handlers rather than MVUX models.

## Communication and concurrency

Communication between the UI and task layer is in-process and crosses a single barrier in each direction: a **Gateway**. `OutMapper.GatewayToTaskManager` and `TaskManager.GatewayToOutMapper` are the only two classes allowed to reach across the `OutMapper`/`TaskManager` project boundary; every message enters or leaves a project through its Gateway. Crossing a Gateway is also where the thread switch to or from the UI thread happens — that responsibility belongs to the Gateway, not to `MessageRouter`.

Outbound, `OutMapper` → `TaskManager`:

1. UI code calls `OutMapper.MessageRouter.SendMessage`, which forwards to `OutMapper.GatewayToTaskManager.SendMessage`.
2. `GatewayToTaskManager` calls `TaskManager.GatewayToOutMapper.ReceiveMessage` directly (`OutMapper` holds a project reference to `TaskManager`, so no indirection is needed for this direction).
3. `GatewayToOutMapper.ReceiveMessage` enqueues the message into `TaskManagerService`'s unbounded, single-reader `Channel<Message>`. This enqueue is the thread switch: `TaskManagerService`'s background consumer (started with `Task.Run`) dequeues and processes messages sequentially, off the UI thread.
4. On that background thread, `TaskManager.MessageRouter.Route` casts the message to its concrete subtype and calls the matching `TaskManagerService` handler directly.

Return, `TaskManager` → `OutMapper` (for messages that produce a response):

1. The `TaskManagerService` handler calls `TaskManager.GatewayToOutMapper.SendMessage` with the response.
2. `TaskManager` has no project reference to `OutMapper`, so `GatewayToOutMapper` forwards the response to a registered `TaskManager.IGatewayReceiver` — the callback that `OutMapper.GatewayToTaskManager` registers with it via `GatewayToTaskManager.Initialize()`, called once from `App.OnLaunched` on the UI thread.
3. That callback marshals onto the UI thread with `DispatcherQueue.TryEnqueue` before doing anything else — the thread switch back to the UI thread.
4. Once on the UI thread, `OutMapper.MessageRouter.Route` casts the response to its concrete subtype and calls the matching handler directly on the live control instance (for example `ProjectsPanel.Current` for `DatasetListResponse`/`CreateDatasetResponse`/`CohortListResponse`/`CreateCohortResponse`, or `ProjectDatasetContent.Current`/`ProjectCohortContent.Current` for `ParseResultResponse`/`CohortParseResultResponse`).

Neither `MessageRouter` uses events; once the concrete message subtype is known, dispatch in both directions is a direct function call. Each response consumer exposes its live instance to route to as a static `Current` reference, since exactly one instance exists for the app's lifetime. `ProjectDatasetContent`/`ProjectCohortContent` forward a received `ParseResultResponse`/`CohortParseResultResponse` to both of their children (the respective Parse and Result content controls), after checking the response's project/dataset (or project/cohort) name against what's currently displayed — because each is a single instance reused across every selection, this guard prevents a response for a previously viewed dataset or cohort from overwriting the current view. `DatasetListResponse` is additionally forwarded to `ProjectCreateCohortContent.Current`, which uses it to populate its dataset-linkage checkbox list — one response consumed by two unrelated controls.

This is not currently an external process, network protocol, or durable queue. Message state is held only for the lifetime of the application process.

### Message boundary and threading invariants

The architectural boundary between the `OutMapper` UI project and `TaskManager` is intended to allow only immutable messages to pass between them.

- `OutMapper` must not read or modify TaskManager state directly. `TaskManagerService`'s and `TaskManager.MessageRouter`'s `internal` visibility enforces this at compile time; `TaskManager.GatewayToOutMapper` is the only public entry point.
- A message received by `OutMapper` must be dispatched to the main UI thread before OutMapper processes it or updates controls.
- Message processing and task work in `TaskManager` must not execute on the main UI thread.
- These two invariants are owned by the Gateway classes: `GatewayToTaskManager` and `GatewayToOutMapper` are where the thread switch happens, so `MessageRouter.Route` on either side can assume it is already running on the correct thread.
- Message payloads must be deeply immutable; immutable record properties are insufficient when a payload contains mutable collections such as arrays. `DatasetListResponse.DatasetNames` uses `ImmutableArray<string>` to satisfy this.

### Current sequential processing

For now, `TaskManager` is intentionally a single sequential message consumer. It accepts messages in channel order and completes all work for one message before processing the next message. This ordering is part of the intended behavior, not a performance defect.

The consumer is started with `Task.Run`, so TaskManager handlers execute on a thread-pool thread rather than the UI thread. The sequential consumer is a logical execution context and must not depend on affinity to one particular physical thread; an asynchronous continuation may resume on another thread-pool thread.

### Future intra-message parallelism

Creating an outcome heatmap consists of dependent processing stages. Stages must execute in order, while independent work within a stage may execute in parallel across multiple processor cores.

For example:

1. Parse multiple CSV files. **Implemented** (`DatasetParsingService.ParseDatasetAsync`), but currently as a plain sequential `foreach` over the dataset's CSV files rather than in parallel — parallelizing this stage is deliberately deferred, not yet done.
2. Wait until every file has been parsed.
3. Count values within configured ranges in parallel across the parsed data. Not yet implemented.
4. Wait until all counting work has completed.
5. Combine results and emit an immutable response message. Implemented for CSV parsing (`ParseResultResponse`, built after every file has been attempted); not yet implemented for the counting stage.

The TaskManager message consumer should await each complete stage and should not accept the next ordinary work message until the current message has completed. Parallel work should use bounded Task Parallel Library primitives, such as `Parallel.ForEachAsync` or an equivalent `Task.WhenAll` design, rather than manually creating dedicated threads.

The degree of parallelism must be bounded and configurable. Implementations must support cancellation, define how partial failures are handled, avoid unnecessary shared mutable state, and prevent nested parallel work from oversubscribing the system. None of this applies yet to CSV parsing, since it isn't parallel today; `SettingsMultitaskingContent.GetMaxDegreeOfParallelism()` remains uncalled (see below).

### Bounding degree of parallelism to protect the rest of the user's computer

A full calculation run is expected to take a few minutes and is highly parallelizable, so an unbounded implementation would use every available core for that entire time. Because OutMapper is a desktop application running alongside whatever else the user has open, this can degrade unrelated applications on the same machine — most noticeably latency-sensitive ones such as video or audio playback, where a missed scheduling slot is audible or visible even if overall system load looks acceptable.

Two mitigations were considered:

- **Lowering worker-thread priority.** This helps only with scheduling contention (who wins when cores are oversubscribed); it does not reduce memory-bandwidth pressure, cache pollution, thermal throttling, fan noise, or battery drain. It is also not a single portable mechanism in .NET: `Thread.Priority` maps to real OS priorities on Windows, but Linux and macOS require different mechanisms (nice values, or QoS classes) with weaker or less consistent support in the .NET runtime.
- **Capping the degree of parallelism** (leaving whole cores unused) — the approach adopted. It is portable across all desktop targets, requires no OS-specific APIs, and also mitigates the thermal/fan/battery side effects that thread priority cannot address.

`OutMapper.SettingsMultitaskingContent` is the settings panel (Settings → Multitasking) where the user chooses how many cores to reserve for the rest of their system. It persists the choice and exposes `SettingsMultitaskingContent.GetMaxDegreeOfParallelism()` as the single source of truth for how many cores parallel work is allowed to use; any bounded parallel primitive used for intra-message parallelism (`Parallel.ForEachAsync`, `ParallelOptions.MaxDegreeOfParallelism`, etc.) should read its bound from this method rather than from `Environment.ProcessorCount` directly or from a hardcoded value. This method is defined now but has no caller yet, since intra-message parallelism itself is not yet implemented.

Cancellation and progress control require further design. A cancellation message placed behind a long-running work message in the same sequential queue cannot take effect promptly, so cancellation may require an out-of-band token or separate control path. No approach has yet been selected.

## State ownership

### Workspace selection

`SettingsWorkspaceContent` persists the selected workspace path in `ApplicationData.Current.LocalSettings` under `WorkspaceFolderPath`.

On startup and whenever the user selects a workspace, a `WorkspaceChanged` message synchronizes that path to the static state held by `TaskManagerService`.

Some UI-side project operations read the persisted workspace path directly, while dataset and cohort operations carry no workspace folder in their request messages at all and rely entirely on `TaskManagerService` resolving its own synchronized state. The workspace therefore currently has two representations that must remain synchronized.

Dataset and cohort requests additionally carry the selected project's name explicitly on every message; unlike the workspace path, `TaskManagerService` does not hold a synchronized "current project" field, so the UI is solely responsible for supplying a valid project name each time.

### Project selection

The selected project name and its workspace path are persisted in `ApplicationData.Current.LocalSettings`. Only one project is selected at a time. Changing the workspace clears the selection, and a persisted selection is rejected if its project directory no longer exists.

The future requirement for work belonging to previously selected projects to continue in the background has not yet been designed or implemented.

### Compute mode selection

`SettingsMultitaskingContent` persists the user's chosen compute mode in `ApplicationData.Current.LocalSettings` under `ComputeMode`, as one of four values: all cores, leave one core free (the default), leave two cores free, or use only one core. `SettingsMultitaskingContent.GetMaxDegreeOfParallelism()` resolves the persisted mode against `Environment.ProcessorCount` into a concrete core count, clamped to at least 1.

### UI state

Navigation and form state are held in control instances created by `MainPage` and its content controls. There is no separate application-wide view-model or MVUX state model for the implemented screens.

## Persistence and workspace layout

The selected workspace is an ordinary filesystem directory.

```text
<workspace>/
└── Projects/
    └── <project-name>/
        ├── OutMapper_InternalFiles/
        │   └── Datasets/
        │       ├── <dataset-name>.omds
        │       └── <dataset-name>/
        │           ├── Imported raw data/
        │           │   └── <copied .csv files>
        │           ├── Parsed data/
        │           │   └── <csv-basename>.json
        │           └── parse-result.json
        ├── Cohorts/
        │   ├── <cohort-name>.omch
        │   └── <cohort-name>/
        │       ├── Imported raw data/
        │       │   └── <copied .csv file>
        │       ├── Parsed data/
        │       │   └── cohort.json
        │       ├── parse-result.json
        │       └── linked-datasets.json
        └── OutMapper_ProjectOutput/
            └── Graph.pdf
```

- Each immediate subdirectory of `Projects` is treated as a project.
- Creating a project creates its directory, then its `OutMapper_InternalFiles` and `OutMapper_ProjectOutput` subdirectories, after validating the name and checking for an existing directory.
- `OutMapper_InternalFiles` holds files the app manages internally, such as `Datasets` and `Cohorts`. `OutMapper_ProjectOutput` holds files generated for the user, such as the exported PDF.
- Datasets are currently represented by an empty `.omds` file and a same-named folder, both created by `TaskManagerService` inside their owning project's `OutMapper_InternalFiles/Datasets` directory; a dataset cannot exist without an existing project. The dataset folder contains an `Imported raw data` subfolder, into which `.csv` files are copied from the raw data folder the user selected during dataset creation, if any.
- Parsing a dataset (`DatasetParsingService.ParseDatasetAsync`) reads every `.csv` file in `Imported raw data`, and for each one that parses successfully, writes the resulting `TimeSeries.ToByteArray()` to a same-named `.json` file in a sibling `Parsed data` folder. Whether or not every file succeeded, a `parse-result.json` summary (parse timestamp, counts, and a per-file success/error outcome) is written directly in the dataset folder, overwriting any previous run's summary — parsing is idempotent and re-runnable. `ParseResultRequest` reads this file back without reparsing, which is how the Result panel can show the outcome of a previous session's parse.
- Cohorts follow the same shape as Datasets, one level down: an empty `.omch` file and a same-named folder inside `OutMapper_InternalFiles/Cohorts`, created by `TaskManagerService`. The cohort folder contains an `Imported raw data` subfolder holding the single `.csv` file copied from the path the user picked during cohort creation, and a `linked-datasets.json` file recording the dataset names selected in the linkage picker at creation time (not yet used for any patient-matching logic).
- Parsing a cohort (`CohortParsingService.ParseCohortAsync`) reads the one `.csv` file in `Imported raw data` (failing with a clear message if zero or more than one is found), and on success writes the resulting `Cohort.ToByteArray()` to `Parsed data/cohort.json`. A `parse-result.json` summary (parse timestamp, success/error, and patient count) is written directly in the cohort folder either way, overwriting any previous run's summary, and `CohortParseResultRequest` reads it back without reparsing — mirroring the dataset parse-result flow but for a single outcome instead of a per-file array.
- The current PDF prototype writes `Graph.pdf` into the selected project's `OutMapper_ProjectOutput` directory.

No project metadata format, dataset schema, migration strategy, or transactional persistence layer is currently implemented.

## UI architecture

The authored UI is primarily C# using WinUI/Uno controls and Uno C# Markup helpers. `MainPage` swaps `ContentControl.Content` values in response to button events rather than using route-based navigation for its inner panels.

The specialized content controls currently include:

- `SettingsWorkspaceContent` for selecting and displaying the workspace.
- `SettingsProjectsContent` for listing project directories.
- `SettingsSelectProjectContent` for selecting the current project.
- `SettingsCreateProjectContent` for project creation.
- `SettingsMultitaskingContent` for choosing how many cores calculations may use.
- `ProjectsPanel` for dataset and cohort listing within the Projects tab, scoped to the selected project; `ProjectCreateDatasetContent` for dataset creation; `ProjectCreateCohortContent` for cohort creation (including a checkbox picker, populated via `DatasetListRequest`, for the dataset(s) to link the cohort to).
- `ProjectDatasetContent` for a selected dataset, hosting its own nested Parse/Result navigation; `ProjectDatasetParseContent` for configuring and triggering a CSV parse (`CsvParseParams`); `ProjectDatasetResultContent` for displaying the last parse's per-file outcome.
- `ProjectCohortContent` for a selected cohort, hosting the same nested Parse/Result navigation shape; `ProjectCohortParseContent` for configuring and triggering a CSV parse (`CohortParseParams`: delimiter, patient-ID column header, outcome column header); `ProjectCohortResultContent` for displaying the last parse's single outcome (success/patient count, or error).

`ProjectFolderService` contains the shared filesystem rules used by the two project-related Settings panels.

## Target platforms and features

The Uno application targets:

- `net10.0-desktop`
- `net10.0`

Enabled Uno features are C# Markup, Material, Toolkit, MVUX, Skia Renderer, and Storage. The desktop target additionally references SkiaSharp, which is used by the current PDF-generation prototype.

Platform-specific behavior should be verified against the target frameworks and Uno Platform documentation before implementation.

## Testing and validation

The solution has an NUnit test project with FluentAssertions and coverage tooling configured, but it does not yet contain meaningful tests.

Current verification therefore depends primarily on:

- Compiling the complete solution.
- Running the application with Hot Reload.
- Inspecting and interacting with the live app through Uno App MCP when connected.

Important filesystem validation, messaging, and state-synchronization behavior should receive automated coverage as those contracts stabilize.

## Known architectural limitations

- Workspace state is duplicated between local settings and `TaskManagerService`.
- Several services and routers are static, coupling state to the application process lifetime.
- Project filesystem operations currently execute directly from the UI project instead of through `TaskManager`.
- TaskManager has no intra-message parallel processing yet; CSV parsing (the first implemented candidate stage) runs one file at a time, and other heatmap stages have no implementation for bounded multiple-core work at all. `SettingsMultitaskingContent.GetMaxDegreeOfParallelism()` exists to bound that future work but is not yet called from anywhere.
- Cancellation and progress control for long-running sequential messages are not yet designed. A dataset or cohort parse currently runs to completion (or first unexpected filesystem failure) with no way to cancel it mid-run.
- UI composition, event handling, and navigation are concentrated in code rather than separated into view and state layers.
- Automated test coverage is not yet established.
- Cohort-to-dataset linkage (picked at cohort creation time, persisted in `linked-datasets.json`) is recorded but not yet used for any patient-matching or validation logic — see [Cohort](glossary.md#cohort) in the glossary for the intended behavior.

These observations describe the current implementation; changing them requires an explicit product or architectural decision.
