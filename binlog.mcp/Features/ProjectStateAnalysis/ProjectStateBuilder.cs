using Microsoft.Build.Logging.StructuredLogger;

namespace Binlog.MCP.Features.ProjectStateAnalysis;

/// <summary>
/// Builds a snapshot of project state by starting with evaluation and applying
/// state changes from target/task execution.
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
    /// Build state up to (but not including) the specified target.
    /// </summary>
    public void BuildStateBeforeTarget(Target target)
    {
        // Start with evaluation
        BuildEvaluationState();

        // Get all targets that executed before this one
        var targets = GetTargetsInExecutionOrder(_project);
        foreach (var t in targets)
        {
            if (t.Id == target.Id)
                break; // Stop before this target
            
            ApplyTargetStateChanges(t);
        }
    }

    /// <summary>
    /// Build state after the specified target executed.
    /// </summary>
    public void BuildStateAfterTarget(Target target)
    {
        // Start with evaluation
        BuildEvaluationState();

        // Get all targets that executed up to and including this one
        var targets = GetTargetsInExecutionOrder(_project);
        foreach (var t in targets)
        {
            ApplyTargetStateChanges(t);
            
            if (t.Id == target.Id)
                break; // Stop after this target
        }
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

        // Load all properties
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

    private void ApplyTargetStateChanges(Target target)
    {
        // Process all tasks in this target
        foreach (var task in target.Children.OfType<Microsoft.Build.Logging.StructuredLogger.Task>())
        {
            ApplyTaskOutputs(task);
        }
    }

    private void ApplyTaskOutputs(Microsoft.Build.Logging.StructuredLogger.Task task)
    {
        // MSBuild doesn't explicitly mark which properties are outputs in the binlog
        // We look for properties that are set by the task
        // This is a simplified approach - in reality, only properties explicitly marked
        // as outputs in the task definition would modify project state
        
        // Look for Parameter nodes which might indicate outputs
        // In the structured log, task outputs are recorded differently depending on the task
        
        // Common patterns:
        // 1. Properties set by tasks appear as Property children
        // 2. Items added appear in specific patterns
        
        // For now, we'll look for common output patterns
        foreach (var prop in task.Children.OfType<Property>())
        {
            // Only apply if this looks like an output (heuristic)
            // Common output properties have specific patterns
            if (IsLikelyOutputProperty(prop.Name, task.Name))
            {
                _properties[prop.Name] = prop.Value;
            }
        }

        // Note: Full item tracking would require parsing task messages or having
        // explicit output markers in the binlog, which may not always be available
    }

    private bool IsLikelyOutputProperty(string propertyName, string taskName)
    {
        // This is a heuristic - in practice, we'd need metadata about which
        // task parameters are outputs
        
        // Common output properties:
        // - Properties that match common output patterns
        // - Target-specific outputs
        
        // For now, be conservative and assume properties that don't look like
        // standard MSBuild internal properties might be outputs
        
        // Skip well-known input properties
        if (propertyName.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase))
            return false;
        
        // This is a simplified heuristic - ideally we'd have metadata
        // For now, we'll track all properties to be safe
        return true;
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

    private List<Target> GetTargetsInExecutionOrder(Project project)
    {
        // Get all targets and sort by start time
        var targets = project.Children.OfType<Target>().ToList();
        
        // Sort by start time (or order in tree which reflects execution order)
        // The structured logger stores them in execution order
        return targets;
    }
}
