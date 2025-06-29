using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddEventAttentionFunctionality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventPlaceEvent_Places_EventPlaceId",
                table: "EventPlaceEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_EventPlaceOffer_EventPlaceEvent_EventId",
                table: "EventPlaceOffer");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EventPlaceEvent",
                table: "EventPlaceEvent");

            migrationBuilder.RenameTable(
                name: "EventPlaceEvent",
                newName: "EventPlaceEvents");

            migrationBuilder.RenameIndex(
                name: "IX_EventPlaceEvent_EventPlaceId",
                table: "EventPlaceEvents",
                newName: "IX_EventPlaceEvents_EventPlaceId");

            migrationBuilder.AddColumn<string>(
                name: "AttendingEvents",
                table: "Exits",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventPlaceEvents",
                table: "EventPlaceEvents",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EventPlaceEvents_Places_EventPlaceId",
                table: "EventPlaceEvents",
                column: "EventPlaceId",
                principalTable: "Places",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EventPlaceOffer_EventPlaceEvents_EventId",
                table: "EventPlaceOffer",
                column: "EventId",
                principalTable: "EventPlaceEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventPlaceEvents_Places_EventPlaceId",
                table: "EventPlaceEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_EventPlaceOffer_EventPlaceEvents_EventId",
                table: "EventPlaceOffer");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EventPlaceEvents",
                table: "EventPlaceEvents");

            migrationBuilder.DropColumn(
                name: "AttendingEvents",
                table: "Exits");

            migrationBuilder.RenameTable(
                name: "EventPlaceEvents",
                newName: "EventPlaceEvent");

            migrationBuilder.RenameIndex(
                name: "IX_EventPlaceEvents_EventPlaceId",
                table: "EventPlaceEvent",
                newName: "IX_EventPlaceEvent_EventPlaceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventPlaceEvent",
                table: "EventPlaceEvent",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EventPlaceEvent_Places_EventPlaceId",
                table: "EventPlaceEvent",
                column: "EventPlaceId",
                principalTable: "Places",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EventPlaceOffer_EventPlaceEvent_EventId",
                table: "EventPlaceOffer",
                column: "EventId",
                principalTable: "EventPlaceEvent",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
