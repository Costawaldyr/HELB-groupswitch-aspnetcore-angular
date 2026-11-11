# GroupSwitch - Intégration de l'IA et workflow de développement

Ce document explique, en français, comment intégrer l'IA dans ce projet de manière cohérente avec le cahier des charges, l'architecture du projet, les règles métier et les contraintes du cours.

Il complète le `README.md` principal du projet.

## 1. Objectif du projet

GroupSwitch est une application de gestion des changements d'affectation d'étudiants dans des groupes pédagogiques.

Le système doit permettre :

- de gérer les étudiants, les groupes, les blocs et les affectations ;
- qu'un étudiant puisse demander un changement de groupe ;
- qu'un administrateur valide ou refuse cette demande ;
- de vérifier les conflits d'horaires avec des règles métier fiables ;
- de faire du scraping sur Hyperplanning pour récupérer les cours d'un quadrimestre ;
- d'exporter les groupes en CSV et PDF ;
- d'intégrer une IA pour aider l'admin à analyser le motif libre d'une demande.

## 2. Ce que l'IA doit faire et ce qu'elle ne doit pas faire

C'est la règle la plus importante du projet.

### 2.1. Ce que l'IA fait

L'IA lit le texte libre saisi par l'étudiant et le transforme en données structurées, par exemple :

- résumé du motif ;
- préférences de groupe ;
- contraintes personnelles ;
- éléments manquants ;
- contexte de la demande.

Exemple de sortie attendue :

```json
{
  "summary": "L'étudiant souhaite changer de groupe pour éviter un conflit de planning.",
  "preferences": ["groupe plus tôt le matin", "éviter les cours en bloc 2"],
  "constraints": [
    "ne pas modifier les autres affectations",
    "respecter le même bloc"
  ],
  "missingInformation": ["date exacte de la demande", "groupe cible souhaité"]
}
```

### 2.2. Ce que l'IA ne doit pas faire

L'IA ne doit pas calculer les chevauchements d'horaires.

Cela est fait par le backend en C# à partir des données de cours scrapées.

Autrement dit :

- scraping + parsing + logique métier = C#
- analyse du texte libre = IA
- validation des horaires = C#
- décision de compatibilité d'un groupe = C#

C'est un point très important si ton prof attend une bonne compréhension du projet.

### 2.3. Rôle exact des utilisateurs

- L'étudiant remplit un formulaire de demande.
- L'étudiant n'a jamais accès à l'IA directement.
- L'admin reçoit la demande.
- L'admin décide s'il active l'analyse IA ou non.
- L'admin valide ou refuse définitivement la demande.

Donc l'IA est un outil d'assistance pour l'admin, pas un outil pour les étudiants.

## 3. Architecture recommandée

Le projet doit rester proprement séparé par couches.

```text
GroupSwitch/
├── .github/workflows/ci.yml
├── frontend/                     # Angular ou Vue
├── docs/                         # rapport, captures, README
├── backend/
│   ├── GroupSwitch.Api/          # contrôleurs, Program.cs, auth, SignalR
│   ├── GroupSwitch.Business/      # règles métier, scraping, IA, validation
│   ├── GroupSwitch.Data/         # DbContext, entities, repositories, migrations
│   └── GroupSwitch.Tests/        # tests unitaires et d'intégration
└── README_IA_IMPLEMENTATION.md
```

### 3.1. Références .NET recommandées

Tu avais bien noté :

```bash
dotnet add GroupSwitch.Business reference GroupSwitch.Data
dotnet add GroupSwitch.Api reference GroupSwitch.Business GroupSwitch.Data
dotnet add GroupSwitch.Tests reference GroupSwitch.Business GroupSwitch.Data
```

C'est une bonne base pour l'architecture.

## 4. Quel type d'IA utiliser ?

Tu veux une IA gratuite, locale et/ou API.

### 4.1. Option A : API gratuite (recommandée pour le projet)

Le meilleur choix rapide est :

- Groq

Pourquoi ?

- gratuite dans les limites d'usage ;
- rapide ;
- bien adaptée à l'analyse de texte ;
- simple à intégrer en ASP.NET Core ;
- très pratique pour un prototype.

