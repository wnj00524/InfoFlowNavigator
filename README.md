# Info Flow Navigator

Info Flow Navigator is an offline-first Windows desktop intelligence analysis application focused on analyst-friendly workflows, explainable outputs, and local-first data handling.

This repository now contains the first end-to-end workspace core slice:

- create or open a workspace
- define entities and relationships
- save the workspace to JSON
- reload the workspace from JSON
- display the workspace in a simple shell view

## Solution Structure

`src/InfoFlowNavigator.Domain`

- Core domain concepts and naming for analysis workspaces, entities, relationships, events, and source references.
- Intentionally lightweight and free of UI or infrastructure concerns.

`src/InfoFlowNavigator.Application`

- Application-facing abstractions and orchestration for workspace lifecycle, import or export, reporting, and analysis services.
- Holds use-case friendly contracts without committing to specific storage or desktop technology.

`src/InfoFlowNavigator.Infrastructure`

- Local file and interchange adapters.
- Starts with JSON workspace persistence, a plain text report generator, and a GraphML placeholder to support future interchange work.

`src/InfoFlowNavigator.UI`

- Avalonia views and view models for the desktop experience.
- Currently contains a minimal workspace-centric shell window only.

`src/InfoFlowNavigator.App`

- Avalonia desktop entry point and composition root.
- Wires together the UI and infrastructure pieces for local execution.

`tests/InfoFlowNavigator.Domain.Tests`

- Domain-focused unit tests.

`tests/InfoFlowNavigator.Application.Tests`

- Application service and orchestration tests.

## Architectural Notes

- The solution uses a layered structure to keep domain modeling, orchestration, file adapters, and desktop UI separate.
- JSON is the first-class working format for local workspaces.
- GraphML is treated as a future interchange concern, not a current dependency.
- The shell is intentionally minimal and workspace-centric.

## Workspace Schema v1

The persisted workspace format is a human-readable JSON document with a stable root contract:

```json
{
  "schemaVersion": 1,
  "id": "9c9b3a8c-89df-4f2d-8b32-63a965c40b35",
  "name": "Case Alpha",
  "notes": "Optional analyst notes",
  "tags": ["priority"],
  "createdAtUtc": "2026-04-20T14:00:00+00:00",
  "updatedAtUtc": "2026-04-20T14:10:00+00:00",
  "entities": [],
  "relationships": [],
  "events": [],
  "evidence": []
}
```

`Entity`, `Relationship`, `Event`, and `Evidence` each carry:

- `id`
- a small type-specific identity field such as `name`, `relationshipType`, or `title`
- optional `notes`
- optional `confidence` between `0.0` and `1.0`
- `tags`
- `metadata`
- `createdAtUtc`
- `updatedAtUtc`

Relationship documents also persist `sourceEntityId` and `targetEntityId`, and the workspace validates that both referenced entities exist.

## Current Slice

Implemented in this slice:

- create a new workspace in the desktop shell
- open an existing workspace from a JSON path
- save the current workspace to JSON
- add entities
- add relationships between existing entities
- inspect entities and relationships in simple lists
- round-trip workspace files through tests

Intentionally left out of this slice:

- graph visualization
- geospatial features
- timeline visualization
- advanced importers
- reporting workflows beyond the existing placeholder infrastructure
- hypothesis logic
- complex validation frameworks

## Intentionally Left Out

- Real analysis logic
- Link, timeline, or geospatial visualization
- Database storage
- Broad plugin or extension mechanisms
- Full report composition workflows
