# R Code Reference — Outcome Heatmaps (v2.0)

This document maps the existing R implementation of outcome heatmaps, located at
[`R_code/2026-06-24 - Outcome Heatmaps - Version 2.0/`](../R_code/2026-06-24%20-%20Outcome%20Heatmaps%20-%20Version%202.0/),
so that its logic can be located quickly while porting behavior into OutMapper's GUI.

OutMapper reimplements this pipeline with a GUI on top; for the most part the underlying
computations should match. This doc exists so we don't have to re-read the R source every
time we need to check how something currently works — when implementing a feature, find the
relevant file(s) below, then open them for the actual logic/parameter details.

All paths below are relative to the project root: `R_code/2026-06-24 - Outcome Heatmaps - Version 2.0/`.

---

## 1. Overall pipeline / entry point

### Entry files (project root)

Five top-level "settings scripts", one per heatmap mode:

- `GenerateOneParameterHeatmap.R`
- `GenerateTwoParameterHeatmap.R`
- `GenerateTimeParameterHeatmap.R`
- `GenerateSubgroupHeatmap.R`
- `GenerateSecondaryInsultsHeatmap.R`

Each is a giant, heavily-commented **settings script** meant to be opened in RStudio, hand-edited,
and run directly — there is no separate `main()`/CLI; the settings file *is* the run script. Each
defines a fixed sequence of named settings lists (see §4 for the object shape):

1. `project.execution.settings` (force.rerun, max.number.of.subjects)
2. `import.subjects.table.settings`
3. `locate.time.series.files.settings`
4. `import.time.series.tables.settings`
5. `trim.time.series.tables.settings`
6. `gap.imputation.settings`
7. `define.grid.settings` (mode-specific shape)
8. (implicit) `calculate.percent.vmt.settings`
9. `determine.association.settings`
10. `detrimental.zone.settings`
11. `determine.density.settings`
12. `apply.smoothing.settings`
13. `graph.settings`
14. `visualize.individual.grid.cells.settings`

At the bottom of each `Generate*Heatmap.R` file, a **"PROJECT INITIALIZATION"** block:
- Sets `CurrentProjectType` (`"OneParameter"` | `"TwoParameter"` | `"TimeParameter"` | `"Subgroup"` | `"SecondaryInsults"`).
- Sets mode-specific, non-user-editable settings (e.g. `determine.density.settings$normalization.mode`:
  `"Columns"` for TimeParameter with `ignore.empty.time.bins`, else `"Uniform"` for TimeParameter/SecondaryInsults/TwoParameter,
  `"Rows"` for OneParameter/Subgroup).
- Refuses to run if `association.method == "Ordinal Regression"` outside OneParameter/TwoParameter/Subgroup.
- Calls `source("Code/Shared/ProjectExecution/InitializeProject.R")`.

### `Code/Shared/ProjectExecution/InitializeProject.R` — bootstrap

1. Clears console, closes stray graphics devices.
2. Loads required packages: `cli`, `ggplot2`, `patchwork`, `MASS`.
3. Packs all settings variables from the calling script into one `settings` list (the canonical config object, §4).
4. Clears the R environment except `settings`.
5. Sources every `.R` file under `Code/Shared` (recursive) plus every file directly under `Code/<CurrentProjectType>`
   (non-recursive) — this is how mode-specific functions (e.g. `OneParameter_DefineGrid`) become available.
6. Loads previous settings from disk (`Storage_RetrievePreviousSettings()`), used for rerun-skip optimization.
7. Calls `VerifyAllSettings(settings)` — validates every settings sub-list's types, then overwrites the persisted
   "previous settings" file with the newly verified settings.
8. Loads `failed.module` from disk (which module failed last run, if any).
9. Clears leftover progress bars.
10. Calls `ProjectExecution_Start(settings, previous.settings, failed.module)` (in `RunProject.R`) — the pipeline driver.
11. Cleans up the workspace, keeping only `settings`, `results`, `imported.table`, `converted.table`, `final.table`.

### `Code/Shared/ProjectExecution/RunProject.R` — pipeline driver

`ProjectExecution_Start(settings, previous.settings, failed.module)`:
1. If `force.rerun`, prints a note.
2. `RerunRequirement_DetermineModulesToRun(...)` computes which of the 14 pipeline modules actually need to run (§5).
3. Creates a timestamped output folder: `Storage_CreateOutputFolder()` → `Output/<yyyy-mm-dd HH_MM_SS>/`.
4. Iterates `AllModuleIds` (from `ModuleNames.R`, fixed pipeline order, skipping `"ProjectExecution"`):
   - If in `modules.to.run`: header printed, module recorded as "currently running" (so a crash records which module
     failed), `RunModule(module.id, settings, results)` dispatches, results persisted.
   - Else: header + "Settings not changed. Skipping this step.", previous run's cached results reloaded from disk.
   - Loop breaks early if `results$AnErrorOccured`.
5. Calls `FinalizeResults(settings, results)`.

### `Code/Shared/ProjectExecution/ModuleNames.R`

Canonical, mode-independent pipeline order (`AllModuleIds`, 14 real steps + `ProjectExecution`):

```
ImportSubjectsTable → LocateTimeSeriesFiles → ImportTimeSeriesFiles → PreprocessTimeSeriesTables →
GapImputation → DefineGrid → CountObservations → CalculatePercentVMT → AssociationMatrix →
DetrimentalZone → DensityMatrix → ApplySmoothing → GenerateGraphs → VisualizeGridCells
```

i.e.: **data import → data preparation → grid/observation counting → VMT calc → association/statistics →
detrimental zone → density → smoothing → graph generation → grid-cell visualization → output finalization.**

### `Code/Shared/ProjectExecution/RunModule.R`

Pure dispatcher: `RunModule(module.id, settings, results)`, a big if/else chain mapping module id → function call.
For mode-dependent steps (`DefineGrid`, `CountObservations`) it further dispatches on `settings$project.type` to the
mode-specific implementation. Mode-independent steps call shared functions directly.

> **Note:** although `RunModule.R` calls a single generic `CalculatePercentVMT(settings, results)`, each mode folder
> defines its own `CalculatePercentVMT.R` (same function name) — since `InitializeProject.R` sources the mode's folder
> *after* Shared, the mode-specific version shadows any shared one. There is no shared `CalculatePercentVMT.R`; each of
> the 5 mode folders has its own copy.

### End-to-end stage order (file-level)

