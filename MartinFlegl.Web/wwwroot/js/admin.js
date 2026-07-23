window.getApiUrl = function(endpoint) {
    var clean = endpoint.startsWith('/') ? endpoint.substring(1) : endpoint;
    var path = window.location.pathname;
    if (!path.endsWith('/')) {
        if (path.endsWith('MartinFlegl')) {
            path += '/';
        } else {
            path = path.substring(0, path.lastIndexOf('/') + 1);
        }
    }
    return path + clean;
};

document.addEventListener('DOMContentLoaded', () => {
    // 1. Načíst uložený obsah z DB a přepsat data-editable prvky
    loadDynamicContent();

    // 2. Kontrola přihlášení
    checkAdminSession();

    // 3. Klávesová zkratka Ctrl + Shift + L pro přihlášení
    document.addEventListener('keydown', (e) => {
        if (e.ctrlKey && e.shiftKey && (e.key === 'L' || e.key === 'l')) {
            e.preventDefault();
            const modal = document.getElementById('admin-modal');
            if (modal) {
                modal.style.display = 'flex';
                const userInput = document.getElementById('admin-user');
                if (userInput) userInput.focus();
            }
        }
    });

    // 3. Event listenery pro login modal
    const loginTrigger = document.getElementById('admin-login-trigger');
    const modal = document.getElementById('admin-modal');
    const modalClose = document.getElementById('admin-modal-close');
    const loginForm = document.getElementById('admin-login-form');

    if (loginTrigger && modal) {
        loginTrigger.addEventListener('click', (e) => {
            e.preventDefault();
            modal.style.display = 'flex';
            document.getElementById('admin-user').focus();
        });
    }

    if (modalClose && modal) {
        modalClose.addEventListener('click', () => {
            modal.style.display = 'none';
        });
    }

    if (loginForm) {
        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const username = document.getElementById('admin-user').value.trim();
            const password = document.getElementById('admin-pass').value;
            const errorMsg = document.getElementById('admin-login-error');

            if (errorMsg) errorMsg.style.display = 'none';

            try {
                const res = await fetch(getApiUrl('api/admin/login'), {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ username, password })
                });

                const data = await res.json();
                if (res.ok && data.success) {
                    localStorage.setItem('adminToken', data.token);
                    localStorage.setItem('adminUser', data.username);
                    if (modal) modal.style.display = 'none';
                    loginForm.reset();
                    activateAdminMode(data.username);
                } else {
                    if (errorMsg) {
                        errorMsg.textContent = data.message || 'Neplatné přihlašovací údaje.';
                        errorMsg.style.display = 'block';
                    }
                }
            } catch (err) {
                if (errorMsg) {
                    errorMsg.textContent = 'Chyba připojení k serveru.';
                    errorMsg.style.display = 'block';
                }
            }
        });
    }

    // Logout
    const logoutBtn = document.getElementById('admin-logout-btn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', () => {
            localStorage.removeItem('adminToken');
            localStorage.removeItem('adminUser');
            deactivateAdminMode();
        });
    }

    // Save Changes
    const saveBtn = document.getElementById('admin-save-btn');
    if (saveBtn) {
        saveBtn.addEventListener('click', saveContentChanges);
    }
});

let modifiedKeys = new Set();

async function loadDynamicContent() {
    try {
        const res = await fetch(getApiUrl('api/content'));
        if (res.ok) {
            const data = await res.json();
            Object.keys(data).forEach(key => {
                const elements = document.querySelectorAll(`[data-editable="${key}"]`);
                elements.forEach(el => {
                    if (key === 'coop.title') {
                        const logoHtml = `<span class="title-logo-wrapper"><img src="img/ffg-icon-transparent.png?v=20260723_600" alt="FFG Logo" class="title-inline-logo"></span>`;
                        let val = data[key];
                        if (val && !val.includes('title-logo-wrapper')) {
                            if (val.includes('Flegl Finance')) {
                                val = val.replace('Flegl Finance', logoHtml + 'Flegl Finance');
                            } else {
                                val = logoHtml + val;
                            }
                        }
                        el.innerHTML = val;
                    } else {
                        el.innerHTML = data[key];
                    }
                });
            });
        }
    } catch (e) {
        console.warn('Could not load dynamic content:', e);
    }
}

function checkAdminSession() {
    const token = localStorage.getItem('adminToken');
    const user = localStorage.getItem('adminUser');
    if (token && user) {
        activateAdminMode(user);
    }
}

function activateAdminMode(username) {
    document.body.classList.add('admin-mode');
    
    const toolbar = document.getElementById('admin-toolbar');
    const userDisplay = document.getElementById('admin-username-display');
    if (toolbar) toolbar.style.display = 'flex';
    if (userDisplay) userDisplay.textContent = `Přihlášen: ${username}`;

    const editables = document.querySelectorAll('[data-editable]');
    editables.forEach(el => {
        el.setAttribute('contenteditable', 'true');
        el.setAttribute('spellcheck', 'false');

        el.addEventListener('input', () => {
            const key = el.getAttribute('data-editable');
            if (key) {
                modifiedKeys.add(key);
                el.classList.add('is-modified');
                updateAdminToolbarStatus();
            }
        });
    });
}

function deactivateAdminMode() {
    document.body.classList.remove('admin-mode');
    const toolbar = document.getElementById('admin-toolbar');
    if (toolbar) toolbar.style.display = 'none';

    const editables = document.querySelectorAll('[data-editable]');
    editables.forEach(el => {
        el.removeAttribute('contenteditable');
        el.classList.remove('is-modified');
    });

    modifiedKeys.clear();
    location.reload();
}

function updateAdminToolbarStatus() {
    const countPill = document.getElementById('admin-unsaved-count');
    const saveBtn = document.getElementById('admin-save-btn');

    if (modifiedKeys.size > 0) {
        if (countPill) {
            countPill.textContent = `${modifiedKeys.size} neuložených změn`;
            countPill.style.display = 'inline-block';
        }
        if (saveBtn) saveBtn.disabled = false;
    } else {
        if (countPill) countPill.style.display = 'none';
        if (saveBtn) saveBtn.disabled = true;
    }
}

async function saveContentChanges() {
    const token = localStorage.getItem('adminToken');
    if (!token || modifiedKeys.size === 0) return;

    const items = {};
    modifiedKeys.forEach(key => {
        const el = document.querySelector(`[data-editable="${key}"]`);
        if (el) {
            items[key] = el.innerHTML.trim();
        }
    });

    const saveBtn = document.getElementById('admin-save-btn');
    if (saveBtn) {
        saveBtn.disabled = true;
        saveBtn.textContent = '⏳ Ukládám...';
    }

    try {
        const res = await fetch(getApiUrl('api/content'), {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ token, items })
        });

        const data = await res.json();
        if (res.ok && data.success) {
            modifiedKeys.clear();
            document.querySelectorAll('[data-editable].is-modified').forEach(el => {
                el.classList.remove('is-modified');
            });
            updateAdminToolbarStatus();
            alert(`✅ ${data.updatedCount} textů bylo úspěšně uloženo do databáze.`);
        } else {
            alert('❌ Chyba při ukládání: ' + (data.message || 'Neautorizovaný přístup.'));
        }
    } catch (e) {
        alert('❌ Chyba komunikace se serverem.');
    } finally {
        if (saveBtn) {
            saveBtn.textContent = '💾 Uložit změny';
            updateAdminToolbarStatus();
        }
    }
}