Exemples de modèles souvent utilisés :

- Llama 3.x
- Mistral
- modèles Groq compatibles

### 4.2. Option B : LLM local avec Ollama

Si tu veux un mode sans clé API et sans internet :

- Ollama

C'est très bien pour :

- tests locaux ;
- fallback de secours ;
- travail hors ligne.

### 4.3. Option C : mode simulé (très important pour les tests)

Quand le prof dit que l'IA ne doit pas faire planter le reste du système, il faut avoir un mode de secours.

Donc on introduit :

- `FakeRequestAnalyzer`

Ce service simule une réponse IA et permet de continuer le fonctionnement même si l'API est en panne.

### 4.4. Recommandation finale pour ce projet

Pour un projet de cours, le bon mix est :

- Groq en production légère
- Ollama en fallback local
- FakeRequestAnalyzer en tests et mode dégradé

C'est la solution la plus équilibrée.

## 5. Ce que tu dois faire dans le code

### 5.1. Créer une interface d'analyse IA

C'est la base.

```csharp
public interface IRequestAnalyzer
{
    Task<RequestAnalysisResult> AnalyzeAsync(string requestText, CancellationToken cancellationToken = default);
}
```

Et le résultat :

```csharp
public class RequestAnalysisResult
{
    public bool Success { get; set; }
    public string? Summary { get; set; }
    public List<string> Preferences { get; set; } = new();
    public List<string> Constraints { get; set; } = new();
    public List<string> MissingInformation { get; set; } = new();
    public string? RawJson { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### 5.2. Implémentation fake pour les tests

```csharp
public class FakeRequestAnalyzer : IRequestAnalyzer
{
    public Task<RequestAnalysisResult> AnalyzeAsync(string requestText, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new RequestAnalysisResult
        {
            Success = true,
            Summary = "Demande analysée en mode simulé.",
            Preferences = new List<string> { "éviter les cours du matin" },
            Constraints = new List<string> { "même bloc", "ne pas modifier les autres affectations" },
            MissingInformation = new List<string> { "groupe cible précis" },
            RawJson = "{\"summary\":\"Demande analysée en mode simulé.\"}"
        });
    }
}
```

### 5.3. Implémentation Groq

```csharp
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class GroqRequestAnalyzer : IRequestAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GroqRequestAnalyzer(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<RequestAnalysisResult> AnalyzeAsync(string requestText, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Groq:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new RequestAnalysisResult
            {
                Success = false,
                ErrorMessage = "Groq API key not configured."
            };
        }

        var requestBody = new
        {
            model = "llama-3.1-8b-instant",
            messages = new[]
            {
                new { role = "system", content = "Tu analyses une demande de changement de groupe pour un étudiant. Retourne uniquement un JSON valide avec : summary, preferences, constraints, missingInformation." },
                new { role = "user", content = requestText }
            },
            response_format = new { type = "json_object" }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new RequestAnalysisResult
            {
                Success = false,
                ErrorMessage = content
            };
        }

        using var jsonDoc = JsonDocument.Parse(content);
        var answer = jsonDoc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(answer))
        {
            return new RequestAnalysisResult
            {
                Success = false,
                ErrorMessage = "Empty AI response."
            };
        }

        try
        {
            var result = JsonSerializer.Deserialize<RequestAnalysisResult>(answer, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result is null)
            {
                return new RequestAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "AI response could not be parsed."
                };
            }

            result.Success = true;
            result.RawJson = answer;
            return result;
        }
        catch
        {
            return new RequestAnalysisResult
            {
                Success = false,
                ErrorMessage = "AI JSON response is malformed."
            };
        }
    }
}
```

### 5.4. Implémentation Ollama

```csharp
public class OllamaRequestAnalyzer : IRequestAnalyzer
{
    private readonly HttpClient _httpClient;

