import {Firestore, FieldValue} from "firebase-admin/firestore";

/** Admin-only utility, deliberately NOT exported as a callable Cloud Function. */
export async function rollbackHeartAdjustment(db: Firestore, adjustmentId: string, apply = false) {
  if (!/^[a-f0-9]{64}$/.test(adjustmentId)) throw new Error("Provide the full adjustment receipt ID.");
  return db.runTransaction(async (tx) => {
    const receiptRef = db.doc(`heartAdjustments/${adjustmentId}`);
    const receipt = await tx.get(receiptRef);
    if (!receipt.exists) throw new Error("Adjustment receipt not found.");
    const data = receipt.data()!;
    if (data.rolledBackAt) return {alreadyRolledBack: true, applied: false};
    if (!("beforeStanding" in data) || !("beforeWallet" in data)) throw new Error("This old receipt has no rollback snapshot.");
    const standingRef = db.doc(`studentStanding/${data.uid}`), walletRef = db.doc(`wallets/${data.uid}`);
    const [standing, wallet] = await tx.getAll(standingRef, walletRef);
    if (standing.data()?.lastAdjustmentId !== adjustmentId || standing.data()?.hearts !== data.hearts)
      throw new Error("Standing changed after this adjustment. Refusing to overwrite newer progress.");
    if (data.restoreWallet && wallet.data()?.lastMutationId !== adjustmentId)
      throw new Error("Wallet changed after this adjustment. Refusing to overwrite newer rewards.");
    const preview = {alreadyRolledBack: false, applied: apply, beforeHearts: data.hearts,
      restoreHearts: data.beforeStanding?.hearts ?? 50, restoreWallet: data.restoreWallet === true};
    if (!apply) return preview;
    const timestamp = FieldValue.serverTimestamp();
    if (data.beforeStanding) tx.set(standingRef, {...data.beforeStanding, updatedAt: timestamp});
    else tx.delete(standingRef);
    if (data.restoreWallet) {
      if (data.beforeWallet) tx.set(walletRef, {...data.beforeWallet, updatedAt: timestamp});
      else tx.delete(walletRef);
    }
    tx.update(receiptRef, {rolledBackAt: timestamp});
    tx.create(db.doc(`studentActivity/${data.uid}/entries/${adjustmentId}-reversed`), {
      title: "Standing adjustment reversed", detail: "Your earlier standing was restored.", kind: "hearts-rollback",
      deltaHearts: -data.delta,
      deltaXp: data.restoreWallet ? (data.beforeWallet?.xp ?? 0) - (wallet.data()?.xp ?? 0) : 0,
      occurredAt: timestamp,
    });
    return preview;
  });
}
