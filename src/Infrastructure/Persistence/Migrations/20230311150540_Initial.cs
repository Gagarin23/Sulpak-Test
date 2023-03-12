using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderStatus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Order",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Order_OrderStatus_StatusId",
                        column: x => x.StatusId,
                        principalTable: "OrderStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "OrderStatus",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("60ecf8c9-f595-4eed-aa76-05b7966b07c9"), "Обработано" },
                    { new Guid("61ecf8c9-f595-4eed-aa76-05b7966b07c9"), "Не обработано" },
                    { new Guid("62ecf8c9-f595-4eed-aa76-05b7966b07c9"), "Ошибка" },
                    { new Guid("63ecf8c9-f595-4eed-aa76-05b7966b07c9"), "В обработке" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Order_Number",
                table: "Order",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "Id", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Order_StatusId",
                table: "Order",
                column: "StatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Order");

            migrationBuilder.DropTable(
                name: "OrderStatus");
        }
    }
}
