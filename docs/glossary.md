# OutMapper Glossary

## Purpose

This document defines the domain-specific terminology used throughout OutMapper — by users, in the codebase, and in other docs. It exists so terms are used consistently and so their precise meaning doesn't have to be re-derived from scratch in every conversation.

For broader product context, see [`app_overview.md`](app_overview.md). For UI language guidance, see [`user_profile.md`](user_profile.md).

## Terms

<!-- Add terms alphabetically or grouped by topic as they're defined. -->

### Visualization

#### Graph

A visualization in the statistical sense (i.e. a chart, not a graph-theory node/edge structure). OutMapper has two main graph types: [Outcome heatmap](#outcome-heatmap) and [Density heatmap](#density-heatmap).

#### Outcome heatmap

A graph visualizing the association between a range of values (in physiological time-series data) and patient outcome.

#### Density heatmap

A graph visualizing how common a range of values is.

#### Figure

A combination of multiple graphs, arranged in a fixed grid of rows and columns. Each cell may hold an existing [Analysis](#analysis)'s graph, or be left empty. The previous R implementation could not create figures; OutMapper supports creating them.

### Organization

#### Project

A user-created, named container that owns [Dataset](#dataset)s. A project lives at an arbitrary folder the user creates or opens on disk — there is no shared container folder for multiple projects. Exactly one project is open as the current project at a time; OutMapper keeps a bounded, most-recently-used list of project folders so the user can switch back to a previously opened project without browsing to it again.

### Data

#### Time series

The complete physiological record for one patient. There is exactly one time series per patient.

Represented as a table with one timestamp column and one or more [Channel](#channel) columns. All channels in a time series share the same aligned time axis (i.e. one common set of timestamps, not independently-timed per channel) — typically one value per minute.

Produced by parsing one raw CSV file per patient (see [Dataset](#dataset)). A `TimeSeries` object is guaranteed valid once it exists: timestamps are strictly increasing and unique, and there is at least one channel. A channel value may be missing; a timestamp may not.

#### Channel

A physiological variable that is measured, for example intracranial pressure or blood pressure. Within a [Time series](#time-series) table, each channel is one column of measured values, aligned to the shared timestamp column.

Also referred to elsewhere as "physiological variable" — `Channel` is the preferred term.

#### Dataset

A collection of [Time series](#time-series) (i.e. covers multiple patients). A Dataset belongs to exactly one [Project](#project).

A Dataset's raw data is a folder of CSV files, one per patient; parsing the Dataset converts each file into a Time series, reporting success or failure per file.

#### Cohort

A collection of patients with associated data. Represented as a table with one patient per row, containing:

- Exactly one patient ID column.
- At least one outcome data column.
- Possibly additional columns, depending on the analysis.

When creating a Cohort, the user selects which Dataset(s) to link it to. A Cohort is typically linked to one Dataset, but may be linked to several. The linkage that matters for analysis is at the patient level: one patient in the Cohort is linked to one patient in a Dataset (via patient ID).

At link time, this patient-level linkage must be unambiguous: a patient in the Cohort must match exactly one Time series across the linked Dataset(s).

### Analysis

#### Analysis *(tentative name)*

Created by selecting a [Cohort](#cohort) and one of the graph-creation methods (for example "One-variable" or "Two-variable"). Each method can produce both an [Outcome heatmap](#outcome-heatmap) and a [Density heatmap](#density-heatmap), and usually other graph types as well — the pairing is not fixed per Analysis.

At creation, the user only names the Analysis; the method, Cohort, and other settings are chosen afterward, in the Analysis's own settings panel. Currently only the [Two-variable](#two-variable) method is implemented, and it only produces an Outcome heatmap.

The methods themselves ("One-variable", "Two-variable", etc.) and the full set of graph types each can produce are not yet documented in detail.

The creation of a [Graph](#graph) is based on one Cohort.

An Analysis's most recently *successfully* generated graph is what can be assigned into a [Figure](#figure)'s cells — an Analysis that has never generated successfully has no graph available to select.

#### Two-variable

The only [Analysis](#analysis) method implemented so far. The user picks a Cohort and two [Channel](#channel)s (by name) plus a [Bin](#bin) size for each. Each channel's bin edges span its observed value range (across the Cohort's matched patients) at that bin size, forming a 2D grid — one axis per channel.

For each grid cell, the association value is the Spearman correlation, across the Cohort's patients, between "percent of that patient's valid monitoring time spent in this cell" and that patient's outcome. Values are mapped to color for the Outcome heatmap using the Jet color scale.

This first implementation assumes all available data is included (no exclusion thresholds beyond missing values or unmatched/ambiguous patients) and applies no smoothing.

#### Bin

One interval of a [Channel](#channel)'s value range, used as an axis unit in an [Analysis](#analysis) grid. A channel's bins all share the same size (configured by the user); their edges span the channel's observed value range.
