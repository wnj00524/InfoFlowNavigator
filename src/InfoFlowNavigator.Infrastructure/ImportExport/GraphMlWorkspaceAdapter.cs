using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;
using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Domain.Claims;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Infrastructure.ImportExport;

public sealed class GraphMlWorkspaceAdapter : IWorkspaceImportService, IWorkspaceExportService
{
    private static readonly XNamespace GraphMlNamespace = "http://graphml.graphdrawing.org/xmlns";
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public Task<AnalysisWorkspace> ImportAsync(string path, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("GraphML import is a planned interchange capability and is not implemented in the bootstrap skeleton.");

    public Task ExportAsync(AnalysisWorkspace workspace, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var extension = Path.GetExtension(path).Trim().ToLowerInvariant();
        if (extension == ".json")
        {
            var jsonModel = BuildNetworkJsonModel(workspace);
            var json = JsonSerializer.Serialize(jsonModel, JsonSerializerOptions);
            File.WriteAllText(path, json);
            return Task.CompletedTask;
        }

        var document = BuildDocument(workspace);

        using var writer = XmlWriter.Create(
            path,
            new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  "
            });

        document.Save(writer);
        return Task.CompletedTask;
    }

    private static MedWNetworkJsonModel BuildNetworkJsonModel(AnalysisWorkspace workspace)
    {
        var nodes = new List<MedWNetworkNodeModel>();

        nodes.AddRange(workspace.Entities
            .OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .Select(entity => new MedWNetworkNodeModel
            {
                Id = $"entity:{entity.Id:N}",
                Name = entity.Name,
                Shape = InferEntityShape(entity.EntityType),
                X = null,
                Y = null,
                PlaceType = entity.EntityType,
                LoreDescription = entity.Notes,
                ControllingActor = entity.Metadata.TryGetValue("controllingActor", out var actor) ? actor : null,
                Tags = entity.Tags.Count == 0 ? null : entity.Tags.ToArray(),
                TrafficProfiles = []
            }));

        nodes.AddRange(workspace.Relationships
            .OrderBy(relationship => relationship.RelationshipType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(relationship => relationship.Id)
            .Select(relationship => new MedWNetworkNodeModel
            {
                Id = $"relationship-node:{relationship.Id:N}",
                Name = $"{ResolveEntityLabel(workspace, relationship.SourceEntityId)} -> {relationship.RelationshipType} -> {ResolveEntityLabel(workspace, relationship.TargetEntityId)}",
                Shape = "square",
                X = null,
                Y = null,
                PlaceType = "Relationship",
                LoreDescription = relationship.Notes,
                Tags = relationship.Tags.Count == 0 ? null : relationship.Tags.ToArray(),
                TrafficProfiles = []
            }));

        nodes.AddRange(workspace.Events
            .OrderBy(@event => @event.OccurredAtUtc ?? DateTimeOffset.MaxValue)
            .ThenBy(@event => @event.Title, StringComparer.OrdinalIgnoreCase)
            .Select(@event => new MedWNetworkNodeModel
            {
                Id = $"event:{@event.Id:N}",
                Name = @event.Title,
                Shape = "circle",
                X = null,
                Y = null,
                PlaceType = "Event",
                LoreDescription = BuildEventDescription(@event),
                Tags = @event.Tags.Count == 0 ? null : @event.Tags.ToArray(),
                TrafficProfiles = []
            }));

        nodes.AddRange(workspace.Hypotheses
            .OrderBy(hypothesis => hypothesis.Title, StringComparer.OrdinalIgnoreCase)
            .Select(hypothesis => new MedWNetworkNodeModel
            {
                Id = $"hypothesis:{hypothesis.Id:N}",
                Name = hypothesis.Title,
                Shape = "building",
                X = null,
                Y = null,
                PlaceType = "Hypothesis",
                LoreDescription = $"{hypothesis.Status}: {hypothesis.Statement}{AppendOptionalNotes(hypothesis.Notes)}",
                Tags = hypothesis.Tags.Count == 0 ? null : hypothesis.Tags.ToArray(),
                TrafficProfiles = []
            }));

        nodes.AddRange(workspace.Claims
            .OrderBy(claim => claim.Statement, StringComparer.OrdinalIgnoreCase)
            .Select(claim => new MedWNetworkNodeModel
            {
                Id = $"claim:{claim.Id:N}",
                Name = claim.Statement,
                Shape = "square",
                X = null,
                Y = null,
                PlaceType = "Claim",
                LoreDescription = $"{claim.ClaimType} / {claim.Status}{AppendOptionalNotes(claim.Notes)}",
                Tags = claim.Tags.Count == 0 ? null : claim.Tags.ToArray(),
                TrafficProfiles = []
            }));

        nodes.AddRange(workspace.Evidence
            .OrderBy(evidence => evidence.Title, StringComparer.OrdinalIgnoreCase)
            .Select(evidence => new MedWNetworkNodeModel
            {
                Id = $"evidence:{evidence.Id:N}",
                Name = evidence.Title,
                Shape = "building",
                X = null,
                Y = null,
                PlaceType = "Evidence",
                LoreDescription = BuildEvidenceDescription(evidence.Citation, evidence.Notes),
                Tags = evidence.Tags.Count == 0 ? null : evidence.Tags.ToArray(),
                TrafficProfiles = []
            }));

        ApplyDeterministicLayout(nodes);

        var edges = new List<MedWNetworkEdgeModel>();

        edges.AddRange(workspace.Relationships
            .OrderBy(relationship => relationship.RelationshipType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(relationship => relationship.Id)
            .Select(relationship => new MedWNetworkEdgeModel
            {
                Id = $"relationship:{relationship.Id:N}",
                FromNodeId = $"entity:{relationship.SourceEntityId:N}",
                ToNodeId = $"entity:{relationship.TargetEntityId:N}",
                Time = 1d,
                Cost = 1d,
                IsBidirectional = false,
                RouteType = relationship.RelationshipType,
                AccessNotes = relationship.Notes,
                SecurityNotes = BuildConfidenceNote(relationship.Confidence)
            }));

        edges.AddRange(workspace.EventParticipants
            .OrderBy(participant => participant.EventId)
            .ThenBy(participant => participant.EntityId)
            .ThenBy(participant => participant.Role, StringComparer.OrdinalIgnoreCase)
            .Select(participant => new MedWNetworkEdgeModel
            {
                Id = $"participant:{participant.Id:N}",
                FromNodeId = $"entity:{participant.EntityId:N}",
                ToNodeId = $"event:{participant.EventId:N}",
                Time = 1d,
                Cost = 1d,
                IsBidirectional = false,
                RouteType = "participation",
                AccessNotes = participant.Role,
                SecurityNotes = BuildNoteWithConfidence(participant.Notes, participant.Confidence)
            }));

        edges.AddRange(workspace.Claims
            .Where(claim => claim.TargetKind is not null && claim.TargetId is not null)
            .OrderBy(claim => claim.Statement, StringComparer.OrdinalIgnoreCase)
            .Select(claim => new MedWNetworkEdgeModel
            {
                Id = $"claim-target:{claim.Id:N}",
                FromNodeId = $"claim:{claim.Id:N}",
                ToNodeId = ToNodeId(claim.TargetKind!.Value, claim.TargetId!.Value),
                Time = 1d,
                Cost = 1d,
                IsBidirectional = false,
                RouteType = "claimTarget",
                AccessNotes = $"{claim.ClaimType} / {claim.Status}",
                SecurityNotes = BuildNoteWithConfidence(claim.Notes, claim.Confidence)
            }));

        edges.AddRange(workspace.Claims
            .Where(claim => claim.HypothesisId is not null)
            .OrderBy(claim => claim.Statement, StringComparer.OrdinalIgnoreCase)
            .Select(claim => new MedWNetworkEdgeModel
            {
                Id = $"claim-hypothesis:{claim.Id:N}:{claim.HypothesisId!.Value:N}",
                FromNodeId = $"claim:{claim.Id:N}",
                ToNodeId = $"hypothesis:{claim.HypothesisId!.Value:N}",
                Time = 1d,
                Cost = 1d,
                IsBidirectional = false,
                RouteType = "claimHypothesis",
                AccessNotes = "affects",
                SecurityNotes = BuildConfidenceNote(claim.Confidence)
            }));

        edges.AddRange(workspace.EvidenceLinks
            .OrderBy(link => link.TargetKind)
            .ThenBy(link => link.TargetId)
            .ThenBy(link => link.EvidenceId)
            .Select(link => new MedWNetworkEdgeModel
            {
                Id = $"evidence-link:{link.Id:N}",
                FromNodeId = $"evidence:{link.EvidenceId:N}",
                ToNodeId = ToNodeId(link.TargetKind, link.TargetId),
                Time = 1d,
                Cost = 1d,
                IsBidirectional = false,
                RouteType = "evidenceLink",
                AccessNotes = $"{link.RelationToTarget} / {link.Strength}",
                SecurityNotes = BuildNoteWithConfidence(link.Notes, link.Confidence)
            }));

        return new MedWNetworkJsonModel
        {
            Name = string.IsNullOrWhiteSpace(workspace.Name) ? "Untitled Network" : workspace.Name.Trim(),
            Description = workspace.Notes ?? "Exported from InfoFlowNavigator.",
            TrafficTypes = [],
            Nodes = nodes,
            Edges = edges
        };
    }

    private static XDocument BuildDocument(AnalysisWorkspace workspace)
    {
        var root = new XElement(
            GraphMlNamespace + "graphml",
            new XAttribute(XNamespace.Xmlns + "xsi", XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance")),
            new XAttribute(
                XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance") + "schemaLocation",
                "http://graphml.graphdrawing.org/xmlns http://graphml.graphdrawing.org/xmlns/1.0/graphml.xsd"));

        AddKey(root, "graph_name", "graph", "name", "string");
        AddKey(root, "graph_description", "graph", "description", "string");
        AddKey(root, "graph_workspace_id", "graph", "workspaceId", "string");
        AddKey(root, "graph_tags_json", "graph", "tagsJson", "string");

        AddKey(root, "node_name", "node", "name", "string");
        AddKey(root, "node_label", "node", "label", "string");
        AddKey(root, "node_kind", "node", "kind", "string");
        AddKey(root, "node_shape", "node", "shape", "string");
        AddKey(root, "node_confidence", "node", "confidence", "double");
        AddKey(root, "node_status", "node", "status", "string");
        AddKey(root, "node_type", "node", "type", "string");
        AddKey(root, "node_notes", "node", "notes", "string");
        AddKey(root, "node_tags_json", "node", "tagsJson", "string");
        AddKey(root, "node_metadata_json", "node", "metadataJson", "string");
        AddKey(root, "node_created_at_utc", "node", "createdAtUtc", "string");
        AddKey(root, "node_updated_at_utc", "node", "updatedAtUtc", "string");
        AddKey(root, "node_occurred_at_utc", "node", "occurredAtUtc", "string");
        AddKey(root, "node_statement", "node", "statement", "string");
        AddKey(root, "node_citation", "node", "citation", "string");

        AddKey(root, "edge_label", "edge", "label", "string");
        AddKey(root, "edge_kind", "edge", "kind", "string");
        AddKey(root, "edge_id", "edge", "id", "string");
        AddKey(root, "edge_time", "edge", "time", "double");
        AddKey(root, "edge_cost", "edge", "cost", "double");
        AddKey(root, "edge_is_bidirectional", "edge", "isBidirectional", "boolean");
        AddKey(root, "edge_confidence", "edge", "confidence", "double");
        AddKey(root, "edge_notes", "edge", "notes", "string");
        AddKey(root, "edge_role", "edge", "role", "string");
        AddKey(root, "edge_relation", "edge", "relationToTarget", "string");
        AddKey(root, "edge_strength", "edge", "strength", "string");
        AddKey(root, "edge_tags_json", "edge", "tagsJson", "string");

        var graph = new XElement(
            GraphMlNamespace + "graph",
            new XAttribute("id", $"workspace-{workspace.Id:N}"),
            new XAttribute("edgedefault", "directed"));

        AddData(graph, "graph_name", workspace.Name);
        AddData(graph, "graph_description", workspace.Notes ?? string.Empty);
        AddData(graph, "graph_workspace_id", workspace.Id.ToString());
        AddData(graph, "graph_tags_json", JsonSerializer.Serialize(workspace.Tags));

        foreach (var entity in workspace.Entities)
        {
            var node = CreateNode($"entity:{entity.Id:N}", entity.Name, "entity");
            AddData(node, "node_type", entity.EntityType);
            AddOptionalData(node, "node_confidence", entity.Confidence);
            AddOptionalData(node, "node_notes", entity.Notes);
            AddData(node, "node_tags_json", JsonSerializer.Serialize(entity.Tags));
            AddData(node, "node_metadata_json", JsonSerializer.Serialize(entity.Metadata));
            AddData(node, "node_created_at_utc", entity.CreatedAtUtc.ToString("O"));
            AddData(node, "node_updated_at_utc", entity.UpdatedAtUtc.ToString("O"));
            graph.Add(node);
        }

        foreach (var @event in workspace.Events)
        {
            var node = CreateNode($"event:{@event.Id:N}", @event.Title, "event");
            AddData(node, "node_shape", "Square");
            AddOptionalData(node, "node_confidence", @event.Confidence);
            AddOptionalData(node, "node_notes", @event.Notes);
            AddOptionalData(node, "node_occurred_at_utc", @event.OccurredAtUtc?.ToString("O"));
            AddData(node, "node_tags_json", JsonSerializer.Serialize(@event.Tags));
            AddData(node, "node_metadata_json", JsonSerializer.Serialize(@event.Metadata));
            AddData(node, "node_created_at_utc", @event.CreatedAtUtc.ToString("O"));
            AddData(node, "node_updated_at_utc", @event.UpdatedAtUtc.ToString("O"));
            graph.Add(node);
        }

        foreach (var hypothesis in workspace.Hypotheses)
        {
            var node = CreateNode($"hypothesis:{hypothesis.Id:N}", hypothesis.Title, "hypothesis");
            AddData(node, "node_type", hypothesis.Status.ToString());
            AddData(node, "node_status", hypothesis.Status.ToString());
            AddData(node, "node_statement", hypothesis.Statement);
            AddOptionalData(node, "node_confidence", hypothesis.Confidence);
            AddOptionalData(node, "node_notes", hypothesis.Notes);
            AddData(node, "node_tags_json", JsonSerializer.Serialize(hypothesis.Tags));
            AddData(node, "node_metadata_json", JsonSerializer.Serialize(hypothesis.Metadata));
            AddData(node, "node_created_at_utc", hypothesis.CreatedAtUtc.ToString("O"));
            AddData(node, "node_updated_at_utc", hypothesis.UpdatedAtUtc.ToString("O"));
            graph.Add(node);
        }

        foreach (var relationship in workspace.Relationships)
        {
            var label = $"{ResolveEntityLabel(workspace, relationship.SourceEntityId)} -> {relationship.RelationshipType} -> {ResolveEntityLabel(workspace, relationship.TargetEntityId)}";
            var node = CreateNode($"relationship-node:{relationship.Id:N}", label, "relationship");
            AddData(node, "node_type", relationship.RelationshipType);
            AddOptionalData(node, "node_confidence", relationship.Confidence);
            AddOptionalData(node, "node_notes", relationship.Notes);
            AddData(node, "node_tags_json", JsonSerializer.Serialize(relationship.Tags));
            AddData(node, "node_metadata_json", JsonSerializer.Serialize(relationship.Metadata));
            AddData(node, "node_created_at_utc", relationship.CreatedAtUtc.ToString("O"));
            AddData(node, "node_updated_at_utc", relationship.UpdatedAtUtc.ToString("O"));
            graph.Add(node);
        }

        foreach (var claim in workspace.Claims)
        {
            var node = CreateNode($"claim:{claim.Id:N}", claim.Statement, "claim");
            AddData(node, "node_type", claim.ClaimType.ToString());
            AddData(node, "node_status", claim.Status.ToString());
            AddData(node, "node_statement", claim.Statement);
            AddOptionalData(node, "node_confidence", claim.Confidence);
            AddOptionalData(node, "node_notes", claim.Notes);
            AddData(node, "node_tags_json", JsonSerializer.Serialize(claim.Tags));
            AddData(node, "node_metadata_json", JsonSerializer.Serialize(claim.Metadata));
            AddData(node, "node_created_at_utc", claim.CreatedAtUtc.ToString("O"));
            AddData(node, "node_updated_at_utc", claim.UpdatedAtUtc.ToString("O"));
            graph.Add(node);
        }

        foreach (var evidence in workspace.Evidence)
        {
            var node = CreateNode($"evidence:{evidence.Id:N}", evidence.Title, "evidence");
            AddOptionalData(node, "node_confidence", evidence.Confidence);
            AddOptionalData(node, "node_notes", evidence.Notes);
            AddOptionalData(node, "node_citation", evidence.Citation);
            AddData(node, "node_tags_json", JsonSerializer.Serialize(evidence.Tags));
            AddData(node, "node_metadata_json", JsonSerializer.Serialize(evidence.Metadata));
            AddData(node, "node_created_at_utc", evidence.CreatedAtUtc.ToString("O"));
            AddData(node, "node_updated_at_utc", evidence.UpdatedAtUtc.ToString("O"));
            graph.Add(node);
        }

        foreach (var relationship in workspace.Relationships)
        {
            graph.Add(CreateEdge(
                $"relationship:{relationship.Id:N}",
                $"entity:{relationship.SourceEntityId:N}",
                $"entity:{relationship.TargetEntityId:N}",
                relationship.RelationshipType,
                "relationship",
                relationship.Confidence,
                relationship.Notes));
        }

        foreach (var participant in workspace.EventParticipants)
        {
            var edge = CreateEdge(
                $"participant:{participant.Id:N}",
                $"entity:{participant.EntityId:N}",
                $"event:{participant.EventId:N}",
                participant.Role,
                "participation",
                participant.Confidence,
                participant.Notes);
            AddData(edge, "edge_role", participant.Role);
            graph.Add(edge);
        }

        foreach (var claim in workspace.Claims)
        {
            if (claim.TargetKind is not null && claim.TargetId is not null)
            {
                graph.Add(CreateEdge(
                    $"claim-target:{claim.Id:N}",
                    $"claim:{claim.Id:N}",
                    ToNodeId(claim.TargetKind.Value, claim.TargetId.Value),
                    "targets",
                    "claim-target",
                    claim.Confidence,
                    claim.Notes));
            }

            if (claim.HypothesisId is not null)
            {
                graph.Add(CreateEdge(
                    $"claim-hypothesis:{claim.Id:N}:{claim.HypothesisId.Value:N}",
                    $"claim:{claim.Id:N}",
                    $"hypothesis:{claim.HypothesisId.Value:N}",
                    "affects",
                    "claim-hypothesis",
                    claim.Confidence,
                    claim.Notes));
            }
        }

        foreach (var link in workspace.EvidenceLinks)
        {
            var edge = CreateEdge(
                $"evidence-link:{link.Id:N}",
                $"evidence:{link.EvidenceId:N}",
                ToNodeId(link.TargetKind, link.TargetId),
                link.RelationToTarget.ToString(),
                "evidence-link",
                link.Confidence,
                link.Notes);
            AddData(edge, "edge_relation", link.RelationToTarget.ToString());
            AddData(edge, "edge_strength", link.Strength.ToString());
            graph.Add(edge);
        }

        root.Add(graph);
        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
    }

    private static XElement CreateNode(string id, string label, string kind)
    {
        var node = new XElement(
            GraphMlNamespace + "node",
            new XAttribute("id", id));

        AddData(node, "node_name", label);
        AddData(node, "node_label", label);
        AddData(node, "node_kind", kind);
        AddData(node, "node_shape", "Square");
        return node;
    }

    private static XElement CreateEdge(string id, string source, string target, string label, string kind, double? confidence, string? notes)
    {
        var edge = new XElement(
            GraphMlNamespace + "edge",
            new XAttribute("id", id),
            new XAttribute("source", source),
            new XAttribute("target", target));

        AddData(edge, "edge_id", id);
        AddData(edge, "edge_label", label);
        AddData(edge, "edge_kind", kind);
        AddData(edge, "edge_time", "1");
        AddData(edge, "edge_cost", "1");
        AddData(edge, "edge_is_bidirectional", "false");
        AddOptionalData(edge, "edge_confidence", confidence);
        AddOptionalData(edge, "edge_notes", notes);
        return edge;
    }

    private static string ToNodeId(EvidenceLinkTargetKind targetKind, Guid targetId) =>
        targetKind switch
        {
            EvidenceLinkTargetKind.Entity => $"entity:{targetId:N}",
            EvidenceLinkTargetKind.Relationship => $"relationship-node:{targetId:N}",
            EvidenceLinkTargetKind.Event => $"event:{targetId:N}",
            EvidenceLinkTargetKind.Hypothesis => $"hypothesis:{targetId:N}",
            EvidenceLinkTargetKind.Claim => $"claim:{targetId:N}",
            _ => $"target:{targetId:N}"
        };

    private static string ToNodeId(ClaimTargetKind targetKind, Guid targetId) =>
        targetKind switch
        {
            ClaimTargetKind.Entity => $"entity:{targetId:N}",
            ClaimTargetKind.Relationship => $"relationship-node:{targetId:N}",
            ClaimTargetKind.Event => $"event:{targetId:N}",
            ClaimTargetKind.Hypothesis => $"hypothesis:{targetId:N}",
            _ => $"target:{targetId:N}"
        };

    private static string ResolveEntityLabel(AnalysisWorkspace workspace, Guid entityId) =>
        workspace.Entities.FirstOrDefault(entity => entity.Id == entityId)?.Name ?? entityId.ToString();

    private static string InferEntityShape(string entityType)
    {
        var normalized = entityType.Trim().ToLowerInvariant();
        if (normalized.Contains("person") || normalized.Contains("individual"))
        {
            return "person";
        }

        if (normalized.Contains("vehicle") || normalized.Contains("car"))
        {
            return "car";
        }

        if (normalized.Contains("location") || normalized.Contains("site"))
        {
            return "circle";
        }

        if (normalized.Contains("organization") || normalized.Contains("company") || normalized.Contains("facility"))
        {
            return "building";
        }

        return "square";
    }

    private static void ApplyDeterministicLayout(IReadOnlyList<MedWNetworkNodeModel> nodes)
    {
        var orderByKind = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Entity"] = 0,
            ["Relationship"] = 1,
            ["Event"] = 2,
            ["Claim"] = 3,
            ["Hypothesis"] = 4,
            ["Evidence"] = 5
        };

        const double leftMargin = 140d;
        const double topMargin = 140d;
        const double columnSpacing = 260d;
        const double rowSpacing = 160d;

        var grouped = nodes
            .GroupBy(node => node.PlaceType ?? "Other", StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => orderByKind.TryGetValue(group.Key, out var index) ? index : int.MaxValue)
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        for (var column = 0; column < grouped.Length; column++)
        {
            var orderedNodes = grouped[column]
                .OrderBy(node => node.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(node => node.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            for (var row = 0; row < orderedNodes.Length; row++)
            {
                orderedNodes[row].X = leftMargin + (column * columnSpacing);
                orderedNodes[row].Y = topMargin + (row * rowSpacing);
            }
        }
    }

    private static string BuildEventDescription(Domain.Events.Event @event)
    {
        var parts = new List<string>();
        if (@event.OccurredAtUtc is not null)
        {
            parts.Add($"Occurred: {@event.OccurredAtUtc:O}");
        }

        if (!string.IsNullOrWhiteSpace(@event.Notes))
        {
            parts.Add(@event.Notes!);
        }

        return string.Join(" | ", parts);
    }

    private static string? BuildEvidenceDescription(string? citation, string? notes)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(citation))
        {
            parts.Add(citation.Trim());
        }

        if (!string.IsNullOrWhiteSpace(notes))
        {
            parts.Add(notes.Trim());
        }

        return parts.Count == 0 ? null : string.Join(" | ", parts);
    }

    private static string AppendOptionalNotes(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? string.Empty : $" | {notes.Trim()}";

    private static string? BuildConfidenceNote(double? confidence) =>
        confidence is null ? null : $"confidence={confidence.Value.ToString("0.###", CultureInfo.InvariantCulture)}";

    private static string? BuildNoteWithConfidence(string? notes, double? confidence)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(notes))
        {
            parts.Add(notes.Trim());
        }

        var confidenceNote = BuildConfidenceNote(confidence);
        if (!string.IsNullOrWhiteSpace(confidenceNote))
        {
            parts.Add(confidenceNote);
        }

        return parts.Count == 0 ? null : string.Join(" | ", parts);
    }

    private static void AddKey(XElement root, string id, string target, string name, string type)
    {
        root.Add(
            new XElement(
                GraphMlNamespace + "key",
                new XAttribute("id", id),
                new XAttribute("for", target),
                new XAttribute("attr.name", name),
                new XAttribute("attr.type", type)));
    }

    private static void AddData(XElement element, string key, string value)
    {
        element.Add(
            new XElement(
                GraphMlNamespace + "data",
                new XAttribute("key", key),
                value));
    }

    private static void AddOptionalData(XElement element, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            AddData(element, key, value);
        }
    }

