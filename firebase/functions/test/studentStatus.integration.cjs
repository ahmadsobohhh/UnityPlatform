/* Run only through the Firebase Emulator Suite. No production users or data. */
const {test, before, after} = require('node:test');
const assert = require('node:assert/strict');
const admin = require('firebase-admin');
const project = process.env.GCLOUD_PROJECT || 'demo-imaginequest-status';
if (!process.env.FIRESTORE_EMULATOR_HOST || !process.env.FIREBASE_AUTH_EMULATOR_HOST || !project.startsWith('demo-'))
  throw new Error('These tests require a demo project and both emulators.');
admin.initializeApp({projectId: project});
const db = admin.firestore();
const {rollbackHeartAdjustment} = require('../lib/rollbackHeartAdjustment.js');
const base = `http://127.0.0.1:5001/${project}/northamerica-northeast1/`;
let counter = 0;
async function account(role) {
  const response = await fetch(`http://${process.env.FIREBASE_AUTH_EMULATOR_HOST}/identitytoolkit.googleapis.com/v1/accounts:signUp?key=emulator-key`,
    {method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify({returnSecureToken:true})});
  const user = await response.json(); assert.ok(user.localId, 'Emulator account created');
  await db.doc(`users/${user.localId}`).set({uid:user.localId, role, username:role});
  return {uid:user.localId, token:user.idToken};
}
async function call(name, user, data={}) {
  const response = await fetch(base + name, {method:'POST', headers:{'Content-Type':'application/json', ...(user ? {Authorization:`Bearer ${user.token}`} : {})}, body:JSON.stringify({data})});
  const value = await response.json();
  if (value.error) { const error = new Error(value.error.message); error.status = value.error.status; throw error; }
  assert.equal(response.ok, true); return value.result;
}
async function fixture() {
  const teacher = await account('teacher'), student = await account('student');
  const classId = `status-test-${++counter}`;
  await db.doc(`classes/${classId}`).set({ownerUid:teacher.uid, name:'Emulator class'});
  await db.doc(`classes/${classId}/members/${student.uid}`).set({displayName:'Demo student'});
  await call('ensureStudentStatus', student);
  return {teacher, student, classId};
}
const change = (f, extra={}) => ({classId:f.classId, studentId:f.student.uid, eventId:'heart-1', delta:-1, reason:'Late arrival', ...extra});
const quest = (f, extra={}) => ({classId:f.classId, assignmentId:'blacksmith-workshop', contentVersion:'1', action:'load', eventId:'', station:-1, choice:-1, ...extra});
async function assign(f) { await call('setWorkshopAssignment', f.teacher, {classId:f.classId, isOpen:true}); }
async function craft(f, station) {
  await call('blacksmithQuest', f.student, quest(f, {action:'lesson', eventId:`lesson-${station}`, station}));
  return call('blacksmithQuest', f.student, quest(f, {action:'answer', eventId:`answer-${station}`, station, choice:station}));
}
test('Unauthenticated and teacher accounts cannot initialize student balances', async () => {
  await assert.rejects(call('ensureStudentStatus', null), {status:'UNAUTHENTICATED'});
  await assert.rejects(call('ensureStudentStatus', await account('teacher')), {status:'PERMISSION_DENIED'});
});
test('Initialization is idempotent and preserves zero Hearts and existing XP', async () => {
  const f = await fixture();
  assert.equal((await db.doc(`studentStanding/${f.student.uid}`).get()).data().hearts,50);
  await db.doc(`studentStanding/${f.student.uid}`).update({hearts:0});
  await db.doc(`wallets/${f.student.uid}`).update({xp:123});
  await call('ensureStudentStatus', f.student);
  assert.equal((await db.doc(`studentStanding/${f.student.uid}`).get()).data().hearts,0);
  assert.equal((await db.doc(`wallets/${f.student.uid}`).get()).data().xp,123);
});
test('Students and other teachers cannot adjust or inspect class standing', async () => {
  const f = await fixture(), outsider = await account('teacher');
  for(const actor of [f.student, outsider]) {
    await assert.rejects(call('adjustStudentHearts',actor,change(f)),{status:'PERMISSION_DENIED'});
    await assert.rejects(call('getClassStudentStatus',actor,change(f)),{status:'PERMISSION_DENIED'});
    await assert.rejects(call('setWorkshopAssignment',actor,{classId:f.classId,isOpen:true}),{status:'PERMISSION_DENIED'});
  }
});
test('A teacher adjustment is atomic, visible and safe to retry concurrently', async () => {
  const f=await fixture();
  const results=await Promise.all([call('adjustStudentHearts',f.teacher,change(f)),call('adjustStudentHearts',f.teacher,change(f))]);
  assert.equal(results.filter(x=>x.alreadyProcessed).length,1);
  assert.equal((await call('getClassStudentStatus',f.teacher,change(f))).hearts,49);
  assert.equal((await db.collection(`studentActivity/${f.student.uid}/entries`).get()).size,1);
  await assert.rejects(call('adjustStudentHearts',f.teacher,change(f,{delta:-5})),{status:'ALREADY_EXISTS'});
});
test('Heart bounds, required reasons, and membership are enforced', async () => {
  const f=await fixture();
  for (const delta of [-51,51,0,1.5]) await assert.rejects(call('adjustStudentHearts',f.teacher,change(f,{delta})),{status:'INVALID_ARGUMENT'});
  await assert.rejects(call('adjustStudentHearts',f.teacher,change(f,{reason:''})),{status:'INVALID_ARGUMENT'});
  await assert.rejects(call('adjustStudentHearts',f.teacher,change(f,{delta:1})),{status:'OUT_OF_RANGE'});
  await db.doc(`classes/${f.classId}/members/${f.student.uid}`).delete();
  await assert.rejects(call('adjustStudentHearts',f.teacher,change(f)),{status:'PERMISSION_DENIED'});
});
test('Zero Hearts resets currencies and invalidates old Powers exactly once', async () => {
  const f=await fixture();
  await db.doc(`wallets/${f.student.uid}`).update({xp:175,crystals:20,gold:7,level:4});
  await db.doc(`studentStanding/${f.student.uid}`).update({hearts:1});
  assert.equal((await call('adjustStudentHearts',f.teacher,change(f))).reset,true);
  await call('adjustStudentHearts',f.teacher,change(f));
  const wallet=(await db.doc(`wallets/${f.student.uid}`).get()).data();
  assert.deepEqual([wallet.xp,wallet.crystals,wallet.gold,wallet.level,wallet.epoch],[0,0,0,1,1]);
  const standing=(await db.doc(`studentStanding/${f.student.uid}`).get()).data();
  assert.equal(standing.powerEpoch,1); assert.equal(standing.powersRevoked,true);
  await call('adjustStudentHearts',f.teacher,change(f,{eventId:'restore',delta:5,reason:'Standing restored'}));
  assert.equal((await db.doc(`wallets/${f.student.uid}`).get()).data().xp,0);
});
test('Admin rollback previews and restores a reset including currencies and Powers epoch', async () => {
  const f=await fixture();
  await db.doc(`wallets/${f.student.uid}`).update({xp:175,crystals:9,gold:2,level:3});
  await db.doc(`studentStanding/${f.student.uid}`).update({hearts:1});
  await call('adjustStudentHearts',f.teacher,change(f));
  const receipt=(await db.collection('heartAdjustments').where('uid','==',f.student.uid).get()).docs[0];
  assert.equal((await rollbackHeartAdjustment(db,receipt.id)).applied,false);
  assert.equal((await db.doc(`studentStanding/${f.student.uid}`).get()).data().hearts,0);
  assert.equal((await rollbackHeartAdjustment(db,receipt.id,true)).applied,true);
  assert.equal((await db.doc(`studentStanding/${f.student.uid}`).get()).data().hearts,1);
  const wallet=(await db.doc(`wallets/${f.student.uid}`).get()).data();
  assert.deepEqual([wallet.xp,wallet.crystals,wallet.gold,wallet.level,wallet.epoch],[175,9,2,3,0]);
  assert.equal((await rollbackHeartAdjustment(db,receipt.id,true)).alreadyRolledBack,true);
  await assert.rejects(call('adjustStudentHearts',f.teacher,change(f)),{status:'FAILED_PRECONDITION'});
});
test('Admin rollback refuses to overwrite subsequent standing or wallet changes', async () => {
  const f=await fixture();
  await call('adjustStudentHearts',f.teacher,change(f,{delta:-50,reason:'Standing reset'}));
  const receipt=(await db.collection('heartAdjustments').where('uid','==',f.student.uid).get()).docs[0];
  await db.doc(`wallets/${f.student.uid}`).update({xp:10,lastMutationId:'newer-admin-change'});
  await assert.rejects(rollbackHeartAdjustment(db,receipt.id,true), /Wallet changed/);
  await call('adjustStudentHearts',f.teacher,change(f,{eventId:'restore',delta:5,reason:'Standing restored'}));
  await assert.rejects(rollbackHeartAdjustment(db,receipt.id,true), /Standing changed/);
});
test('Workshop requires a matching open assignment and membership', async () => {
  const f=await fixture();
  await assert.rejects(call('blacksmithQuest',f.student,quest(f)),{status:'FAILED_PRECONDITION'});
  await assign(f);
  const stranger=await account('student');
  await assert.rejects(call('blacksmithQuest',stranger,quest(f)),{status:'PERMISSION_DENIED'});
  await assert.rejects(call('blacksmithQuest',f.student,quest(f,{contentVersion:'fake'})),{status:'INVALID_ARGUMENT'});
});
test('Wrong answers never earn XP or deduct Hearts; notes are required', async () => {
  const f=await fixture(); await assign(f);
  await assert.rejects(call('blacksmithQuest',f.student,quest(f,{action:'answer',eventId:'early',station:0,choice:0})),{status:'FAILED_PRECONDITION'});
  await call('blacksmithQuest',f.student,quest(f,{action:'lesson',eventId:'note',station:0}));
  const result=await call('blacksmithQuest',f.student,quest(f,{action:'answer',eventId:'wrong',station:0,choice:1}));
  assert.equal(result.awardedXp,0); assert.equal(result.completedMask,0);
  assert.equal((await db.doc(`studentStanding/${f.student.uid}`).get()).data().hearts,50);
});
test('Verified crafts, delivery and replays award exactly 175 XP', async () => {
  const f=await fixture(); await assign(f);
  for(let i=0;i<3;i++) assert.equal((await craft(f,i)).awardedXp,25);
  const finish=quest(f,{action:'complete',eventId:'delivery'});
  assert.equal((await call('blacksmithQuest',f.student,finish)).awardedXp,100);
  await call('blacksmithQuest',f.student,finish);
  await call('blacksmithQuest',f.student,{...finish,eventId:'delivery-again'});
  await call('blacksmithQuest',f.student,quest(f,{action:'answer',eventId:'new-answer',station:0,choice:0}));
  assert.equal((await db.doc(`wallets/${f.student.uid}`).get()).data().xp,175);
  const saved=await call('blacksmithQuest',f.student,quest(f));
  assert.equal(saved.completedMask,7); assert.equal(saved.turnedIn,true); assert.equal(saved.verifiedXp,175);
  assert.equal((await db.collection(`studentActivity/${f.student.uid}/entries`).get()).size,4);
});
test('Premature completion and event-ID substitution are rejected', async () => {
  const f=await fixture(); await assign(f);
  await assert.rejects(call('blacksmithQuest',f.student,quest(f,{action:'complete',eventId:'early'})),{status:'FAILED_PRECONDITION'});
  await craft(f,0);
  await assert.rejects(call('blacksmithQuest',f.student,quest(f,{action:'answer',eventId:'answer-0',station:0,choice:2})),{status:'ALREADY_EXISTS'});
});
test('Zero Hearts blocks new rewards; restored Hearts do not replay old rewards', async () => {
  const f=await fixture(); await assign(f); await craft(f,0);
  await call('adjustStudentHearts',f.teacher,change(f,{delta:-50,reason:'Standing reset'}));
  await assert.rejects(craft(f,1),{status:'FAILED_PRECONDITION'});
  await call('adjustStudentHearts',f.teacher,change(f,{eventId:'restore',delta:5,reason:'Return to good standing'}));
  await craft(f,0); assert.equal((await db.doc(`wallets/${f.student.uid}`).get()).data().xp,0);
  await craft(f,1); assert.equal((await db.doc(`wallets/${f.student.uid}`).get()).data().xp,25);
});
test('Closing an assignment preserves reads and safe retries but blocks new progress', async () => {
  const f=await fixture(); await assign(f); await craft(f,0);
  await call('setWorkshopAssignment',f.teacher,{classId:f.classId,isOpen:false});
  assert.equal((await call('blacksmithQuest',f.student,quest(f))).completedMask,1);
  await call('blacksmithQuest',f.student,quest(f,{action:'answer',eventId:'answer-0',station:0,choice:0}));
  await assert.rejects(craft(f,1),{status:'FAILED_PRECONDITION'});
});
test('Generic quest rewards also respect zero Hearts and create recap history',async()=>{
  const f=await fixture();
  await db.doc(`classes/${f.classId}/assignments/celestial`).set({questDefinitionId:'celestial-clock',contentVersion:'v1',state:'open',title:'Celestial Clock',rewardsPolicy:{challengeXp:25,completionXp:100}});
  await db.doc('questDefinitions/celestial-clock/versions/v1').set({requiredChallengeIds:['anvil'],challenges:{anvil:{acceptedAnswers:['anvil']}}});
  const input={classId:f.classId,assignmentId:'celestial',runId:'run-1',eventId:'correct-1',eventType:'challenge-submitted',challengeId:'anvil',response:'anvil'};
  assert.equal((await call('recordQuestEvent',f.student,input)).awardedXp,25);
  assert.equal((await db.collection(`studentActivity/${f.student.uid}/entries`).get()).size,1);
  await call('adjustStudentHearts',f.teacher,change(f,{delta:-50,reason:'Standing reset'}));
  await assert.rejects(call('recordQuestEvent',f.student,{...input,eventId:'finish-1',eventType:'quest-completed'}),{status:'FAILED_PRECONDITION'});
  assert.equal((await db.doc(`wallets/${f.student.uid}`).get()).data().xp,0);
});
test('Firestore rules allow self reads and deny balance tampering and other students',async()=>{
  const f=await fixture(), other=await account('student');
  const url=`http://${process.env.FIRESTORE_EMULATOR_HOST}/v1/projects/${project}/databases/(default)/documents/`;
  for(const path of [`wallets/${f.student.uid}`,`studentStanding/${f.student.uid}`]) {
    assert.equal((await fetch(url+path,{headers:{Authorization:`Bearer ${f.student.token}`}})).status,200);
    assert.equal((await fetch(url+path,{headers:{Authorization:`Bearer ${other.token}`}})).status,403);
    assert.equal((await fetch(url+path,{method:'PATCH',headers:{Authorization:`Bearer ${f.student.token}`,'Content-Type':'application/json'},body:JSON.stringify({fields:{xp:{integerValue:'999999'},hearts:{integerValue:'50'}}})})).status,403);
  }
});
after(async()=>{await admin.app().delete();});
