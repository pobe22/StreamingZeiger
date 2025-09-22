using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamingZeiger.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Genres_MediaItems_MediaItemId",
                table: "Genres");

            migrationBuilder.DropIndex(
                name: "IX_Genres_MediaItemId",
                table: "Genres");

            migrationBuilder.DropColumn(
                name: "MediaItemId",
                table: "Genres");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MediaItemId",
                table: "Genres",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Genres_MediaItemId",
                table: "Genres",
                column: "MediaItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Genres_MediaItems_MediaItemId",
                table: "Genres",
                column: "MediaItemId",
                principalTable: "MediaItems",
                principalColumn: "Id");
        }
    }
}
