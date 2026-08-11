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

A combination of multiple graphs. The previous R implementation could not create figures; OutMapper is planned to support creating them.

### Data

#### Time series

The complete physiological record for one patient. There is exactly one time series per patient.

Represented as a table with one timestamp column and one or more [Channel](#channel) columns. All channels in a time series share the same aligned time axis (i.e. one common set of timestamps, not independently-timed per channel) — typically one value per minute.

#### Channel

A physiological variable that is measured, for example intracranial pressure or blood pressure. Within a [Time series](#time-series) table, each channel is one column of measured values, aligned to the shared timestamp column.

Also referred to elsewhere as "physiological variable" — `Channel` is the preferred term.

#### Dataset

A collection of [Time series](#time-series) (i.e. covers multiple patients).

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

The methods themselves ("One-variable", "Two-variable", etc.) and the full set of graph types each can produce are not yet documented in detail.

The creation of a [Graph](#graph) is based on one Cohort.
