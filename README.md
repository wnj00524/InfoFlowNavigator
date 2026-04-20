# Info Flow Navigator

Info Flow Navigator is an offline-first Windows desktop intelligence analysis application focused on analyst-friendly workflows, explainable outputs, and local-first data handling.

This repository currently contains the initial solution skeleton only. The goal of this bootstrap is to create a clean path for the first future vertical slice:

- create or open a workspace
- define entities and relationships
- save the workspace to JSON
- display the workspace in a simple shell view
- export a minimal report artifact

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
- The shell is intentionally minimal so the next slice can add workspace creation, save or load flows, and basic report export without refactoring the project layout.

## Intentionally Left Out

- Real analysis logic
- Link, timeline, or geospatial visualization
- Database storage
- Broad plugin or extension mechanisms
- Full report composition workflows
