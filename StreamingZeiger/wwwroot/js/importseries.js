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
                const response = await fetch(`/Admin/ImportFromTmdb?tmdbId=${tmdbId}&type=series`);
                if (!response.ok) throw new Error("Fehler beim Abrufen der Daten.");

                const series = await response.json();

                // Basisfelder automatisch füllen
                document.getElementById("Title").value = series.title || "";
                document.getElementById("OriginalTitle").value = series.originalTitle || "";
                document.getElementById("StartYear").value = series.startYear || "";
                document.getElementById("EndYear").value = series.endYear || "";
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

                // Staffeln und Episoden in die View einfügen
                const seasonsContainer = document.getElementById("seasonsContainer");
                seasonsContainer.innerHTML = ""; // vorherige Einträge löschen
                let seasonIndex = 0;

                if (series.seasons && Array.isArray(series.seasons)) {
                    series.seasons.forEach((season, seasonIdx) => {
                        const seasonCard = document.createElement("div");
                        seasonCard.classList.add("card", "mb-3", "season-card", "p-3");
                        seasonCard.innerHTML = `
            <h5>Staffel ${season.seasonNumber}
                <button type="button" class="btn btn-sm btn-danger float-end" onclick="this.closest('.season-card').remove()">Löschen</button>
            </h5>
            <input type="number" name="Seasons[${seasonIdx}].SeasonNumber" class="form-control mb-2" value="${season.seasonNumber}" required />
            <textarea name="Seasons[${seasonIdx}].Description" class="form-control mb-2">${season.description || ""}</textarea>
            <button type="button" class="btn btn-sm btn-outline-secondary mb-2" onclick="addEpisode(${seasonIdx})">Episode hinzufügen</button>
            <div class="episodesContainer"></div>
        `;
                        seasonsContainer.appendChild(seasonCard);

                        // Episoden hinzufügen
                        if (season.episodes && Array.isArray(season.episodes)) {
                            const episodesContainer = seasonCard.querySelector(".episodesContainer");
                            season.episodes.forEach((ep, epIdx) => {
                                const epHtml = `
                    <div class="input-group mb-2">
                        <span class="input-group-text">Episode ${ep.episodeNumber}</span>
                        <input type="number" name="Seasons[${seasonIdx}].Episodes[${epIdx}].EpisodeNumber" class="form-control" value="${ep.episodeNumber}" required />
                        <input type="text" name="Seasons[${seasonIdx}].Episodes[${epIdx}].Title" class="form-control" value="${ep.title}" required />
                        <input type="text" name="Seasons[${seasonIdx}].Episodes[${epIdx}].Description" class="form-control" value="${ep.description || ""}" />
                        <button type="button" class="btn btn-danger" onclick="this.closest('.input-group').remove()">Löschen</button>
                    </div>
                `;
                                episodesContainer.insertAdjacentHTML("beforeend", epHtml);
                            });
                        }
                    });
                }


                alert("Daten erfolgreich importiert!");
            } catch (err) {
                console.error(err);
                alert("Fehler beim Importieren der Serie.");
            }
        });
    }
});

// Die Funktion addEpisode muss global verfügbar sein
function addEpisode(seasonIdx) {
    const seasonCard = document.getElementsByClassName('season-card')[seasonIdx];
    const episodesContainer = seasonCard.querySelector('.episodesContainer');
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
