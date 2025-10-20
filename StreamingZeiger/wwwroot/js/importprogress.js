document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('importForm');
    const progressBarContainer = document.querySelector('.progress');
    const progressBar = document.querySelector('.progress-bar');

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        const submitButton = form.querySelector('button[type="submit"]');
        submitButton.disabled = true;

        const formData = new FormData(form);
        progressBarContainer.style.display = 'block';
        progressBar.style.width = '0%';
        progressBar.textContent = '0/0 Medien importiert';

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: formData
            });

            if (!response.ok) {
                alert('Fehler beim Import.');
                return;
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder('utf-8');
            let buffer = '';
            let total = 0;

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                buffer += decoder.decode(value, { stream: true });
                const lines = buffer.split('\n');
                buffer = lines.pop(); // letzte Zeile unvollständig

                for (const line of lines) {
                    // PROGRESS: x / total
                    const match = line.match(/^PROGRESS:(\d+)\/(\d+)/);
                    if (match) {
                        const imported = parseInt(match[1]);
                        total = parseInt(match[2]);

                        const percent = total ? Math.round((imported / total) * 100) : 0;
                        progressBar.style.width = percent + '%';
                        progressBar.textContent = `${imported} von ${total} Medien importiert`;
                    }
                }
            }

            // Vollständig
            progressBar.style.width = '100%';
            progressBar.textContent = `${total} von ${total} Medien importiert`;

            setTimeout(() => location.reload(), 500);

        } catch (err) {
            alert('Fehler beim Import: ' + err.message);
        } finally {
            submitButton.disabled = false;
        }
    });
});
