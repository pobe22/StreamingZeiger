document.addEventListener("DOMContentLoaded", () => {
    const importBtn = document.getElementById("importTmdbSeries");

    if (importBtn) {
        importBtn.addEventListener("click", async () => {
            const tmdbId = document.getElementById("tmdbId").value;
            if (!tmdbId) {
                alert("Bitte eine TMDb ID eingeben.");
                return;
            }

            try {
                const response = await fetch(`/Admin/ImportSeriesFromTmdb?tmdbId=${tmdbId}`);
                if (!response.ok) throw new Error("Fehler beim Abrufen der Daten.");

                const series = await response.json();

                // Felder automatisch füllen
                document.getElementById("Title").value = series.title || "";
                document.getElementById("OriginalTitle").value = series.originalTitle || "";
                document.getElementById("StartYear").value = series.startYear || "";
                document.getElementById("EndYear").value = series.endYear || "";
                document.getElementById("Seasons").value = series.seasons || "";
                document.getElementById("Episodes").value = series.episodes || "";
                document.getElementById("Description").value = series.description || "";
                document.getElementById("Director").value = series.director || "";
                document.getElementById("PosterFile").value = series.posterFile || "";
                document.getElementById("TrailerUrl").value = series.trailerUrl || "";

                if (series.cast && Array.isArray(series.cast)) {
                    document.getElementById("CastCsv").value = series.cast.join(", ");
                }

                if (series.genres && Array.isArray(series.genres)) {
                    document.getElementById("GenreCsv").value = series.genres.join(", ");
                }

                alert("Daten erfolgreich importiert!");
            } catch (err) {
                console.error(err);
                alert("Fehler beim Importieren der Serie.");
            }
        });
    }
});
