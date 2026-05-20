using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusFlow.Workflows.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddWorkflowLayout : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Layout",
            table: "wf_workflows",
            type: "jsonb",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Layout", table: "wf_workflows");
    }
}
