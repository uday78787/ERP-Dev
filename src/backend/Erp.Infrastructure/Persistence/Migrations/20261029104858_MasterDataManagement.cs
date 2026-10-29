using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MasterDataManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "Suppliers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "Suppliers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Suppliers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Suppliers",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Suppliers",
                type: "character varying(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateOrProvince",
                table: "Suppliers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorCode",
                table: "ProductVariants",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorName",
                table: "ProductVariants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SizeCode",
                table: "ProductVariants",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryType",
                table: "ItemCategories",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "general");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "Customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "Customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Customers",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Customers",
                type: "character varying(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateOrProvince",
                table: "Customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ItemCodeRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Separator = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Padding = table.Column<int>(type: "integer", nullable: false),
                    NextNumber = table.Column<long>(type: "bigint", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCodeRules", x => x.Id);
                    table.CheckConstraint("CK_ItemCodeRules_NextNumber", " \"NextNumber\" >= 1 ");
                    table.CheckConstraint("CK_ItemCodeRules_Padding", " \"Padding\" >= 1 AND \"Padding\" <= 12 ");
                    table.CheckConstraint("CK_ItemCodeRules_Prefix_NotBlank", "length(trim(\"Prefix\")) > 0");
                    table.CheckConstraint("CK_ItemCodeRules_Revision", " \"Revision\" >= 0 ");
                    table.ForeignKey(
                        name: "FK_ItemCodeRules_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Machines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProductionLine = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Machines", x => x.Id);
                    table.UniqueConstraint("AK_Machines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_Machines_Code_NotBlank", "length(trim(\"Code\")) > 0");
                    table.ForeignKey(
                        name: "FK_Machines_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsOutsourced = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOperations", x => x.Id);
                    table.UniqueConstraint("AK_ProductionOperations_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_ProductionOperations_Code_NotBlank", "length(trim(\"Code\")) > 0");
                    table.ForeignKey(
                        name: "FK_ProductionOperations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_CompanyId_ProductId_SizeCode_ColorCode",
                table: "ProductVariants",
                columns: new[] { "CompanyId", "ProductId", "SizeCode", "ColorCode" },
                unique: true,
                filter: "\"SizeCode\" IS NOT NULL AND \"ColorCode\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ItemCategories_CategoryType",
                table: "ItemCategories",
                sql: " \"CategoryType\" IN ('general', 'fabric', 'trims', 'accessory', 'packaging', 'finished_good') ");

            migrationBuilder.CreateIndex(
                name: "IX_ItemCodeRules_CompanyId",
                table: "ItemCodeRules",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Machines_CompanyId_Code",
                table: "Machines",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOperations_CompanyId_Code",
                table: "ProductionOperations",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "Permissions" ("Id", "Code", "Name")
                VALUES
                    (md5('itemcategories.access')::uuid, 'itemcategories.access', 'Item categories module access'),
                    (md5('itemcategories.read')::uuid, 'itemcategories.read', 'Read item categories'),
                    (md5('itemcategories.create')::uuid, 'itemcategories.create', 'Create item categories'),
                    (md5('itemcategories.update')::uuid, 'itemcategories.update', 'Update item categories'),
                    (md5('itemcategories.deactivate')::uuid, 'itemcategories.deactivate', 'Deactivate item categories'),
                    (md5('uom.access')::uuid, 'uom.access', 'Units module access'),
                    (md5('uom.read')::uuid, 'uom.read', 'Read units'),
                    (md5('uom.create')::uuid, 'uom.create', 'Create units'),
                    (md5('uom.update')::uuid, 'uom.update', 'Update units'),
                    (md5('uom.deactivate')::uuid, 'uom.deactivate', 'Deactivate units'),
                    (md5('warehouses.access')::uuid, 'warehouses.access', 'Warehouses module access'),
                    (md5('warehouses.read')::uuid, 'warehouses.read', 'Read warehouses'),
                    (md5('warehouses.create')::uuid, 'warehouses.create', 'Create warehouses'),
                    (md5('warehouses.update')::uuid, 'warehouses.update', 'Update warehouses'),
                    (md5('warehouses.deactivate')::uuid, 'warehouses.deactivate', 'Deactivate warehouses'),
                    (md5('machines.access')::uuid, 'machines.access', 'Machines module access'),
                    (md5('machines.read')::uuid, 'machines.read', 'Read machines'),
                    (md5('machines.create')::uuid, 'machines.create', 'Create machines'),
                    (md5('machines.update')::uuid, 'machines.update', 'Update machines'),
                    (md5('machines.deactivate')::uuid, 'machines.deactivate', 'Deactivate machines'),
                    (md5('operations.access')::uuid, 'operations.access', 'Production operations module access'),
                    (md5('operations.read')::uuid, 'operations.read', 'Read production operations'),
                    (md5('operations.create')::uuid, 'operations.create', 'Create production operations'),
                    (md5('operations.update')::uuid, 'operations.update', 'Update production operations'),
                    (md5('operations.deactivate')::uuid, 'operations.deactivate', 'Deactivate production operations'),
                    (md5('boms.access')::uuid, 'boms.access', 'BOM module access'),
                    (md5('boms.read')::uuid, 'boms.read', 'Read BOMs'),
                    (md5('boms.create')::uuid, 'boms.create', 'Create BOMs'),
                    (md5('boms.update')::uuid, 'boms.update', 'Update BOMs'),
                    (md5('boms.approve')::uuid, 'boms.approve', 'Approve BOM versions'),
                    (md5('boms.deactivate')::uuid, 'boms.deactivate', 'Deactivate BOMs')
                ON CONFLICT ("Code") DO NOTHING;

                INSERT INTO "RolePermissions" ("CompanyId", "RoleId", "PermissionId")
                SELECT role."CompanyId", role."Id", permission."Id"
                FROM "Roles" AS role
                JOIN (VALUES
                    ('ADMINISTRATION', 'itemcategories.access'),
                    ('ADMINISTRATION', 'itemcategories.read'),
                    ('ADMINISTRATION', 'itemcategories.create'),
                    ('ADMINISTRATION', 'itemcategories.update'),
                    ('ADMINISTRATION', 'itemcategories.deactivate'),
                    ('ADMINISTRATION', 'uom.access'),
                    ('ADMINISTRATION', 'uom.read'),
                    ('ADMINISTRATION', 'uom.create'),
                    ('ADMINISTRATION', 'uom.update'),
                    ('ADMINISTRATION', 'uom.deactivate'),
                    ('ADMINISTRATION', 'warehouses.access'),
                    ('ADMINISTRATION', 'warehouses.read'),
                    ('ADMINISTRATION', 'warehouses.create'),
                    ('ADMINISTRATION', 'warehouses.update'),
                    ('ADMINISTRATION', 'warehouses.deactivate'),
                    ('ADMINISTRATION', 'machines.access'),
                    ('ADMINISTRATION', 'machines.read'),
                    ('ADMINISTRATION', 'machines.create'),
                    ('ADMINISTRATION', 'machines.update'),
                    ('ADMINISTRATION', 'machines.deactivate'),
                    ('ADMINISTRATION', 'operations.access'),
                    ('ADMINISTRATION', 'operations.read'),
                    ('ADMINISTRATION', 'operations.create'),
                    ('ADMINISTRATION', 'operations.update'),
                    ('ADMINISTRATION', 'operations.deactivate'),
                    ('ADMINISTRATION', 'boms.access'),
                    ('ADMINISTRATION', 'boms.read'),
                    ('ADMINISTRATION', 'boms.create'),
                    ('ADMINISTRATION', 'boms.update'),
                    ('ADMINISTRATION', 'boms.approve'),
                    ('ADMINISTRATION', 'boms.deactivate'),
                    ('STORES', 'itemcategories.access'),
                    ('STORES', 'itemcategories.read'),
                    ('STORES', 'itemcategories.create'),
                    ('STORES', 'itemcategories.update'),
                    ('STORES', 'itemcategories.deactivate'),
                    ('STORES', 'uom.access'),
                    ('STORES', 'uom.read'),
                    ('STORES', 'uom.create'),
                    ('STORES', 'uom.update'),
                    ('STORES', 'uom.deactivate'),
                    ('STORES', 'warehouses.access'),
                    ('STORES', 'warehouses.read'),
                    ('STORES', 'warehouses.create'),
                    ('STORES', 'warehouses.update'),
                    ('STORES', 'warehouses.deactivate'),
                    ('PURCHASE', 'itemcategories.access'),
                    ('PURCHASE', 'itemcategories.read'),
                    ('PURCHASE', 'uom.access'),
                    ('PURCHASE', 'uom.read'),
                    ('PRODUCTION', 'machines.access'),
                    ('PRODUCTION', 'machines.read'),
                    ('PRODUCTION', 'machines.create'),
                    ('PRODUCTION', 'machines.update'),
                    ('PRODUCTION', 'machines.deactivate'),
                    ('PRODUCTION', 'operations.access'),
                    ('PRODUCTION', 'operations.read'),
                    ('PRODUCTION', 'operations.create'),
                    ('PRODUCTION', 'operations.update'),
                    ('PRODUCTION', 'operations.deactivate'),
                    ('PRODUCTION', 'boms.access'),
                    ('PRODUCTION', 'boms.read'),
                    ('PRODUCTION', 'boms.create'),
                    ('PRODUCTION', 'boms.update'),
                    ('PRODUCTION', 'boms.approve'),
                    ('MAINTENANCE', 'machines.access'),
                    ('MAINTENANCE', 'machines.read'),
                    ('MAINTENANCE', 'machines.create'),
                    ('MAINTENANCE', 'machines.update'),
                    ('MAINTENANCE', 'machines.deactivate')
                ) AS grant_map("RoleCode", "PermissionCode")
                    ON role."Code" = grant_map."RoleCode"
                JOIN "Permissions" AS permission
                    ON permission."Code" = grant_map."PermissionCode"
                ON CONFLICT ("CompanyId", "RoleId", "PermissionId") DO NOTHING;

                INSERT INTO "ItemCodeRules"
                    ("Id", "CompanyId", "Prefix", "Separator", "Padding", "NextNumber", "Revision", "IsActive")
                SELECT md5(company."Id"::text || ':item-code-rule')::uuid,
                    company."Id", 'ITEM', '-', 6, 1, 0, TRUE
                FROM "Companies" AS company
                ON CONFLICT ("CompanyId") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "RolePermissions"
                WHERE "PermissionId" IN (
                    SELECT "Id" FROM "Permissions"
                    WHERE "Code" LIKE 'itemcategories.%'
                       OR "Code" LIKE 'uom.%'
                       OR "Code" LIKE 'warehouses.%'
                       OR "Code" LIKE 'machines.%'
                       OR "Code" LIKE 'operations.%'
                       OR "Code" LIKE 'boms.%');
                DELETE FROM "Permissions"
                WHERE "Code" LIKE 'itemcategories.%'
                   OR "Code" LIKE 'uom.%'
                   OR "Code" LIKE 'warehouses.%'
                   OR "Code" LIKE 'machines.%'
                   OR "Code" LIKE 'operations.%'
                   OR "Code" LIKE 'boms.%';
                """);

            migrationBuilder.DropTable(
                name: "ItemCodeRules");

            migrationBuilder.DropTable(
                name: "Machines");

            migrationBuilder.DropTable(
                name: "ProductionOperations");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_CompanyId_ProductId_SizeCode_ColorCode",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ItemCategories_CategoryType",
                table: "ItemCategories");

            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "StateOrProvince",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ColorCode",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ColorName",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "SizeCode",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CategoryType",
                table: "ItemCategories");

            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "StateOrProvince",
                table: "Customers");
        }
    }
}
