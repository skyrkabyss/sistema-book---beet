// BiblioBeat: lector PDF renderizado en canvas sin botones de descargar/imprimir.
// PDF.js es software libre de Mozilla; se carga desde CDN cuando está disponible.
// En modo desconectado usa el lector del navegador con el toolbar oculto.
export async function initPdfReader() {
const root = document.getElementById('bookPdfViewer');
if (root) {
  const source = root.dataset.pdfUrl;
  const canvas = document.getElementById('pdfCanvas');
  const wrap = root.querySelector('.pdf-canvas-wrap');
  const status = document.getElementById('pdfPageStatus');
  const error = document.getElementById('pdfError');
  const prev = document.getElementById('pdfPrev');
  const next = document.getElementById('pdfNext');
  const fallback = document.getElementById('pdfReaderFallback');
  let documentPdf = null;
  let current = 1;
  let rendering = false;
  let pending = null;
  const say = (message) => { status.textContent = message; };
  const draw = async (pageNumber) => {
    if (!documentPdf) return;
    if (rendering) { pending = pageNumber; return; }
    rendering = true;
    try {
      const page = await documentPdf.getPage(pageNumber);
      const availableWidth = Math.max(260, Math.min(900, wrap.clientWidth - 28));
      const initial = page.getViewport({ scale: 1 });
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      const viewport = page.getViewport({ scale: availableWidth / initial.width });
      const context = canvas.getContext('2d');
      canvas.width = Math.floor(viewport.width * dpr);
      canvas.height = Math.floor(viewport.height * dpr);
      canvas.style.width = `${Math.floor(viewport.width)}px`;
      canvas.style.height = `${Math.floor(viewport.height)}px`;
      await page.render({ canvasContext: context, viewport,
        transform: dpr === 1 ? null : [dpr, 0, 0, dpr, 0, 0] }).promise;
      current = pageNumber;
      say(`Página ${current} de ${documentPdf.numPages}`);
      prev.disabled = current <= 1;
      next.disabled = current >= documentPdf.numPages;
    } catch (e) {
      console.error('Error renderizando PDF', e);
      error.hidden = false;
      error.textContent = 'No fue posible mostrar esta página. Intenta de nuevo.';
    } finally {
      rendering = false;
      if (pending !== null) { const following = pending; pending = null; void draw(following); }
    }
  };
  prev.addEventListener('click', () => { if (current > 1) void draw(current - 1); });
  next.addEventListener('click', () => { if (documentPdf && current < documentPdf.numPages) void draw(current + 1); });
  try {
    const pdfjsLib = await import('https://cdnjs.cloudflare.com/ajax/libs/pdf.js/4.10.38/pdf.min.mjs');
    pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/4.10.38/pdf.worker.min.mjs';
    const loading = pdfjsLib.getDocument({ url: source, withCredentials: true });
    documentPdf = await loading.promise;
    await draw(1);
  } catch (e) {
    console.warn('Canvas no disponible. Se usa lector integrado del navegador.', e);
    say('Lectura con visor del navegador');
    wrap.hidden = true;
    prev.hidden = true;
    next.hidden = true;
    fallback.hidden = false;
    fallback.src = source + '#toolbar=0&navpanes=0&scrollbar=0';
    error.hidden = false;
    error.textContent = 'Si el navegador está sin Internet, se utiliza su visor de PDF. Algunos navegadores pueden mostrar controles propios.';
  }
}

}
