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

//// 2. OnLoad-Event (Reminder)
//window.onload = function () {
//    alert("Reminder: bereits gewählte Suchfilter über Session setzen");
//};

//// 3. Fixe Suchfilter setzen
//document.addEventListener("DOMContentLoaded", () => {
//    // 1. Text- und Zahlenfelder vorbelegen
//    const textNumberDefaults = [
//        { selector: 'input[name="Query"]', defaultValue: "Star Wars" },
//        { selector: 'input[name="YearFrom"]', defaultValue: "2000" },
//        { selector: 'input[name="YearTo"]', defaultValue: "2025" }
//    ];

//    textNumberDefaults.forEach(f => {
//        const element = document.querySelector(f.selector);
//        if (element) {
//            if (!element.id) element.id = element.name + "Filter";
//            element.value = f.defaultValue;
//        }
//    });

//    // 2. Select-Felder vorbelegen (nach sichtbarem Text)
//    const selectDefaults = [
//        { selector: 'select[name="Genre"]', defaultText: "Action" },
//        { selector: 'select[name="Service"]', defaultText: "Netflix" },
//        { selector: 'select[name="MinRating"]', defaultText: "7+" }
//    ];

//    selectDefaults.forEach(f => {
//        const element = document.querySelector(f.selector);
//        if (element) {
//            if (!element.id) element.id = element.name + "Filter";

//            const option = Array.from(element.options)
//                .find(o => o.text.trim() === f.defaultText);

//            if (option) option.selected = true;
//            else element.selectedIndex = 0; // Fallback: erste Option
//        }
//    });
//});

//// 4. Eingabeprüfung beim Senden
//document.addEventListener("DOMContentLoaded", () => {
//    const form = document.getElementById("searchForm");
//    const errorMsg = document.getElementById("errorMsg");

//    if (form) {
//        form.addEventListener("submit", (e) => {
//            const category = document.getElementById("categoryFilter")?.value;
//            const type = document.getElementById("typeFilter")?.value;

//            if ((!category || category === "") && (!type || type === "")) {
//                e.preventDefault();
//                if (errorMsg) {
//                    errorMsg.textContent = "Bitte einen Suchfilter setzen";
//                    errorMsg.style.display = "block";
//                }
//            } else {
//                if (errorMsg) errorMsg.style.display = "none";
//            }
//        });
//    }
//});

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
