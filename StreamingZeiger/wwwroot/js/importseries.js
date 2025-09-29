document.addEventListener("DOMContentLoaded", () => {
    console.log("✅ DOM geladen, Script aktiv.");

    const importBtn = document.getElementById("importTmdbSeries");

    if (importBtn) {
        importBtn.addEventListener("click", async () => {
            const tmdbId = document.getElementById("tmdbId")?.value;
            if (!tmdbId) {
                alert("Bitte eine TMDb ID eingeben.");
                return;
            }

            try {
                const response = await fetch(`/Admin/ImportFromTmdb?tmdbId=${tmdbId}&type=series`);
                if (!response.ok) throw new Error("Fehler beim Abrufen der Daten.");

                const series = await response.json();
                console.log("Series-Daten empfangen:", series);

                // Basisfelder automatisch füllen
                const baseFields = [
                    { id: "Title", value: series.title },
                    { id: "OriginalTitle", value: series.originalTitle },
                    { id: "StartYear", value: series.startYear },
                    { id: "EndYear", value: series.endYear },
                    { id: "Description", value: series.description },
                    { id: "Director", value: series.director },
                    { id: "TrailerUrl", value: series.trailerUrl },
                    { id: "PosterFile", value: series.posterFile }
                ];

                baseFields.forEach(f => {
                    const el = document.getElementById(f.id);
                    if (el) el.value = f.value || "";
                });

                // Cast & Genres
                const castInput = document.getElementById("CastCsv");
                if (castInput && Array.isArray(series.cast)) castInput.value = series.cast.join(", ");

                const genreInput = document.getElementById("GenreCsv");
                if (genreInput && Array.isArray(series.genres)) genreInput.value = series.genres.join(", ");

                // Staffeln & Episoden
                const seasonsContainer = document.getElementById("seasonsContainer");
                seasonsContainer.innerHTML = "";
                seasonIndex = 0; // Reset globaler Index

                if (series.seasons && Array.isArray(series.seasons)) {
                    series.seasons.forEach((season) => {
                        seasonsContainer.insertAdjacentHTML("beforeend", createSeasonInput(seasonIndex));
                        const seasonCard = seasonsContainer.lastElementChild;

                        const seasonNumberInput = seasonCard.querySelector(`input[name="Seasons[${seasonIndex}].SeasonNumber"]`);
                        if (seasonNumberInput) seasonNumberInput.value = season.seasonNumber || "";

                        const seasonDesc = seasonCard.querySelector(`textarea[name="Seasons[${seasonIndex}].Description"]`);
                        if (seasonDesc) seasonDesc.value = season.description || "";

                        const episodesContainer = seasonCard.querySelector(".episodesContainer");

                        if (season.episodes && Array.isArray(season.episodes)) {
                            season.episodes.forEach((ep, epIdx) => {
                                episodesContainer.insertAdjacentHTML("beforeend", createEpisodeInput(seasonIndex, epIdx));
                                const epGroup = episodesContainer.lastElementChild;

                                const epNumberInput = epGroup.querySelector(`input[name="Seasons[${seasonIndex}].Episodes[${epIdx}].EpisodeNumber"]`);
                                if (epNumberInput) epNumberInput.value = ep.episodeNumber || "";

                                const epTitleInput = epGroup.querySelector(`input[name="Seasons[${seasonIndex}].Episodes[${epIdx}].Title"]`);
                                if (epTitleInput) epTitleInput.value = ep.title || "";

                                const epDescInput = epGroup.querySelector(`input[name="Seasons[${seasonIndex}].Episodes[${epIdx}].Description"]`);
                                if (epDescInput) epDescInput.value = ep.description || "";
                            });
                        }

                        seasonIndex++;
                    });
                }

                refreshSeasonIndex();
                alert("Daten erfolgreich importiert!");
            } catch (err) {
                console.error("❌ Fehler beim Import:", err);
                alert("Fehler beim Importieren der Serie.");
            }
        });
    }
});

// Funktion global verfügbar
function addEpisode(seasonIdx) {
    console.log(`➕ Neue Episode zu Staffel ${seasonIdx} hinzufügen.`);
    const seasonCard = document.getElementsByClassName('season-card')[seasonIdx];
    if (!seasonCard) return;

    const episodesContainer = seasonCard.querySelector('.episodesContainer');
    if (!episodesContainer) return;

    const episodeIdx = episodesContainer.children.length;
    const epHtml = `
        <div class="input-group mb-2">
            <span class="input-group-text">Episode ${episodeIdx + 1}</span>
            <input type="number" name="Seasons[${seasonIdx}].Episodes[${episodeIdx}].EpisodeNumber" class="form-control" placeholder="Nummer" required />
            <input type="text" name="Seasons[${seasonIdx}].Episodes[${episodeIdx}].Title" class="form-control" placeholder="Titel" required />
            <input type="text" name="Seasons[${seasonIdx}].Episodes[${episodeIdx}].Description" class="form-control" placeholder="Beschreibung" />
            <button type="button" class="btn btn-danger" onclick="this.closest('.input-group').remove()">Löschen</button>
        </div>
    `;
    episodesContainer.insertAdjacentHTML('beforeend', epHtml);
}
