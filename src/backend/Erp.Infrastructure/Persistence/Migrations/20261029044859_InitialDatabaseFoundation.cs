using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDatabaseFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                    table.CheckConstraint("CK_Companies_Code_NotBlank", "length(trim(\"Code\")) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.CheckConstraint("CK_Permissions_Code_NotBlank", "length(trim(\"Code\")) > 0");
                });

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalSubject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.Id);
                    table.CheckConstraint("CK_UserAccounts_Subject_NotBlank", "length(trim(\"ExternalSubject\")) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.UniqueConstraint("AK_Customers_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_Customers_Code_NotBlank", "length(trim(\"Code\")) > 0");
                    table.ForeignKey(
                        name: "FK_Customers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TrackingMode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCategories", x => x.Id);
                    table.UniqueConstraint("AK_ItemCategories_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_ItemCategories_Code_NotBlank", "length(trim(\"Code\")) > 0");
                    table.CheckConstraint("CK_ItemCategories_TrackingMode", " \"TrackingMode\" IN ('none', 'lot', 'serial') ");
                    table.ForeignKey(
                        name: "FK_ItemCategories_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IndustryCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.UniqueConstraint("AK_Products_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_Products_Code_NotBlank", "length(trim(\"Code\")) > 0");
                    table.ForeignKey(
                        name: "FK_Products_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.UniqueConstraint("AK_Roles_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_Roles_Code_NotBlank", "length(trim(\"Code\")) > 0");
                    table.ForeignKey(
                        name: "FK_Roles_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.UniqueConstraint("AK_Suppliers_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_Suppliers_Code_NotBlank", "length(trim(\"Code\")) > 0");
                    table.ForeignKey(
                        name: "FK_Suppliers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitsOfMeasure",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Dimension = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitsOfMeasure", x => x.Id);
                    table.UniqueConstraint("AK_UnitsOfMeasure_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_UnitsOfMeasure_Code_NotBlank", "length(trim(\"Code\")) > 0");
                    table.CheckConstraint("CK_UnitsOfMeasure_DecimalPlaces", " \"DecimalPlaces\" >= 0 AND \"DecimalPlaces\" <= 6 ");
                    table.ForeignKey(
                        name: "FK_UnitsOfMeasure_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                    table.UniqueConstraint("AK_Warehouses_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_Warehouses_Code_NotBlank", "length(trim(\"Code\")) > 0");
                    table.ForeignKey(
                        name: "FK_Warehouses_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorSubjectSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.CheckConstraint("CK_AuditEvents_Action_NotBlank", "length(trim(\"Action\")) > 0");
                    table.ForeignKey(
                        name: "FK_AuditEvents_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditEvents_UserAccounts_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Boms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boms", x => x.Id);
                    table.UniqueConstraint("AK_Boms_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_Boms_Code_NotBlank", "length(trim(\"Code\")) > 0");
                    table.ForeignKey(
                        name: "FK_Boms_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Boms_Products_CompanyId_ProductId",
                        columns: x => new { x.CompanyId, x.ProductId },
                        principalTable: "Products",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.CompanyId, x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_CompanyId_RoleId",
                        columns: x => new { x.CompanyId, x.RoleId },
                        principalTable: "Roles",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserCompanyRoles",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompanyRoles", x => new { x.CompanyId, x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserCompanyRoles_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserCompanyRoles_Roles_CompanyId_RoleId",
                        columns: x => new { x.CompanyId, x.RoleId },
                        principalTable: "Roles",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserCompanyRoles_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PurchaseToStockFactor = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.UniqueConstraint("AK_Items_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_Items_Code_NotBlank", "length(trim(\"Code\")) > 0");
                    table.CheckConstraint("CK_Items_PurchaseToStockFactor", " CAST(\"PurchaseToStockFactor\" AS NUMERIC) > 0 ");
                    table.ForeignKey(
                        name: "FK_Items_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Items_ItemCategories_CompanyId_CategoryId",
                        columns: x => new { x.CompanyId, x.CategoryId },
                        principalTable: "ItemCategories",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Items_UnitsOfMeasure_CompanyId_PurchaseUnitId",
                        columns: x => new { x.CompanyId, x.PurchaseUnitId },
                        principalTable: "UnitsOfMeasure",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Items_UnitsOfMeasure_CompanyId_StockUnitId",
                        columns: x => new { x.CompanyId, x.StockUnitId },
                        principalTable: "UnitsOfMeasure",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UomConversions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Factor = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UomConversions", x => x.Id);
                    table.UniqueConstraint("AK_UomConversions_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_UomConversions_DifferentUnits", " \"FromUnitId\" <> \"ToUnitId\" ");
                    table.CheckConstraint("CK_UomConversions_EffectiveDates", " \"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\" ");
                    table.CheckConstraint("CK_UomConversions_Factor", " CAST(\"Factor\" AS NUMERIC) > 0 ");
                    table.ForeignKey(
                        name: "FK_UomConversions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UomConversions_UnitsOfMeasure_CompanyId_FromUnitId",
                        columns: x => new { x.CompanyId, x.FromUnitId },
                        principalTable: "UnitsOfMeasure",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UomConversions_UnitsOfMeasure_CompanyId_ToUnitId",
                        columns: x => new { x.CompanyId, x.ToUnitId },
                        principalTable: "UnitsOfMeasure",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseLocations", x => x.Id);
                    table.UniqueConstraint("AK_WarehouseLocations_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.UniqueConstraint("AK_WarehouseLocations_CompanyId_WarehouseId_Id", x => new { x.CompanyId, x.WarehouseId, x.Id });
                    table.CheckConstraint("CK_WarehouseLocations_Code_NotBlank", "length(trim(\"Code\")) > 0");
                    table.CheckConstraint("CK_WarehouseLocations_NotOwnParent", " \"ParentLocationId\" IS NULL OR \"ParentLocationId\" <> \"Id\" ");
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_WarehouseLocations_CompanyId_WarehouseId~",
                        columns: x => new { x.CompanyId, x.WarehouseId, x.ParentLocationId },
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "CompanyId", "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_Warehouses_CompanyId_WarehouseId",
                        columns: x => new { x.CompanyId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BomVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BomId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    ApprovedBySubject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomVersions", x => x.Id);
                    table.UniqueConstraint("AK_BomVersions_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_BomVersions_Approval", "((\"Status\" = 'draft' AND \"ApprovedAt\" IS NULL AND \"ApprovedBySubject\" IS NULL) OR (\"Status\" IN ('approved', 'retired') AND \"ApprovedAt\" IS NOT NULL AND length(trim(\"ApprovedBySubject\")) > 0))");
                    table.CheckConstraint("CK_BomVersions_EffectiveDates", " \"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\" ");
                    table.CheckConstraint("CK_BomVersions_Status", " \"Status\" IN ('draft', 'approved', 'retired') ");
                    table.CheckConstraint("CK_BomVersions_VersionNumber", " \"VersionNumber\" > 0 ");
                    table.ForeignKey(
                        name: "FK_BomVersions_Boms_CompanyId_BomId",
                        columns: x => new { x.CompanyId, x.BomId },
                        principalTable: "Boms",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BomVersions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.UniqueConstraint("AK_ProductVariants_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_ProductVariants_Code_NotBlank", "length(trim(\"Code\")) > 0");
                    table.ForeignKey(
                        name: "FK_ProductVariants_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Items_CompanyId_ItemId",
                        columns: x => new { x.CompanyId, x.ItemId },
                        principalTable: "Items",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_CompanyId_ProductId",
                        columns: x => new { x.CompanyId, x.ProductId },
                        principalTable: "Products",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BomComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BomVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    WastePercent = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    AlternativeGroupCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    IsOptional = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomComponents", x => x.Id);
                    table.UniqueConstraint("AK_BomComponents_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_BomComponents_LineNumber", " \"LineNumber\" > 0 ");
                    table.CheckConstraint("CK_BomComponents_Quantity", " CAST(\"Quantity\" AS NUMERIC) > 0 ");
                    table.CheckConstraint("CK_BomComponents_WastePercent", " CAST(\"WastePercent\" AS NUMERIC) >= 0 AND CAST(\"WastePercent\" AS NUMERIC) <= 100 ");
                    table.ForeignKey(
                        name: "FK_BomComponents_BomVersions_CompanyId_BomVersionId",
                        columns: x => new { x.CompanyId, x.BomVersionId },
                        principalTable: "BomVersions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BomComponents_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BomComponents_Items_CompanyId_ItemId",
                        columns: x => new { x.CompanyId, x.ItemId },
                        principalTable: "Items",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BomComponents_UnitsOfMeasure_CompanyId_UnitOfMeasureId",
                        columns: x => new { x.CompanyId, x.UnitOfMeasureId },
                        principalTable: "UnitsOfMeasure",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantAttributes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Value = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantAttributes", x => x.Id);
                    table.UniqueConstraint("AK_ProductVariantAttributes_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_ProductVariantAttributes_AttributeCode_NotBlank", "length(trim(\"AttributeCode\")) > 0");
                    table.CheckConstraint("CK_ProductVariantAttributes_Value_NotBlank", "length(trim(\"Value\")) > 0");
                    table.ForeignKey(
                        name: "FK_ProductVariantAttributes_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductVariantAttributes_ProductVariants_CompanyId_ProductV~",
                        columns: x => new { x.CompanyId, x.ProductVariantId },
                        principalTable: "ProductVariants",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_ActorUserId_OccurredAt",
                table: "AuditEvents",
                columns: new[] { "ActorUserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_CompanyId_OccurredAt",
                table: "AuditEvents",
                columns: new[] { "CompanyId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EntityType_EntityId",
                table: "AuditEvents",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_BomComponents_BomVersionId_LineNumber",
                table: "BomComponents",
                columns: new[] { "BomVersionId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BomComponents_CompanyId_BomVersionId",
                table: "BomComponents",
                columns: new[] { "CompanyId", "BomVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_BomComponents_CompanyId_ItemId",
                table: "BomComponents",
                columns: new[] { "CompanyId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_BomComponents_CompanyId_UnitOfMeasureId",
                table: "BomComponents",
                columns: new[] { "CompanyId", "UnitOfMeasureId" });

            migrationBuilder.CreateIndex(
                name: "IX_Boms_CompanyId_Code",
                table: "Boms",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Boms_CompanyId_ProductId",
                table: "Boms",
                columns: new[] { "CompanyId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_BomVersions_CompanyId_BomId_VersionNumber",
                table: "BomVersions",
                columns: new[] { "CompanyId", "BomId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Code",
                table: "Companies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId_Code",
                table: "Customers",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_CompanyId_Code",
                table: "ItemCategories",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_CompanyId_CategoryId",
                table: "Items",
                columns: new[] { "CompanyId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_CompanyId_Code",
                table: "Items",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_CompanyId_PurchaseUnitId",
                table: "Items",
                columns: new[] { "CompanyId", "PurchaseUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_CompanyId_StockUnitId",
                table: "Items",
                columns: new[] { "CompanyId", "StockUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_Code",
                table: "Products",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAttributes_CompanyId_AttributeCode_Value",
                table: "ProductVariantAttributes",
                columns: new[] { "CompanyId", "AttributeCode", "Value" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAttributes_CompanyId_ProductVariantId_Attribu~",
                table: "ProductVariantAttributes",
                columns: new[] { "CompanyId", "ProductVariantId", "AttributeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_CompanyId_Code",
                table: "ProductVariants",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_CompanyId_ItemId",
                table: "ProductVariants",
                columns: new[] { "CompanyId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_CompanyId_ProductId_Code",
                table: "ProductVariants",
                columns: new[] { "CompanyId", "ProductId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_CompanyId_Code",
                table: "Roles",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CompanyId_Code",
                table: "Suppliers",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitsOfMeasure_CompanyId_Code",
                table: "UnitsOfMeasure",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UomConversions_CompanyId_FromUnitId_ToUnitId_EffectiveFrom",
                table: "UomConversions",
                columns: new[] { "CompanyId", "FromUnitId", "ToUnitId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UomConversions_CompanyId_ToUnitId",
                table: "UomConversions",
                columns: new[] { "CompanyId", "ToUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_ExternalSubject",
                table: "UserAccounts",
                column: "ExternalSubject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyRoles_CompanyId_RoleId",
                table: "UserCompanyRoles",
                columns: new[] { "CompanyId", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyRoles_UserId",
                table: "UserCompanyRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_CompanyId_WarehouseId_Code",
                table: "WarehouseLocations",
                columns: new[] { "CompanyId", "WarehouseId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_CompanyId_WarehouseId_ParentLocationId",
                table: "WarehouseLocations",
                columns: new[] { "CompanyId", "WarehouseId", "ParentLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_CompanyId_Code",
                table: "Warehouses",
                columns: new[] { "CompanyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "BomComponents");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "ProductVariantAttributes");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "UomConversions");

            migrationBuilder.DropTable(
                name: "UserCompanyRoles");

            migrationBuilder.DropTable(
                name: "WarehouseLocations");

            migrationBuilder.DropTable(
                name: "BomVersions");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "UserAccounts");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "Boms");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ItemCategories");

            migrationBuilder.DropTable(
                name: "UnitsOfMeasure");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
