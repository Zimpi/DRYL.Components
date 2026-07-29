# Sprachmodus für das Canvas-Dock — gpt-realtime über WebRTC

**Datum:** 2026-07-29
**Betrifft:** `DRYL.Components.Agents` (Kern), `DRYL.Portfolio` (erster Konsument), `DRYL.Website` (Katalog)
**Status:** freigegeben (Ansatz A)

---

## 1. Ziel

Der Assistent bekommt eine Stimme. Man drückt im Dock auf das Mikrofon und redet — ohne
Aufnahmeknopf, ohne Senden, ohne Warten. Man darf mitten in die Antwort hineinreden und die KI
hört auf. Die KI kann dabei alles, was sie im Textmodus kann: Werkzeuge aufrufen, Ansichten auf
dem Canvas bauen, Rückfragen als Dialog stellen.

Das Vorbild ist der Voice-Modus von ChatGPT: ein durchgehendes Gespräch, nicht eine Kette von
Sprachnachrichten.

## 2. Nicht-Ziele

- **Kein Einstellungs-UI.** Stimme, Tonalität, Modell und Gesprächsverhalten stellt der
  Entwickler in C# ein. Weder das Paket noch das Portfolio bieten dafür eine Oberfläche.
- **Keine Provider-Abstraktion.** Das ist die OpenAI-Realtime-API, nicht ein
  „Sprach-Framework". Ein zweiter Anbieter ist heute nicht in Sicht; eine Abstraktion für einen
  hypothetischen zweiten wäre Ballast.
- **Kein SIP/Telefonie**, keine Übersetzungs- oder reinen Transkriptionssessions.
- **Keine eigene Chat-Oberfläche.** Das Transkript lebt im Dock-Log, das der Host schon füllt.

## 3. Fachliche Entscheidungen (aus dem Brainstorming)

| Frage | Entscheidung |
|---|---|
| Wo lebt der Code? | In der Bibliothek, `DRYL.Components.Agents` |
| Wie viel kann die Stimme? | Alles — dieselbe Werkzeugliste wie der Text-Assistent |
| Ein Assistent oder zwei? | Ein Verlauf: Übergabe beim Ein- und Aussteigen |
| Konfiguration | Entwickler-API in C#, kein UI |
| Form im Dock | Übernahme — das Dock wird zum Sprach-Panel |

## 4. Warum WebRTC und nicht der Circuit

Die Alternative wäre gewesen, das Audio durch den Blazor-Circuit zu leiten und serverseitig eine
WebSocket-Session zu fahren. Das kostet pro Richtung eine Base64-Kodierung, einen SignalR-Hop und
die Puffer an beiden Enden — zusammen leicht ein paar hundert Millisekunden. Genau die
Millisekunden sind der Unterschied zwischen „Gespräch" und „Funkgerät". Barge-in (der Nutzer redet
in die Antwort hinein) wird darüber unbrauchbar, weil der Abbruch denselben Weg zurücklaufen muss.

WebRTC gibt dem Browser einen direkten Medienpfad zu OpenAI. Der Server bleibt trotzdem der Chef:
er prägt das Zugangstoken, er kennt den Systemprompt, und er führt jedes Werkzeug aus.

## 5. Aufbau

Sechs Teile, jeder mit einer Aufgabe.

```
Host (z. B. AssistantAgentService)
  │  besitzt
  ▼
DrylVoiceRun ────────────────────────► DrylCanvasDock Voice="run"
  │  StartAsync/StopAsync                  rendert Übernahme + DrylVoiceOrb
  │
  ├─► DrylVoiceRunner (scoped DI)
  │      • POST /v1/realtime/client_secrets   (API-Key bleibt hier)
  │      • führt AIFunction-Aufrufe aus
  │
  └─► dryl-voice.js  (Browser)
         • getUserMedia → RTCPeerConnection → /v1/realtime/calls
         • Datenkanal "oai-events"
         • Pegelmessung, Orb-Ansteuerung
```

