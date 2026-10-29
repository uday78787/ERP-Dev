using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthenticationAndRbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                table: "UserAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "UserAccounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "UserAccounts",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "UserAccounts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "UserAccounts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "SecurityAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditEvents", x => x.Id);
                    table.CheckConstraint("CK_SecurityAuditEvents_Action_NotBlank", "length(trim(\"Action\")) > 0");
                    table.ForeignKey(
                        name: "FK_SecurityAuditEvents_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityAuditEvents_UserAccounts_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityAuditEvents_UserAccounts_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_NormalizedEmail",
                table: "UserAccounts",
                column: "NormalizedEmail",
                unique: true,
                filter: "\"NormalizedEmail\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserAccounts_AccessFailedCount",
                table: "UserAccounts",
                sql: " \"AccessFailedCount\" >= 0 ");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_ActorUserId_OccurredAt",
                table: "SecurityAuditEvents",
                columns: new[] { "ActorUserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_CompanyId_OccurredAt",
                table: "SecurityAuditEvents",
                columns: new[] { "CompanyId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_TargetUserId",
                table: "SecurityAuditEvents",
                column: "TargetUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityAuditEvents");

            migrationBuilder.DropIndex(
                name: "IX_UserAccounts_NormalizedEmail",
                table: "UserAccounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserAccounts_AccessFailedCount",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "AccessFailedCount",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "UserAccounts");
        }
    }
}
