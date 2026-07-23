document.addEventListener('DOMContentLoaded', () => {
    const isMobile = window.innerWidth <= 768 || ('ontouchstart' in window);

    // 1. WOW Effect: Interactive Canvas Particles Background (Optimized for Mobile)
    initParticlesCanvas(isMobile);

    // 2. WOW Effect: Initialize VanillaTilt (Disabled on small mobile touchscreens to preserve smooth scroll)
    if (typeof VanillaTilt !== 'undefined' && !isMobile) {
        const tiltElement = document.querySelector('.wow-tilt');
        if (tiltElement) {
            VanillaTilt.init(tiltElement, {
                max: 5,
                speed: 400,
                glare: true,
                "max-glare": 0.15,
                scale: 1.01
            });
        }
    }

    // 3. Form Handling & Mobile Haptics
    const form = document.getElementById('insuranceForm');
    const submitBtn = document.getElementById('submitBtn');
    const btnText = submitBtn.querySelector('.btn-text');
    const loader = submitBtn.querySelector('.loader');
    const messageDiv = document.getElementById('formMessage');

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        // Haptická odezva na mobilu při stisku tlačítka (vibrace)
        if ('vibrate' in navigator) {
            try { navigator.vibrate(25); } catch (vEx) {}
        }

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
            topic: `Povinné ručení | SPZ/VIN: ${data.spz} | Vozidlo: ${data.brand} | Datum narození / IČO: ${data.idNumber} | PSČ: ${data.zipCode}`
        };

        try {
            const response = await fetch('/MartinFlegl/api/leads', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(payload)
            });

            if (response.ok) {
                // Haptická odezva pro úspěšné odeslání (double-pulse)
                if ('vibrate' in navigator) {
                    try { navigator.vibrate([40, 50, 40]); } catch (vEx) {}
                }

                // WOW Effect: Confetti Explosion & Button Morph
                triggerConfetti();
                
                submitBtn.classList.add('success-state');
                btnText.textContent = 'Poptávka úspěšně odeslána! ✓';
                btnText.classList.remove('hidden');
                loader.classList.remove('active');

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
            
            submitBtn.disabled = false;
            btnText.textContent = 'Odeslat poptávku ✨';
            btnText.classList.remove('hidden');
            loader.classList.remove('active');
        }
    });
});

// Interactive Particle Mesh System (Mobile CPU & Battery Optimized)
function initParticlesCanvas(isMobile) {
    const canvas = document.getElementById('particlesCanvas');
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    
    let width = canvas.width = window.innerWidth;
    let height = canvas.height = window.innerHeight;
    
    window.addEventListener('resize', () => {
        width = canvas.width = window.innerWidth;
        height = canvas.height = window.innerHeight;
    });

    const particles = [];
    const particleCount = isMobile ? 18 : Math.min(Math.floor(width / 20), 45);

    for (let i = 0; i < particleCount; i++) {
        particles.push({
            x: Math.random() * width,
            y: Math.random() * height,
            vx: (Math.random() - 0.5) * (isMobile ? 0.5 : 0.8),
            vy: (Math.random() - 0.5) * (isMobile ? 0.5 : 0.8),
            radius: Math.random() * 2 + 1,
            color: Math.random() > 0.5 ? 'rgba(59, 130, 246, ' : 'rgba(16, 185, 129, '
        });
    }

    function animate() {
        ctx.clearRect(0, 0, width, height);
        
        for (let i = 0; i < particles.length; i++) {
            const p = particles[i];
            p.x += p.vx;
            p.y += p.vy;

            if (p.x < 0 || p.x > width) p.vx *= -1;
            if (p.y < 0 || p.y > height) p.vy *= -1;

            ctx.beginPath();
            ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
            ctx.fillStyle = p.color + '0.6)';
            ctx.fill();

            // Connect nearby particles with glowing lines
            const maxDist = isMobile ? 100 : 130;
            for (let j = i + 1; j < particles.length; j++) {
                const p2 = particles[j];
                const dx = p.x - p2.x;
                const dy = p.y - p2.y;
                const dist = Math.sqrt(dx * dx + dy * dy);

                if (dist < maxDist) {
                    ctx.beginPath();
                    ctx.moveTo(p.x, p.y);
                    ctx.lineTo(p2.x, p2.y);
                    const alpha = (1 - dist / maxDist) * 0.22;
                    ctx.strokeStyle = p.color + alpha + ')';
                    ctx.lineWidth = 0.8;
                    ctx.stroke();
                }
            }
        }
        requestAnimationFrame(animate);
    }
    animate();
}

// Confetti Burst Trigger
function triggerConfetti() {
    if (typeof confetti === 'function') {
        confetti({
            particleCount: 70,
            spread: 60,
            origin: { y: 0.65 },
            colors: ['#10b981', '#3b82f6', '#34d399', '#60a5fa']
        });
    }
}
