# OutMapper Application Overview

## Purpose

OutMapper is a research tool for creating outcome heatmaps.

In OutMapper, an outcome heatmap visualizes how values in physiological time-series data are associated with patient outcomes. Its purpose is to help researchers explore and communicate relationships between physiological measurements over time and clinically relevant outcomes.

## Intended users

The expected users are medical researchers working with physiological time-series data and patient outcomes. For guidance on their likely technical background and how that should shape UI language, see [`user_profile.md`](user_profile.md).

The application should therefore prioritize:

- Accurate and transparent handling of research data.
- Clear presentation of analytical results.
- Reproducible research workflows.
- Terminology and interactions appropriate for medical research.

## Core domain concepts

### Outcome heatmap

A visualization showing associations between values in physiological time-series data and patient outcomes.

### Physiological time-series data

Measurements of physiological variables collected across time. Sourced today from per-patient CSV files with a configurable delimiter, decimal separator, and timestamp column/format — everything else in the file is a channel. Timestamps must be strictly increasing and unique; a value cell may be empty (missing), but any cell that isn't empty and doesn't parse fails the whole file. The supported variables and sampling structure are not yet further documented.

### Patient outcome

The clinical or research outcome against which physiological values are analyzed. The supported outcome types and representations are not yet documented.

## Application structure

OutMapper currently includes concepts for projects, datasets, and cohorts. A project lives at whatever folder the user creates or opens it at, and each dataset or cohort belongs to exactly one project. Other domain relationships and responsibilities will be documented as the product model is clarified.

## Research workflow

Importing and parsing raw CSV data into a dataset's time series is implemented; see [Data and folder structure](#data-and-folder-structure) below. Importing and parsing a single CSV file into a [Cohort](glossary.md#cohort) — one patient ID column and one outcome column, identified by header name — is also implemented, including a basic picker for which dataset(s) the cohort is linked to.

A first, minimal [Analysis](glossary.md#analysis) workflow is also implemented, covering only the [Two-variable](glossary.md#two-variable) method: the user creates an Analysis (name only), then configures it (Cohort, two channel names, a bin size per channel) and triggers graph generation. Generation matches the Cohort's patients to their time series by patient ID across the Cohort's linked dataset(s) — the patient-level matching the linkage picker exists to support — computes a per-cell Spearman correlation between percent-time-in-cell and outcome, and exports an Outcome heatmap PDF into the project's Output folder. Everything else in the R reference implementation's workflow (other analysis methods, Density heatmaps, smoothing, confidence intervals, subgroup/secondary-insults modes) is not yet implemented; see `docs/architecture.md`'s "Known architectural limitations" for specifics.

## Data and folder structure

A project lives at an arbitrary folder the user picks: either a new folder created via the Create Project panel (which lets the user choose the parent location, then names the project) or an existing project folder opened directly. Users select one project at a time as the current project, and OutMapper keeps a bounded, most-recently-used list of project folders so previously opened projects can be reopened without browsing to them again.

Each project contains an `OutMapper_InternalFiles` directory (which may contain `Datasets` and `Cohorts` directories) and an `OutMapper_ProjectOutput` directory (for generated files such as exported PDFs). A dataset or cohort is scoped to exactly one project and has no existence independent of a project.

Creating a dataset also creates a same-named folder inside `Datasets`, containing an `Imported raw data` subfolder. When the user selects a folder of raw data while creating the dataset, its `.csv` files are copied into `Imported raw data`.

A dataset's raw CSV files can then be parsed: the user configures how to read them (delimiter, decimal separator, time column, timestamp format) and triggers a parse, which validates and converts each file into a [Time series](glossary.md#time-series) — one file per patient. The outcome (which files succeeded, and why any failed) is shown per file and kept so it can be reviewed again later without re-parsing. See [`architecture.md`](architecture.md) for the validation rules (`DataStructures`/`Algorithms` projects) and the resulting file layout (Persistence and project layout).

Creating a [Cohort](glossary.md#cohort) works the same way, one level simpler: the user selects a single raw `.csv` file (instead of a folder), which is copied into a same-named folder inside `Cohorts`, and picks which existing dataset(s) in the project the cohort should be linked to. The cohort's CSV file can then be parsed: the user configures the delimiter and the patient-ID/outcome column headers, and triggers a parse, which validates and converts the file into a Cohort — one row per patient. The outcome (patient count, or the reason parsing failed) is kept so it can be reviewed again later without re-parsing.

Additional dataset and cohort formats, project metadata, and persistence rules are not yet fully documented.

Future project calculations may continue in the background when the user selects another project. The execution and state model for that behavior has not yet been designed or implemented.

## Architecture

The solution contains the `OutMapper`, `TaskManager`, `Messages`, `DataStructures`, and `Algorithms` projects, plus a test project per project with meaningful logic (see [`architecture.md`](architecture.md#test-projects)). Their current responsibilities and communication boundaries are documented in [`architecture.md`](architecture.md).
