using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WMS.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TableName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RecordId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OldValue = table.Column<string>(type: "TEXT", nullable: false),
                    NewValue = table.Column<string>(type: "TEXT", nullable: false),
                    ChangedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "CustomerOrders",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    CompleteDeliveryRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOrders", x => x.OrderId);
                });

            migrationBuilder.CreateTable(
                name: "SKUs",
                columns: table => new
                {
                    SkuId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TotalQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    AvailableQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    WarehouseLocation = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsLocationLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SKUs", x => x.SkuId);
                    table.CheckConstraint("CK_SKU_AvailableQuantity", "AvailableQuantity <= TotalQuantity");
                });

            migrationBuilder.CreateTable(
                name: "OrderLineItems",
                columns: table => new
                {
                    LineItemId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OrderId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RequestedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    AllocatedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLineItems", x => x.LineItemId);
                    table.ForeignKey(
                        name: "FK_OrderLineItems_CustomerOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "CustomerOrders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockAllocations",
                columns: table => new
                {
                    AllocationId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OrderId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LineItemId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SkuId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AllocatedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    AllocationDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAllocations", x => x.AllocationId);
                    table.ForeignKey(
                        name: "FK_StockAllocations_CustomerOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "CustomerOrders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockAllocations_OrderLineItems_LineItemId",
                        column: x => x.LineItemId,
                        principalTable: "OrderLineItems",
                        principalColumn: "LineItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAllocations_SKUs_SkuId",
                        column: x => x.SkuId,
                        principalTable: "SKUs",
                        principalColumn: "SkuId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "SKUs",
                columns: new[] { "SkuId", "AvailableQuantity", "CreatedDate", "IsLocationLocked", "LastModifiedDate", "ProductNumber", "TotalQuantity", "WarehouseLocation" },
                values: new object[,]
                {
                    { "SKU001", 100, new DateTime(2026, 1, 27, 5, 2, 48, 420, DateTimeKind.Utc).AddTicks(4096), false, new DateTime(2026, 1, 27, 5, 2, 48, 420, DateTimeKind.Utc).AddTicks(4097), "P001", 100, "A-01-01" },
                    { "SKU002", 50, new DateTime(2026, 1, 27, 5, 2, 48, 420, DateTimeKind.Utc).AddTicks(4099), false, new DateTime(2026, 1, 27, 5, 2, 48, 420, DateTimeKind.Utc).AddTicks(4099), "P001", 50, "A-01-02" },
                    { "SKU003", 75, new DateTime(2026, 1, 27, 5, 2, 48, 420, DateTimeKind.Utc).AddTicks(4101), false, new DateTime(2026, 1, 27, 5, 2, 48, 420, DateTimeKind.Utc).AddTicks(4102), "P002", 75, "B-02-01" },
                    { "SKU004", 25, new DateTime(2026, 1, 27, 5, 2, 48, 420, DateTimeKind.Utc).AddTicks(4104), true, new DateTime(2026, 1, 27, 5, 2, 48, 420, DateTimeKind.Utc).AddTicks(4104), "P002", 25, "B-02-02" },
                    { "SKU005", 200, new DateTime(2026, 1, 27, 5, 2, 48, 420, DateTimeKind.Utc).AddTicks(4106), false, new DateTime(2026, 1, 27, 5, 2, 48, 420, DateTimeKind.Utc).AddTicks(4106), "P003", 200, "C-03-01" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_ChangedDate",
                table: "AuditLog",
                column: "ChangedDate");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_TableName_RecordId",
                table: "AuditLog",
                columns: new[] { "TableName", "RecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_Priority_OrderDate",
                table: "CustomerOrders",
                columns: new[] { "Priority", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_Status",
                table: "CustomerOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLineItems_OrderId",
                table: "OrderLineItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLineItems_ProductNumber",
                table: "OrderLineItems",
                column: "ProductNumber");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLineItems_Status",
                table: "OrderLineItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SKUs_ProductNumber",
                table: "SKUs",
                column: "ProductNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SKUs_ProductNumber_AvailableQuantity_IsLocationLocked",
                table: "SKUs",
                columns: new[] { "ProductNumber", "AvailableQuantity", "IsLocationLocked" });

            migrationBuilder.CreateIndex(
                name: "IX_SKUs_WarehouseLocation",
                table: "SKUs",
                column: "WarehouseLocation");

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_LineItemId",
                table: "StockAllocations",
                column: "LineItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_OrderId",
                table: "StockAllocations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_OrderId_LineItemId",
                table: "StockAllocations",
                columns: new[] { "OrderId", "LineItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_SkuId",
                table: "StockAllocations",
                column: "SkuId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "StockAllocations");

            migrationBuilder.DropTable(
                name: "OrderLineItems");

            migrationBuilder.DropTable(
                name: "SKUs");

            migrationBuilder.DropTable(
                name: "CustomerOrders");
        }
    }
}
