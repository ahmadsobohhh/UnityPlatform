/* Local emulators only. Never connects to a real Firebase project or account. */
const {test} = require('node:test');
const assert = require('node:assert/strict');
const project = process.env.GCLOUD_PROJECT;
const firestore = process.env.FIRESTORE_EMULATOR_HOST;
const auth = process.env.FIREBASE_AUTH_EMULATOR_HOST;
if (!project?.startsWith('demo-') || !/^127\.0\.0\.1:\d+$/.test(firestore || '') || !/^127\.0\.0\.1:\d+$/.test(auth || ''))
  throw new Error('Use a demo project and loopback Auth/Firestore emulators.');

const database = `projects/${project}/databases/(default)`;
const endpoint = `http://${firestore}/v1/${database}`;
const headers = token => ({'Content-Type':'application/json', Authorization:`Bearer ${token}`});
const string = value => ({stringValue:value});
async function account() {
  const response = await fetch(`http://${auth}/identitytoolkit.googleapis.com/v1/accounts:signUp?key=emulator-key`,
    {method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify({returnSecureToken:true})});
  assert.equal(response.status,200,'Emulator sign-up succeeds');
  const {localId:uid,idToken:token} = await response.json();
  assert.ok(uid && token,'Emulator supplies an account');
  return {uid,token};
}
async function seed(path,fields) {
  const response=await fetch(`${endpoint}/documents/${path}`,{method:'PATCH',headers:headers('owner'),body:JSON.stringify({fields})});
  assert.equal(response.status,200,'Local fixture is created');
}
async function read(path,token='owner') {
  const response=await fetch(`${endpoint}/documents/${path}`,{headers:headers(token)});
  assert.equal(response.status,200,'Document read succeeds');
  return (await response.json()).fields;
}
function saveAppearance(owner,token,id) {
  return fetch(`${endpoint}/documents:commit`,{method:'POST',headers:headers(token),body:JSON.stringify({writes:[{
    update:{name:`${database}/documents/users/${owner}`,fields:{avatarId:string(id),characterClass:string('Deckhand'),characterColor:string('FFFFFF'),avatarChosen:{booleanValue:true}}},
    updateMask:{fieldPaths:['avatarId','characterClass','characterColor','avatarChosen']},
    updateTransforms:[{fieldPath:'updatedAt',setToServerValue:'REQUEST_TIME'}]
  }]})});
}
test('Saved 3D appearance is private, preserves the profile, and cannot change rewards',async()=>{
  const student=await account(),other=await account();
  const createdAt={timestampValue:'2025-01-01T00:00:00Z'};
  await seed(`users/${student.uid}`,{uid:string(student.uid),role:string('student'),username:string('Emulator navigator'),createdAt});
  await seed(`wallets/${student.uid}`,{xp:{integerValue:'175'},crystals:{integerValue:'9'}});
  await seed(`studentStanding/${student.uid}`,{hearts:{integerValue:'50'}});
  const originalWallet=await read(`wallets/${student.uid}`),originalStanding=await read(`studentStanding/${student.uid}`);
  const id='pirate-v1:1:4:3:2';
  const saved=await saveAppearance(student.uid,student.token,id);
  assert.equal(saved.status,200,'Existing rules allow the exact creator profile update');
  const restored=await read(`users/${student.uid}`,student.token);
  assert.equal(restored.avatarId.stringValue,id);
  assert.equal(restored.avatarChosen.booleanValue,true);
  assert.deepEqual(restored.createdAt,createdAt);
  assert.equal(restored.username.stringValue,'Emulator navigator');
  assert.equal(restored.role.stringValue,'student');
  assert.ok(restored.updatedAt.timestampValue,'Server timestamp is recorded');
  assert.deepEqual(await read(`wallets/${student.uid}`),originalWallet);
  assert.deepEqual(await read(`studentStanding/${student.uid}`),originalStanding);
  const rejected=await saveAppearance(student.uid,other.token,'pirate-v1:0:0:0:0');
  assert.equal(rejected.status,403,'Another account cannot overwrite the appearance');
  assert.equal((await read(`users/${student.uid}`,student.token)).avatarId.stringValue,id);
  const privateRead=await fetch(`${endpoint}/documents/users/${student.uid}`,{headers:headers(other.token)});
  assert.equal(privateRead.status,403,'Another account cannot read the profile');
});
