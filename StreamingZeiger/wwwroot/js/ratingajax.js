// Zusatz: Bewertung via AJAX

 document.addEventListener('DOMContentLoaded', function () {
    const ratingBtn = document.getElementById('ratingBtn');
    if (!ratingBtn) return;

    ratingBtn.addEventListener('click', function () {
        const score = parseInt(document.getElementById('ratingSelect').value);
        const mediaId = parseInt(this.getAttribute('data-media-id'));

        // CSRF Token
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        fetch('/Rating/AddAjax', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ mediaItemId: mediaId, score: score })
        })
            .then(response => {
                if (!response.ok) throw new Error('Network response was not ok');
                return response.json();
            })
            .then(data => {
                // User-Bewertung aktualisieren
                let userStarsHtml = '';
                for (let i = 1; i <= 5; i++) {
                    userStarsHtml += i <= data.userScore ? '<i class="bi bi-star-fill"></i>' : '<i class="bi bi-star"></i>';
                }
                document.getElementById('userStars').innerHTML = userStarsHtml;
                document.getElementById('userScore').innerText = `${data.userScore} / 5`;

                // Community-Bewertung aktualisieren
                let avgStarsHtml = '';
                for (let i = 1; i <= 5; i++) {
                    avgStarsHtml += i <= data.average ? '<i class="bi bi-star-fill"></i>' : '<i class="bi bi-star"></i>';
                }
                document.getElementById('averageStars').innerHTML = avgStarsHtml;
                document.getElementById('voteCount').innerText = `(${data.votes} Stimmen)`;
            })
            .catch(error => {
                console.error('Error:', error);
            });
    });
});
