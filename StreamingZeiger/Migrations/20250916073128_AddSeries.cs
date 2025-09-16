using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamingZeiger.Migrations
{
    /// <inheritdoc />
    public partial class AddSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PosterUrl",
                table: "Series",
                newName: "TrailerUrl");

            migrationBuilder.RenameColumn(
                name: "Genre",
                table: "Series",
                newName: "PosterFile");

            migrationBuilder.AddColumn<int>(
                name: "SeriesId",
                table: "WatchlistItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cast",
                table: "Series",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Director",
                table: "Series",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EndYear",
                table: "Series",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalTitle",
                table: "Series",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "Series",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "StartYear",
                table: "Series",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SeriesId",
                table: "Ratings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SeriesGenres",
                columns: table => new
                {
                    SeriesId = table.Column<int>(type: "INTEGER", nullable: false),
                    GenreId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesGenres", x => new { x.SeriesId, x.GenreId });
                    table.ForeignKey(
                        name: "FK_SeriesGenres_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeriesGenres_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistItems_SeriesId",
                table: "WatchlistItems",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_SeriesId",
                table: "Ratings",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesGenres_GenreId",
                table: "SeriesGenres",
                column: "GenreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_Series_SeriesId",
                table: "Ratings",
                column: "SeriesId",
                principalTable: "Series",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WatchlistItems_Series_SeriesId",
                table: "WatchlistItems",
                column: "SeriesId",
                principalTable: "Series",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_Series_SeriesId",
                table: "Ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_WatchlistItems_Series_SeriesId",
                table: "WatchlistItems");

            migrationBuilder.DropTable(
                name: "SeriesGenres");

            migrationBuilder.DropIndex(
                name: "IX_WatchlistItems_SeriesId",
                table: "WatchlistItems");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_SeriesId",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "SeriesId",
                table: "WatchlistItems");

            migrationBuilder.DropColumn(
                name: "Cast",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "Director",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "EndYear",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "OriginalTitle",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "StartYear",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "SeriesId",
                table: "Ratings");

            migrationBuilder.RenameColumn(
                name: "TrailerUrl",
                table: "Series",
                newName: "PosterUrl");

            migrationBuilder.RenameColumn(
                name: "PosterFile",
                table: "Series",
                newName: "Genre");
        }
    }
}
