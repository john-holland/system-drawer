(function () {
  window.MayorDogModUploadSettings = {
    async render(root, api) {
      root.innerHTML = `<section class="md-panel"><h2>Mod portal skin (USC document set)</h2>
        <p>Upload USC library documents and attach them to your mod for portal theming.</p>
        <label>Mod ID<input id="skin-mod-id" placeholder="mod_..." /></label>
        <label>Library document IDs (comma-separated)<input id="skin-doc-ids" placeholder="doc1,doc2" /></label>
        <label>Theme accent<input id="skin-accent" value="#7b1fa2" /></label>
        <button id="save-skin">Save portal settings</button>
        <button id="preview-skin">Preview skin</button></section>`;
      root.querySelector('#save-skin')?.addEventListener('click', async () => {
        const modId = root.querySelector('#skin-mod-id')?.value?.trim();
        const ids = (root.querySelector('#skin-doc-ids')?.value || '').split(',').map((s) => s.trim()).filter(Boolean);
        const accent = root.querySelector('#skin-accent')?.value || '#7b1fa2';
        await api(`/api/mods/portal-settings/${encodeURIComponent(modId)}`, {
          method: 'PUT',
          body: { libraryDocumentIds: ids, settings: { accentColor: accent } },
        });
        alert('Portal settings saved');
      });
      root.querySelector('#preview-skin')?.addEventListener('click', () => {
        const accent = root.querySelector('#skin-accent')?.value || '#7b1fa2';
        if (window.MayorDogModPortalSkin) window.MayorDogModPortalSkin.apply({ settings: { accentColor: accent } });
      });
    },
    async uploadLibraryDocument(api, file) {
      const form = new FormData();
      form.append('file', file);
      const headers =
        window.ContinuuuumUserSession && ContinuuuumUserSession.getHeaders
          ? ContinuuuumUserSession.getHeaders()
          : { 'X-User-ID': 'anonymous' };
      const res = await fetch('/api/library/upload', { method: 'POST', headers, body: form });
      if (!res.ok) throw new Error('Upload failed');
      return res.json();
    },
  };
})();
