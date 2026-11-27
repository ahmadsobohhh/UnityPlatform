using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ImagineQuest.Gameplay;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class BlacksmithWorkshopPlayTests
{
    private string checkpoint;
    private BlacksmithRoomBootstrap room;
    private static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private object Field(string name) => room.GetType().GetField(name, Private).GetValue(room);
    private void Call(string name, params object[] args) => room.GetType().GetMethod(name, Private).Invoke(room, args);

    [UnityTest]
    public IEnumerator WorkshopCanLearnRestoreUnlockAndReview()
    {
        QuestSession.Begin(BlacksmithQuestContent.Id, "local-practice");
        yield return SceneManager.LoadSceneAsync("BlacksmithRoom");
        yield return null;
        room = UnityEngine.Object.FindFirstObjectByType<BlacksmithRoomBootstrap>();
        Assert.That(room, Is.Not.Null);
        var service = room.GetComponent<BlacksmithQuestService>();
        Assert.That(service.Ready, Is.True);
        string assignment = "smoke-" + Guid.NewGuid().ToString("N");
        service.Initialize(null, assignment);
        checkpoint = new BlacksmithQuestStore(Path.Combine(Application.persistentDataPath, "BlacksmithQuest"))
            .FilePath(service.State.userId, service.State.classId, assignment);
        Assert.That(service.IsPractice, Is.True);
        Call("ShowModal", new object[] { null });
        // Initial URP variants can display a cyan placeholder during asynchronous compilation.
        yield return new WaitForSecondsRealtime(3);
#if UNITY_EDITOR
        float deadline = Time.realtimeSinceStartup + 30;
        while (UnityEditor.ShaderUtil.anythingCompiling && Time.realtimeSinceStartup < deadline) yield return null;
#endif
        if (!Application.isBatchMode)
        {
            yield return new WaitForEndOfFrame();
            var frame = ScreenCapture.CaptureScreenshotAsTexture();
            int cyan = 0, sampled = 0;
            for (int y = 0; y < frame.height; y += 40)
                for (int x = 0; x < frame.width; x += 40)
                {
                    Color pixel = frame.GetPixel(x, y); sampled++;
                    if (pixel.r < .05f && pixel.g > .9f && pixel.b > .9f) cyan++;
                }
            UnityEngine.Object.Destroy(frame);
            Assert.That(cyan / (float)sampled, Is.LessThan(.10f), "The workshop must render geometry, not pending shader placeholders.");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/BlacksmithWorkshop-Room.png"));
        }
        Assert.That(((List<TMP_Text>)Field("stationLabels")).Count, Is.EqualTo(3));
        Assert.That(((List<GameObject>)Field("repairParts")).TrueForAll(p => !p.activeSelf), Is.True);

        foreach (int station in new[] { 0, 1, 2 })
        {
            Call("OpenStation", station);
            Assert.That(((GameObject)Field("lessonPanel")).activeSelf, Is.True);
            Assert.That(service.State.HasLesson(station), Is.True);
            Call("AnswerStation", (station + 1) % 3);
            Assert.That(service.State.HasAnswer(station), Is.False);
            if (station == 1 && !Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/BlacksmithWorkshop-Lesson.png"));
            }
            Call("AnswerStation", station);
            Assert.That(service.State.HasAnswer(station), Is.True);
            Assert.That(((List<GameObject>)Field("repairParts"))[station].activeSelf, Is.True);
            Call("ShowModal", new object[] { null });
        }
        Assert.That(service.State.HasKey, Is.True);
        Assert.That(((TMP_Text)Field("keyText")).text, Does.Contain("READY"));
        Assert.That(service.State.verifiedXp, Is.Zero);
        service.Initialize(null, assignment);
        Assert.That(service.State.CompletedCount, Is.EqualTo(3), "Completion must survive loading its checkpoint.");
        Assert.That(service.State.TryOpenPortal(), Is.True);
        service.Save();
        Call("ShowCompletion");
        Assert.That(((GameObject)Field("completionPanel")).activeSelf, Is.True);
        if (!Application.isBatchMode)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/BlacksmithWorkshop-Complete.png"));
        }
        Call("ShowModal", new object[] { null });
        var player = (CharacterController)Field("player");
        player.enabled = false;
        player.transform.position = new Vector3(0, .05f, -.9f);
        player.transform.rotation = Quaternion.Euler(0, 180, 0);
        player.enabled = true;
        if (!Application.isBatchMode)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/BlacksmithWorkshop-Portal.png"));
        }
        Assert.That(service.State.TurnIn(), Is.True);
        Assert.That(service.Save(), Is.True);
        service.Initialize(null, assignment);
        Assert.That(service.State.turnedIn, Is.True);
        // Screenshots are asynchronous; allow the render thread to finish writing.
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator CinderQuayCanFinishBothWavesWithTheWeakestBlade()
    {
        if (QuestSession.Instance != null) QuestSession.Instance.Clear();
        yield return SceneManager.LoadSceneAsync("BlacksmithRoom");
        yield return null;
        room = UnityEngine.Object.FindFirstObjectByType<BlacksmithRoomBootstrap>();
        Assert.That(room.GetComponent<BlacksmithQuestService>(), Is.Null, "The raid must not write assigned quest progress.");
        Assert.That(room.transform.Find("CINDER QUAY arrival"), Is.Not.Null);
        yield return new WaitForSecondsRealtime(2.45f);
        Assert.That(room.transform.Find("CINDER QUAY arrival"), Is.Null);
        Assert.That(room.transform.Find("Forge hands"), Is.Null);
        var imported = room.transform.Find("Cinder Quay — Blacksmith's Forge");
        Assert.That(imported, Is.Not.Null);
        var dressing = imported.Find("Tide Crypt forge dressing");
        Assert.That(dressing, Is.Not.Null);
        var forgeTile = dressing.Find("Crypt floor_tile_large").GetComponentInChildren<MeshRenderer>();
        var cryptTile = room.transform.Find("Crypt floor_tile_large").GetComponentInChildren<MeshRenderer>();
        Assert.That(forgeTile.sharedMaterial, Is.SameAs(cryptTile.sharedMaterial));
        Assert.That(dressing.Find("Smithing workbench"), Is.Not.Null);
        var tableParts = dressing.Find("Anvil smithing table").GetComponentsInChildren<MeshRenderer>();
        var anvilTable = tableParts[0].bounds;
        foreach (var part in tableParts) anvilTable.Encapsulate(part.bounds);
        var anvilParts = System.Array.Find(imported.GetComponentsInChildren<Transform>(),
            part => part.name == "fi_vil_forge_anvil" || part.name == "Forge practice anvil").GetComponentsInChildren<MeshRenderer>();
        var anvil = anvilParts[0].bounds;
        foreach (var part in anvilParts) anvil.Encapsulate(part.bounds);
        Assert.That(anvil.min.x, Is.GreaterThan(1.65f), "The anvil should leave the center aisle clear.");
        Assert.That(dressing.Find("Quench trough"), Is.Not.Null);
        Assert.That(dressing.Find("Vise base"), Is.Not.Null);
        Assert.That(anvilTable.min.y, Is.LessThanOrEqualTo(.02f));
        Assert.That(anvilTable.max.y, Is.EqualTo(anvil.min.y).Within(.025f), "The tabletop must meet the anvil base.");
        int tiles = 0, candleStands = 0, wallTorches = 0;
        foreach (Transform child in room.transform)
        {
            if (child.name == "Crypt floor") tiles++;
            if (child.name == "Crypt torch_mounted")
            {
                wallTorches++;
                var light = child.GetComponentInChildren<Light>();
                var flame = child.Find("Torch ember");
                Assert.That(Vector3.Distance(light.transform.position, flame.position), Is.LessThan(.12f));
                Assert.That(light.shadows, Is.EqualTo(LightShadows.Soft));
                Assert.That(Physics.Raycast(child.position + child.forward * .15f, -child.forward,
                    out var wall, .6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore), Is.True);
                Assert.That(wall.collider.name, Is.EqualTo("Crypt wall"));
            }
            if (child.name != "Grounded crypt candle stand") continue;
            candleStands++;
            Bounds support = child.GetComponentInChildren<MeshRenderer>().bounds;
            Bounds candles = child.Find("Crypt candle_triple").GetComponentInChildren<MeshRenderer>().bounds;
            Assert.That(support.min.y, Is.InRange(-.03f, .02f));
            Assert.That(candles.min.y, Is.EqualTo(support.max.y - .012f).Within(.025f));
        }
        Assert.That(tiles, Is.GreaterThanOrEqualTo(50));
        Assert.That(candleStands, Is.EqualTo(6), "Keep candle stands beside treasure, away from the fighting floor.");
        Assert.That(wallTorches, Is.GreaterThanOrEqualTo(10));
        Assert.That(room.transform.Find("Crypt entrance light"), Is.Null);
        if (Field("authoredForgePrefab") != null)
        {
            Assert.That(imported.localScale.x, Is.GreaterThan(1f));
            Assert.That(imported.localScale.z, Is.GreaterThan(1f));
        }
        Assert.That(room.transform.Find("Workshop door"), Is.Null, "The prototype interior must not overlap the imported room.");
        var spots = (Vector3[])Field("stationPositions");
        var view = (Transform)Field("view");
        var player = (CharacterController)Field("player");
        var door = (GameObject)Field("raidDoor");
        Physics.SyncTransforms();
        Assert.That(Physics.Raycast(new Ray(new Vector3(0, 1, -4.1f), Vector3.back), out var closed, 1.4f), Is.True);
        Assert.That(closed.collider.transform.IsChildOf(door.transform), Is.True);
        var approach = new[] { new Vector3(.35f, .05f, -2), new Vector3(-1.5f, .05f, 1.6f), new Vector3(.55f, .05f, .2f) };
        for (int i = 0; i < spots.Length; i++)
        {
            var enlargedApproach = new Vector3(approach[i].x * 1.25f, approach[i].y,
                -5f + (approach[i].z + 5f) * 1.18f);
            if (i == 0)
                enlargedApproach = new Vector3(1.35f, .05f, spots[i].z - .55f);
            Call("PlaceRaidPlayer", enlargedApproach, 0f);
            view.rotation = Quaternion.LookRotation(spots[i] - view.position);
            Assert.That((int)room.GetType().GetMethod("NearbyStation", Private).Invoke(room, null), Is.EqualTo(i));
            Assert.That(Physics.Raycast(enlargedApproach + Vector3.up * .2f, Vector3.down, .5f), Is.True);
        }
        var trial = (WorkshopBladeTrial)Field("bladeTrial");
        trial.TryAnswer(0, 0, out _);
        trial.TryAnswer(1, 0, out _);
        trial.TryAnswer(2, 1, out _);
        Assert.That(trial.Damage, Is.EqualTo(16));
        Call("ForgeRaidBlade");
        Assert.That(door.activeSelf, Is.False);
        var weapon = view.Find("Forged longsword grip");
        Assert.That(weapon, Is.Not.Null, "Use a textured sword in both forge variants.");
        Assert.That(weapon.Find("Authored steel longsword").GetComponentInChildren<MeshRenderer>(), Is.Not.Null);
        var hand = weapon.Find("Right hand viewmodel");
        var handMesh = hand.GetComponent<MeshFilter>().sharedMesh;
        Assert.That(handMesh.vertexCount, Is.GreaterThan(60), "Main's fitted gauntlet must have real hand/sleeve geometry.");
        Assert.That(handMesh.subMeshCount, Is.EqualTo(3));
        Assert.That(hand.GetComponent<Renderer>().sharedMaterials.Length, Is.EqualTo(3));
        Vector3 onScreen = view.GetComponent<Camera>().WorldToViewportPoint(hand.position);
        Assert.That(onScreen.x, Is.InRange(0f, 1f));
        Assert.That(onScreen.y, Is.InRange(0f, 1f));
        Call("PlaceRaidPlayer", new Vector3(0, .05f, -3.85f), 180f);
        Physics.SyncTransforms();
        for (int step = 0; step < 35; step++) player.Move(new Vector3(0, -.03f, -.1f));
        Assert.That(player.transform.position.z, Is.LessThan(-7f), "The player must be able to walk from the imported room into the raid.");
        Vector3 entry = player.transform.position;
        Quaternion facing = player.transform.rotation;
        Call("StartRaid");
        Assert.That(player.transform.position, Is.EqualTo(entry));
        Assert.That(player.transform.rotation, Is.EqualTo(facing));
        var shutter = (Collider)Field("shutterCollider");
        Assert.That(shutter.enabled, Is.False);
        Call("StepClosingShutter", .18f);
        Assert.That(shutter.enabled, Is.False, "The moving door must remain nonblocking.");
        Call("StepClosingShutter", .6f);
        Assert.That(shutter.enabled, Is.True);
        Call("ShowModal", new object[] { Field("pausePanel") });
        var combat = Field("raidCombat");
        var combatType = combat.GetType();
        var tick = combatType.GetMethod("Tick");
        var captureStrike = combatType.GetMethod("CaptureStrike", Private);
        var resolveStrike = combatType.GetMethod("ResolveStrike", Private);
        var enemies = (IList)combatType.GetField("enemies", Private).GetValue(combat);
        var targets = (IDictionary)combatType.GetField("targets", Private).GetValue(combat);
        var entrance = (GameObject)Field("cryptSealGate");
        var vault = (GameObject)Field("cryptVaultGate");
        Assert.That(entrance.activeSelf && vault.activeSelf, Is.True);
        var sealTarget = new Vector3(-3.5f, .73f, -11.6f);
        Call("PlaceRaidPlayer", new Vector3(-3.5f, .06f, -10f), 180f);
        Physics.SyncTransforms();
        view.rotation = Quaternion.LookRotation(sealTarget - view.position);
        var reach = room.GetType().GetMethod("CryptInReach", Private);
        Assert.That((bool)reach.Invoke(room, new object[] { sealTarget }), Is.True,
            $"Eye {view.position:F2}, forward {view.forward:F2}, hit: " +
            (Physics.Linecast(view.position, sealTarget, out var sightHit) ? sightHit.collider.name : "none"));
        view.Rotate(0, 23f, 0, Space.World);
        Assert.That((bool)reach.Invoke(room, new object[] { sealTarget }), Is.False);
        Call("PlaceRaidPlayer", new Vector3(0, .06f, -11.7f), 180f);
        Physics.SyncTransforms();
        WalkTowardVault(22);
        Assert.That(player.transform.position.z, Is.GreaterThan(-13f));
        Call("TryCryptSeal", 0, false);
        Assert.That((int)Field("cryptOrder"), Is.Zero);
        foreach (int seal in new[] { 1, 2, 0 }) Call("TryCryptSeal", seal, false);
        Assert.That(entrance.activeSelf, Is.False);
        WalkTowardVault(35);
        Assert.That(player.transform.position.z, Is.LessThan(-15f), "The first gate must open a real walkable route.");
        for (int wave = 1; wave <= 2; wave++)
        {
            if (wave == 2)
            {
                Call("TryCryptSeal", 2, true);
                Assert.That(vault.activeSelf, Is.True);
                Call("TryCryptSeal", 1, true);
                Assert.That(vault.activeSelf, Is.False);
                Call("PlaceRaidPlayer", new Vector3(0, .06f, -27.2f), 180f);
                Physics.SyncTransforms();
                WalkTowardVault(38);
                Assert.That(player.transform.position.z, Is.LessThan(-30.5f));
            }
            Call("PlaceRaidPlayer", new Vector3(0, .06f, wave == 1 ? -18f : -38f), 180f);
            tick.Invoke(combat, new object[] { 2f, null });
            Assert.That(enemies.Count, Is.EqualTo(wave == 1 ? 3 : 5));
            tick.Invoke(combat, new object[] { .6f, null }); // Finish the spawn rise before attempting a real hit.
            while (enemies.Count > 0)
            {
                var foe = enemies[0];
                var root = (Transform)foe.GetType().GetField("Root").GetValue(foe);
                int health = (int)foe.GetType().GetField("Health").GetValue(foe);
                var torsoTarget = (Collider)foe.GetType().GetField("TorsoTarget").GetValue(foe);
                // Find a clear rear/side approach around the crypt's pillars; keep the Warder's shield rules intact.
                bool reached = false;
                foreach (float angle in new[] {0f, -45f, 45f, -90f, 90f})
                {
                    Vector3 strikeApproach = Quaternion.Euler(0, angle, 0) * -root.forward * 1.35f;
                    Call("PlaceRaidPlayer", root.position + strikeApproach + Vector3.up * .035f, root.eulerAngles.y);
                    Physics.SyncTransforms();
                    view.rotation = Quaternion.LookRotation(torsoTarget.bounds.center - view.position);
                    captureStrike.Invoke(combat, null);
                    reached = combatType.GetField("aimedRaider", Private).GetValue(combat) == foe;
                    if (reached) break;
                }
                Assert.IsTrue(reached, "There must be an unobstructed approach to " + root.name);
                // A miss at windup can become a hit at impact, including at the legs.
                view.rotation = Quaternion.LookRotation(Vector3.up);
                captureStrike.Invoke(combat, null);
                Assert.That(combatType.GetField("aimedRaider", Private).GetValue(combat), Is.Null);
                view.rotation = Quaternion.LookRotation(root.position + Vector3.up * .4f - view.position);
                resolveStrike.Invoke(combat, null);
                Assert.That((int)foe.GetType().GetField("Health").GetValue(foe), Is.EqualTo(health - trial.Damage));
                health -= trial.Damage;
                view.rotation = Quaternion.LookRotation(torsoTarget.bounds.center - view.position);
                // Even with melee aim tolerance, a solid obstacle must stop the strike.
                var obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstruction.transform.position = Vector3.Lerp(view.position, torsoTarget.bounds.center, .5f);
                obstruction.transform.localScale = new Vector3(.8f, 2f, .8f);
                resolveStrike.Invoke(combat, null);
                Assert.That((int)foe.GetType().GetField("Health").GetValue(foe), Is.EqualTo(health));
                obstruction.GetComponent<Collider>().enabled = false;
                UnityEngine.Object.Destroy(obstruction);
                Vector3 nearEdge = torsoTarget.bounds.center + view.right * (((CapsuleCollider)torsoTarget).radius + .1f);
                view.rotation = Quaternion.LookRotation(nearEdge - view.position);
                Assert.That(torsoTarget.Raycast(new Ray(view.position, view.forward), out _, 2.05f), Is.False);
                captureStrike.Invoke(combat, null);
                Assert.That(combatType.GetField("aimedRaider", Private).GetValue(combat), Is.SameAs(foe),
                    "A sword-width near miss should still connect.");
                view.rotation = Quaternion.LookRotation(Vector3.up);
                resolveStrike.Invoke(combat, null);
                Assert.That((int)foe.GetType().GetField("Health").GetValue(foe), Is.EqualTo(health),
                    "Turning away before impact must cancel the hit.");
                view.rotation = Quaternion.LookRotation(torsoTarget.bounds.center - view.position);
                for (int hit = 0; hit < Mathf.CeilToInt(health / (float)trial.Damage); hit++)
                {
                    captureStrike.Invoke(combat, null);
                    Assert.That(combatType.GetField("aimedRaider", Private).GetValue(combat), Is.SameAs(foe),
                        "Aim must reach the skeleton before the strike is resolved: " + root.name);
                    resolveStrike.Invoke(combat, null);
                }
                Assert.That(enemies.Contains(foe), Is.False, "The weakest blade must defeat each enemy with its existing health.");
                Assert.That(targets.Count, Is.EqualTo(enemies.Count * 2), "Defeated skeletons must release their hit targets.");
                var fallen = (IList)combatType.GetField("debris", Private).GetValue(combat);
                Assert.That(fallen.Count, Is.GreaterThanOrEqualTo(6));
                var skull = (GameObject)fallen[fallen.Count - 6];
                Assert.That(skull.TryGetComponent<Rigidbody>(out var body) && !body.isKinematic, Is.True);
                Assert.That(skull.TryGetComponent<BoxCollider>(out _), Is.True);
            }
        }
        Call("CollectCryptLoot", 0);
        Call("CollectCryptLoot", 0);
        Assert.That((int)Field("cryptGoldEarned"), Is.EqualTo(15), "Opening the same chest twice cannot pay twice.");
        Assert.That((int)Field("cryptXpEarned"), Is.EqualTo(20));
        Assert.That((bool)tick.Invoke(combat, new object[] { .02f, null }), Is.True);
        Assert.That(player.enabled, Is.True);
        var result = (GameObject)Field("raidResult");
        Assert.That(System.Array.Exists(result.GetComponentsInChildren<Button>(true),
            button => button.GetComponentInChildren<TMP_Text>().text == "Reforge"), Is.False);
        Call("RetryRaid");
        Assert.That(trial.Total, Is.EqualTo(3), "Retry keeps your forged sword.");
        Assert.That((int)Field("cryptGoldEarned"), Is.Zero);
        Assert.That((int)Field("cryptXpEarned"), Is.Zero);
        Assert.That(entrance.activeSelf && vault.activeSelf, Is.True);

        void WalkTowardVault(int steps)
        {
            for (int i = 0; i < steps; i++) player.Move(new Vector3(0, -.03f, -.1f));
        }
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (room != null) UnityEngine.Object.Destroy(room.gameObject);
        if (QuestSession.Instance != null) QuestSession.Instance.Clear();
        yield return null;
        if (checkpoint != null && File.Exists(checkpoint)) File.Delete(checkpoint);
    }
}
