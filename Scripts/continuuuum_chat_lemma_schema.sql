-- Structural chat open/close lemma property specs (game multiplayer only; Continuuuum tools stay unrated).
INSERT OR IGNORE INTO localization_property_specs (key, value_type, allowed_values_json, default_value, description)
VALUES
  ('chat-op', 'String', '["open","close","toggle"]', 'open', '{P:chat|op=open} open|close|toggle. Placeholders open-chat/close-chat/dismiss infer op.'),
  ('product-id', 'String', NULL, '', 'Saurce product id for lexicon/history'),
  ('session-id', 'String', NULL, '', 'Chat session id'),
  ('compose-mode', 'String', '["preview","sendButton"]', 'preview', 'preview streams compose BT; sendButton commits on Send'),
  ('chat-surface', 'String', NULL, 'unity-mp-text', 'Structural chat surface id; Continuuuum tools stay unrated'),
  ('auto-close-on-exit', 'Bool', '["true","false"]', 'false', 'Close chat when leaving the bound stop / scene'),
  ('require-entitlement', 'Bool', '["true","false"]', 'true', 'Block open unless chat entitlement is granted');