    public OllamaRequestAnalyzer(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RequestAnalysisResult> AnalyzeAsync(string requestText, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = "llama3.1",
            prompt = $"Analyse cette demande de changement de groupe. Retourne un JSON valide avec summary, preferences, constraints, missingInformation. Demande:\n{requestText}",
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync("http://localhost:11434/api/generate", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new RequestAnalysisResult
            {
                Success = false,
                ErrorMessage = "Ollama unavailable"
            };
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return new RequestAnalysisResult
        {
            Success = true,
            Summary = "Analyse locale effectuée avec Ollama.",
            RawJson = content
        };
    }
}
```

## 6. Injection de dépendances dans ASP.NET Core

Dans `Program.cs` :

```csharp
builder.Services.AddHttpClient<IRequestAnalyzer, GroqRequestAnalyzer>();

// ou selon un switch configuré
builder.Services.AddSingleton<IRequestAnalyzer, FakeRequestAnalyzer>();
```

Et l'alternative plus propre avec configuration :

```csharp
var aiMode = builder.Configuration["Ai:Mode"] ?? "Fake";

if (aiMode == "Groq")
{
    builder.Services.AddHttpClient<IRequestAnalyzer, GroqRequestAnalyzer>();
}
else if (aiMode == "Ollama")
{
    builder.Services.AddHttpClient<IRequestAnalyzer, OllamaRequestAnalyzer>();
}
else
{
    builder.Services.AddSingleton<IRequestAnalyzer, FakeRequestAnalyzer>();
}
```

Dans `appsettings.json` :

```json
{
  "Ai": {
    "Mode": "Fake"
  },
  "Groq": {
    "ApiKey": ""
  }
}
```

Et en local, avec User Secrets :

```bash
dotnet user-secrets set "Groq:ApiKey" "votre_cle"
```

C'est très important : la clé ne doit jamais aller dans Git.

## 7. Sécuriser l'API et empêcher un étudiant d'utiliser l'IA

Tu dois avoir ce comportement :

- L'étudiant remplit sa demande.
- L'admin reçoit la demande.
- L'admin décide d'utiliser l'IA.
- Seul l'admin accède à l'endpoint d'analyse.

Exemple :

```csharp
[Authorize(Roles = "Admin")]
[HttpPost("change-requests/{id}/analyze-ai")]
public async Task<IActionResult> AnalyzeWithAi(int id)
{
    var request = await _changeRequestService.GetByIdAsync(id);
    if (request == null)
    {
        return NotFound();
    }

    var analysis = await _requestAnalyzer.AnalyzeAsync(request.Reason);
    if (!analysis.Success)
    {
        return BadRequest(new { error = analysis.ErrorMessage });
    }

    request.AiAnalysisJson = analysis.RawJson;
    await _changeRequestService.UpdateAsync(request);

    return Ok(analysis);
}
```

Important :

- l'étudiant ne doit pas avoir accès à cette route ;
- la route admin doit être protégée par le rôle `Admin` ;
- `SignalR` doit informer les admins de la mise à jour ;
- les étudiants ne doivent voir ni le bouton d'analyse IA ni le résultat.

## 8. Workflow complet de la demande

Voici le flux correct à implémenter :

1. L'étudiant crée une demande avec un texte libre.
2. L'admin la reçoit dans l'interface.
3. L'admin décide d'activer l'analyse IA.
4. L'backend envoie seulement le texte libre à l'IA.
5. L'IA retourne un JSON structuré.
6. Le backend valide ce JSON.
7. Le moteur C# vérifie :
   - même bloc ;
   - groupe cible valide ;
   - capacité suffisante ;
   - horaires compatibles ;
   - aucun autre impact sur les autres affectations.
8. L'admin accepte ou refuse.

## 9. Importance du scraping

Tu as dit :

- on doit faire du scraping hyperplanning ;
- on veut récupérer tous les cours d'un quadrimestre ;
- on veut un bouton “one shot” pour lancer la récupération ;
- on veut reconstruire les données dans la base de données.

C'est une bonne idée.

### 9.1. Idée technique

Créer un service :

```csharp
public interface IHyperplanningScraper
{
    Task<List<CourseDto>> ScrapeQuadrimesterAsync(string quadrimester, CancellationToken cancellationToken = default);
}
```

### 9.2. Ce que le scraper doit faire

- ouvrir la page Hyperplanning ;
- récupérer les cours du quadrimestre ;
- parser les événements ;
- convertir en objets internes ;
- enregistrer dans la base ;
- éviter les doublons ;
- pouvoir relancer sans dupliquer les données.

### 9.3. Exemple de modèle de cours

```csharp
public class Course
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string GroupName { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Room { get; set; }
    public string Teacher { get; set; }
    public string Quadrimester { get; set; }
}
```

### 9.4. Vérification des conflits

La détection des conflits doit être faite en C# et non par l'IA.

Exemple :

```csharp
public static bool HasOverlap(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
{
    return startA < endB && startB < endA;
}
```

Cela permet de dire :

- 10h-12h et 12h-14h = pas de conflit ;
- 10h-12h et 11h-13h = conflit.

## 10. SignalR et alertes admin

Tu as aussi bien compris le besoin :

- l'étudiant a une demande ;
- l'admin la voit ;
- l'admin décide ;
- le frontend met à jour sans rechargement ;
- les évènements ne doivent pas être vus par les étudiants.

### Exemple de hub

```csharp
public class ChangeRequestHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
```

Et côté backend :

```csharp
await _hubContext.Clients.Group("Admins").SendAsync("ChangeRequestUpdated", new
{
    requestId = request.Id,
    status = request.Status,
    message = "Demande mise à jour"
});
```

Donc, côté frontend Angular, on se connecte à SignalR et on écoute cet évènement.

## 11. Gestion des rôles

Le projet doit avoir seulement 2 rôles :

- `Student`
- `Admin`

### Exemple de sécurisation de route

```csharp
[Authorize(Roles = "Admin")]
[HttpGet("admin-dashboard")]
public IActionResult AdminDashboard()
{
    return Ok();
}
```

Et pour les étudiants :

```csharp
[Authorize(Roles = "Student")]
[HttpGet("student-dashboard")]
public IActionResult StudentDashboard()
{
    return Ok();
}
```

Si un étudiant tombe sur une route admin par URL directe, il doit être bloqué immédiatement par l'authentification et les rôles.

## 12. Export final CSV et PDF

Tu as bien indiqué qu’il faut exporter les groupes.

### 12.1. CSV

Le CSV doit :

- avoir un séparateur clair ;
- avoir des en-têtes explicites ;
- gérer les accents ;
- gérer les caractères spéciaux ;
- traiter les groupes vides.

### 12.2. PDF

Le PDF doit :

- présenter le bloc et le groupe ;
- afficher la date ;
- lister les étudiants ;
- garder les accents et les noms composés.

## 13. L'IA pour coder : quel modèle choisir ?

Tu demandes :

- Claude ou ChatGPT ?
- modèle avancé, pas le modèle classique ;
- le but c’est d'accélérer le développement.

### Réponse pratique

Pour le développement de ce projet, le meilleur choix est :

- Claude Sonnet 4 ou Claude 3.5/3.7 selon ton accès
- ou GPT-4.1 / GPT-4o selon ce que tu as

Pas le modèle basique ou le modèle très limité.

Pourquoi ?

- architecture .NET plus complexe ;
- plusieurs couches ;
- tests ;
- logique métier ;
- scraping ;
- intégration IA ;
- refactoring cohérent.

Un modèle avancé est plus utile pour :

- générer des classes ;
- expliquer le design ;
- corriger les erreurs ;
- revoir le code ;
- comparer plusieurs approches ;
- générer des tests ;
- produire une bonne structure de projet.

### Règle simple

- Pour le code : utiliser un modèle avancé.
- Pour le business AI du projet : Groq / Ollama / Fake.

Ce sont deux usages différents.

## 14. Organisation de travail par multi-agents

Tu veux essayer le système multi-agent.

C'est une bonne idée pour organiser le projet en 6 jours.

### Exemple de découpage

- `feature/skeleton`
- `feature/scraping`
- `feature/domain-model`
- `feature/csv-import`
- `feature/auth`
- `feature/change-requests`
- `feature/schedule-conflicts`
- `feature/chain-exchange`
- `feature/ai-analysis`
- `feature/signalr`
- `feature/export`
- `docs/report`

### Exemple de workflow d'agent

```text
Use $groupswitch-change-request.

Goal:
Approve a student's change request without modifying their assignments in other study blocks.

Workflow:
1. Ask schedule_explorer to inspect the current approval flow.
2. Define expected behavior from the specification.
3. Implement the focused correction.
4. Ask test_engineer to add and run the relevant tests.
5. Ask dotnet_reviewer to review the final changes.
6. Fix defects and rerun affected checks.

Respect AGENTS.md.
Do not commit or push.
Do not apply migrations.
Write code in English.
Explain the implementation and verification in French.
```

## 15. Exemple complet de prompt pour ton projet

Voici un prompt très bien adapté à ce projet :

```text
Use $groupswitch-ai-analysis.

Goal:
Add AI analysis for student change requests while keeping the rest of the app functional if the AI is unavailable.

Workflow:
1. Inspect the current request flow and the admin-only authorization rules.
2. Define the contract between the backend and the AI service.
3. Implement IRequestAnalyzer with Groq, Ollama, and FakeRequestAnalyzer.
4. Validate the admin-only access to the analysis endpoint.
5. Add targeted tests for valid JSON, malformed JSON, and fallback behavior.
6. Ask dotnet_reviewer to inspect the final patch.
7. Rerun the affected tests.

Constraints:
- Respect AGENTS.md.
- Students never access the AI endpoint directly.
- Preserve unrelated changes.
- Do not commit, push, or apply migrations.
- Use English identifiers and comments.
- Explain the implementation and validation in French.
```

## 16. Ce qu'il faut faire en priorité

Le bon ordre de travail pour ce projet est :

### Phase 1 — base technique

- créer le squelette solution
- mettre en place CI GitHub
- initialiser Angular
- initialiser les projets .NET
- faire les références entre projets

### Phase 2 — scraping

- faire le scraper one-shot
- récupérer les cours du quadrimestre
- stocker les données dans la base
- filtrer les cours utiles

### Phase 3 — données métier

- étudiants
- blocs
- groupes
- affectations
- catégories virtuelles

### Phase 4 — demandes et règles métier

- formulaire de demande
- validation du même bloc
- validation de l'affectation ciblée
- capacité du groupe
- conflits horaires

### Phase 5 — IA

- interface `IRequestAnalyzer`
- Groq
- Ollama
- FakeRequestAnalyzer
- route admin-only

### Phase 6 — export + SignalR + tests

- CSV
- PDF
- SignalR
- tests automatisés
- validation finale

## 17. Conseils pratiques pour le rapport / documentation

Pour la documentation finale du projet, le rapport doit rester court mais clair.

Tu peux y inclure :

- le contexte du projet ;
- l'architecture ;
- le choix des technologies ;
- le modèle de données ;
- le scraping Hyperplanning ;
- le rôle de l'IA ;
- le rôle des utilisateurs ;
- la sécurité ;
- les tests ;
- les captures d'écran.

Le rapport doit expliquer :

- comment fonctionne le scraping ;
- comment l'IA est utilisée ;
- comment le système de roles fonctionne ;
- comment l'admin valide les demandes ;
- pourquoi le backend, et non l'IA, valide les horaires.

## 18. Conclusion

Le bon point de vue pour ce projet est le suivant :

- le cœur du système est la logique métier C# ;
- l'IA n'est pas le moteur central ;
- l'IA sert à aider l'admin à comprendre les demandes ;
- le scraping donne la vraie source de vérité ;
- le système doit être sûr, limité aux rôles, transparent, testable et robuste.

L'IA doit accélérer le travail, pas remplacer la logique métier.

C'est précisément ce qu'attend ton projet et ton prof.

## 19. Exemple de synthèse à retenir

En une phrase :

"L'IA aide l'admin à analyser le besoin exprimé par l'étudiant, mais le backend C# reste le système décisionnaire pour les horaires, les capacités, les affectations et les validations métier."

C'est la clé du projet.
