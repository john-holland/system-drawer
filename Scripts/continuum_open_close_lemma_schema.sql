-- Open/close lemma property seeds for Continuum thesaurus.
INSERT OR IGNORE INTO localization_property_specs (property_key, value_type, default_value, description)
VALUES
  ('open-angle-deg', 'Float', '90', 'Target hinge open angle in degrees'),
  ('drive-mode', 'String', 'hybrid', 'Physics, animation, or hybrid drive'),
  ('arrival-blend-coefficient', 'Float', '0', '0=stop-first, 1=reach-and-retry open'),
  ('reach-radius-meters', 'Float', '0.6', 'Handle reach radius for open attempts'),
  ('require-facing-target', 'Bool', 'true', 'Require facing target before open when blend < 1'),
  ('auto-close-bt', 'String', 'on-stop-exit', 'Auto-close BT compile mode'),
  ('auto-close-on-exit', 'Bool', 'false', 'Runtime close when leaving stop'),
  ('compile-close-ambulation', 'Bool', 'false', 'Ambulate back before auto-close'),
  ('linear-only', 'Bool', 'false', 'Ignore disabled topology branches'),
  ('quest-hint-kind', 'String', 'none', 'Quest hint on beat'),
  ('quest-objective-id', 'String', '', 'Quest objective id'),
  ('open-animation-ref', 'String', '', 'Open animation reference'),
  ('close-animation-ref', 'String', '', 'Close animation reference'),
  ('closure-mode', 'String', 'auto', 'Open/close beat closure mode');
