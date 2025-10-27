document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".movie-details-btn").forEach(btn => {
        btn.addEventListener("click", async function () {
            const movieId = this.dataset.id;
            const modalContainer = document.getElementById("movieDetailsContainer");

            const response = await fetch(`/Movies/DetailsPartial/${movieId}`);
            if (response.ok) {
                const html = await response.text();
                modalContainer.innerHTML = html;
                const modal = new bootstrap.Modal(document.getElementById("movieDetailsModal"));
                modal.show();
            } else {
                alert("Details konnten nicht geladen werden.");
            }
        });
    });
});
