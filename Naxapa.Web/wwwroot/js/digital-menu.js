(() => {
  const input = document.getElementById('digitalMenuSearch');
  if (!input) return;
  const products = [...document.querySelectorAll('[data-menu-product]')];
  const categories = [...document.querySelectorAll('[data-menu-category]')];

  const normalize = value => (value ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase();

  input.addEventListener('input', () => {
    const term = normalize(input.value.trim());
    products.forEach(card => {
      const visible = !term || normalize(card.dataset.search).includes(term);
      card.hidden = !visible;
    });
    categories.forEach(category => {
      category.hidden = !category.querySelector('[data-menu-product]:not([hidden])');
    });
  });
})();
