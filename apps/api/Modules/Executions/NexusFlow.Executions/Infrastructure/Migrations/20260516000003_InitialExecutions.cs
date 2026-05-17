using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusFlow.Executions.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialExecutions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "executions");

        migrationBuilder.CreateTable(
            name: "exec_executions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                TriggeredBy = table.Column<int>(type: "integer", nullable: false),
                TriggerPayload = table.Column<string>(type: "jsonb", nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DurationMs = table.Column<int>(type: "integer", nullable: true),
                ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_exec_executions", x => x.Id));

        migrationBuilder.CreateTable(
            name: "exec_step_executions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                StepId = table.Column<Guid>(type: "uuid", nullable: false),
                OrderIndex = table.Column<int>(type: "integer", nullable: false),
                ActionType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                Input = table.Column<string>(type: "jsonb", nullable: false),
                Output = table.Column<string>(type: "jsonb", nullable: false),
                Error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DurationMs = table.Column<int>(type: "integer", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_exec_step_executions", x => x.Id);
                table.ForeignKey(
                    name: "FK_exec_step_executions_exec_executions_ExecutionId",
                    column: x => x.ExecutionId,
                    principalTable: "exec_executions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "exec_workflow_outputs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                Payload = table.Column<string>(type: "jsonb", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_exec_workflow_outputs", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_exec_executions_UserId_CreatedAt",
            table: "exec_executions",
            columns: ["UserId", "CreatedAt"]);

        migrationBuilder.CreateIndex(
            name: "IX_exec_executions_WorkflowId_Status",
            table: "exec_executions",
            columns: ["WorkflowId", "Status"]);

        migrationBuilder.CreateIndex(
            name: "IX_exec_executions_WorkflowId_CreatedAt",
            table: "exec_executions",
            columns: ["WorkflowId", "CreatedAt"]);

        migrationBuilder.CreateIndex(
            name: "IX_exec_step_executions_ExecutionId_OrderIndex",
            table: "exec_step_executions",
            columns: ["ExecutionId", "OrderIndex"]);

        migrationBuilder.CreateIndex(
            name: "IX_exec_workflow_outputs_WorkflowId_CreatedAt",
            table: "exec_workflow_outputs",
            columns: ["WorkflowId", "CreatedAt"]);

        migrationBuilder.CreateIndex(
            name: "IX_exec_workflow_outputs_ExecutionId",
            table: "exec_workflow_outputs",
            column: "ExecutionId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "exec_step_executions");
        migrationBuilder.DropTable(name: "exec_workflow_outputs");
        migrationBuilder.DropTable(name: "exec_executions");
    }
}