### 5.1 `DrylVoiceOptions` — was der Entwickler einstellt

Ein einfaches Objekt, kein Fluent-Builder.

| Eigenschaft | Vorgabe | Bedeutung |
|---|---|---|
| `ApiKey` | — | Pflicht. Bleibt im Server. |
| `Model` | `gpt-realtime-2.1` | auch `gpt-realtime-2`, `gpt-realtime-2.1-mini` |
| `Instructions` | `null` | Persona/Tonalität. Der Host reicht denselben Systemprompt durch wie im Textmodus. |
| `Voice` | `marin` | eine der zehn Stimmen; `marin`/`cedar` sind die besten |
| `Speed` | `1.0` | 0.25–1.5 |
| `TurnDetection` | `SemanticVad` | `SemanticVad` / `ServerVad` / `PushToTalk` |
| `ReasoningEffort` | `null` | `low` / `medium` / `high`; nur 2.1 und mini |
| `TranscriptionModel` | `gpt-4o-transcribe` | ohne das gibt es keinen Nutzertext im Log; auch `whisper-1`, `gpt-4o-mini-transcribe` |
| `Language` | `null` | ISO-Code für die Transkription |
| `Tools` | leer | `IList<AITool>` — dieselbe Liste wie der Text-Agent |
| `NoiseReduction` | `NearField` | `NearField` / `FarField` / `Off` |
| `IdleTimeout` | 2 min | Stille, nach der die Session von selbst zumacht |
| `MaxDuration` | 30 min | harte Obergrenze (API-Limit sind 60 min) |
| `BaseUrl` | `https://api.openai.com/v1` | für Azure/Proxy |
| `SafetyIdentifier` | `null` | gehashte Nutzer-ID für den `OpenAI-Safety-Identifier`-Header |

`ToSessionPayload()` übersetzt das in den `session`-Block, den
`POST /v1/realtime/client_secrets` erwartet — inklusive der Werkzeug-Schemas, die aus
`AIFunction.JsonSchema` fallen.

Die Stimme ist nach der ersten Audioausgabe für die Session gesperrt. Deshalb wird sie im Token
festgelegt und **nicht** zur Laufzeit geändert; wer sie wechseln will, startet eine neue Session.
Das ist eine API-Eigenschaft, keine Einschränkung dieses Entwurfs — sie steht so in der XML-Doku.

### 5.2 `DrylVoiceRun : DrylRunBase` — der beobachtbare Zustand

Der Host erzeugt und besitzt ihn, so wie er heute `DrylCanvasRun` besitzt. Er überlebt damit
Navigation und Dock-Reset.

```csharp
public enum VoicePhase { Idle, Connecting, Live, Closing }
public enum VoiceActivity { Listening, UserSpeaking, Thinking, Speaking }
```

- `Phase`, `Activity`, `Transcript` (`IReadOnlyList<DrylVoiceMessage>`), geerbtes `Error`,
  geerbtes `ToolCalls`, geerbtes `OnChange`.
- `StartAsync(IEnumerable<DrylVoiceMessage>? history = null)` / `StopAsync()`.
- `State` (die geerbte `AiState`) wird abgeleitet, **es entsteht kein neues AI-Vokabular**
  (Regel 2.10):

  | Phase / Activity | `AiState` |
  |---|---|
  | Idle | `None` |
  | Connecting | `Thinking` |
  | Listening / UserSpeaking | `Active` |
  | Thinking | `Thinking` |
  | Speaking | `Streaming` |

**Die Pegel stehen bewusst nicht im Run.** Ein Lautstärkewert, der 30-mal pro Sekunde durch den
Circuit läuft und `StateHasChanged` auslöst, ist 30 Renderdurchläufe pro Sekunde für eine
Animation — auf Blazor Server ist das der sichere Weg, die Seite zäh zu machen. Der Pegel bleibt
im Browser und schreibt direkt eine CSS-Variable auf den Orb. Nur Zustandswechsel
(spricht/hört/denkt) überqueren die Grenze, und das sind ein paar pro Minute.

