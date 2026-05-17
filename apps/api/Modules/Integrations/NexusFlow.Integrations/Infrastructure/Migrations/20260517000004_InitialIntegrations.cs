using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusFlow.Integrations.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialIntegrations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "integrations");

        migrationBuilder.CreateTable(
            name: "int_user_integrations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Provider = table.Column<int>(type: "integer", nullable: false),
                Label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                CredentialsEncrypted = table.Column<string>(type: "text", nullable: false),
                RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_int_user_integrations", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_int_user_integrations_UserId_Provider",
            table: "int_user_integrations",
            columns: ["UserId", "Provider"]);

        migrationBuilder.CreateIndex(
            name: "IX_int_user_integrations_UserId_RevokedAt",
            table: "int_user_integrations",
            columns: ["UserId", "RevokedAt"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "int_user_integrations");
    }
}
