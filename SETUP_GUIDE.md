# Troubleshooter - Setup & Fertigstellung Guide

## 🎮 Neue Features hinzugefügt

### 1. **Wave/Runden System**
- `WaveManager.cs`: Verwaltet Runden mit progressiver Schwierigkeit
  - Runde N: 1,3^(N-1) mal mehr Feinde
  - Runde N: Feinde 1,1^(N-1) mal schneller
  - Runde N: Feinde 1,15^(N-1) mal mehr HP
  - Pausen zwischen Runden (3s)

- `WaveController.cs`: Tracking von Feind-Kills, automatischer Wave-Übergang

### 2. **Highscore System**
- `HighScoreManager.cs`: Persistierte Top-10 Scores
  - Speichert: Name, Score, Wave erreicht, Datum
  - Nutzt PlayerPrefs + JSON
  - Automatisch beim Game Over aufgefordert zu speichern

### 3. **Allied Units System**
- Freundliche Einheiten, die die Base erreichen, werden zu `AlliedDefender`
- AlliedDefender spawnt neben der Base
- Schießt automatisch auf Feinde in Range
  - Shootrange: 8 Units
  - Cooldown: 1,5s
  - Damage: 2 pro Hit
- `AlliedProjectile`: Auto-verfolgend, zerstört Feinde

### 4. **Audio System**
- `AudioManager.cs`: Zentral verwaltet alle Audio-Events
  - Master Volume, Music Volume, SFX Volume (getrennt)
  - SFX Pool (max 8 gleichzeitige Sounds)
  - Settings persistiert in PlayerPrefs

**Audio Events (zu implementieren als AudioClips):**
- `enemy_spawn`: Feind spawnt
- `gaze_hit`: Gaze trifft Feind
- `enemy_death`: Feind stirbt (Gaze)
- `enemy_attack`: Feind attackiert Base
- `score_gained`: Score erhöht sich
- `base_destroyed`: Base zerstört
- `ally_arrived`: Freundliche Einheit erreicht Base
- `allied_shoot`: Allied Defender schießt
- `projectile_hit`: Projektil trifft Feind

### 5. **Main Menu & Settings**
- `MainMenuUI.cs`: Play, Settings, Highscores, Quit Buttons
- `SettingsPanel.cs`: Volume Control mit Schiebereglern
  - Master, Music, SFX separate kontrolle
  - Reset to Defaults Button

## 🛠️ Setup-Schritte

### Phase 1: Scene Struktur

1. **Rename SampleScene → Gameplay**
   - Öffne `Assets/Scenes/SampleScene.unity`
   - Rechtsklick → Rename zu `Gameplay.unity`

2. **Erstelle MainMenu Scene**
   - Neue Scene erstellen: `File → New Scene`
   - Speichern als `Assets/Scenes/MainMenu.unity`

### Phase 2: MainMenu Scene Setup

1. **Basis-Elemente:**
   - Canvas erstellen (falls nicht vorhanden)
   - Background Image (optional)
   - 4 Buttons: Play, Settings, Highscores, Quit

2. **Script hinzufügen:**
   - MainMenuUI.cs an Canvas angehängen
   - SettingsPanel.cs an Settings-Panel angehängen
   - Play Button → OnClick → MainMenuUI.PlayGame()
   - Etc.

3. **Settings Panel:**
   - Neues Panel (UI) mit:
     - 3 Slider (Master, Music, SFX)
     - 3 Labels (Wert-Anzeige)
     - 1 Back Button
   - Standard deaktiviert

4. **Highscores Panel:**
   - Neues Panel mit:
     - TextMeshPro für Scores-Liste
     - Back Button
   - Standard deaktiviert

### Phase 3: GamePlay Scene Updates

1. **Manager hinzufügen:**
   - Neues GameObject: `Managers`
   - Child GameObjects:
     - WaveManager (mit WaveManager.cs Script)
     - WaveController (mit WaveController.cs Script)
   - Optional: GameInitializer für Audio/HighScore Manager

2. **UI aktualisieren:**
   - Score Text: Zeigt jetzt "Score: X"
   - Neuer Wave Text: "Wave: N"
   - Game Over Screen:
     - Final Score Text
     - Final Wave Text
     - Player Name InputField
     - Submit Score Button
   - Submit Button → BaseHealth.OnSubmitScore()

3. **Prefabs vorbereiten:**
   - AlliedDefenderPrefab:
     - GameObject mit AlliedDefender.cs
     - Sprite (grüner Charakter)
     - Rigidbody2D + BoxCollider2D
   - Projektil-Prefab:
     - GameObject mit AlliedProjectile.cs
     - Rigidbody2D + BoxCollider2D
     - Sprite/Visuals
   - FriendReturningHome:
     - AlliedDefenderPrefab zuweisen im Inspector

