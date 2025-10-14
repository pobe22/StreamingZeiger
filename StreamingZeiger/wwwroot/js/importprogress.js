document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('importForm');
    form.addEventListener('submit', async function (e) {
        e.preventDefault();

        const formData = new FormData(form);

        const progressBarContainer = document.querySelector('.progress');
        const progressBar = document.querySelector('.progress-bar');
        const resultDiv = document.getElementById('importResult');

        progressBarContainer.style.display = 'block';
        progressBar.style.width = '0%';
        progressBar.textContent = '0%';
        resultDiv.innerHTML = '';

        const response = await fetch('@Url.Action("ImportMultipleAjax","Admin")', {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            resultDiv.innerHTML = 'Fehler beim Import.';
            return;
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder('utf-8');
        let buffer = '';

        while (true) {
            const { done, value } = await reader.read();
            if (done) break;

            buffer += decoder.decode(value, { stream: true });

            let lines = buffer.split('\n');
            buffer = lines.pop(); // letzte Zeile unvollständig

            for (const line of lines) {
                const progressMatch = line.match(/^PROGRESS:(\d+)/);
                if (progressMatch) {
                    const percent = parseInt(progressMatch[1]);
                    progressBar.style.width = percent + '%';
                    progressBar.textContent = percent + '%';
                } else {
                    resultDiv.innerHTML += line + '<br>';
                }
            }
        }

        if (buffer) resultDiv.innerHTML += buffer + '<br>'; // letzte Zeile

        progressBar.style.width = '100%';
        progressBar.textContent = '100%';
        resultDiv.innerHTML += '<br>Import abgeschlossen!';
    });
});
