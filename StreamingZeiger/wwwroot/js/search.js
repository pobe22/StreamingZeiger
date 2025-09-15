// 1. Toggle Suchformular ein-/ausblenden
document.addEventListener("DOMContentLoaded", () => {
    const toggleBtn = document.getElementById("toggleSearchBtn");
    const searchForm = document.getElementById("searchForm");

    if (toggleBtn && searchForm) {
        toggleBtn.addEventListener("click", () => {
            if (searchForm.style.display === "none" || searchForm.style.display === "") {
                searchForm.style.display = "block";
                toggleBtn.textContent = "Filter ausblenden";
            } else {
                searchForm.style.display = "none";
                toggleBtn.textContent = "Filter einblenden";
            }
        });
    }
});

// 2. OnLoad-Event (Reminder)
window.onload = function () {
    alert("Reminder: bereits gewählte Suchfilter über Session setzen");
};

// 3. Fixe Suchfilter setzen
document.addEventListener("DOMContentLoaded", () => {
    const categoryFilter = document.getElementById("categoryFilter");
    const typeFilter = document.getElementById("typeFilter");

    if (categoryFilter) categoryFilter.value = "Action";
});

// 4. Eingabeprüfung beim Senden
document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("searchForm");
    const errorMsg = document.getElementById("errorMsg");

    if (form) {
        form.addEventListener("submit", (e) => {
            const category = document.getElementById("categoryFilter")?.value;
            const type = document.getElementById("typeFilter")?.value;

            if ((!category || category === "") && (!type || type === "")) {
                e.preventDefault();
                if (errorMsg) {
                    errorMsg.textContent = "Bitte einen Suchfilter setzen";
                    errorMsg.style.display = "block";
                }
            } else {
                if (errorMsg) errorMsg.style.display = "none";
            }
        });
    }
});

// 5. Ergebnisliste filtern (nach Titel)
document.addEventListener("DOMContentLoaded", () => {
    const titleSearch = document.getElementById("titleSearch");
    const movies = document.querySelectorAll("#movieList .movie");

    if (titleSearch && movies.length > 0) {
        titleSearch.addEventListener("input", function () {
            const searchValue = this.value.toLowerCase();

            movies.forEach(movie => {
                if (movie.textContent.toLowerCase().includes(searchValue)) {
                    movie.style.display = "";
                } else {
                    movie.style.display = "none";
                }
            });
        });
    }
});