### 5.3 `DrylVoiceRunner` — die Serverseite

Scoped, registriert in `AddDrylAgents()`. Zwei Aufgaben:

1. **Token prägen.** `POST {BaseUrl}/realtime/client_secrets` mit vollständiger Session im Body.
   Antwort ist ein `ek_…`-Wert. Nur dieser Wert geht in den Browser — nie der API-Key. Der
   Nutzen der eingebetteten Session: der Browser kann Instructions, Werkzeuge oder Modell nicht
   verändern.
2. **Werkzeuge ausführen.** Ruft die passende `AIFunction` aus `Options.Tools` auf. Der Browser
   liefert nur einen Namen; ausgeführt wird ausschließlich, was in der Liste steht.

### 5.4 `dryl-voice.js` — der Browserteil

Lazy geladenes ES-Modul unter `_content/DRYL.Components.Agents/js/dryl-voice.js`, wie
`dryl-aifield.js`.

Verbindungsaufbau:

1. `getUserMedia({ audio: { echoCancellation: true, noiseSuppression: true } })`
2. `new RTCPeerConnection()`, Mikro-Track hinzufügen
3. `pc.createDataChannel('oai-events')`
4. `ontrack` → verstecktes `<audio autoplay>` bekommt den Remote-Stream
5. `createOffer` → `POST {BaseUrl}/realtime/calls` mit `Content-Type: application/sdp` und
   `Authorization: Bearer ek_…` → SDP-Antwort → `setRemoteDescription`

Danach übersetzt das Modul Serverereignisse in Aufrufe nach .NET:

| Ereignis | Wirkung |
|---|---|
| `input_audio_buffer.speech_started` | `Activity = UserSpeaking` |
| `input_audio_buffer.speech_stopped` | `Activity = Thinking` |
| `response.output_audio.delta` (erstes) | `Activity = Speaking` |
| `response.done` | `Activity = Listening`, Antworttext ins Transkript |
| `conversation.item.input_audio_transcription.completed` | Nutzertext ins Transkript |
| `response.function_call_arguments.done` | Werkzeug-Brücke (5.5) |
| `error` | `Error` setzen |

Die Pegelmessung läuft über zwei `AnalyserNode` (Mikro und Remote-Stream) in einer
`requestAnimationFrame`-Schleife und schreibt `--voice-level` auf das Orb-Element. Kein
Interop-Aufruf pro Frame.

### 5.5 Die Werkzeug-Brücke

```
Modell ──function_call──► Datenkanal ──► JS ──DotNetObjectReference──► DrylVoiceRun
                                                                          │
                                                            AIFunction.InvokeAsync
                                                                          │
        Datenkanal ◄── JS ◄────────────── Ergebnis-JSON ◄─────────────────┘
        conversation.item.create (function_call_output) + response.create
```

Damit greifen die vorhandenen Werkzeuge unverändert: `AssistantTools` (Portfolio-CRUD, Posteingang,
E-Mail, GitHub), `DrylCanvasTools` (`open_view`, `create_artifact`, `update_artifact`) und die
HITL-Dialoge (`ask_choice`, `request_permission`). Der Bestätigungsdialog vorm Löschen erscheint
also auch mitten im Gespräch — das Audio läuft dabei weiter, der Nutzer kann die Frage beantworten
oder sie aussprechen.

Jeder Aufruf wird als `DrylToolInvocation` an den Run gehängt, damit `DrylAgentToolCalls` ihn im
Dock-Log zeigt — man sieht, was die Stimme gerade tut.

**Fehler werden zurückgemeldet, nicht verschluckt.** Wirft ein Werkzeug, geht ein
`function_call_output` mit der Fehlermeldung zurück. Sonst wartet das Modell auf ein Ergebnis, das
nie kommt, und das Gespräch bleibt stumm stehen — der schlechteste aller Fehlerzustände.

### 5.6 Die Übergabe: ein Verlauf

