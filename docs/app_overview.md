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

Measurements of physiological variables collected across time. The supported variables, sampling structure, and source formats are not yet documented.

### Patient outcome

The clinical or research outcome against which physiological values are analyzed. The supported outcome types and representations are not yet documented.

## Application structure

OutMapper currently includes concepts for workspaces, projects, and datasets. A workspace may contain multiple projects, and each dataset belongs to exactly one project. Other domain relationships and responsibilities will be documented as the product model is clarified.

## Research workflow

The complete workflow for importing data, configuring an analysis, generating an outcome heatmap, and exporting results is not yet documented.

## Data and folder structure

The application uses a selected workspace folder. A workspace may contain a `Projects` directory, where each immediate subdirectory represents one project. Users may select one project at a time as the current project.

Each project contains an `OutMapper_InternalFiles` directory (which may contain a `Datasets` directory) and an `OutMapper_ProjectOutput` directory (for generated files such as exported PDFs). A dataset is scoped to exactly one project and has no existence independent of a project.

Additional workspace contents, dataset formats, project metadata, and persistence rules are not yet fully documented.

Future project calculations may continue in the background when the user selects another project. The execution and state model for that behavior has not yet been designed or implemented.

## Architecture

The solution contains the `OutMapper`, `TaskManager`, `Messages`, and `OutMapper.Tests` projects. Their current responsibilities and communication boundaries are documented in [`architecture.md`](architecture.md).
