# Pet Warden & Opinion

- `RagdollSystem.OpinionFor(actorOrObject)` → `RagdollOpinion` (affinity, trust, fear, ownership, likes/dislikes)
- `PetWarden.Judge(pet, actor, kind)` → Allow / SoftRedirect / Deny / EscalateThreat
- Used by ConsentWarden / ThreatWarden integration points
