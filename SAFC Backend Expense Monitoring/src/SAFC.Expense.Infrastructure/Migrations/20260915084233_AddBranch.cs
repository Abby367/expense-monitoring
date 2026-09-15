using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAFC.Expense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_MicrosoftId",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RemovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RemovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RemovedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_MicrosoftId",
                table: "Users",
                column: "MicrosoftId",
                unique: true,
                filter: "\"MicrosoftId\" IS NOT NULL AND \"RemovedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Code",
                table: "Branches",
                column: "Code",
                unique: true,
                filter: "\"RemovedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Users_MicrosoftId",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_MicrosoftId",
                table: "Users",
                column: "MicrosoftId",
                unique: true,
                filter: "\"MicrosoftId\" IS NOT NULL");
        }
    }
}
