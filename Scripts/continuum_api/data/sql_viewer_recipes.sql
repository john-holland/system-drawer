-- Continuum SQL Viewer recipe queries (read-only)
-- Metadata lines use: -- @recipe id=... label="..." description="..."

-- @recipe id=schema_tables label="All user tables" description="List tables and views in continuum.db"
SELECT name, type
FROM sqlite_master
WHERE type IN ('table', 'view')
  AND name NOT LIKE 'sqlite_%'
ORDER BY type, name;

-- @recipe id=thesaurus_entries label="Thesaurus entries sample" description="Recent custom thesaurus rows"
SELECT id, term, pos_tag, language_id
FROM thesaurus_entries
ORDER BY term
LIMIT 50;

-- @recipe id=thesaurus_properties label="Entry properties sample" description="Property keys attached to lemmas"
SELECT entry_id, property_key, property_value
FROM thesaurus_entry_properties
ORDER BY property_key, entry_id
LIMIT 100;

-- @recipe id=clause_bindings label="Clause bindings" description="Script clause bindings by kind"
SELECT binding_kind, COUNT(*) AS binding_count
FROM localization_clause_bindings
GROUP BY binding_kind
ORDER BY binding_count DESC;

-- @recipe id=clause_bindings_recent label="Recent clause bindings" description="Latest localization clause bindings"
SELECT selection_text, property_key, property_value, binding_kind, entry_id
FROM localization_clause_bindings
ORDER BY updated_at DESC
LIMIT 50;

-- @recipe id=spatial_4d_sample label="Spatial 4D sample" description="Sample spatial volume rows"
SELECT id, episode_id, payload_label, center_x, center_y, center_z, t_min, t_max
FROM spatial_4d
ORDER BY created_at DESC
LIMIT 25;

-- @recipe id=composition_rows label="Composed lemmas" description="Parent/child lemma composition graph"
SELECT parent_entry_id, child_entry_id, sort_order, spatial_4d_id
FROM thesaurus_entry_compositions
ORDER BY parent_entry_id, sort_order
LIMIT 100;

-- @recipe id=draft_episodes label="Draft episodes" description="Draft episode rows"
SELECT id, title, status, updated_at
FROM draft_episodes
ORDER BY updated_at DESC
LIMIT 50;

-- @recipe id=stories_by_status label="Stories by status" description="Agile stories grouped by workflow status"
SELECT status, COUNT(*) AS story_count
FROM stories
GROUP BY status
ORDER BY story_count DESC;

-- @recipe id=work_orders_for_story label="Work orders (sample)" description="Recent work orders with asset refs"
SELECT id, story_id, episode_id, status, asset_kind, causality_leaf_id, work_order_source
FROM work_orders
ORDER BY updated_at DESC
LIMIT 50;

-- @recipe id=causality_structure_for_episode label="Causality structure" description="Episode causality structure rows"
SELECT id, episode_id, structure_type, detection_source, description
FROM causality_structure
ORDER BY episode_id
LIMIT 50;

-- @recipe id=work_orders_with_assets label="Work orders with assets" description="Work orders including asset_kind and asset_ref_json"
SELECT id, story_id, episode_id, status, asset_kind, asset_ref_json, causality_test_status, causality_leaf_id
FROM work_orders
ORDER BY updated_at DESC
LIMIT 100;

-- @recipe id=story_schedule_crossref label="Stories with schedule/budget links" description="Stories linked to resaurce schedule and budget plans"
SELECT id, summary, status, resaurce_schedule_id, resaurce_budget_plan_id, story_value, calendar_start_date, calendar_end_date
FROM stories
ORDER BY updated_at DESC
LIMIT 100;

-- @recipe id=api_audit_tail label="API audit tail" description="Recent API audit log entries"
SELECT timestamp, user_id, method, api_path, status_code
FROM api_audit_log
ORDER BY timestamp DESC
LIMIT 100;

-- @recipe id=localization_specs label="Localization property specs" description="Registered localization property keys"
SELECT key, value_type, default_value, description
FROM localization_property_specs
ORDER BY key;

-- @recipe id=society_cities label="Society cities" description="Configured society cities"
SELECT city_id, planet_id, display_name, annual_budget_usd, city_size_sqm
FROM society_cities
ORDER BY display_name
LIMIT 50;

-- @recipe id=table_info_example label="PRAGMA table_info example" description="Column metadata for thesaurus_entries"
PRAGMA table_info(thesaurus_entries);
