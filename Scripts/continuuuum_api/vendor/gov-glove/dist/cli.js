#!/usr/bin/env node
'use strict';

const engine = require('./engine');

const METHODS = {
  calculateTaxImpact: engine.calculateTaxImpact,
  processLobbyistImpacts: engine.processLobbyistImpacts,
  generateScenario: engine.generateScenario,
  computeZoning: engine.computeZoning,
};

function main() {
  const args = process.argv.slice(2);
  if (args.includes('--version') || args.includes('-v')) {
    console.log('gov-glove 1.0.0');
    process.exit(0);
  }

  let input = '';
  process.stdin.setEncoding('utf8');
  process.stdin.on('data', (chunk) => { input += chunk; });
  process.stdin.on('end', () => {
    try {
      const req = input.trim() ? JSON.parse(input) : {};
      const method = req.method || args.find((a) => !a.startsWith('-'));
      if (!method || !METHODS[method]) {
        process.stdout.write(JSON.stringify({ ok: false, error: `unknown method: ${method}` }));
        process.exit(1);
      }
      const result = METHODS[method](req.params || {});
      process.stdout.write(JSON.stringify({ ok: true, result }));
    } catch (e) {
      process.stdout.write(JSON.stringify({ ok: false, error: String(e.message || e) }));
      process.exit(1);
    }
  });

  if (process.stdin.isTTY) {
    const method = args[0];
    if (method === '--version' || method === '-v') return;
    if (method && METHODS[method]) {
      const params = args[1] ? JSON.parse(args[1]) : {};
      process.stdout.write(JSON.stringify({ ok: true, result: METHODS[method](params) }));
    } else {
      process.stderr.write('Usage: echo \'{"method":"computeZoning","params":{}}\' | gov-glove\n');
      process.exit(1);
    }
  }
}

main();