1. **Import Subjects Table** — `SubjectsTable.R`
2. **Locate Time Series Files** — `LocateTimeSeriesFiles.R`
3. **Import Time Series Tables** — `ImportTimeSeriesTables.R` (+ `ReadCsvFile.R`, `ConvertColumnToTimestamp.R`, `ConvertColumnToNumeric.R`)
4. **Trim/Preprocess Time Series Tables** — `TrimTimeSeriesTables.R` (+ `SelectObservations.R`, `VerifyOnsetColumn.R`)
5. **Gap Imputation** — `GapImputation.R`
6. **Define Grid** — mode-specific `*_DefineGrid.R` (+ shared `Shared_DefineGrid_DetermineNumericDefinitions.R`)
7. **Count Observations** — mode-specific `*_CountObservations.R` (+ shared `AddIntervalIndexToTable.R`, `CreateCountMatrix.R`; SecondaryInsults also `IdentifyInsults.R`)
8. **Calculate Percent VMT** — mode-specific `CalculatePercentVMT.R`
9. **Determine Association** — `MatrixAssociation.R` → per-cell `GridCellAssociation.R` → one of `BasicCorrelation.R` / `OutcomeBinning.R` / `OptimizedDichotomy.R` / `OrdinalRegression.R` (+ `CorrelationCI.R`)
10. **Detrimental Zone** — `DetrimentalZoneModule.R` → `DetrimentalZoneForMatrix.R` or (SecondaryInsults) `DetrimentalZoneForInsults.R`
11. **Determine Density** — `DetermineDensity.R`
12. **Apply Smoothing** — `ApplySmoothing.R` → `Upsample.R` + `SmoothMatrix.R`/`SmoothRows.R`/`SmoothColumns.R`
13. **Generate Graphs** — `GenerateGraphs.R` → mode-specific `*_DetermineGraphParameters.R` + shared `Shared_DetermineGraphParameters.R` → `CreateGraph.R` + `ColorScale.R`
14. **Visualize Individual Grid Cells** — `VisualizeGridCells.R` (optional; combined PDF of per-cell scatterplots)
15. **Finalize Results** — `FinalizeResults.R` → `CopySettingsToOutput.R`, `CopyTablesToCsv.R`, `Console_ReportSuccessfulRun`/`Console_ReportError`

---

## 2. The five heatmap modes

Domain: physiological time-series (e.g. **PRx** — pressure reactivity index, **CPPopt**) analyzed against patient
**outcome** (e.g. **GOSE** — Glasgow Outcome Scale Extended) in a TBI (traumatic brain injury) cohort. The core
statistic is the association between "percent valid monitoring time" (%VMT — time spent in a given value range)
per grid cell and outcome. Also supports **subgroups** (e.g. age, GCS), **secondary insults** (thresholded excursions,
e.g. ICP/CPP crossing a threshold for a sustained duration), and **time-parameter** heatmaps (parameter value vs. time
since disease onset).

### 2.1 OneParameter — `GenerateOneParameterHeatmap.R`, `Code/OneParameter/`

1-D heatmap: single row, x-axis = parameter value bins (e.g. PRx from -1 to 1, 40 intervals), no y-axis grouping.

- `VerifyOneParameterSettings.R` — verifies `trim.time.series.tables.settings` (`parameter.column.header`),
  `define.grid.settings` (`parameter.lower.limit/upper.limit/number.of.intervals/right.inclusive.intervals/include.both.endpoints`),
  `graph.settings` (`parameter.axis.title`, `parameter.axis.num.labels` + shared graph specs).
- `OneParameter_DefineGrid.R` — `NumMatrixRows <- 1`; `NumMatrixColumns <- parameter.number.of.intervals`; x-axis via
  `Shared_DefineGrid_DetermineNumericDefinitions`; y-axis is an empty stub.
- `OneParameter_CountObservations.R` — per subject: drop incomplete rows, bin `FirstParameter` → `ColumnIndex`
  (`AddIntervalIndexToTable`), constant `RowIndex = 1`, build 1×N count matrix (`CreateCountMatrix`).
- `OneParameter/CalculatePercentVMT.R` — %VMT per column = counts/total*100 (total = in-range or all valid obs, per
  `include.values.outside.grid.range.in.vmt`).
- `OneParameter_DetermineGraphParameters.R` — x-axis from grid settings; empty y-axis.

### 2.2 TwoParameter — `GenerateTwoParameterHeatmap.R`, `Code/TwoParameter/`

2-D heatmap of two simultaneous physiological channels (e.g. CPP x-axis, PRx y-axis); both must exist per timepoint
in the same row (`second.parameter.column.header`).

- `VerifyTwoParameterSettings.R` — `parameter.column.header` + `second.parameter.column.header`; grid settings
  `first.parameter.*` (x) / `second.parameter.*` (y), each with lower/upper/intervals/right-inclusive/include-both-endpoints.
- `TwoParameter_DefineGrid.R` — `NumMatrixRows = second.parameter.number.of.intervals`,
  `NumMatrixColumns = first.parameter.number.of.intervals`.
- `TwoParameter_CountObservations.R` — bins `FirstParameter`→`ColumnIndex`, `SecondParameter`→`RowIndex`, drops
  incomplete rows, builds 2-D count matrix.
- `TwoParameter/CalculatePercentVMT.R` — %VMT = counts/total*100 across the whole matrix (not column-normalized).
- `TwoParameter_DetermineGraphParameters.R` — x/y axis labels independently from grid settings; y-axis label rotation supported.

### 2.3 TimeParameter — `GenerateTimeParameterHeatmap.R`, `Code/TimeParameter/`

x-axis = time since disease onset (bins e.g. days), y-axis = parameter value; requires an onset timestamp column in
the subjects table. Unique overlay: % of subjects contributing data per time bin, plotted as a secondary-axis line.

- `VerifyTimeParameterSettings.R` — grid settings: `parameter.*` (y-axis) plus `time.units`, `time.lower.limit`,
  `time.upper.limit`, `time.number.of.bins`, `time.right.inclusive.intervals`, `time.include.both.endpoints`,
  `onset.column.header`, `ignore.empty.time.bins`. Graph settings add `time.axis.title/num.labels` and density-line
  settings (`include.percent.of.subjects.line`, `percent.line.color` — validated against R's `colors()`,
  `percent.line.width`, `include.secondary.y.axis`, `secondary.y.axis.title`).
- `TimeParameter_DefineGrid.R` — pulls onset column into `ProcessedSubjectsTable$Onset`;
  `NumMatrixRows = parameter.number.of.intervals`, `NumMatrixColumns = time.number.of.bins`. X-axis (time) breakpoints
  via `TimeParameter_DefineTimeBreaks` (evenly spaced); intervals computed with `right=FALSE` fixed regardless of
  setting (minor inconsistency vs. stored `RightInclusive` value — worth checking when porting).
