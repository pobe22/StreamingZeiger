document.addEventListener("DOMContentLoaded", () => {
        document.querySelectorAll(".watchlist-toggle").forEach(btn => {
            btn.addEventListener("click", async (e) => {
                e.preventDefault();

                const mediaId = btn.dataset.mediaId;
                const inWatchlist = btn.dataset.inWatchlist === "true";

                //2. Ajax Frontend
                const response = await fetch("/Watchlist/Toggle", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]')?.value
                    },
                    body: JSON.stringify({ mediaItemId: mediaId })
                });

                const result = await response.json();

                // 3. Button-Anzeige
                if (result.success) {
                    btn.dataset.inWatchlist = result.added;
                    const icon = btn.querySelector("i");
                    const text = btn.querySelector("span");

                    if (result.added) {
                        btn.classList.remove("btn-success");
                        btn.classList.add("btn-danger");
                        icon.classList.remove("bi-bookmark-heart");
                        icon.classList.add("bi-bookmark-heart-fill");
                        text.textContent = "Aus Watchlist entfernen";
                    } else {
                        btn.classList.remove("btn-danger");
                        btn.classList.add("btn-success");
                        icon.classList.remove("bi-bookmark-heart-fill");
                        icon.classList.add("bi-bookmark-heart");
                        text.textContent = "Zur Watchlist hinzufügen";
                    }
                } else {
                    alert(result.message || "Fehler beim Aktualisieren der Watchlist.");
                }
            });
        });
});

