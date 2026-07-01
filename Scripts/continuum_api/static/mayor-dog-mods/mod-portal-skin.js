(function () {
  window.MayorDogModPortalSkin = {
    apply(portalSkin) {
      const settings = (portalSkin && portalSkin.settings) || portalSkin || {};
      const accent = settings.accentColor || settings.accent || '#7b1fa2';
      document.documentElement.style.setProperty('--md-accent', accent);
      if (settings.backgroundColor) {
        document.documentElement.style.setProperty('--md-bg', settings.backgroundColor);
      }
    },
    async loadForMod(api, modSlug, portalSkins) {
      const skin = portalSkins && portalSkins[modSlug];
      if (skin) this.apply(skin);
    },
  };
})();
