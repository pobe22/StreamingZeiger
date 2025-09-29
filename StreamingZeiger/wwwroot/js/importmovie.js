document.getElementById('importTmdbMovie').addEventListener('click', async () => {
    const tmdbId = document.getElementById('tmdbId').value;
    if (!tmdbId) return alert('Bitte TMDb ID eingeben');

    const region = "DE"; // optional
    const response = await fetch(`/Admin/ImportFromTmdb?tmdbId=${tmdbId}&type=movie&region=${region}`);
    if (!response.ok) return alert('Fehler beim Abrufen von TMDb');

    const movie = await response.json();

    // Formularfelder befüllen
    document.getElementById('Title').value = movie.title;
    document.getElementById('OriginalTitle').value = movie.originalTitle;
    document.getElementById('Description').value = movie.description;
    document.getElementById('DurationMinutes').value = movie.durationMinutes;
    document.getElementById('Year').value = movie.year;
    document.getElementById('Director').value = movie.director;
    document.getElementById('PosterFile').value = movie.posterFile;
    document.getElementById('TrailerUrl').value = movie.trailerUrl;
    document.getElementById('CastCsv').value = movie.cast.join(', ');
    document.getElementById('GenreCsv').value = movie.genres.join(', ');

    document.querySelectorAll('input[name="Services"]').forEach(cb => cb.checked = false);

    movie.services.forEach(s => {
        const checkbox = document.querySelector(`input[name="Services"][value="${s}"]`);
        if (checkbox) checkbox.checked = true;
    });
});