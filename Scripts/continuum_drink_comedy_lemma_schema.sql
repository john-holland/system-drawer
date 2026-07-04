-- Comedy drink lemma property extensions (run after continuum_drink_lemma_schema.sql)

INSERT OR IGNORE INTO localization_property_specs (key, value_type, allowed_values_json, default_value, description)
VALUES
    ('partially-raise-amount', 'Float', NULL, '1', 'Fraction of full raise toward mouth/spout (1 = all the way)'),
    ('partial-raise-default-when-stalled', 'Float', NULL, '0.65', 'Default partial raise when stalled/almost+mouth lemmas apply'),
    ('train-for-perfect-drink', 'Bool', '["true","false"]', 'false', 'Train/score for zero spill vs empty vessel'),
    ('max-spill-liters-tolerance', 'Float', NULL, '0.05', 'Max spill when train-for-perfect-drink is true'),
    ('closure-mode', 'String', NULL, 'auto', 'auto|mouth|empty-vessel|stalled|spill-beat|infinite-drain-beat'),
    ('mouth-volume-liters-target', 'Float', NULL, '0', 'Mouth delivery target for closure (0 = sip math)'),
    ('infinite-drain', 'Bool', '["true","false"]', 'false', 'Fantasia mode: vessel volume never depletes'),
    ('infinite-drain-closure-seconds', 'Float', NULL, '0', 'Close infinite-drain beat after N seconds (0 = spill/mouth only)');