- `TimeParameter_AppendDelayColumn.R` — `MeasurementDelay = difftime(Time, onset.timepoint, units = time.unit)`, cast to integer.
- `TimeParameter_CountObservations.R` — appends delay column, bins delay→`ColumnIndex`, `FirstParameter`→`RowIndex`,
  builds count matrix. Tracks, per time bin, how many subjects had ≥1 non-empty observation
  (`results$NumSubjectsPerTimeBin` / `FractionOfSubjectsPerTimeBin`). If `ignore.empty.time.bins`, empty columns in a
  subject's count matrix are set to `NA` (excluded from %VMT/density for that bin rather than counted as 0%).
- `TimeParameter/CalculatePercentVMT.R` — %VMT is **column-normalized** (per time bin: row count / column sum * 100); NA columns propagate NA.
- `TimeParameter_DetermineGraphParameters.R` — usual x/y axes, plus `TimeParameter_DetermineGraphParameters_FractionLine`
  (Density graph only) building the %-of-subjects overlay line from `FractionOfSubjectsPerTimeBin`. Added in
  `CreateGraph.R` via `CreateGraph_AddFractionOfSubjectsWithDataLine` + `CreateGraph_AddDualYAxises`.

### 2.4 Subgroup — `GenerateSubgroupHeatmap.R`, `Code/Subgroup/`

y-axis = patient subgroup (categorical or numeric-binned), x-axis = parameter value.

- `VerifySubgroupSettings.R` — grid settings add `subgroup.column.header`, `subgroup.type` (`"Categorical"`|`"Numeric"`),
  `subgroup.categories` (categorical), and for numeric: `subgroup.lower.limit/upper.limit/number.of.intervals/right.inclusive.intervals/include.both.endpoints`.
  Graph settings add `group.axis.title/num.labels`, `use.group.assingment.for.labels` [sic], `rotate.group.axis.labels`.
- `AssignGroups.R` — core subgroup logic:
  - Numeric: `AssignGroups_ForNumericColumn` computes breakpoints via `seq(lower, upper, length.out=num.bins+1)`,
    `cut()` assigns each subject to a bin (factor).
  - Categorical: `AssignGroups_ForCategoricalColumn` builds a factor from the raw column, using `subgroup.categories`
    as levels if provided else auto-detected.
  - `AssignGroups_UpdateInclusion` — subjects with `NA` group are excluded (`"Not assigned to a group"`).
- `Subgroup_DefineGrid.R` — `NumMatrixRows = length(PossibleGroups)`, `NumMatrixColumns = parameter.number.of.intervals`.
  Y-axis intervals: categorical labels (`DefineGrid_CategoricalYAxis`) or numeric bins (`DefineGrid_NumericYAxis`).
- `Subgroup_CountObservations.R` — `RowIndex` per subject = `as.numeric(assigned.group)` (constant per subject);
  `ColumnIndex` from parameter binning.
- `Subgroup/CalculatePercentVMT.R` — %VMT = counts/total*100 across full matrix (same style as TwoParameter).
- `Subgroup_DetermineGraphParameters.R` — y-axis labeling switches between literal group labels
  (`_YAxisForCategories`, using `results$PossibleGroups`) and numeric scale (`_YAxisForNumericScale`) based on
  `use.group.assingment.for.labels`.

### 2.5 SecondaryInsults — `GenerateSecondaryInsultsHeatmap.R`, `Code/SecondaryInsults/`

Detects discrete threshold-crossing "insult" episodes (contiguous runs above/below a threshold) instead of raw value
bins; bins insults by (intensity threshold, duration) — x-axis = intensity threshold, y-axis = duration. An
event/episode-detection problem, not a value-histogram problem.

- `VerifySecondaryInsultsSettings.R` — grid settings: `intensity.lower.limit/upper.limit/number.of.intervals`,
  `insult.above.threshold` (direction), `duration.lower.limit/upper.limit/number.of.intervals`,
  `remove.open.ended.insults`, `adjust.count.by.total.vmt` (present but not obviously wired into `CalculatePercentVMT`
  in this snapshot — check before relying on it). Graph settings: `intensity.axis.title/num.labels`,
  `duration.axis.title`, `rotate.duration.axis.labels`, `duration.axis.num.labels`.
- `IdentifyInsults.R` — core episode-detection: `IdentifyInsults(values, thresholds, insult.above.threshold)`.
  - For each threshold (one per intensity bin), flags each timepoint as "is insult" if `value > threshold` (or `<` if
    `insult.above.threshold==FALSE`); `NA` = not-insult.
  - Detects contiguous runs via shifted-vector comparison (manual run-length-encoding).
  - `Duration = end - start + 1` (samples).
  - Flags "open ended" insults: starts at row 1, ends at last row, or immediately adjacent to an `NA` — true
    start/end unobservable. Optionally discarded via `remove.open.ended.insults`.
  - Returns one row per detected insult per threshold: `IntensityThreshold`, `IntensityIndex`, `StartIndex`,
    `EndIndex`, `Duration`, `IsOpenEnded`.
  - Each intensity bin (column) is independently scanned with its own threshold, so a single physiological episode
    can contribute to multiple (intensity, duration) cells — this is how "how high and how long" 2D framing works.
- `SecondaryInsults_DefineGrid.R` — `intensity.breakpoints` evenly spaced; per-column threshold = the breakpoint's
  "low" edge (if `insult.above.threshold`) or "high" edge (otherwise) — not the midpoint. `DurationDefinition` via
  `Shared_DefineGrid_DetermineNumericDefinitions` (right-inclusive fixed TRUE, include-both-limits fixed FALSE).
  `NumMatrixRows` = duration bins, `NumMatrixColumns` = intensity thresholds.
- `SecondaryInsults_CountObservations.R` — calls `IdentifyInsults()` per subject with the grid's thresholds; drops
  open-ended insults if configured; bins `Duration`→`RowIndex`; renames `IntensityIndex`→`ColumnIndex`; count matrix
  cells = number of insult episodes (not raw samples); persists per-subject indexed insult table
  (`Storage_StoreInsultTable`, used by `DetrimentalZoneForInsults`).
