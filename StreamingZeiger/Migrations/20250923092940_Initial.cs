using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamingZeiger.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MediaItemId1",
                table: "WatchlistItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistItems_MediaItemId1",
                table: "WatchlistItems",
                column: "MediaItemId1");

            migrationBuilder.AddForeignKey(
                name: "FK_WatchlistItems_MediaItems_MediaItemId1",
                table: "WatchlistItems",
                column: "MediaItemId1",
                principalTable: "MediaItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WatchlistItems_MediaItems_MediaItemId1",
                table: "WatchlistItems");

            migrationBuilder.DropIndex(
                name: "IX_WatchlistItems_MediaItemId1",
                table: "WatchlistItems");

            migrationBuilder.DropColumn(
                name: "MediaItemId1",
                table: "WatchlistItems");
        }
    }
}
