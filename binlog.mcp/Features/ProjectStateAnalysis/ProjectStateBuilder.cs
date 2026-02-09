using Microsoft.Build.Logging.StructuredLogger;

namespace Binlog.MCP.Features.ProjectStateAnalysis;

/// <summary>
/// Builds a snapshot of project state from evaluation.
/// </summary>
internal class ProjectStateBuilder
{
    private readonly Project _project;
    private readonly Dictionary<string, string> _properties;
    private readonly Dictionary<string, List<ProjectStateItem>> _items;

    public ProjectStateBuilder(Project project)
    {
        _project = project;
        _properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _items = new Dictionary<string, List<ProjectStateItem>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Build state from evaluation only (initial state before any targets run).
    /// </summary>
    public void BuildEvaluationState()
    {
        var build = FindBuild(_project);
        if (build == null) return;

        var evaluation = build.FindEvaluation(_project.EvaluationId);
        if (evaluation == null) return;

        LoadEvaluationProperties(evaluation);
        LoadEvaluationItems(evaluation);
    }

    /// <summary>
    /// Get current properties.
    /// </summary>
    public Dictionary<string, string> GetProperties()
    {
        return new Dictionary<string, string>(_properties, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get current items.
    /// </summary>
    public Dictionary<string, ProjectStateItem[]> GetItems()
    {
        return _items.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    private void LoadEvaluationProperties(ProjectEvaluation evaluation)
    {
        // Load global properties
        var globalPropsFolder = evaluation.FindChild<Folder>("Properties")?.FindChild<Folder>("Global");
        if (globalPropsFolder != null)
        {
            foreach (var prop in globalPropsFolder.Children.OfType<Property>())
            {
                _properties[prop.Name] = prop.Value;
            }
        }

        // Load all other properties
        var propsFolder = evaluation.FindChild<Folder>("Properties");
        if (propsFolder != null)
        {
            foreach (var prop in propsFolder.Children.OfType<Property>())
            {
                _properties[prop.Name] = prop.Value;
            }
        }
    }

    private void LoadEvaluationItems(ProjectEvaluation evaluation)
    {
        var itemsFolder = evaluation.FindChild<Folder>("Items");
        if (itemsFolder == null) return;

        foreach (var addItem in itemsFolder.Children.OfType<AddItem>())
        {
            var itemType = addItem.Name;
            if (!_items.ContainsKey(itemType))
            {
                _items[itemType] = new List<ProjectStateItem>();
            }

            foreach (var item in addItem.Children.OfType<Item>())
            {
                var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var meta in item.Children.OfType<Metadata>())
                {
                    metadata[meta.Name] = meta.Value;
                }

                _items[itemType].Add(new ProjectStateItem(item.Name, metadata));
            }
        }
    }

    private static Build? FindBuild(BaseNode node)
    {
        while (node != null)
        {
            if (node is Build build)
                return build;
            node = node.Parent;
        }
        return null;
    }
}
