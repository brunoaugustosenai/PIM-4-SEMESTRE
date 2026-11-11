(() => {
  const gtinButton = document.getElementById('generateGtinButton');
  const gtinInput = document.getElementById('CodigoBarras');

  gtinButton?.addEventListener('click', async () => {
    const url = gtinButton.dataset.url;
    if (!url || !gtinInput) return;
    gtinButton.disabled = true;
    const original = gtinButton.textContent;
    gtinButton.textContent = 'Gerando...';
    try {
      const response = await fetch(url, { headers: { 'Accept': 'application/json' } });
      if (!response.ok) throw new Error('Falha ao gerar GTIN-13.');
      const data = await response.json();
      gtinInput.value = data.gtin ?? '';
      gtinInput.dispatchEvent(new Event('change', { bubbles: true }));
    } catch (error) {
      alert(error?.message ?? 'Não foi possível gerar o GTIN-13.');
    } finally {
      gtinButton.disabled = false;
      gtinButton.textContent = original;
    }
  });

  const dropzone = document.getElementById('productImageDropzone');
  const fileInput = document.getElementById('productImageInput');
  const preview = document.getElementById('productImagePreview');
  if (!dropzone || !fileInput || !preview) return;

  const showPreview = (file) => {
    if (!file || !file.type.startsWith('image/')) return;
    const url = URL.createObjectURL(file);
    preview.src = url;
    preview.classList.remove('is-hidden');
    preview.onload = () => URL.revokeObjectURL(url);
  };

  const acceptFiles = (files) => {
    if (!files?.length) return;
    const file = files[0];
    if (!file.type.startsWith('image/')) {
      alert('Selecione uma imagem JPG, PNG ou WEBP.');
      return;
    }
    const transfer = new DataTransfer();
    transfer.items.add(file);
    fileInput.files = transfer.files;
    showPreview(file);
  };

  dropzone.addEventListener('click', (event) => {
    if (event.target !== fileInput) fileInput.click();
  });
  dropzone.addEventListener('keydown', (event) => {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      fileInput.click();
    }
  });
  fileInput.addEventListener('change', () => showPreview(fileInput.files?.[0]));

  ['dragenter', 'dragover'].forEach(name => dropzone.addEventListener(name, event => {
    event.preventDefault();
    dropzone.classList.add('is-dragging');
  }));
  ['dragleave', 'drop'].forEach(name => dropzone.addEventListener(name, event => {
    event.preventDefault();
    dropzone.classList.remove('is-dragging');
  }));
  dropzone.addEventListener('drop', event => acceptFiles(event.dataTransfer?.files));
})();
