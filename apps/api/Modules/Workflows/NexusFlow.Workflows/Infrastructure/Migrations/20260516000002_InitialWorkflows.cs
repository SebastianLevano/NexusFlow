using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusFlow.Workflows.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialWorkflows : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "workflows");

        migrationBuilder.CreateTable(
            name: "wf_workflows",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                TriggerType = table.Column<int>(type: "integer", nullable: false),
                TriggerConfig = table.Column<string>(type: "jsonb", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_wf_workflows", x => x.Id));

        migrationBuilder.CreateTable(
            name: "wf_steps",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                OrderIndex = table.Column<int>(type: "integer", nullable: false),
                ActionType = table.Column<int>(type: "integer", nullable: false),
                Config = table.Column<string>(type: "jsonb", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_wf_steps", x => x.Id);
                table.ForeignKey(
                    name: "FK_wf_steps_wf_workflows_WorkflowId",
                    column: x => x.WorkflowId,
                    principalTable: "wf_workflows",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_wf_workflows_UserId_IsActive",
            table: "wf_workflows",
            columns: ["UserId", "IsActive"]);

        migrationBuilder.CreateIndex(
            name: "IX_wf_workflows_UserId_CreatedAt",
            table: "wf_workflows",
            columns: ["UserId", "CreatedAt"]);

        migrationBuilder.CreateIndex(
            name: "IX_wf_steps_WorkflowId_OrderIndex",
            table: "wf_steps",
            columns: ["WorkflowId", "OrderIndex"],
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "wf_steps");
        migrationBuilder.DropTable(name: "wf_workflows");
    }
}
