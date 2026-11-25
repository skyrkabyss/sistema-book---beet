/* BiblioBeat 6: reproductor persistente + navegación entre Música/Libros sin recargar.
 * Solo se usa API oficial de YouTube en iframe y archivos locales/Commons autorizados.
 * Si la navegación suave falla, el enlace tradicional sigue funcionando.
 */
(() => {
  'use strict';
  const player = document.getElementById('bbGlobalPlayer');
  const audio = document.getElementById('bbContinuousAudio');
  const iframe = document.getElementById('bbContinuousYouTube');
  const videoBox = document.getElementById('bbYoutubeFrameBox');
  const title = document.getElementById('bbNowPlaying');
  const source = document.getElementById('bbNowSource');
  const drawer = document.getElementById('bbPlaylistDrawer');
  const scrim = document.getElementById('bbPlaylistScrim');
  const playlistSelect = document.getElementById('bbPlaylistSelect');
  const songs = document.getElementById('bbPlaylistSongs');
  const drawerStatus = document.getElementById('bbPlaylistStatus');
  const prev = document.getElementById('bbPlayerPrevious');
  const next = document.getElementById('bbPlayerNext');
  const playback = { type: null, title: '', artist: '', playlist: [], index: -1 };
  let playlists = [];
  let navigating = false;

  function updateNavButtons() {
    const enabled = playback.playlist.length > 1 && playback.index >= 0;
    prev.disabled = !enabled;
    next.disabled = !enabled;
  }
  function showTrack(name, from) {
    title.textContent = name || 'Música de Book & Beet';
    source.textContent = from || 'Escucha mientras lees';
    player.hidden = false;
    document.body.classList.add('bb-is-playing');
  }
  function stop() {
    audio.pause();
    audio.removeAttribute('src');
    audio.load();
    iframe.src = 'about:blank';
    videoBox.hidden = true;
    player.hidden = true;
    playback.type = null;
    playback.index = -1;
    playback.playlist = [];
    document.body.classList.remove('bb-is-playing');
    updateNavButtons();
  }
  function playTrack(track, { preserveQueue = false } = {}) {
    if (!track) return;
    const kind = track.modo || track.mode;
    if (kind !== 'audio' && kind !== 'youtube') return;
    if (!preserveQueue) { playback.playlist = []; playback.index = -1; }
    const trackTitle = track.titulo || track.title || 'Canción';
    const artist = track.artista || track.artist || (kind === 'youtube' ? 'YouTube' : 'Book & Beet');
    playback.type = kind;
    playback.title = trackTitle;
    playback.artist = artist;
    showTrack(trackTitle, artist);
    if (kind === 'audio') {
      const path = track.audioUrl;
      if (!path || !/^\//.test(path) && !/^https:\/\//i.test(path)) return;
      iframe.src = 'about:blank';
      videoBox.hidden = true;
      audio.hidden = false;
      audio.src = path;
      audio.play().catch(() => {
        source.textContent = `${artist} · Pulsa ▶ en el reproductor para iniciar`;
      });
    } else {
      const id = track.youtubeId;
      if (!/^[A-Za-z0-9_-]{11}$/.test(id || '')) return;
      audio.pause();
      audio.removeAttribute('src');
      audio.load();
      audio.hidden = true;
      videoBox.hidden = false;
      // iframe OFICIAL; no se extrae audio ni se elimina la reproducción visual.
      iframe.src = `https://www.youtube-nocookie.com/embed/${encodeURIComponent(id)}?rel=0&autoplay=1&playsinline=1`;
    }
    updateNavButtons();
  }
  function playFromQueue(index) {
    const row = playback.playlist[index];
    if (!row || row.modo === 'no-disponible') return;
    playback.index = index;
    playTrack(row, { preserveQueue: true });
    drawSongs();
  }
  function step(direction) {
    if (!playback.playlist.length || playback.index < 0) return;
    for (let offset = 1; offset <= playback.playlist.length; offset++) {
      const target = (playback.index + direction * offset + playback.playlist.length * 2) % playback.playlist.length;
      if (playback.playlist[target].modo !== 'no-disponible') { playFromQueue(target); return; }
    }
  }
  function addTextElement(tag, className, value) {
    const el = document.createElement(tag);
    if (className) el.className = className;
    el.textContent = value;
    return el;
  }
  function drawSongs() {
    songs.replaceChildren();
    const current = playlists[Number(playlistSelect.value) || 0];
    const tracks = current?.tracks || [];
    if (!tracks.length) {
      songs.append(addTextElement('p', 'bb-drawer-empty', 'Esta playlist todavía no tiene canciones. Agrégalas desde Música.'));
      return;
    }
    tracks.forEach((t, i) => {
      const b = document.createElement('button');
      b.type = 'button';
      b.className = 'bb-playlist-track';
      b.disabled = t.modo === 'no-disponible';
      if (playback.playlist === tracks && playback.index === i) b.classList.add('is-current');
      const label = addTextElement('strong', null, t.titulo);
      const sub = addTextElement('small', null, `${t.artista} · ${t.fuente}`);
      const glyph = addTextElement('span', 'bb-track-play', b.disabled ? 'No disponible' : '▶');
      b.append(label, sub, glyph);
      b.addEventListener('click', () => {
        playback.playlist = tracks;
        playFromQueue(i);
        closeDrawer(); // El usuario vuelve al libro y la música sigue activa.
      });
      songs.appendChild(b);
    });
  }
  async function openDrawer() {
    drawer.hidden = false;
    scrim.hidden = false;
    document.body.classList.add('bb-drawer-open');
    playlistSelect.replaceChildren();
    songs.textContent = 'Cargando tus canciones…';
    drawerStatus.textContent = '';
    try {
      const response = await fetch('/Musica/ColaLector', { credentials: 'same-origin', cache: 'no-store', headers: { Accept: 'application/json' } });
      if (!response.ok) throw new Error('Respuesta no disponible');
      const data = await response.json();
      if (!data.authenticated) {
        songs.replaceChildren(addTextElement('p', null, 'Inicia sesión para ver tus playlists personales.'));
        const link = document.createElement('a');
        link.href = '/Cuenta/Login';
        link.textContent = 'Iniciar sesión';
        link.className = 'btn btn-earth btn-sm';
        songs.appendChild(link);
        return;
      }
      playlists = Array.isArray(data.playlists) ? data.playlists : [];
      if (!playlists.length) {
        songs.replaceChildren(addTextElement('p', null, 'Todavía no tienes playlists. Busca música y crea una desde «Mis playlists».'));
        const link = document.createElement('a');
        link.href = '/Musica/Playlists'; link.className = 'btn btn-earth btn-sm'; link.textContent = 'Crear playlist';
        songs.append(link);
        return;
      }
      playlists.forEach((p, i) => {
        const option = document.createElement('option'); option.value = String(i); option.textContent = p.nombre;
        playlistSelect.append(option);
      });
      drawSongs();
    } catch {
      songs.textContent = 'No se pudieron cargar tus playlists. Comprueba tu conexión con Book & Beet.';
    }
  }
  function closeDrawer() {
    drawer.hidden = true;
    scrim.hidden = true;
    document.body.classList.remove('bb-drawer-open');
  }

  prev?.addEventListener('click', () => step(-1));
  next?.addEventListener('click', () => step(1));
  document.getElementById('bbPlayerStop')?.addEventListener('click', stop);
  document.getElementById('bbClosePlaylists')?.addEventListener('click', closeDrawer);
  scrim?.addEventListener('click', closeDrawer);
  playlistSelect?.addEventListener('change', drawSongs);
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && !drawer.hidden) closeDrawer(); });
  updateNavButtons();

  // Maneja todos los botones de música sin depender del documento parcial actual.
  document.addEventListener('click', e => {
    const opener = e.target.closest('[data-open-playlists]');
    if (opener) { e.preventDefault(); void openDrawer(); return; }
    const audioButton = e.target.closest('[data-audio-url]');
    if (audioButton) {
      e.preventDefault();
      playTrack({ modo: 'audio', audioUrl: audioButton.dataset.audioUrl,
        titulo: audioButton.dataset.trackTitle || audioButton.dataset.title || 'Canción',
        artista: audioButton.dataset.artist || 'Book & Beet' });
      return;
    }
    const youtubeButton = e.target.closest('[data-youtube-id]');
    if (youtubeButton) {
      e.preventDefault();
      playTrack({ modo: 'youtube', youtubeId: youtubeButton.dataset.youtubeId,
        titulo: youtubeButton.dataset.trackTitle || youtubeButton.dataset.title || 'YouTube',
        artista: youtubeButton.dataset.artist || 'YouTube' });
    }
  });
  audio?.addEventListener('ended', () => { if (playback.playlist.length > 1) step(1); });

  function canSoftNavigate(url) {
    return url.origin === location.origin && /^\/(libros|musica|home)(\/|$)/i.test(url.pathname);
  }
  function initReadingControls() {
    const text = document.getElementById('readerText');
    if (text) {
      let size = 19;
      document.getElementById('larger')?.addEventListener('click', () => {
        size = Math.min(30, size + 2); text.style.fontSize = size + 'px';
      });
      document.getElementById('smaller')?.addEventListener('click', () => {
        size = Math.max(14, size - 2); text.style.fontSize = size + 'px';
      });
    }
    if (document.getElementById('bookPdfViewer')) {
      void import('/js/lector-bibliobeat.mjs').then(m => m.initPdfReader()).catch(() => {
        const error = document.getElementById('pdfError');
        if (error) { error.hidden = false; error.textContent = 'No se pudo cargar el lector PDF.'; }
      });
    }
  }
  async function softNavigate(url, options = {}) {
    if (navigating) return;
    navigating = true;
    try {
      const response = await fetch(url.href, {
        method: options.method || 'GET',
        body: options.body,
        credentials: 'same-origin',
        cache: 'no-store',
        headers: { 'X-BiblioBeat-Navigation': '1', Accept: 'text/html' }
      });
      // No intentar dibujar el contenido si es un PDF, respuesta externa o error.
      const actual = new URL(response.url);
      if (!response.ok || actual.origin !== location.origin ||
          !(response.headers.get('content-type') || '').includes('text/html')) throw new Error('Navegación tradicional');
      const doc = new DOMParser().parseFromString(await response.text(), 'text/html');
      const replacement = doc.getElementById('bbPageContent');
      const main = document.getElementById('bbPageContent');
      if (!main || !replacement) throw new Error('No se encontró contenido HTML');
      main.replaceChildren(...Array.from(replacement.childNodes).map(n => document.importNode(n, true)));
      document.title = doc.title || 'Book & Beet';
      if (!options.popState) history.pushState({ bbSoft: true }, '', actual.pathname + actual.search + actual.hash);
      window.scrollTo(0, 0);
      initReadingControls();
      closeDrawer();
    } catch (err) {
      if (!options.popState) {
        // La navegación suave no es obligatoria. En errores seguimos usando MVC normalmente.
        location.assign(url.href);
      } else {
        location.reload();
      }
    } finally { navigating = false; }
  }
  document.addEventListener('click', e => {
    if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
    const a = e.target.closest('a[href]');
    if (!a || a.hasAttribute('download') || a.target && a.target !== '_self' ||
        a.closest('[contenteditable]') || a.dataset.noSoftNav !== undefined) return;
    const url = new URL(a.href, location.href);
    if (!canSoftNavigate(url) || url.href === location.href && url.hash) return;
    e.preventDefault();
    void softNavigate(url);
  });
  document.addEventListener('submit', e => {
    if (e.defaultPrevented) return;
    const form = e.target;
    if (!(form instanceof HTMLFormElement) || form.target || form.enctype === 'multipart/form-data') return;
    const url = new URL(form.action, location.href);
    if (!canSoftNavigate(url)) return;
    const method = (form.method || 'get').toUpperCase();
    const data = new FormData(form, e.submitter || undefined);
    if (method === 'GET') {
      for (const [key, value] of data) url.searchParams.set(key, String(value));
      e.preventDefault(); void softNavigate(url);
    } else if (method === 'POST' && /^\/musica(\/|$)/i.test(url.pathname)) {
      e.preventDefault(); void softNavigate(url, { method: 'POST', body: data });
    }
  });
  window.addEventListener('popstate', () => { void softNavigate(new URL(location.href), { popState: true }); });
  initReadingControls();
  // Diagnóstico de interfaz sin exponer estado de autenticación ni tokens.
  window.BiblioBeatPlayer = Object.freeze({ isPlaying: () => playback.type !== null, openPlaylists: openDrawer });
})();
