/**
 * Theme toggle logic (TYP-210)
 * Handles switching between dark and light themes and persisting the preference.
 */

function toggleTheme() {
    const html = document.documentElement;
    if (html.getAttribute('data-theme') === 'light') {
        html.removeAttribute('data-theme');
        localStorage.removeItem('theme');
    } else {
        html.setAttribute('data-theme', 'light');
        localStorage.setItem('theme', 'light');
    }
}

function applyStoredTheme() {
    if (localStorage.getItem('theme') === 'light') {
        document.documentElement.setAttribute('data-theme', 'light');
    }
}

if (typeof module !== 'undefined' && module.exports) {
    module.exports = { toggleTheme, applyStoredTheme };
}
