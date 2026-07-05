-- Drink lemma property specs (extends continuuuum_localization_schema.sql)

INSERT OR IGNORE INTO localization_property_specs (key, value_type, allowed_values_json, default_value, description)
VALUES
    ('drink-animation-ref', 'String', NULL, '', 'Asset path or id for DrinkAnimationReference'),
    ('auto-middle-mouth-jaw', 'Bool', '["true","false"]', 'true', 'Auto-align nozzle to middle mouth / jaw opening'),
    ('jaw-tilt-animation-audit-insert', 'Bool', '["true","false"]', 'false', 'Enable jaw-tilt keyframe audit and optional insertion'),
    ('hold-without-return', 'Bool', '["true","false"]', 'false', 'Hold BT: skip return-to-rest cards'),
    ('put-without-release', 'Bool', '["true","false"]', 'false', 'Put BT: skip release after placement'),
    ('nozzle-loop-enabled', 'Bool', '["true","false"]', 'false', 'Optional continuous nozzle pour loop clip'),
    ('liquid-simulation-enabled', 'Bool', '["true","false"]', 'true', 'Enable local liquid sim on vessel'),
    ('place-nozzle-on-mouth', 'Bool', '["true","false"]', 'false', 'IK/orient: place nozzle on mouth'),
    ('drink-efficacy', 'Float', NULL, '0.7', 'Fraction of flow reaching mouth vs spill (0-1)'),
    ('sip-count', 'Int', NULL, '1', 'Number of sips to imbibe over'),
    ('total-volume-liters', 'Float', NULL, '0', 'Target/stored volume in liters');
