#!/usr/bin/env node
/** YAML Cave/Tome loader CLI — prints config overview JSON. */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import yaml from 'yaml';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const caveDir = __dirname;

function loadYaml(name) {
  const p = path.join(caveDir, name);
  if (!fs.existsSync(p)) return {};
  return yaml.parse(fs.readFileSync(p, 'utf8')) || {};
}

const tomesDir = path.join(caveDir, 'tomes');
const tomes = fs.existsSync(tomesDir)
  ? fs.readdirSync(tomesDir).filter((f) => f.endsWith('.yaml')).map((f) => yaml.parse(fs.readFileSync(path.join(tomesDir, f), 'utf8')))
  : [];

const overview = {
  cave: loadYaml('cave.yaml'),
  caveRobit: loadYaml('cave-robit.yaml'),
  tomes,
  logViewMachine: { version: '2.1.1' },
};

console.log(JSON.stringify(overview, null, 2));
