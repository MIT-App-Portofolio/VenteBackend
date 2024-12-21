using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "AspNetUsers",
                newName: "EventStatusId");

            migrationBuilder.AddColumn<string>(
                name: "IgHandle",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "EventStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    Time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Location = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventStatus", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EventStatusId",
                table: "AspNetUsers",
                column: "EventStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_EventStatus_EventStatusId",
                table: "AspNetUsers",
                column: "EventStatusId",
                principalTable: "EventStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_EventStatus_EventStatusId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "EventStatus");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_EventStatusId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IgHandle",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "EventStatusId",
                table: "AspNetUsers",
                newName: "Status");
        }
    }
}
