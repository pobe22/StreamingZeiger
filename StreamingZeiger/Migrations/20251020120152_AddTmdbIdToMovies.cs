using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamingZeiger.Migrations
{
    /// <inheritdoc />
    public partial class AddTmdbIdToMovies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TmdbId",
                table: "MediaItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_TmdbId",
                table: "MediaItems",
                column: "TmdbId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaItems_TmdbId",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "TmdbId",
                table: "MediaItems");
        }
    }
}
