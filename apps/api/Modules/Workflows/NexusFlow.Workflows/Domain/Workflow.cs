using NexusFlow.Shared.Domain;

namespace NexusFlow.Workflows.Domain;

public sealed class Workflow : BaseEntity
{
    private Workflow() { }

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public TriggerType TriggerType { get; private set; }
    public string TriggerConfig { get; private set; } = "{}";
    public bool IsActive { get; private set; }
    public string? Layout { get; private set; }

    private readonly List<WorkflowStep> _steps = [];
    public IReadOnlyList<WorkflowStep> Steps => _steps.AsReadOnly();

    public void SetLayout(string? layoutJson, DateTimeOffset now)
    {
        Layout = string.IsNullOrWhiteSpace(layoutJson) ? null : layoutJson;
        UpdatedAt = now;
    }

    public static Workflow Create(
        Guid userId,
        string name,
        string? description,
        TriggerType triggerType,
        string triggerConfig,
        DateTimeOffset now)
    {
        return new Workflow
        {
            UserId = userId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            TriggerType = triggerType,
            TriggerConfig = triggerConfig,
            IsActive = false,
            CreatedAt = now,
        };
    }

    public void Update(
        string name,
        string? description,
        TriggerType triggerType,
        string triggerConfig,
        DateTimeOffset now)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        TriggerType = triggerType;
        TriggerConfig = triggerConfig;
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAt = now;
    }

    public void ReplaceSteps(IEnumerable<WorkflowStep> steps)
    {
        _steps.Clear();
        var index = 0;
        foreach (var step in steps.OrderBy(s => s.OrderIndex))
        {
            step.SetOrderIndex(index++);
            _steps.Add(step);
        }
    }
}
