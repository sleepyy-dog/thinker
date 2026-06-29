# Windows Green Release Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish a Windows x64 self-contained single-file `Thinker-win-x64.exe` on GitHub Releases.

**Architecture:** Keep the application unchanged. Add a tag-triggered GitHub Actions workflow that restores, tests, publishes the WinForms app as a self-contained single-file executable, renames the output, and creates or updates the matching GitHub Release asset.

**Tech Stack:** .NET 8 SDK, WinForms, GitHub Actions `windows-latest`, GitHub CLI.

---

### Task 1: Release Workflow

**Files:**
- Create: `.github/workflows/release.yml`

- [ ] **Step 1: Add tag-triggered workflow**

Create a workflow that runs on `v*` tags and grants `contents: write` so `gh release` can create the release.

- [ ] **Step 2: Build and test**

Run `dotnet restore Thinker.sln` and `dotnet test Thinker.sln --configuration Release --no-restore --filter "Category!=Hardware"`. The real `powercfg` integration test is marked `Category=Hardware` because GitHub-hosted runners do not expose laptop lid-action settings.

- [ ] **Step 3: Publish single-file exe**

Run `dotnet publish src/Thinker.App/Thinker.App.csproj --configuration Release --runtime win-x64 --self-contained true` with `PublishSingleFile=true`, `IncludeNativeLibrariesForSelfExtract=true`, `EnableCompressionInSingleFile=true`, `DebugType=none`, and `DebugSymbols=false`.

- [ ] **Step 4: Upload release asset**

Rename `Thinker.exe` to `Thinker-win-x64.exe`, start the renamed executable as a smoke test, then create the GitHub Release for the tag or upload the asset with `--clobber` if the release already exists.

### Task 2: Documentation

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Document user download path**

Tell users to download `Thinker-win-x64.exe` from GitHub Releases and run it directly.

- [ ] **Step 2: Document green-app startup caveat**

Tell users to place the exe in a stable folder before enabling startup because the startup entry records the current executable path.

- [ ] **Step 3: Document source publish command**

Replace the framework-dependent publish command with the self-contained single-file command used by the release workflow.

### Task 3: Verification and Release

**Files:**
- Verify only

- [ ] **Step 1: Local verification**

Run `dotnet restore Thinker.sln`, `dotnet test Thinker.sln --configuration Release --no-restore`, and local `dotnet publish` with the workflow flags.

- [ ] **Step 2: Commit and push**

Commit the workflow and documentation, then push `main`.

- [ ] **Step 3: Create release tag**

Create and push `v0.1.0` unless a newer tag already exists.

- [ ] **Step 4: Verify GitHub Release**

Wait for the GitHub Actions run to pass, then verify the release contains `Thinker-win-x64.exe`.