### Phase 4: Audio Setup

1. **Folder erstellen:**
   - `Assets/Audio/SFX/` - alle SoundEffects
   - `Assets/Audio/Music/` - Background Music

2. **AudioManager Prefab:**
   - Erstelle leeres GameObject: `AudioManager`
   - Hänge AudioManager.cs an
   - Erstelle Array für `soundEffects` (ID + Clip)
   - Alle Audio IDs hinzufügen (siehe Liste oben)
   - Speichern als Prefab in `Assets/Prefabs/`

3. **GameInitializer:**
   - Neues GameObject in MainMenu + Gameplay Szene
   - GameInitializer.cs angehängt
   - Prefab Referenzen einstellen

### Phase 5: Scene Manager Update

1. **Build Settings:**
   - `File → Build Settings`
   - Szenen hinzufügen:
     - 0: MainMenu
     - 1: Gameplay
   - Speichern

## 📊 Balance-Werte

Anpassungen in `WaveManager.cs`:

```csharp
[SerializeField] private float baseEnemyCount = 3f;      // Wave 1: 3 Feinde
[SerializeField] private float baseSpawnInterval = 2f;   // Spawn-Speed
[SerializeField] private float baseDifficultyMultiplier = 1.3f; // Pro Wave +30%
[SerializeField] private float wavePauseDuration = 3f;   // Pause zwischen Wellen
```

**Beispiel-Progression:**
- Wave 1: 3 Feinde, 5 HP, 1x Speed, 2s Spawn
- Wave 2: 4 Feinde, 6 HP, 1.1x Speed, 1.5s Spawn
- Wave 3: 5 Feinde, 7 HP, 1.2x Speed, 1.2s Spawn
- Wave 5: 8 Feinde, 8 HP, 1.4x Speed, 1.0s Spawn

Anpassbar via Inspector!

## 🎵 Audio Clips Beschaffung

**Freie Sound-Ressourcen:**
- Freesound.org (CC Lizenz)
- OpenGameArt.org
- Zapsplat.com
- Kenney.nl (Game Audio)

**Benötigte Sounds (~2s Länge):**
- Enemy Spawn: Sci-Fi Ping
- Gaze Hit: Laser/Beam Sound
- Enemy Death: Explosion/Puff
- Enemy Attack: Impact
- Score Gained: Coin/Chime
- Base Destroyed: Alarm/Boom
- Ally Arrived: Positive Chime
- Allied Shoot: Blaster/Laser
- Projectile Hit: Impact

## ✅ Testing Checklist

- [ ] Neue Szenen laden korrekt
- [ ] WaveManager startet Wellen mit korrektem Schwierigkeitsmultiplikator
- [ ] Feinde spawnen korrekte Anzahl pro Welle
- [ ] AlliedDefender spawnt wenn Friendly die Base erreicht
- [ ] AlliedDefender schießt auf Feinde
- [ ] Audio Manager lädt Clips korrekt
- [ ] Highscores speichern/laden funktioniert
- [ ] Settings UI aktualisiert Volumes korrekt
- [ ] Game Over Screen zeigt Final Score + Wave
- [ ] Score Submission funktioniert
- [ ] MainMenu zu Gameplay Navigation funktioniert

## 🐛 Häufige Fehler

1. **Scenes nicht im Build Settings**: Gameplay kann nicht laden
   - Lösung: Build Settings → MainMenu + Gameplay hinzufügen

2. **Manager nicht in Szene**: Singleton ist null
   - Lösung: GameInitializer nutzen oder manuell instantiieren

3. **Audio Clips nicht zugewiesen**: Playback funktioniert nicht
   - Lösung: AudioManager Inspector → soundEffects Array befüllen

4. **AlliedDefender findet Enemies nicht**: Keine Schüsse
   - Lösung: Feinde müssen Tag "Enemy" haben
   - Oder AlliedDefender.cs anpassen

## 🚀 Nächste Schritte (Optional)

- [ ] Visuelle Wave-Übergänge (Flash, Text)
- [ ] Pause Button im Gameplay
- [ ] Grafische Verbesserungen (Sprites, Animationen)
- [ ] Soundtrack (statt SFX)
- [ ] Leaderboard Online-Integration
- [ ] Mobile Support (Gaze-Tracking auf VR)
- [ ] Tutorial/Onboarding
- [ ] Unterschiedliche Enemy-Typen
- [ ] PowerUps System

---

**Stand**: May 2026  
**Autor**: AI Assistant  
**Status**: Feature-Complete, Bereit für Setup & Testing
