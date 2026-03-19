-- Extend work_orders for screenplay-derived tasks (dialogue and SFX).
-- Run after continuum_episodes_schema.sql and continuum_screenplay_schema.sql (script_speech_audio, script_sound_effects).
-- Remarks: work_order_source = 'causality' | 'dialogue' | 'sfx'. For screenplay-derived rows, speech_audio_id or sound_effect_id and episode_script_id are set.

ALTER TABLE work_orders ADD COLUMN work_order_source TEXT NOT NULL DEFAULT 'causality';
ALTER TABLE work_orders ADD COLUMN speech_audio_id TEXT;
ALTER TABLE work_orders ADD COLUMN sound_effect_id TEXT;
ALTER TABLE work_orders ADD COLUMN episode_script_id TEXT;
ALTER TABLE work_orders ADD COLUMN farey_left_num INTEGER;
ALTER TABLE work_orders ADD COLUMN farey_left_den INTEGER;
ALTER TABLE work_orders ADD COLUMN farey_right_num INTEGER;
ALTER TABLE work_orders ADD COLUMN farey_right_den INTEGER;

CREATE INDEX IF NOT EXISTS idx_wo_source ON work_orders(work_order_source);
CREATE INDEX IF NOT EXISTS idx_wo_episode_script ON work_orders(episode_script_id);
