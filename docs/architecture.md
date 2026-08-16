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
- Cohort user interactions, scoped to the currently selected project, including a basic dataset-linkage picker at creation time.
- Analysis user interactions, scoped to the currently selected project: creation (name only) and a settings panel (Cohort + two channel names + a bin size per channel) that triggers graph generation.
- Figure user interactions, scoped to the currently selected project: creation (name only), a Size panel (row/column counts), and a Select-graphs panel (assigns an existing Analysis's persisted graph to each row/column cell, populated from `AnalysesWithGraphListResponse`) that triggers PDF assembly.
- PDF generation currently implemented on the desktop target: one heatmap PDF per Analysis, named `<analysis-name>.pdf`, driven by the association grid a `GenerateAnalysisGraphResponse` carries; and one grid-of-heatmaps PDF per Figure, named `<figure-name>.pdf`, driven by the per-cell graph data a `CreateFigureGraphResponse` carries. Both are drawn with the shared `OutMapper.HeatmapDrawing` helper.
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
- Analysis discovery under the current project's `Analyses` directory (`Projects/<project-name>/OutMapper_InternalFiles/Analyses`), and creation of `.oman` analysis files and same-named analysis folders (no raw data to import — an Analysis only references an existing Cohort and dataset(s) already in the project).
- Generating a Two-variable analysis graph (`AnalysisService.GenerateGraphAsync`): matching a Cohort's patients to their parsed time series by filename (patient ID) across the Cohort's linked dataset(s) — exactly one match required per patient, else the patient is excluded as unmatched or ambiguous — parsing each matched patient's outcome as a number, computing per-channel bin edges from the observed data, and computing a per-cell Spearman correlation (via `Algorithms`) between percent-time-in-cell and outcome across patients, mapped to Jet colors. Persists a generation-result summary and re-reads it on request without recomputing. On a *successful* generation, also persists the association grid itself (channel names, bin edges, row-major cell colors) to `graph-data.json` — unlike `generation-result.json`, this file is only overwritten on success, so a later failed regeneration attempt does not discard the last good grid. `AnalysisService.ReadPersistedGraphData` reads it back, and `AnalysisService.ListAnalysesWithPersistedGraph` reports which Analyses currently have one, for use by Figures.
- Figure discovery under the current project's `Figures` directory (`Projects/<project-name>/OutMapper_InternalFiles/Figures`), and creation of `.omfg` figure files and same-named figure folders (no raw data to import, mirroring Analysis creation).
- Saving a Figure's size (`FigureService.SaveSize`) and assembling a Figure's graph (`FigureService.CreateGraph`), both persisting `figure-config.json` (row count, column count, and a row-major array of per-cell Analysis-name assignments). Saving a new size remaps prior cell assignments by `(row, col)` coordinate into the new dimensions — assignments that still fall within the new bounds are kept, everything else (including cells beyond a shrunk dimension) is dropped. Assembling a Figure's graph reads each assigned cell's Analysis via `AnalysisService.ReadPersistedGraphData`; a cell with no assignment, or whose Analysis has no persisted graph data, is reported as an empty cell rather than failing the whole request — the Figure is still produced, with that cell left blank.
- Emission of dataset, cohort, analysis-list/creation, analysis-generation, figure-list/creation, figure-layout/size, and parse/generation-result responses.

`TaskManager` references `Algorithms` and `Messages`, and does not reference the UI project. `DatasetParsingService`, `CohortParsingService`, `AnalysisService`, and `FigureService` (all internal) hold the orchestration logic for their respective entities; `TaskManagerService`'s parse-, generation-, and figure-related handlers delegate to them, mirroring the existing thin-handler shape used for dataset creation.

`TaskManagerService`, `TaskManager.MessageRouter`, `DatasetParsingService`, `CohortParsingService`, `AnalysisService`, and `FigureService` are all `internal`; `TaskManager.GatewayToOutMapper` is the only public entry point, so the message-only boundary with `OutMapper` is enforced by the compiler rather than by convention alone.

### `DataStructures`

A .NET class library with no project references of its own — the dependency-free base of the solution. It owns:

- `Result<T>`: an abstract record with `Success<T>`/`Failure<T>` subtypes, used as the errors-as-values return type for any operation in `DataStructures`/`Algorithms` that can fail.
- `TimeSeries`, `CsvParseParams`, `Cohort`, `CohortParseParams`, and `TwoVariableAnalysisSettings`: value types that follow a "guaranteed valid by construction" pattern — a private constructor plus a static `Create(...)` (and, for `TimeSeries`/`Cohort`, `FromByteArray`) that performs all validation and returns `Result<T>`. Once an instance exists, callers can rely on it being valid without re-checking; there is no other way to construct one. `TimeSeries.FromByteArray`/`Cohort.FromByteArray` re-run `Create` on the deserialized data for the same reason, so the guarantee also holds for data loaded back from disk. `Cohort` holds one patient ID and one outcome value per patient (`ImmutableArray<string> PatientIds`/`Outcomes`), rejecting empty or duplicate patient IDs and empty outcomes. `TwoVariableAnalysisSettings` (Cohort name, two channel names, a bin size per channel) is a submitted-fresh-each-time settings type, like `CohortParseParams` — it isn't persisted as-is.

### `Algorithms`

A .NET class library referencing only `DataStructures`, kept dependency-free and side-effect-free (no file or network I/O) so it can be unit tested and reasoned about as pure functions. It owns:

- `Csv.ParseBytes(bytes, parseParams)`: parses raw CSV bytes into a `Result<TimeSeries>` given a `CsvParseParams`. Callers (currently only `TaskManager`) are responsible for reading the file bytes and, on success, persisting the resulting `TimeSeries`.
- `CohortCsv.ParseBytes(bytes, parseParams)`: parses raw CSV bytes into a `Result<Cohort>` given a `CohortParseParams`, locating the patient-ID and outcome columns by header name (rather than by fixed position) so column order in the source file doesn't matter.
- `GridBinning`: computes bin edges from an observed min/max and a bin size, and finds which bin a value falls into (half-open, except the last bin which is closed on both ends).
- `PercentTimeGrid`: for one patient, computes the percent of their valid joint (both-channel) monitoring time spent in each grid cell.
- `SpearmanCorrelation`: Spearman's rank correlation (Pearson correlation of average-tie-ranks), hand-implemented since no stats/math package is referenced anywhere in the solution.
- `AssociationGrid`: the per-cell Spearman correlation between every patient's percent-time-in-cell and their outcome, across a whole grid.
- `JetColorScale`: maps a value in a fixed range to a Jet-scale hex color, piecewise-linearly interpolated across 9 anchor colors (matching the R reference implementation's `ColorScale.R`).

These five are used together by `TaskManager.AnalysisService` to compute a Two-variable Analysis's association grid; see [Persistence and workspace layout](#persistence-and-workspace-layout) for where the result is written, and [`glossary.md`](glossary.md#two-variable) for the domain-level description.

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
- Analysis list requests (`AnalysisListRequest`) and responses (`AnalysisListResponse`).
- Analysis creation requests (`CreateAnalysisRequest`) and responses (`CreateAnalysisResponse`).
- Analysis graph generation requests (`GenerateAnalysisGraphRequest`, carrying a `TwoVariableAnalysisSettings`) and responses (`GenerateAnalysisGraphResponse`) — the response carries primitives only (bin edges, row-major hex cell colors, patient-matching counts), not a `DataStructures` type, since `OutMapper`'s PDF drawing only needs to consume plain data.
- Analysis result requests (`AnalysisResultRequest`) and responses (`AnalysisResultResponse`), mirroring `CohortParseResultResponse` — a summary only (no grid data), for redisplaying the Result tab without recomputing.
- Figure list requests (`FigureListRequest`) and responses (`FigureListResponse`).
- Figure creation requests (`CreateFigureRequest`) and responses (`CreateFigureResponse`).
- Figure layout requests (`FigureLayoutRequest`) and responses (`FigureLayoutResponse`) — reads back a Figure's saved row/column counts and row-major per-cell Analysis-name assignments without recomputing, for populating the Size and Select-graphs panels.
- Figure size save requests (`SaveFigureSizeRequest`) and responses (`SaveFigureSizeResponse`) — the response carries the server-remapped cell assignments for the new dimensions, so `OutMapper` never reimplements the remap.
- Requests (`AnalysesWithGraphListRequest`) and responses (`AnalysesWithGraphListResponse`) listing which Analyses in a project currently have persisted graph data, for populating the Select-graphs panel's pickers.
- Figure graph creation requests (`CreateFigureGraphRequest`, carrying the full current row/column counts and cell assignments) and responses (`CreateFigureGraphResponse`, carrying one `FigureCellGraphData` per cell) — this both persists the final cell assignments and gathers each assigned Analysis's graph data in one round trip, for `OutMapper` to draw the Figure's PDF. `FigureCellGraphData` is a nested payload record (does not inherit `Message`), mirroring how `CsvFileParseOutcome` is nested inside `ParseResultResponse`; a cell with no assignment, or an assigned Analysis missing its persisted graph data, is reported with `HasGraph: false` rather than failing the whole response.

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
4. Once on the UI thread, `OutMapper.MessageRouter.Route` casts the response to its concrete subtype and calls the matching handler directly on the live control instance (for example `ProjectsPanel.Current` for `DatasetListResponse`/`CreateDatasetResponse`/`CohortListResponse`/`CreateCohortResponse`/`AnalysisListResponse`/`CreateAnalysisResponse`/`FigureListResponse`/`CreateFigureResponse`, or `ProjectDatasetContent.Current`/`ProjectCohortContent.Current`/`ProjectAnalysisContent.Current`/`ProjectFigureContent.Current` for `ParseResultResponse`/`CohortParseResultResponse`/`GenerateAnalysisGraphResponse`/`AnalysisResultResponse`/`FigureLayoutResponse`/`SaveFigureSizeResponse`/`CreateFigureGraphResponse`).

Neither `MessageRouter` uses events; once the concrete message subtype is known, dispatch in both directions is a direct function call. Each response consumer exposes its live instance to route to as a static `Current` reference, since exactly one instance exists for the app's lifetime. `ProjectDatasetContent`/`ProjectCohortContent`/`ProjectAnalysisContent`/`ProjectFigureContent` forward a received response to both of their children (the respective Parse/Settings and Result content controls, or Size and Select-graphs content controls), after checking the response's project/dataset (or project/cohort, project/analysis, or project/figure) name against what's currently displayed — because each is a single instance reused across every selection, this guard prevents a response for a previously viewed dataset, cohort, analysis, or figure from overwriting the current view. `DatasetListResponse` is additionally forwarded to `ProjectCreateCohortContent.Current`, `CohortListResponse` to `ProjectAnalysisSettingsContent.Current`, and `AnalysesWithGraphListResponse` to `ProjectFigureSelectGraphsContent.Current` — each of these list responses is consumed by more than one unrelated control (a "create/configure X" picker, in addition to `ProjectsPanel`'s own navigation list).

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
        ├── Analyses/
        │   ├── <analysis-name>.oman
        │   └── <analysis-name>/
        │       ├── generation-result.json
        │       └── graph-data.json
        ├── Figures/
        │   ├── <figure-name>.omfg
        │   └── <figure-name>/
        │       └── figure-config.json
        └── OutMapper_ProjectOutput/
            ├── <analysis-name>.pdf
            └── <figure-name>.pdf
```

- Each immediate subdirectory of `Projects` is treated as a project.
- Creating a project creates its directory, then its `OutMapper_InternalFiles` and `OutMapper_ProjectOutput` subdirectories, after validating the name and checking for an existing directory.
- `OutMapper_InternalFiles` holds files the app manages internally, such as `Datasets`, `Cohorts`, `Analyses`, and `Figures`. `OutMapper_ProjectOutput` holds files generated for the user, such as the exported PDFs.
- Datasets are currently represented by an empty `.omds` file and a same-named folder, both created by `TaskManagerService` inside their owning project's `OutMapper_InternalFiles/Datasets` directory; a dataset cannot exist without an existing project. The dataset folder contains an `Imported raw data` subfolder, into which `.csv` files are copied from the raw data folder the user selected during dataset creation, if any.
- Parsing a dataset (`DatasetParsingService.ParseDatasetAsync`) reads every `.csv` file in `Imported raw data`, and for each one that parses successfully, writes the resulting `TimeSeries.ToByteArray()` to a same-named `.json` file in a sibling `Parsed data` folder. Whether or not every file succeeded, a `parse-result.json` summary (parse timestamp, counts, and a per-file success/error outcome) is written directly in the dataset folder, overwriting any previous run's summary — parsing is idempotent and re-runnable. `ParseResultRequest` reads this file back without reparsing, which is how the Result panel can show the outcome of a previous session's parse.
- Cohorts follow the same shape as Datasets, one level down: an empty `.omch` file and a same-named folder inside `OutMapper_InternalFiles/Cohorts`, created by `TaskManagerService`. The cohort folder contains an `Imported raw data` subfolder holding the single `.csv` file copied from the path the user picked during cohort creation, and a `linked-datasets.json` file recording the dataset names selected in the linkage picker at creation time — used by `AnalysisService` for patient-ID matching (see below), though it's still not validated at cohort-creation time itself.
- Parsing a cohort (`CohortParsingService.ParseCohortAsync`) reads the one `.csv` file in `Imported raw data` (failing with a clear message if zero or more than one is found), and on success writes the resulting `Cohort.ToByteArray()` to `Parsed data/cohort.json`. A `parse-result.json` summary (parse timestamp, success/error, and patient count) is written directly in the cohort folder either way, overwriting any previous run's summary, and `CohortParseResultRequest` reads it back without reparsing — mirroring the dataset parse-result flow but for a single outcome instead of a per-file array.
- Analyses are represented by an empty `.oman` file and a same-named folder inside `OutMapper_InternalFiles/Analyses`, created by `TaskManagerService`; unlike Datasets/Cohorts, there's no raw data to import at creation time.
- Generating an Analysis's graph (`AnalysisService.GenerateGraphAsync`) reads the configured Cohort's parsed `cohort.json` and `linked-datasets.json`, then, for each cohort patient ID, searches every linked dataset's `Parsed data` folder for a same-named `<patientId>.json` time series — this is the patient-ID matching the Cohort's linkage picker exists to support, implemented here for the first time (previously recorded but unused). Exactly one match is required per patient; zero or multiple matches exclude that patient (counted separately as unmatched/ambiguous). A `generation-result.json` summary (timestamp, success/error, matched/total patient counts) is written directly in the analysis folder either way, overwriting any previous run's summary, and `AnalysisResultRequest` reads it back without recomputing.
- The PDF (`<analysis-name>.pdf`, one per Analysis) is drawn by `OutMapper.AnalysisGraphPdfService` from the association grid a `GenerateAnalysisGraphResponse` carries, and written into the selected project's `OutMapper_ProjectOutput` directory. On a successful generation, `AnalysisService` additionally writes `graph-data.json` (channel names, bin edges, row-major cell colors) into the analysis folder — unlike `generation-result.json`, this file is left untouched by a later failed regeneration, so it always reflects the last successful run. This is what makes an Analysis's graph selectable for a Figure.
- Figures are represented by an empty `.omfg` file and a same-named folder inside `OutMapper_InternalFiles/Figures`, created by `TaskManagerService`; like Analyses, there's no raw data to import at creation time.
- Saving a Figure's size (`FigureService.SaveSize`) validates that both row and column counts are greater than zero, then remaps any existing `figure-config.json` cell assignments to the new dimensions by `(row, col)` coordinate: assignments that still fall within both the old and new bounds are kept, everything else — cells dropped by a shrink, or newly added cells from a grow — becomes unassigned. The remapped result is persisted and echoed back in `SaveFigureSizeResponse`, so `OutMapper` never reimplements the remap itself.
- Creating a Figure's graph (`FigureService.CreateGraph`) persists the request's row/column counts and cell assignments as the figure's authoritative `figure-config.json`, then for each cell reads the assigned Analysis's `graph-data.json` via `AnalysisService.ReadPersistedGraphData`. A cell with no assignment, or whose Analysis has no persisted graph data (never successfully generated, or later deleted), is reported as `HasGraph: false` rather than failing the whole response — the Figure is still produced, with that cell left blank in `OutMapper.FigureGraphPdfService`'s drawing.
- The Figure PDF (`<figure-name>.pdf`, one per Figure) is drawn by `OutMapper.FigureGraphPdfService`, laying out one small heatmap per assigned cell (scaled to fit the figure's row/column grid within a single page) using the same `OutMapper.HeatmapDrawing` helper `AnalysisGraphPdfService` uses, and written into the selected project's `OutMapper_ProjectOutput` directory.

No project metadata format, dataset schema, migration strategy, or transactional persistence layer is currently implemented.

## UI architecture

The authored UI is primarily C# using WinUI/Uno controls and Uno C# Markup helpers. `MainPage` swaps `ContentControl.Content` values in response to button events rather than using route-based navigation for its inner panels.

The specialized content controls currently include:

- `SettingsWorkspaceContent` for selecting and displaying the workspace.
- `SettingsProjectsContent` for listing project directories.
- `SettingsSelectProjectContent` for selecting the current project.
- `SettingsCreateProjectContent` for project creation.
- `SettingsMultitaskingContent` for choosing how many cores calculations may use.
- `ProjectsPanel` for dataset, cohort, analysis, and figure listing within the Projects tab, scoped to the selected project; `ProjectCreateDatasetContent` for dataset creation; `ProjectCreateCohortContent` for cohort creation (including a checkbox picker, populated via `DatasetListRequest`, for the dataset(s) to link the cohort to); `ProjectCreateAnalysisContent` for analysis creation (name only); `ProjectCreateFigureContent` for figure creation (name only).
- `ProjectDatasetContent` for a selected dataset, hosting its own nested Parse/Result navigation; `ProjectDatasetParseContent` for configuring and triggering a CSV parse (`CsvParseParams`); `ProjectDatasetResultContent` for displaying the last parse's per-file outcome.
- `ProjectCohortContent` for a selected cohort, hosting the same nested Parse/Result navigation shape; `ProjectCohortParseContent` for configuring and triggering a CSV parse (`CohortParseParams`: delimiter, patient-ID column header, outcome column header); `ProjectCohortResultContent` for displaying the last parse's single outcome (success/patient count, or error).
- `ProjectAnalysisContent` for a selected analysis, hosting a Settings/Result nested navigation shape; `ProjectAnalysisSettingsContent` for configuring (`TwoVariableAnalysisSettings`: Cohort picked via a `CohortListRequest`-populated combo box, two channel names, a bin size per channel) and triggering graph generation, then drawing the resulting PDF via `AnalysisGraphPdfService`; `ProjectAnalysisResultContent` for displaying the last generation's outcome (matched/total patient counts, or error).
- `ProjectFigureContent` for a selected figure, hosting a Size/Select-graphs nested navigation shape; `ProjectFigureSizeContent` for entering and saving the figure's row/column counts; `ProjectFigureSelectGraphsContent` for assigning an existing Analysis (picked via an `AnalysesWithGraphListRequest`-populated combo box per grid cell) to each row/column position, and triggering figure assembly, then drawing the resulting PDF via `FigureGraphPdfService`.

`ProjectFolderService` contains the shared filesystem rules used by the two project-related Settings panels.

## Target platforms and features

The Uno application targets:

- `net10.0-desktop`
- `net10.0`

Enabled Uno features are C# Markup, Material, Toolkit, MVUX, Skia Renderer, and Storage. The desktop target additionally references SkiaSharp, which `AnalysisGraphPdfService` uses to draw each Analysis's heatmap PDF.

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
- Cohort-to-dataset linkage (picked at cohort creation time, persisted in `linked-datasets.json`) is now used for patient-ID matching when generating an Analysis's graph (`AnalysisService`), but still isn't validated at cohort-creation or -linking time itself — see [Cohort](glossary.md#cohort) in the glossary for the intended behavior, and the "Generating an Analysis's graph" bullet under [Persistence and workspace layout](#persistence-and-workspace-layout) for what's implemented so far.
- The Two-variable Analysis association grid has no per-cell minimum-observation filter (only a flat minimum-patient-count placeholder, `Algorithms.AssociationGrid.MinimumPatientsPerCell = 3`, not exposed as a setting), no confidence intervals/p-values, no smoothing, and no density/detrimental-zone/dichotomy/regression variants — the R reference implementation (`docs/r_code_reference.md`) has all of these; this is a deliberately minimal first pass.
- The Figure PDF lays out its whole grid on a single page with no pagination, scaling each cell's heatmap to fit — for large grids (e.g. 8x8 or more) individual cells become small and their fills illegible. This is a deliberately minimal first pass.
- An Analysis's persisted `graph-data.json` is not automatically invalidated when the underlying Cohort or Dataset data it was generated from changes. A Figure assembled from that Analysis will keep drawing the stale grid until the Analysis is explicitly regenerated.

These observations describe the current implementation; changing them requires an explicit product or architectural decision.
