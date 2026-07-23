document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('insuranceForm');
    const submitBtn = document.getElementById('submitBtn');
    const btnText = submitBtn.querySelector('.btn-text');
    const loader = submitBtn.querySelector('.loader');
    const messageDiv = document.getElementById('formMessage');

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        // Získání dat z formuláře
        const formData = new FormData(form);
        const data = Object.fromEntries(formData.entries());
        
        // Změna UI tlačítka na načítání
        submitBtn.disabled = true;
        btnText.classList.add('hidden');
        loader.classList.add('active');
        messageDiv.classList.add('hidden');
        messageDiv.className = 'message';

        // Sestavení payloadu pro backendové API /MartinFlegl/api/leads
        const payload = {
            fullName: data.fullname,
            email: data.email,
            phone: data.phone,
            topic: `Povinné ručení | SPZ/VIN: ${data.spz} | Vozidlo: ${data.brand} | RČ/IČO: ${data.idNumber} | PSČ: ${data.zipCode}`
        };

        try {
            // Volání funkčního serverového API v subaplikaci MartinFlegl
            const response = await fetch('/MartinFlegl/api/leads', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(payload)
            });

            if (response.ok) {
                messageDiv.textContent = 'Poptávka byla úspěšně odeslána! Potvrzení jsme poslali na Váš e-mail.';
                messageDiv.classList.add('success');
                messageDiv.classList.remove('hidden');
                form.reset();
            } else {
                const errData = await response.json().catch(() => ({}));
                throw new Error(errData.message || 'Chyba při zpracování na serveru.');
            }
        } catch (error) {
            console.error('Chyba odesílání:', error);
            messageDiv.textContent = 'Došlo k chybě při odesílání poptávky: ' + error.message;
            messageDiv.classList.add('error');
            messageDiv.classList.remove('hidden');
        } finally {
            submitBtn.disabled = false;
            btnText.classList.remove('hidden');
            loader.classList.remove('active');
        }
    });
});
