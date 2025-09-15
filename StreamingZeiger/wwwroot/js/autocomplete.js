document.addEventListener("DOMContentLoaded", function () {
    const searchBox = document.getElementById("searchBox");
    const autocompleteList = document.getElementById("autocompleteList");

    // jedes Mal, wenn eine Taste gedrückt wird
    searchBox.addEventListener("input", function () {
        const query = this.value.trim();

        if (!query) {
            autocompleteList.innerHTML = "";
            return;
        }

        fetch(`/Movies/Autocomplete?term=${encodeURIComponent(query)}`)
            .then(response => response.json())
            .then(data => {
                autocompleteList.innerHTML = "";

                data.forEach(item => {
                    const li = document.createElement("li");
                    li.textContent = item;
                    li.classList.add("list-group-item", "list-group-item-action");
                    li.style.cursor = "pointer";

                    // Klick auf einen Vorschlag
                    li.addEventListener("click", function () {
                        searchBox.value = item;
                        autocompleteList.innerHTML = "";
                        searchBox.form.submit();
                    });

                    autocompleteList.appendChild(li);
                });
            });
    });

    // Klick außerhalb schließt die Liste
    document.addEventListener("click", function (e) {
        if (!searchBox.contains(e.target)) {
            autocompleteList.innerHTML = "";
        }
    });
});
