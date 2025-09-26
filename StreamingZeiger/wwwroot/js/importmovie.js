document.getElementById('importTmdbMovie').addEventListener('click', async () => {
    const tmdbId = document.getElementById('tmdbId').value;
    if (!tmdbId) return alert('Bitte TMDb ID eingeben');

    const response = await fetch(`/Admin/ImportFromTmdb?tmdbId=${tmdbId}&type=movie`);
    if (!response.ok) return alert('Fehler beim Abrufen von TMDb');

    const movie = await response.json();

    // Formularfelder befüllen
    document.getElementById('Title').value = movie.title;
    document.getElementById('OriginalTitle').value = movie.originalTitle;
    document.getElementById('Description').value = movie.description;
    document.getElementById('Year').value = movie.year;
    document.getElementById('Director').value = movie.director;
    document.getElementById('PosterFile').value = movie.posterFile;
    document.getElementById('TrailerUrl').value = movie.trailerUrl;
    document.getElementById('CastCsv').value = movie.cast.join(', ');
    document.getElementById('GenreCsv').value = movie.genres.join(', ');
});