# OutMapper User Profile

## Purpose

This document describes what OutMapper can assume about its users, to guide UI language, terminology choices, and feature framing. For product purpose and domain context, see [`app_overview.md`](app_overview.md).

## Who the users are

The expected users are medical researchers working with physiological time-series data and patient outcomes, as described in [`app_overview.md`](app_overview.md#intended-users).

## Learning ability

Users are expected to be fast learners within their own domain. They are accustomed to complex clinical and statistical methodology and can be expected to pick up new domain-specific concepts (for example, an outcome heatmap) quickly when given a clear explanation.

This should not be read as general technical aptitude; it is specifically domain fluency in medical research.

## Technical literacy

Users should not be assumed to have a software or computing background. Many will have heard common computing terms — such as "core," "CPU," or "cloud" — without knowing precisely what they mean or how they affect the application's behavior.

## Implications for UI and language

- Prefer outcome-oriented language over mechanism-oriented language. For example, describe what a setting does for the user ("keep other apps responsive") rather than the underlying mechanism ("adjust thread priority").
- When a general computing term is unavoidable (for example, "processor core"), include a brief, plain-language explanation the first time it appears in a given panel. Do not assume the term is already understood.
- Domain-specific medical or statistical terminology can be used more freely, since users are expected to be fluent there.

## Open questions

- Specific institutional or regulatory constraints affecting users (for example, IRB or data-handling policies) are not yet documented.
- Whether users typically work individually or in teams sharing a workspace has not yet been determined.
