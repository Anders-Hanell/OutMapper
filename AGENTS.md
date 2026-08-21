# OutMapper Agent Instructions

## Build verification

- Run solution builds from the repository root.
- Do not start overlapping builds. If a build produces no output for an unusually long time, inspect for an existing OutMapper `dotnet build`, MSBuild node, or compiler process before retrying.
- Use the normal solution build first:

  ```sh
  dotnet build OutMapper.sln --no-restore --verbosity:minimal
  ```

- If reusable or parallel MSBuild processes cause the build to stall, use this verified fallback:

  ```sh
  dotnet build OutMapper.sln --no-restore --verbosity:minimal --maxcpucount:1 --nodeReuse:false -p:UseSharedCompilation=false
  ```

- Report the final warning count, error count, and exit status. Do not describe a build as successful unless the command completes with exit code 0.

## Testing

- Run `dotnet test OutMapper.sln --no-restore --verbosity:minimal` from the repository root; report pass/fail/skip counts per project, and do not describe tests as passing unless the command exits 0.
- Run `dotnet test` as a standalone command, not chained or piped with other commands (e.g. `rm -rf ...; dotnet test ... | tail`) — the `.claude/settings.json` allow rule for it is a prefix match against the whole command string, so combining it with anything else causes an avoidable permission prompt. Do any cleanup as a separate prior command.
- PDF-generating tests that exist for visual inspection (e.g. `Tests/OutMapper.Tests/SampleFigurePdfGenerator.cs`, `SamplePdfGenerator.cs`) write into their own subfolder under `OutMapperAutomatedTestsOutput/` at the solution root, via `TestSupport.SampleOutputDirectory.For(nameof(YourTestClass))` — this keeps concurrent generators from writing to the same file and makes the output directly readable (e.g. by Claude's Read tool) without a permission prompt, since that folder is gitignored and allow-listed for reads. They run as regular `[Fact]`s (not skipped) since they're fast; follow the same pattern for any new sample-output generator.
- New code that touches disk, `ApplicationData` settings, or a file/folder picker should take `TaskManager.IFileSystem` / `OutMapper.ISettingsStore` / `OutMapper.IFolderPicker`/`IFilePicker` as an explicit parameter instead of calling `System.IO`, `ApplicationData`, or `Windows.Storage.Pickers` directly — see `docs/architecture.md#testing-and-validation` for the pattern and existing examples (`ProjectFolderService`, `TaskManagerService`). Test it against the fakes in `Tests/TestSupport` (`InMemoryFileSystem`, `InMemorySettingsStore`, `FakeFolderPicker`, `FakeFilePicker`), not real disk — each fake is independent, so this stays safe under parallel test runs.
- UI navigation/workflow logic is only unit-testable once pulled out of a live Uno control into a plain class depending on small seam interfaces, the way `NavigationManager` depends on `IContentHost`/`IRefreshable` instead of `ContentControl`/`ProjectsPanel`. Apply the same extraction to a panel before trying to unit test its logic, rather than driving the real UI in a test.
- Don't drive the live app (Uno App MCP or otherwise) as a substitute for an automated test that could instead run in `dotnet test` — reserve that for what automated tests genuinely can't cover.

## Uno Platform MCPs

- Uno MCP usage is metered and was approaching its limit as of 2026-08-10. Prefer solving problems without the Uno MCPs first (read code, use existing knowledge, build/test locally); fall back to the MCPs only when a task genuinely requires them (e.g. actually needing to inspect or drive the live running app, or needing docs content not otherwise available).
- Consult the Uno Platform documentation MCP whenever a task involves Uno APIs, controls, layouts, navigation, themes, platform behavior, Hot Reload, or recommended practices — but only after non-MCP approaches are insufficient.
- Use Uno App MCP to inspect and validate the running application when its runtime tools are connected.
- If either MCP is unavailable, report that clearly and continue only when the task can be completed safely without it.
- Do not claim runtime validation when Uno App MCP was unavailable or not connected to the application.

### Getting the Uno App MCP connected

- `uno_health` reporting `connectionState: Connected` only means the DevServer bridge itself is up — it does NOT mean an app is attached. Check `hostProcessId` / `hostEndpoint`: both are `null` until an app instance actually connects.
- The DevServer connection to a running app requires being **signed in to an Uno Platform account** in VS Code. If `hostProcessId` stays `null` even after restarting the app, sign-in is the first thing to check:
  1. In VS Code's bottom status bar, switch the selected item from the `.sln` to the app's `.csproj`.
  2. Click the "Sign in / Register" notification that appears (or run "Uno Platform: Open Studio" from the Command Palette if no notification shows).
  3. Complete sign-in in the browser, then relaunch the app.
- Launching the app via a plain VS Code `F5`/debug session does not reliably attach it to the MCP-managed DevServer. Prefer starting it with the `uno_app_start` MCP tool instead (pass the `.csproj` path and target framework, e.g. `net10.0-desktop`) — this launches the app under the DevServer directly so runtime tools (screenshot, click, visual tree, etc.) work immediately.
- `uno_app_start` kills any already-running app instance before launching a new one.
- If running the app produces an Uno Platform error about a missing SDK, try running "Uno Platform: Select Active Project" from the VS Code Command Palette to reselect the active project — this has resolved the issue before (e.g. after moving projects into the `Source`/`Tests` folders).

## Product context

- Read `docs/glossary.md` before using or introducing domain-specific terminology, to stay consistent with established definitions.
- Read `docs/app_overview.md` before planning or implementing changes that affect application workflows, domain concepts, persistence, navigation, or architecture.
- Read `docs/architecture.md` before changing project boundaries, messaging, state ownership, persistence responsibilities, UI structure, or target platforms.
- Keep the overview synchronized when an approved change alters the behavior described there.
- Keep the architecture document synchronized when an approved change alters the structure or responsibilities described there.
- When a term that looks domain-specific comes up and isn't in `docs/glossary.md`, add it there (or ask for its definition first if it's not already clear from context).
- Ask for clarification when requested behavior conflicts with, or is not covered by, the documented product model, or when new information contradicts an existing glossary definition.

## Existing R implementation

- OutMapper reimplements a working R prototype (heatmap generation logic only, no GUI), located at `R_code/2026-06-24 - Outcome Heatmaps - Version 2.0/`.
- Read `docs/r_code_reference.md` before implementing any heatmap-generation logic (data import, gap imputation, grid/binning, observation counting, %VMT, association statistics, detrimental zone, density, smoothing, color scales/graph generation) — it maps which R file covers which piece of logic and documents the key data structures, so the R source doesn't need to be re-read from scratch each time.
- The reference doc also flags a handful of known bugs/inconsistencies in the R snapshot (see its final section) — do not silently replicate those when porting.