**Hinein:** `StartAsync(history)` bekommt die bisherigen Text-Turns. Sobald der Datenkanal offen
ist, schickt das Modul sie als `conversation.item.create`-Events. Die Stimme weiß damit, worüber
geschrieben wurde.

**Hinaus:** `run.Transcript` steht dem Host nach dem Beenden zur Verfügung. Wie er ihn in seine
Text-Session zurückspielt, ist Sache des Hosts — die Bibliothek schreibt ihm keine
`AgentSession`-Mechanik vor. Das Portfolio hängt die Sprach-Turns als Kontext an die nächste
Text-Runde an (Details im Plan).

### 5.7 `DrylVoiceOrb` — die sichtbare Stimme

Eine runde Fläche, die aus den vorhandenen AI-Primitiven gebaut ist: `.ai-aura` +
`.ai-aura-ring` + `.ai-aura-comet` + `.ai-aura-glow`, dazu scoped CSS für Form und Pegel. **Keine
neue Farbe, kein neues Timing, keine Änderung an `dryl.css`** (Regel 2.10).

- Der Zustand wird über die schon vorhandenen Aura-Klassen gefahren (`ai-thinking`,
  `ai-streaming`), also über dieselbe `AiState`-Ableitung wie jede andere KI-Fläche.
- Der Pegel skaliert den Orb: `scale: calc(1 + var(--voice-level) * .16)` — eine
  Compositor-Eigenschaft, keine Layout-Eigenschaft.
- `prefers-reduced-motion: reduce` schaltet die Pegelskalierung ab; die Aura bringt ihre
  Reduktion schon mit.
- Der Orb ist `aria-hidden` — dekorativ. Angesagt wird über die Statuszeile des Docks, die
  bereits `aria-live="polite"` trägt.

### 5.8 Die Dock-Übernahme

`DrylCanvasDock` bekommt zwei Parameter:

```csharp
[Parameter] public DrylVoiceRun? Voice { get; set; }
[Parameter] public string VoiceLabel { get; set; } = "Talk to the assistant";
```

Ohne `Voice` ändert sich am Dock nichts — die Erweiterung ist rückwärtskompatibel.

- **Phase `Idle`:** ein Mikrofon-Knopf im Kopf, links vom Log-Umschalter, mit `DrylTooltip` und
  `AriaLabel` (Regel 2.11).
- **Phase ≠ `Idle`:** Composer, Vorschläge und Kontext-Chip weichen; an ihrer Stelle stehen Orb,
  die zuletzt gesprochene Zeile und ein Beenden-Knopf. Der Log-Umschalter bleibt, das Transkript
  fließt weiter ins Log.
- Beide Richtungen laufen über `DrylPresence` — der Wechsel ist eine Bewegung, kein Umschalten
  (Regel 2.12).

Die Statuszeile des Docks spricht den Zustand aus: „Hört zu", „Denkt nach", „Spricht", „Verbindet"
— vom Host über `Status` überschreibbar wie bisher.

## 6. Fehler und Grenzen

| Fall | Verhalten |
|---|---|
| Mikrofon verweigert | `Phase = Idle`, `Error` mit klarem Text, Dock fällt in den Textmodus zurück |
| Kein API-Key | Der Host reicht keinen `Voice`-Run — der Mikrofon-Knopf existiert gar nicht |
| Token abgelehnt (401/429) | `Error` mit der API-Meldung, Verbindung wird abgeräumt |
| Verbindung bricht | `oniceconnectionstatechange` → `failed`/`disconnected` schließt sauber |
| Stille | Nach `IdleTimeout` schließt die Session von selbst |
| Dauer | Nach `MaxDuration` schließt die Session; das API-Limit sind 60 min |
| Prerender / kein JS | Start ist ein no-op; das Dispose ist über ein `_attached`-Flag abgesichert |
| Circuit weg | `JSDisconnectedException` wird geschluckt, PeerConnection stirbt mit der Seite |

## 7. Sicherheit

