/** Node tests for Mayor Dog Mod ack helpers. */
const assert = require('assert');
const Ack = require('../continuum_api/static/shared/continuum-script-output/continuum-script-ack.js');

assert.strictEqual(Ack.MAYOR_DOG_MOD_ITEM_TYPE, 'mayor_dog_mod_section_altered');

const items = Ack.buildMayorDogModAckItems(true);
assert.strictEqual(items.length, 1);
assert.strictEqual(items[0].severity, 'required');
assert.strictEqual(items[0].itemType, Ack.MAYOR_DOG_MOD_ITEM_TYPE);

const blocked = Ack.unacknowledgedRequired(items);
assert.strictEqual(blocked.length, 1);

console.log('test_mayor_dog_mod_ack: ok');
