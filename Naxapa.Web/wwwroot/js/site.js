(() => {
  const root = document.documentElement;
  const sidebar = document.getElementById('sidebar');
  const backdrop = document.getElementById('sidebarBackdrop');
  const menuButton = document.getElementById('menuButton');
  const themeButton = document.getElementById('themeButton');
  const appearanceButton = document.getElementById('appearanceButton');

  const setTheme = (theme) => {
    root.dataset.theme = theme;
    localStorage.setItem('naxapa-theme', theme);
    const icon = themeButton?.querySelector('.material-symbols-rounded');
    if (icon) icon.textContent = theme === 'dark' ? 'light_mode' : 'dark_mode';
  };

  const storedTheme = localStorage.getItem('naxapa-theme');
  const preferredDark = window.matchMedia?.('(prefers-color-scheme: dark)').matches;
  setTheme(storedTheme || (preferredDark ? 'dark' : 'light'));

  const toggleTheme = () => setTheme(root.dataset.theme === 'dark' ? 'light' : 'dark');
  themeButton?.addEventListener('click', toggleTheme);
  appearanceButton?.addEventListener('click', toggleTheme);

  const closeMenu = () => {
    sidebar?.classList.remove('open');
    backdrop?.classList.remove('show');
  };

  menuButton?.addEventListener('click', () => {
    sidebar?.classList.toggle('open');
    backdrop?.classList.toggle('show');
  });
  backdrop?.addEventListener('click', closeMenu);
  document.querySelectorAll('.nav-item').forEach(item => item.addEventListener('click', closeMenu));

  let toastTimer;
  const showToast = (message) => {
    document.querySelector('.toast')?.remove();
    const toast = document.createElement('div');
    toast.className = 'toast';
    toast.textContent = message;
    document.body.appendChild(toast);
    requestAnimationFrame(() => toast.classList.add('show'));
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => {
      toast.classList.remove('show');
      setTimeout(() => toast.remove(), 200);
    }, 2800);
  };

  document.querySelectorAll('[data-toast]').forEach(button => {
    button.addEventListener('click', () => showToast(button.dataset.toast));
  });
})();