- `SecondaryInsults/CalculatePercentVMT.R` — really "percent of insults": column-normalized like TimeParameter
  (cell's insult count / total insults at that intensity level * 100).
- `DetrimentalZoneForInsults.R` (via `DetrimentalZoneModule.R` when `project.type == "SecondaryInsults"`) — per
  subject, per insult: looks up the association value at that insult's (row,column) grid cell; "detrimental" if
  sign of association combined with `high.outcome.scores.are.beneficial` indicates harm (negative association +
  high-is-good ⇒ detrimental; positive association + high-is-bad ⇒ detrimental); marks all raw samples between
  `StartIndex`/`EndIndex` of detrimental insults; `PercentageInDetrimentalZone = 100 × detrimental-samples / total
  non-missing samples`. Differs from `DetrimentalZoneForMatrix.R` (other 4 modes), which works off count-matrix cells
  directly rather than discrete episodes.
- `SecondaryInsults_DetermineGraphParameters.R` — x-axis = intensity, y-axis = duration, y-axis rotation option.

---

## 3. Shared modules (`Code/Shared/*`)

### 3.1 DataImport

| File | Purpose / key functions | Returns |
|---|---|---|
| `SubjectsTable.R` | `SubjectsTable_Import(settings, results)`: reads subjects CSV (`ReadCsvFile`), verifies ID column (no missing/duplicate — `SubjectsTable_VerifyIdColumn`), converts numeric/timestamp columns, builds `ProcessedSubjectsTable` (Id, Included=TRUE, ExclusionReason=""), applies `max.number.of.subjects` cap (excludes tail). | `results` with `OriginalSubjectsTable`, `ConvertedSubjectsTable`, `ProcessedSubjectsTable` |
| `LocateTimeSeriesFiles.R` | `LocateTimeSeriesFiles(settings, results)`: lists `.csv` files in `storage.folder`, matches each subject ID as a substring of filenames (`grep(fixed=TRUE)`); errors on 0 or >1 matches per subject; excludes unmatched subjects (`"No time series file"`); errors on 0 total matches or duplicate file assignment. | `results$ProcessedSubjectsTable` + `TimeSeriesFile` column |
| `ReadCsvFile.R` | `ReadCsvFile(path, sep.char)`: validates `.csv` extension + existence, `read.table(header=TRUE, stringsAsFactors=FALSE, check.names=FALSE)`, checks ≥2 columns, ≥1 row, no duplicate headers. | `list(Success, Table)` |
| `ConvertColumnToNumeric.R` | `ConvertColumnToNumeric_InTable(table.name, table, columns, dec.char)`: handles decimal-comma vs decimal-point, per-value `as.numeric()` with error collection (up to 10 bad rows then summary). | `list(Success, Table)` |
| `ConvertColumnToTimestamp.R` | `ConvertColumnToTimestamp_InTable(...)`: fast vectorized `as.POSIXct()` first, falls back to per-value conversion with error reporting. | `list(Success, Table)` |
| `ImportTimeSeriesTables.R` | `ImportTimeSeriesTables(settings, results)`: per included subject, reads matched CSV, converts time→timestamp and other columns→numeric, saves via `Storage_StoreImportedTimeSeries`. Uses `ProgressBar_*`. | `results` |
| `VerifyOnsetColumn.R` | `VerifyOnsetColumn(selection.mode, timestamp.headers, onset.time.header, results)`: no-op unless `inclusion.mode == "Relative to disease onset"`; validates onset column exists/declared as timestamp; copies into `ProcessedSubjectsTable$Onset`. | `list(Success, ProcessedSubjectsTable)` |

### 3.2 DataPreparation

| File | Purpose / key functions | Returns |
|---|---|---|
| `SelectObservations.R` | `SelectObservations_SelectColumns(table, time.header, parameter.header, second.parameter.header=NULL)`: normalizes to `Time`/`FirstParameter`/(`SecondParameter`) columns. `SelectObservations_AllValues` (passthrough). `SelectObservations_RelativeToStartOfRecording(table, first.obs, last.obs)`: row-index slice. `SelectObservations_RelativeToDiseaseOnset(table, onset.time, relative.start, relative.end)`: filters rows within `[onset+start*60, onset+end*60]` seconds (settings in minutes). | trimmed data.frame |
| `TrimTimeSeriesTables.R` | `TrimTimeSeriesTables(settings, results)`: per subject: checks required parameter column(s) exist; calls `SelectObservations_*` per `inclusion.mode` ("All values" / "Relative to start of recording" / "Relative to disease onset"); records `NumObservationsInRange`; excludes subjects below `num.required.valid.observations`; stores trimmed table (`Storage_StoreProcessedTimeSeries`). Errors if zero subjects remain. | `results` |
| `GapImputation.R` | `GapImputation(settings, results)`: for `FirstParameter`/`SecondParameter`, `GapImputation_IdentifyGaps(values, max.gap.length, id, channel)` finds `NA` runs bounded by valid values, ≤`max.gap.length` (excludes gaps touching series start/end). `GapImputation_CloseAllGaps` applies **Last Observation Carried Forward** or **Linear Interpolation** (`seq(from=before, to=after, length.out=gap+2)`) per gap. Aggregates `GapTable` across subjects. | `results$GapTable`, stores imputed series |
| `PlotImputedGap.R` | Debug-only helper (not called by pipeline) — visualizes original vs. imputed values around a gap. | plot (side effect) |

### 3.3 ObservationCounting

| File | Purpose / key functions | Returns |
|---|---|---|
| `Shared_DefineGrid_DetermineNumericDefinitions.R` | `Shared_DefineGrid_DetermineNumericDefinitions(lower, upper, num.bins, right.inclusive, include.both.limits)`: `breakpoints <- seq(lower, upper, length.out=num.bins+1)`, interval labels via `levels(cut(NA, breaks, right=right.inclusive, include.lowest=include.both.limits))`. **Canonical grid/bin definition routine used by every numeric axis in every mode.** | `list(Breakpoints, Intervals, RightInclusive, IncludeBothLimits)` |
| `AddIntervalIndexToTable.R` | `AddIntervalIndexToTable(table, value.column.name, interval.def, index.column.name)`: `cut()` with the interval def's breakpoints/right-inclusivity/include-lowest, `labels=FALSE` → integer bin indices. Out-of-range values → `NA` (dropped later via `complete.cases`). | table + index column |
| `CreateCountMatrix.R` | `CreateCountMatrix(indexed.table, num.matrix.rows, num.matrix.columns)`: zero matrix, increments `count.matrix[RowIndex, ColumnIndex]` per row. **The per-subject observation/insult count matrix** — atomic "histogram" unit for every mode. | numeric matrix |
| `DetermineDensity.R` | `DetermineDensity(settings, results)`: sums each subject's count matrix divided by that subject's total valid observation count (`NumObservationsInRange`) — per-subject-normalized sum (prevents long-monitored subjects dominating). Optional `DetermineDensity_Logarithmic` (`log(count+1)`) vs `_Linear`. `DetermineDensity_NormalizeMatrix` divides by global max (`"Uniform"`), per-row max (`"Rows"`), or per-column max (`"Columns"`) per `normalization.mode` (set per-mode, see §1/§2). | `results$OriginalMatrices$Density` |

### 3.4 DetermineAssociation

| File | Purpose / key functions | Returns |
|---|---|---|
| `MatrixAssociation.R` | `MatrixAssociation_Build(settings, results)`: outer driver. Validates/attaches outcome column (`_ValidateOutcomeColumn` — must be numeric, excludes subjects with missing outcome). Loads every included subject's count/%VMT matrices. For every grid cell, extracts per-subject (%VMT, outcome, count) triples (`_ExtractGridCellData`, drops incomplete via `complete.cases`), calls `GridCellAssociation(...)`, persists raw per-cell result (`Storage_StoreGridCellData`, for grid-cell PDF viz), folds results into output matrices (`_UpdateMatrices`). Produces: `Association`, `PValue`, `Intercept`, `Slope`, optionally `CILower`/`CIUpper`, and (optimized dichotomy) `DichotomyOutcome`, `DichotomyPercentVMT`, `DichotomyPercentile`. | `results$OriginalMatrices` |
| `GridCellAssociation.R` | `GridCellAssociation(settings, results, percent.vmts, outcomes, observation.counts)`: gatekeeper — fails if too few subjects meet `num.required.observations`/`num.required.subjects`, or %VMTs/outcomes are all identical (no variance). Else dispatches to one of 4 algorithms by `association.method`. | `list(Success, Association, PValue, CILower, CIUpper, Intercept, Slope, ...)` or `list(Success=FALSE)` |
| `BasicCorrelation.R` | `pearson`/`kendall`/`spearman`: `cor.test(percent.vmts, outcomes, method=...)`. CI via `CorrelationCI_Fisher`/`_Bootstrap`/none. If `require.significant.correlation` and `p >= threshold`, association+CI nulled to `NA` (rendered white). Also fits `lm(outcomes ~ percent.vmts)` for intercept/slope (scatterplot regression line). | association.result list |
| `OutcomeBinning.R` | `Binned - Pearson/Spearman/Kendall`: groups subjects by unique outcome value, computes each group's mean %VMT (`outcome.level.means`), correlates that against outcome levels (one point per outcome category, not per subject) via `cor.test`. CI/significance/regression logic mirrors `BasicCorrelation`. Extra fields `Estimate`, `OutcomeLevels`, `OutcomeMeans` used for red-dot overlays in `VisualizeGridCells.R`. | association.result list |
| `OptimizedDichotomy.R` | `optimized dichotomy`: `OptimizedDichotomy_Calculate` brute-forces all midpoints between consecutive unique values of outcome and %VMT (`_FindPotentialDichotomizationPoints`) as candidate binary splits; for every (outcome-split, vmt-split) pair, dichotomizes both to booleans, computes Spearman correlation between them; skips splits leaving either group below `num.subjects.required.after.dichotomization` (`_DichotomizationIsInvalid`); keeps the split pair with largest `abs(correlation)`. Records `DichotomyOutcome`, `DichotomyPercentVMT` (winning thresholds), `DichotomyPercentile`. CI via `_EstimateConfidenceInterval` (Fisher on winning correlation, or bootstrap on dichotomized vectors). | association.result list, `Association = strongest.correlation` |
| `OrdinalRegression.R` | `Ordinal Regression`: proportional-odds ordinal logistic regression via `MASS::polr(Outcome ~ PercentVmts + confounders, data=..., Hess=TRUE)`, `Outcome = as.factor(outcomes)`; confounders from `ord.reg.confounders`, pulled from `ConvertedSubjectsTable`. Extracts `PercentVmts` coefficient, SE, Wald z, 2-sided normal p-value. CI = `beta ± SE` (fixed, not configurable). Also fits a separate `lm()` purely for intercept/slope overlay (on the raw, non-ordinal outcome). ⚠️ Writes `TempResults.rds` on every call (debug artifact — remove when porting). ⚠️ No explicit `return()` — relies on implicit last-expression return; works but fragile. | association.result list (implicit) |
| `CorrelationCI.R` | `CorrelationCI_Fisher(r, n, percentage)`: Fisher z-transform CI, back-transformed via `tanh`. `CorrelationCI_Bootstrap(x, y, percentage, num.samples)`: resamples with replacement, Spearman correlation each time (skips degenerate zero-variance resamples), returns empirical `quantile()`. | `list(LowerLimit, UpperLimit)` |
| `DetrimentalZoneModule.R` | Dispatcher: no-op if `determine.time.spent.in.the.detrimental.zone` is FALSE; else routes to `DetrimentalZoneForInsults` (SecondaryInsults) or `DetrimentalZoneForMatrix` (other modes). | `results` |
| `DetrimentalZoneForMatrix.R` | Per subject: classifies each cell as positive/negative-or-≤0 association (NA excluded); sums subject's raw counts falling into each; maps positive/negative→beneficial/detrimental via `high.outcome.scores.are.beneficial`; `PercentageInDetrimentalZone = 100 * detrimental/(detrimental+beneficial)` (NA if none classified). | adds `PercentageInDetrimentalZone` to `ProcessedSubjectsTable` |

### 3.5 Smoothing

| File | Purpose / key functions | Returns |
|---|---|---|
| `Upsample.R` | `Upsample_Matrix(input, scale.factor)`: replicates each cell into a `scale.factor × scale.factor` block. `Upsample_Rows`: horizontal-only replication (rows unchanged) — used for "Rows" smoothing direction. `Upsample_Columns`: vertical-only. | upsampled matrix |
| `SmoothMatrix.R` | `SmoothMatrix(input.matrix, sd)`: 2-D Gaussian kernel smoothing. Kernel radius = `ceiling(sd*3)`; weight matrix via `_GetDensityFor2dGaussian` (isotropic 2D normal density, `1/(2π sd²) * exp(-1/2 (x²+y²)/sd²)`). Per cell: extract surrounding window (`_GetSurroundingGridCells`, out-of-bounds→NA), weighted average with weights renormalized excluding NA neighbors (`_CalculateWeightedAverage`); **cells whose original value is NA remain NA**. Progress bar. | smoothed matrix |
| `SmoothRows.R` / `SmoothColumns.R` | Same Gaussian-weighted-average algorithm restricted to a 1-D kernel along a single row/column — used for `smoothing.direction = "Rows"`/`"Columns"`. Weight vector via `dnorm(positions, mean=0, sd)`. | smoothed matrix |
| `ApplySmoothing.R` | `ApplySmoothing(settings, results)`: no-op if `use.smoothing` is FALSE. Else, for every matrix in `results$OriginalMatrices` (excluding `Intercept`/`Slope`; excluding dichotomy matrices unless `visualize.dichtomization`; excluding `PValue` unless `require.significant.correlation`): upsample by `upsample.matrix.factor`, then Gaussian-smooth with `sd = gaussian.smoothing.std.dev * upsample.matrix.factor` (configured SD is in original-grid-cell units, scaled to subcell units), using mode/setting-selected direction (`Uniform`→`SmoothMatrix`, `Rows`→`SmoothRows`, `Columns`→`SmoothColumns`). | `results$SmoothedMatrices` |

### 3.6 GraphGeneration

| File | Purpose / key functions | Returns |
|---|---|---|
| `ColorScale.R` | `ColorScale_AvailableScales()`: `jet, viridis, magma, inferno, plasma, cividis, rocket, mako, turbo, blue_red_binary`. `ColorScale_Create(name, reverse, num.colors)`: `jet` and `blue_red_binary` hand-built (`jet` via `colorRampPalette` over 9 anchor colors; `blue_red_binary` = exactly 2 flat colors, `#2e24b2` blue / `#e31a1a` red, split at midpoint — used for p-value graphs to threshold significant vs not); others delegate to `viridisLite::<name>(n, direction)`. | hex color vector |
| `CreateGraph.R` | `CreateGraph(graph.parameters)`: `ggplot2` heatmap — `theme_classic()`, `geom_tile(aes(x=Column, y=Row, fill=Value))` on long-format table, color via `scale_fill_gradientn` (continuous, `na.value="white"`) or `scale_fill_stepsn` (discrete, only the "Original"-style `DichotomyOutcome` graph), custom axis breaks/labels/titles with optional rotation, optional secondary y-axis + overlay line (TimeParameter %-of-subjects line), optional title, styled vertical color-bar legend. | `ggplot` object |
| `GenerateGraphs.R` | `GenerateGraphs(settings, results)`: builds ordered graph list (`_GetGraphList`) — `CILower`, `Association`, `CIUpper` (if CI enabled), `Density`, dichotomy graphs (`DichotomyOutcome`, `DichotomyPercentVMT`, `DichotomyPercentile` — only optimized dichotomy + `plot.dichotomization.graphs`), `PValue` (only if `plot.p.value.graph` AND `require.significant.correlation`) — crossed with styles `Original` (+ `Smoothed` if enabled). Per graph: mode-specific `*_DetermineGraphParameters` → `CreateGraph` → prints + saves `<output.folder>/<Label>_<Style>.pdf`. | `results$CreatedGraphs` |
| `Shared_DetermineGraphParameters.R` | Shared building blocks reused by every mode's `*_DetermineGraphParameters.R`: `_Legend` (continuous vs discrete, by graph type), `_NumLegendLabels`, `_LegendTitle`, `_Title`, `_ColorVector` (gradient — 1000 colors normally, or `num.possible.outcomes - 1` for discrete dichotomy-outcome graph, + `_AddColorTransitionZone` paints colors 450:550 grey if `visualize.transition.zone` — marks association-≈-0 zone), `_SelectColorScale`/`_ShouldReverseColorScale`, `_DetermineValueRange` (per-graph-type min/max, e.g. Density always 0–1), `_DetermineValueMatrix`/`_SelectValueMatrix` (Original vs Smoothed) + `_ApplyValueRangeToMatrix` (clamps to scale range), `_TurnMatrixIntoTable` (wide matrix → long `Row`/`Column`/`Value` via base `reshape`, feeds `geom_tile`). | assorted parameter lists/tables |
| `VisualizeGridCells.R` | `VisualizeGridCells(settings, results)`: optional diagnostic (`visualize.grid.cells`). For configured row/col window (max ~90×90, PDF size limit), retrieves each cell's stored raw data (`Storage_RetrieveGridCellData`), builds a small %VMT-vs-outcome scatterplot per cell (`GenerateGridCellGraph`): jittered outcome values, border colored by the cell's association-graph color (`_DetermineColorMatrix`, reuses main graph's color-vector/range logic), fitted regression line (`geom_abline` from `Intercept`/`Slope`), dichotomy threshold lines if applicable, binned-outcome-mean red dots (Binned methods). Title shows axis-interval labels, association value, significance-starred p-value (`_FormatPValueString`: `***` p<0.001, `**` p<0.01, `*` p<0.05). Combined via `patchwork::wrap_plots` into one multi-panel PDF (`GridCellGraphs.pdf`, `2in × ncols/nrows`). | pdf (side effect) |

### 3.7 FinalizeOutput

| File | Purpose / key functions | Returns |
|---|---|---|
| `FinalizeResults.R` | `FinalizeResults(settings, results)`: on error, stashes `results` into global `PartialResults`, reports via `Console_ReportError`. On success, publishes `settings`/`results`/`imported.table`/`converted.table`/`final.table` to global env, saves `results.rds`, calls `CopySettingsToOutput` + `CopyTablesToCsv`, reports success. | side effects |
| `CopySettingsToOutput.R` | Copies the literal settings source file to `<output>/settings.txt`; serializes full settings list to `<output>/settings.rds`. | side effects |
| `CopyTablesToCsv.R` | Writes `OriginalSubjectsTable`, `ConvertedSubjectsTable`, `ProcessedSubjectsTable` each to CSV in output folder (comma-sep, no quoting, blank for NA). | side effects |

### 3.8 ConsoleOutput

| File | Purpose / key functions |
|---|---|
| `Console.R` | `Console_ReportSuccessfulRun()`/`_ReportError(module.id, error.code)`: boxed messages via `cli::boxx` (green success; brown error, showing module name + `ErrorCodes_GetErrorDescription`). `_ReportSkippedStep()`: magenta "Settings not changed. Skipping this step." `_PrintNote(note)`: cyan underlined note. |
| `ErrorCodes.R` | Named list of error codes: `MissingSettings`, `IncorrectSettingType`, `MissingIdColumnInSubjectsTable`, `AttemptingToVisualizeTooManyGridCellRows`/`Columns`. `_GetErrorDescription(code)`: code→message, default "No error description available." ⚠️ Naming mismatch: `VisualizeGridCells.R` sets `ErrorCodes$AttemptingToVisualizeNonExistingGridCellRows`/`Columns`, which are **not defined** (only `...TooMany...` exists) — throws an undefined-lookup error instead of the intended message. Fix rather than replicate. |
| `ProgressBar.R` | Thin wrapper over `cli::cli_progress_bar`/`_update`/`_done`/`_cleanup`, targeting `parent.frame(2)` so the bar's lifetime ties to the *calling* function. `_Initialize` sleeps 1.5s to guarantee render before fast loops proceed — replace this pattern with a real GUI progress indicator (see §5). |

### 3.9 TypeSafety

| File | Purpose / key functions |
|---|---|
| `TypeIsCorrect.R` | `TypeIsCorrect(variable, expected.attributes)`: mini type-assertion DSL. First attribute `"vector"`/`"list"`; remaining (for vectors): `has.elements`, `one.element`, `logical`, `character`, `numeric`, `integer` (custom "rounds to itself" test, not `is.integer`), `no.na`, `positive`, `non.negative`. | `list(CorrectType, Cause)` |
| `VerifyDataType.R` | `VerifyDataType(variable, expected.type)`: wraps `TypeIsCorrect`; on failure walks `sys.calls()` for the caller's name, prints a verbose diagnostic (call, `str()`, `typeof()`, value), stashes bad value in global `the.invalid.variable`, then `stop()`s. Used pervasively as an internal assertion mechanism throughout DetermineAssociation and Smoothing, not just for settings. | — |
| `VerifySettingsList.R` | `VerifySettingsList(actual.settings, expected.types, module.id)`: checks every expected name exists (else `Console_ReportError(MissingSettings)` + `stop()`), warns on extra/unverified settings, checks each value's type via `TypeIsCorrect` (`Console_ReportError(IncorrectSettingType)` + `_DisplayVerificationError` on failure). | — |
| `VerifySharedSettings.R` | Per-module expected-type specs for settings blocks common to all 5 modes (`ProjectExecution`, `ImportSubjectsTable`, `LocateTimeSeriesFiles`, `ImportTimeSeriesFiles`, `GapImputation`, `DetermineAssociation`, `DetermineDensity`, `ApplySmoothing`) + reusable graph-spec builders (`_GetAssociationGraphSpecification`/`_GetDensityGraphSpecification`/`_GetDichotomizationGraphSpecification`/`_GetPValueGraphSpecification`) consumed by every mode's `Verify<Mode>Settings_GenerateGraphs`. Also validates `association.method`/`confidence.interval.method` against whitelists, ordinal-regression confounders are numeric and outcome isn't also a confounder, and every color scale name is in `ColorScale_AvailableScales()`. | — |
| `VerifyAllSettings.R` | `VerifyAllSettings(settings)`: top-level orchestrator (called once per run from `InitializeProject.R`); calls shared verifiers in pipeline order, dispatches mode-specific `Verify<Mode>Settings_TrimTimeSeriesTables`/`_DefineGrid`/`_GenerateGraphs` by `settings$project.type`. ⚠️ Last line, `VerifySharedSettings_VisualizeIndividualGridCells`, is a bare function reference — never actually called — so grid-cell-visualization settings are never type-verified at startup. Fix rather than replicate. | — |

### 3.10 ProjectExecution

Also see §1: `RunProject.R`, `InitializeProject.R`, `RunModule.R`, `ModuleNames.R`.

- **`RerunRequirement.R`** — `RerunRequirement_DetermineModulesToRun(current.settings, previous.settings, failed.module)`:
  reruns everything if there's no previous run, `force.rerun` is TRUE, or the subject limit changed. Otherwise walks
  the 13 real modules in order; `GenerateGraphs`/`VisualizeGridCells` **always** rerun (cheap/output-only). If a
  module's settings sub-list differs from the previous run (`CompareLists_ListsAreEqual`, from `CompareLists.R` —
  manual recursive equality check treating `NA==NA` as equal) or it's the module that failed last time, that module
  **and every subsequent module** rerun (cascading invalidation). `CountObservations` is exempt from
  settings-comparison — it has no independent settings; its trigger is `DefineGrid` changing.
- **`Storage.R`** — RDS-file-based cache/pipeline-state store under `TemporaryFiles/<ProjectType>/<StageName>/`, one
  `Store*`/`Retrieve*` function pair per artifact type: intermediate results (per module), imported/processed/imputed
  time series (per subject), grid counts (per subject), percent VMT (per subject), grid cell data (per row/column),
  insult tables (per subject, SecondaryInsults only), failed-module marker, previous settings, timestamped output
  folder (`Output/<yyyy-mm-dd HH_MM_SS>/`). This on-disk staged-caching design is what enables skipping unchanged
  pipeline stages between runs — an important pattern to replicate (or replace with an in-memory/DB equivalent).

---

## 4. Key data structures

### `settings` — assembled once in `InitializeProject.R`

Single nested named list, the config object threaded through every function call:

```
settings$
  project.type                          # "OneParameter" | "TwoParameter" | "TimeParameter" | "Subgroup" | "SecondaryInsults"
  settings.file.path                    # e.g. "GenerateOneParameterHeatmap.R"
  output.folder                         # set later by RunProject.R
  project.execution.settings            # force.rerun, max.number.of.subjects
  import.subjects.table.settings        # file path, csv sep/decimal char, id/numeric/timestamp column headers, time format/tz
  locate.time.series.files.settings     # storage.folder
  import.time.series.tables.settings    # csv sep/decimal char, time column header/format/tz
  trim.time.series.tables.settings      # parameter.column.header(s), inclusion.mode + its 3 sub-modes' params, num.required.valid.observations
  gap.imputation.settings               # imputation.method, max.gap.length
  define.grid.settings                  # shape varies per mode — see §2
  calculate.percent.vmt.settings        # include.values.outside.grid.range.in.vmt
  determine.association.settings        # outcome column, association.method, CI method/params, dichotomy/regression params, significance gating
  detrimental.zone.settings             # determine.time.spent.in.the.detrimental.zone, high.outcome.scores.are.beneficial
  determine.density.settings            # use.logarithmic.density, normalization.mode (auto-set per mode)
  apply.smoothing.settings              # use.smoothing, smoothing.direction, upsample.matrix.factor, gaussian.smoothing.std.dev
  graph.settings                        # per-graph-type titles/scales/colors — shape varies per mode (axis titles differ)
  visualize.individual.grid.cells.settings  # visualize.grid.cells, row/col window, scatter options
```

### `results` — accumulator threaded module-to-module

```
results$
  AnErrorOccured, ErrorCode, CurrentModule
  OriginalSubjectsTable, ConvertedSubjectsTable    # raw and type-converted subjects CSV
  ProcessedSubjectsTable                           # canonical per-subject state table (see below)
  GapTable                                         # aggregated gap-imputation report across all subjects
  Grid                                              # $NumMatrixRows, $NumMatrixColumns, $XAxis, $YAxis (interval-def: Breakpoints/Intervals/RightInclusive/IncludeBothLimits/Label), + SecondaryInsults-only $IntensityThresholds/$InsultAboveThreshold/$DurationDefinition
  PossibleGroups, GroupBreakpoints                 # Subgroup mode only
  NumSubjectsPerTimeBin, FractionOfSubjectsPerTimeBin  # TimeParameter mode only
  OriginalMatrices                                 # named list: Association, PValue, Intercept, Slope, [CILower, CIUpper], [DichotomyOutcome, DichotomyPercentVMT, DichotomyPercentile], Density — all NumMatrixRows x NumMatrixColumns
  SmoothedMatrices                                  # same shape/keys as OriginalMatrices, upsampled + Gaussian-smoothed
  CreatedGraphs                                     # named list of ggplot objects, e.g. "Association_Original"
```

### `ProcessedSubjectsTable` (data.frame, one row per subject)

Columns accumulate over the pipeline: `Id`, `Included` (bool), `ExclusionReason` (human-readable string), `Onset`
(POSIXct, if relevant), `HasParameterColumn`/`HasFirstParameterColumn`/`HasSecondParameterColumn`,
`NumObservationsInRange`, `EnoughObservationsInRange`, `TimeSeriesFile`, `TimeSeriesFileSuccessfullyImported`,
`NumObsWithinGrid`, `Outcome` (numeric), `GroupValue`/`AssignedGroup` (Subgroup mode),
`PercentageInDetrimentalZone`. Essentially a running "why was this subject excluded" audit trail plus per-subject
scalar summary stats.

### Per-subject time series table

`Time` (POSIXct), `FirstParameter`, optionally `SecondParameter` (numeric, may contain `NA`). Persisted at 4
successive stages (Imported → Processed/trimmed → Imputed), one RDS file per subject per stage.

### Per-subject count matrix (`RowIndex` × `ColumnIndex`)

Plain numeric matrix, shape = `NumMatrixRows` × `NumMatrixColumns` for that mode's grid; `matrix[r,c]` = number of
raw observations (or, SecondaryInsults, insult episodes) in that cell for that subject. Combined across subjects to
build the density matrix and to extract per-cell (%VMT, outcome) samples for association testing.

### Per-subject %VMT matrix

Same shape as count matrix; `matrix[r,c]` = percent of that subject's (in-range or total) valid monitoring time in
that cell. Normalization differs by mode (global-total for OneParameter/TwoParameter/Subgroup vs. per-column for
TimeParameter/SecondaryInsults).

### Grid cell data (per (row,column) cell, persisted for later grid-cell visualization)

```
list(XAxisLabel, XAxisInterval, YAxisLabel, YAxisInterval, Ids, Counts, PVMT, Outcome, Success,
     Association, CILower, CIUpper, PValue, Intercept, Slope, [Dichotomy*])
```

The full per-subject vectors that went into that cell's statistical test, plus the test's outputs — powers the
diagnostic scatterplot PDF.

### Insult table (SecondaryInsults only, per subject)

data.frame, one row per detected insult episode: `IntensityThreshold`, `IntensityIndex` (renamed →`ColumnIndex`),
`StartIndex`, `EndIndex`, `Duration`, `IsOpenEnded`, `RowIndex` (duration bin).

### Value table for plotting (`Shared_DetermineGraphParameters`'s `_TurnMatrixIntoTable`)

Long-format data.frame: `Row`, `Column`, `Value` — direct input to `ggplot2::geom_tile`.

---

## 5. Cross-cutting concerns (GUI-porting notes)

**Settings verification / type safety.** A hand-rolled type-assertion mini-framework (`TypeIsCorrect.R` /
`VerifyDataType.R` / `VerifySettingsList.R`) checks every settings value's base type (vector/list) plus attributes
(`character`/`numeric`/`integer`/`logical`, `one.element`/`has.elements`, `no.na`, `positive`/`non.negative`)
against a per-module "expected shape" spec (`VerifySharedSettings.R` + per-mode `Verify<Mode>Settings.R`). Runs once
at startup (`VerifyAllSettings`) before any pipeline module executes, and is also used ad-hoc inside numeric
algorithm code (Smoothing, DetermineAssociation) as a lightweight internal assert. **For the C# port:** maps
naturally to a strongly-typed settings model + validation attributes/FluentValidation; surface
`VerifySettingsList_DisplayVerificationError`'s human-readable explanations as form-level validation messages
rather than console output/`stop()`.

**Error handling / error codes.** No R-condition exceptions in normal flow — almost every module function returns a
`results` list with `AnErrorOccured` (bool) + `ErrorCode` (string key into `ErrorCodes.R`); the main loop in
`RunProject.R` checks this after each module and breaks early. Fatal/unexpected conditions (bad types, unknown enum
values) use `stop()` directly (hard crash + printed diagnostic). Known inconsistencies in this snapshot: (1)
`ErrorCodes.R` doesn't define `AttemptingToVisualizeNonExistingGridCellRows`/`Columns` even though
`VisualizeGridCells.R` sets exactly those codes (only `...TooMany...` exists); (2) `VerifyAllSettings.R`'s last line
references `VerifySharedSettings_VisualizeIndividualGridCells` without calling it. **For the GUI:** replace the
`results$AnErrorOccured`/`ErrorCode` pattern with a typed `Result<T>`/exception-with-error-code pattern, seed
user-facing error text from the `ErrorCodes_GetErrorDescription` catalogue (fixing the two mismatches above rather
than reproducing them).

**Console/progress output pattern.** Heavy use of `cli`-package boxed messages (`Console_ReportSuccessfulRun`/
`_ReportError`/`_ReportSkippedStep`/`_PrintNote`) and a `cli_progress_bar` wrapper (`ProgressBar_Initialize/_Update/
_Finalize/_ClearAll`) tied to the calling function's frame, used inside virtually every per-subject or per-grid-cell
loop (import, trim, impute, count, associate, smooth, visualize). **For the GUI:** `ProgressBar_Initialize(label,
count)` + repeated `_Update()` + `_Finalize()` maps directly onto a determinate progress bar per pipeline stage
(`label` → stage-name shown in UI); `Console_Report*` maps onto a stage-by-stage status/log panel (success=green,
error=red/detail using module display name + error description, skipped=grey "unchanged, reused cached result").

**Rerun/caching pattern (state management).** The pipeline is built around **idempotent, disk-cached stages**:
`Storage.R` persists every intermediate artifact (imported/processed/imputed time series, count matrices, %VMT
matrices, grid cell data, insult tables, settings, failed-module marker) as per-subject/per-module RDS files under
`TemporaryFiles/<ProjectType>/...`; `RerunRequirement.R` compares new vs. previously-persisted settings
(`CompareLists_ListsAreEqual`) to decide, module-by-module, whether to recompute or reload cached results, with
cascading invalidation (a changed or previously-failed module forces every subsequent module to rerun too). Worth
preserving in the port (e.g. a per-stage cache keyed by a hash/equality-check of that stage's settings, invalidated
top-down) so tweaking only a graph-cosmetics setting doesn't force re-running data import or association statistics
— directly relevant to GUI responsiveness.

---

## Known issues in the R snapshot (do not silently replicate)

- `ErrorCodes.R` is missing `AttemptingToVisualizeNonExistingGridCellRows`/`Columns`, referenced by `VisualizeGridCells.R`.
- `VerifyAllSettings.R`'s call to `VerifySharedSettings_VisualizeIndividualGridCells` is a bare reference, never invoked — those settings are never validated.
- `TimeParameter_DefineGrid.R` computes time-axis intervals with `right=FALSE` hardcoded, regardless of `time.right.inclusive.intervals`.
- `OrdinalRegression.R` writes `TempResults.rds` on every call (debug leftover) and relies on implicit return.
- `SecondaryInsultsSettings`'s `adjust.count.by.total.vmt` doesn't appear wired into `CalculatePercentVMT` in this snapshot — verify before assuming it's active.