    private static void AddOptionalData(XElement element, string key, double? value)
    {
        if (value is not null)
        {
            AddData(element, key, value.Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    private sealed class MedWNetworkJsonModel
    {
        public string Name { get; set; } = "Untitled Network";

        public string Description { get; set; } = string.Empty;

        public IReadOnlyList<object> TrafficTypes { get; set; } = [];

        public IReadOnlyList<MedWNetworkNodeModel> Nodes { get; set; } = [];

        public IReadOnlyList<MedWNetworkEdgeModel> Edges { get; set; } = [];
    }

    private sealed class MedWNetworkNodeModel
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Shape { get; set; } = "square";

        public double? X { get; set; }

        public double? Y { get; set; }

        public string? PlaceType { get; set; }

        public string? LoreDescription { get; set; }

        public string? ControllingActor { get; set; }

        public IReadOnlyList<string>? Tags { get; set; }

        public IReadOnlyList<object> TrafficProfiles { get; set; } = [];
    }

    private sealed class MedWNetworkEdgeModel
    {
        public string Id { get; set; } = string.Empty;

        public string FromNodeId { get; set; } = string.Empty;

        public string ToNodeId { get; set; } = string.Empty;

        public double Time { get; set; }

        public double Cost { get; set; }

        public bool IsBidirectional { get; set; }

        public string? RouteType { get; set; }

        public string? AccessNotes { get; set; }

        public string? SecurityNotes { get; set; }
    }
}
