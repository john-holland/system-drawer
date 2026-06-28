(function (global) {
  'use strict';

  var mapInstance = null;
  var layerGroup = null;

  function renderLegend(el, zones) {
    if (!el) return;
    el.innerHTML = (zones || []).map(function (z) {
      return '<span style="background:' + z.color + '">' + z.label + '</span>';
    }).join('');
  }

  function renderMap(containerId, data) {
    var el = document.getElementById(containerId);
    if (!el || !global.L) return;
    var bounds = data.bounds || { centerX: 0, centerZ: 0, widthM: 1000, depthM: 1000 };
    var sw = [bounds.centerZ - bounds.depthM / 2, bounds.centerX - bounds.widthM / 2];
    var ne = [bounds.centerZ + bounds.depthM / 2, bounds.centerX + bounds.widthM / 2];
    var latLngBounds = L.latLngBounds(sw, ne);

    if (!mapInstance) {
      mapInstance = L.map(el, { crs: L.CRS.Simple, minZoom: -3 });
      layerGroup = L.layerGroup().addTo(mapInstance);
    }
    layerGroup.clearLayers();
    L.rectangle(latLngBounds, { color: '#888', weight: 2, dashArray: '6', fill: false }).addTo(layerGroup);
    (data.zones || []).forEach(function (z) {
      var latlngs = (z.polygon || []).map(function (p) { return [p[1], p[0]]; });
      if (latlngs.length) {
        L.polygon(latlngs, { color: z.color, fillColor: z.color, fillOpacity: 0.35 })
          .bindTooltip(z.label || z.zoneId).addTo(layerGroup);
      }
    });
    (data.buildings || []).forEach(function (b) {
      L.circleMarker([b.pinLocalZ, b.pinLocalX], { radius: 6, color: '#fff', fillColor: '#f39c12', fillOpacity: 0.9 })
        .bindTooltip(b.displayName || b.stableId).addTo(layerGroup);
    });
    mapInstance.fitBounds(latLngBounds);
    renderLegend(document.getElementById('cc-map-legend'), data.zones);
  }

  global.CitySpatialMap = { render: renderMap };
})(typeof window !== 'undefined' ? window : this);