- Der API-Key verlässt den Server nie. Der Browser bekommt ein `ek_…`-Token, das nach 60 s
  abläuft und nur zum Verbindungsaufbau taugt.
- Die Session ist im Token festgeschrieben: Instructions, Modell und Werkzeugliste kann der
  Browser nicht verändern.
- Werkzeuge laufen im Circuit unter der Identität des angemeldeten Nutzers. Ein manipulierter
  Browser kann keinen Werkzeugnamen erfinden, weil der Runner nur ausführt, was in
  `Options.Tools` steht.
- `SafetyIdentifier` wird als `OpenAI-Safety-Identifier`-Header mitgeschickt, wenn gesetzt.

## 8. Tests

Automatisierbar ist alles außer der eigentlichen WebRTC-Strecke.

- **`DrylVoiceOptions`** — `ToSessionPayload()` erzeugt die erwartete Struktur: Modell, Stimme,
  Tempo, Turn-Detection, Transkriptionsmodell, Werkzeug-Schemas. `PushToTalk` setzt
  `turn_detection: null`.
- **Werkzeug-Brücke** — unbekannter Name ergibt ein Fehler-Output statt einer Exception; ein
  bekannter Name liefert Ergebnis-JSON; die Invocation landet im Run.
- **`DrylVoiceRun`** — die Phase-/Activity-→-`AiState`-Abbildung; `StopAsync` aus `Idle` ist ein
  no-op; doppeltes `StartAsync` startet nicht zweimal.
- **Dock (bUnit)** — ohne `Voice` kein Mikrofon-Knopf; mit `Voice` und `Idle` ein Knopf mit
  Tooltip; bei `Live` ist der Composer weg und der Orb da.

Die Verbindung selbst wird von Hand am laufenden Portfolio geprüft. Das steht so im Plan und wird
nicht als „getestet" ausgegeben, bevor es gelaufen ist.

## 9. Versionierung und Doku

- `DRYL.Components.Agents`: 0.15.0 → **0.16.0** (MINOR — neue Komponenten und Parameter).
- `DRYL.Components` (Kern): unverändert, solange `dryl.css` nicht angefasst wird. Der Orb ist
  bewusst so entworfen, dass er ohne neue Tokens auskommt.
- `CHANGELOG.md`: Eintrag unter `Added`.
- `DRYL.Website` → `ComponentCatalog`: `DrylVoiceOrb` und der neue `Voice`-Parameter am
  `DrylCanvasDock`.

## 10. Annahmen, die ohne Rückfrage getroffen wurden

Der Auftraggeber war während der Umsetzung nicht erreichbar. Diese Punkte sind nach bestem
Ermessen entschieden und leicht zu drehen:

1. **Vorgabestimme `marin`** — von OpenAI als eine der beiden besten empfohlen. Das Portfolio
   setzt sie explizit im Code, also ist der Wechsel eine Zeile.
2. **Vorgabemodell `gpt-realtime-2.1`** — die beste Qualität der drei genannten. Für das
   Portfolio bleibt es dabei; `-mini` kostet ein Drittel und ist eine Zeile entfernt.
3. **`SemanticVad` als Vorgabe** — OpenAI empfiehlt es für Gespräche, weil es auf den Sinn
   wartet statt auf die Stille und damit seltener in eine Denkpause hineinredet.
4. **Transkription an** — ohne sie steht im Log kein Nutzertext, und die Übergabe an den
   Textmodus hätte nur eine Hälfte des Gesprächs.
5. **`IdleTimeout` 2 min / `MaxDuration` 30 min** — eine offene Sprachsession kostet pro Minute,
   auch wenn niemand redet. Beides ist konfigurierbar.
6. **Die Sprach-Session bekommt denselben Systemprompt wie der Textmodus**, ergänzt um einen
   kurzen Sprach-Zusatz (kürzer antworten, keine Markdown-Auszeichnung vorlesen, Zahlen
   ausschreiben). Ein vorgelesener Markdown-Block wäre sonst genau das, was er ist: unhörbar.
