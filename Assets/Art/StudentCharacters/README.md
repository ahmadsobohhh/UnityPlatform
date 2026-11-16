# Student deckhand characters

The creator, student profile/dashboard portrait, and ship player instantiate the same two clothed prefabs from `Assets/Resources/StudentCharacters/Catalog.asset`.

## Included art

Quaternius free Standard downloads, obtained 15 November 2026:

- [Universal Base Characters](https://quaternius.itch.io/universal-base-characters): heads, eyes, eyebrows, source hair meshes and textures.
- [Modular Character Outfits – Fantasy](https://quaternius.itch.io/modular-character-outfits-fantasy): Male/Female Peasant outfits, used as a fixed linen-and-leather deckhand uniform.
- [Universal Animation Library](https://quaternius.itch.io/universal-animation-library): idle, walk, jog and airborne animation clips.

The licence files shipped inside these specific Standard archives explicitly place their contents under CC0 1.0. Exact included licence text is retained in `Licenses/`; source archive paths and SHA-256 hashes for imported files are in `Sources.json`. No paid pack or external runtime plugin is required.

## Assembly

`StudentCharacterAssetTools.Build()` configures Humanoid rigs, preserves the outfit's submeshes, and builds the two prefabs and shared locomotion controller. `StudentCharacterMeshTools` clips and caps a continuous head/neck mesh below the collar, tapers the hidden neck extension inside the shirt, and assigns neck/spine weights rather than shoulder weights. This closes the previous neck gap while keeping shoulder skin out of the uniform during movement. The male outfit's exposed-arm submesh uses a separate skin material matched to the head texture; changing skin never changes clothing.

The open neckline also has a fitted, skinned upper-chest surface. The builder welds the torso's seam vertices, finds the actual neckline boundary, and lofts skin from inside the neck beneath the garment's trim. The outer boundary retains the garment's bone weights; the inner boundary follows the neck/spine. This fills the wider collar and front slit from elevated camera angles without restoring the full body's clipping shoulders. Both collar meshes use the same sampled skin material and appearance tint as exposed arm skin.

Hair is fitted at build time against each body's baked scalp and attached to its Head bone. Boy choices are Bald, Crew Cut, Side Part and Coils; girl choices are Bald, Short Curls, Long and Twin Buns. The curls are generated locally over a fitted scalp shell. Bald has no hair renderer. Compatibility-only fitted styles retain older saved choices without offering extra menu options.

Generated meshes and prefabs belong in source control so teammates do not need to rebuild them. The catalog references the existing cabin background and game fonts; the creator uses its own font materials without changing shared font styling elsewhere. The builder uses an isolated preview scene and does not dirty the open gameplay scene.

Familiars and outfit selection are outside this feature. Both body types have one fixed uniform.
