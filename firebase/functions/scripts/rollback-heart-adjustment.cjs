// Uses an administrator's Application Default Credentials. Dry run unless --apply is explicit.
const admin = require('firebase-admin');
const {rollbackHeartAdjustment} = require('../lib/rollbackHeartAdjustment.js');
const args = process.argv.slice(2);
const project = args[args.indexOf('--project') + 1];
const receipt = args[args.indexOf('--receipt') + 1];
if (!args.includes('--project') || !args.includes('--receipt') || project !== 'capstone-d51c2') {
  console.error('Usage: node scripts/rollback-heart-adjustment.cjs --project capstone-d51c2 --receipt <receipt-id> [--apply]');
  process.exit(1);
}
admin.initializeApp({projectId: project});
rollbackHeartAdjustment(admin.firestore(), receipt, args.includes('--apply'))
  .then(result => console.log(JSON.stringify(result, null, 2)))
  .catch(error => {console.error(error.message); process.exitCode = 1;})
  .finally(() => admin.app().delete());
