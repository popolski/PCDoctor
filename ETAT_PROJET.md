# ETAT_PROJET.md — PCDoctor (édition C# / WinUI 3)

> Fichier de passation pour reprendre le développement dans Claude Code.
> Dernière mise à jour : v3.0 fonctionnelle — 18 pages, exe publié OK.

---

## 1. CONTEXTE DU PROJET

**PCDoctor** est un outil personnel d'administration Windows (audit, nettoyage, durcissement, optimisation) pour le PC perso de Hugues (admin IT). Il remplace une ancienne version PowerShell/WPF monolithique de ~13 000 lignes (`F:\Backup_Profil\PCDoctor.ps1`), abandonnée car difficile à maintenir et impliquée dans un incident (voir section 7).

**Stack technique :**
- **Langage** : C# / .NET 8
- **UI** : WinUI 3 (Windows App SDK)
- **Pattern** : MVVM avec CommunityToolkit.Mvvm
- **IDE** : Visual Studio Community 2022 (l'utilisateur lance/teste ; build CLI via `dotnet build -r win-x64`)
- **Emplacement** : `C:\Users\hugue\source\repos\PCDoctor`
- **Git** : dépôt local (branche master), commit après chaque page validée

**IMPORTANT — mode non-packagé :** `<WindowsPackageType>None</WindowsPackageType>` dans le .csproj. Les apps MSIX packagées bloquent l'accès en écriture à HKLM même en admin. Un `app.manifest` déclare `requireAdministrator` (trustInfo) pour l'élévation UAC automatique.

**Publication exe :**
```
dotnet publish PCDoctor/PCDoctor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
Sortie : `bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\PCDoctor.exe` (~130 Mo).
IMPORTANT : ne jamais utiliser `System.Management` (WMI via COM) — le trimmer le retire lors du publish, causant des erreurs runtime. Utiliser PowerShell+JSON ou P/Invoke à la place (voir section 5).

---

## 2. CONVENTIONS DE TRAVAIL

- **Langue** : français, ton semi-formel.
- **JAMAIS de tirets cadratins (—)** dans le code, les commentaires ou les textes. Utiliser des tirets simples (-).
- **Attention à l'encodage** : éviter les caractères Unicode spéciaux (─, etc.) dans les fichiers .cs.
- **Pas de BoolToVisibilityConverter** : ce projet n'a aucun converter. Gérer la visibilité autrement (string vide, propriété dédiée IsXxxVisible ignorée, ou logique dans le ViewModel).
- L'utilisateur **pilote les décisions** ; l'assistant écrit le code.
- **Toujours valider chaque page** (build + test visuel) avant de passer à la suivante.
- **Commit Git** après chaque page validée.

---

## 3. ARCHITECTURE

```
PCDoctor/
├── Services/        -> logique metier pure (sans UI)
├── ViewModels/      -> MVVM, [ObservableProperty] + [RelayCommand]
├── Views/           -> pages XAML + code-behind
├── MainWindow.xaml  -> NavigationView (menu lateral) + Frame
├── App.xaml.cs      -> demarrage ; restaure le theme sauvegarde
├── app.manifest     -> requireAdministrator
└── PCDoctor.csproj  -> WindowsPackageType=None
```

**Navigation** : `NavView_SelectionChanged` lit le `Tag` de l'item et fait `ContentFrame.Navigate(typeof(Views.XxxPage))` via un switch. L'engrenage natif (`IsSettingsVisible="True"`) donne `pageTag = "AProposPage"` -> SettingsPage.

**Menu actuel (6 categories) :**
- Accueil
- Diagnostic : Audits, Etat systeme, Applications, Demarrage
- Optimisation : Nettoyage, Optimisations, Gaming, Bloatware, Services fantomes
- Securite & Vie privee : Hardening, Confidentialite, Reseau
- Maintenance : MAJ Windows, Pilotes, Planificateur, Paquets (Winget), Outils systeme
- (engrenage) Parametres + A propos

---

## 4. PATTERN MVVM ETABLI

1. **Service** (`Services/XxxService.cs`) : logique pure. Try/catch, log via `Logger`.
2. **ViewModel** (`ViewModels/XxxViewModel.cs`) : herite `ObservableObject`. `[ObservableProperty] private type xxx;` (genere `Xxx` PascalCase apres Rebuild). Actions `[RelayCommand]`.
3. **View** (`Views/XxxPage.xaml`) : `<Page.DataContext><vm:XxxViewModel/></Page.DataContext>`.
4. **Navigation** : ajouter un `case "XxxPage":` dans le switch de `MainWindow.xaml.cs`.

**Pieges connus :**
- Les proprietes generees par `[ObservableProperty]` n'apparaissent qu'apres un **Rebuild**.
- **Toggles** : pattern `_loading` flag + `Sync()` + `partial void OnXxxChanged(bool v)`. ON = fonctionnalite active (convention Windows).
- **ContentDialog en MVVM** : ViewModel expose `public event Func<string, Task<bool>>? ConfirmRequested`. La Page s'abonne dans son constructeur. Utilise dans Nettoyage, Bloatware, Applications, Planificateur, Services fantomes.
- **Boutons dans DataTemplate** : nommer la Page `x:Name="PageRoot"` et binder `Command="{Binding DataContext.XxxCommand, ElementName=PageRoot}"`.
- **DataGrid** : package `CommunityToolkit.WinUI.UI.Controls.DataGrid`. Les bindings de `Header` ne marchent pas -> en-tetes en dur.
- **WMI + Task.Run = STA/MTA** : `System.Management.ManagementObjectSearcher` utilise COM (STA), `Task.Run` utilise MTA -> exception silencieuse, liste vide. FIX : utiliser PowerShell+JSON.
- **Win32_PnPSignedDriver** : ne supporte pas SELECT avec colonnes nommees -> utiliser `SELECT *`.
- **Winget output** : le spinner d'attente et le tableau sont sur la MEME ligne `\n` separee par des `\r`. Parser : split sur `\r`, prendre le dernier segment non-vide. Split sur `\s{2+}` insuffisant (colonnes peuvent etre separees par 1 seul espace) -> utiliser les positions d'index de l'en-tete.

---

## 5. PACKAGES NUGET

- `CommunityToolkit.Mvvm` (8.x) — ObservableObject, RelayCommand
- `CommunityToolkit.WinUI.UI.Controls.DataGrid` — DataGrid (version .UI.)
- `System.Diagnostics.PerformanceCounter` — compteurs CPU temps reel

**NE PAS UTILISER** : `System.Management` — retire par le trimmer lors du PublishSingleFile.
**Alternatives :**
- RAM : `GlobalMemoryStatusEx` (P/Invoke kernel32.dll) — instantane, sans WMI
- Infos WMI : `Get-WmiObject ... | ConvertTo-Json -Compress` via PowerShell+JSON
- Defender : `Get-MpComputerStatus | ConvertTo-Json -Compress`
- Drivers/Services : `Get-CimInstance Win32_* | ConvertTo-Json -Compress`
- OS name : registre `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProductName`
- Uptime : `Environment.TickCount64`

---

## 6. PAGES FAITES (18 fonctionnelles)

| Page | Service | Contenu |
|------|---------|---------|
| **Accueil** | SystemInfoService | Dashboard : OS, RAM, Defender, Uptime, Disques |
| **Audits** | AuditService | DataGrid : Services tiers, Drivers, Defender, Infos systeme (PowerShell+JSON) |
| **Hardening** | HardeningService | 3 toggles : LLMNR, SMBv1 (DISM), PUA/SmartScreen |
| **Confidentialite** | PrivacyService | 6 toggles : Telemetrie, Cortana, Activity History, Pubs, Ad ID, Office |
| **Nettoyage** | CleanupService | Scan Temp/miniatures/corbeille -> cases -> confirmation -> clean |
| **Reseau** | NetworkService | 2 toggles : IPv6, DoH Cloudflare |
| **Optimisations** | OptimService | Hibernation toggle |
| **Demarrage** | StartupService | Entrees registre Run ; desactiver = deplacer vers Run-PCDoctorDisabled (reversible) |
| **Bloatware** | BloatwareService | Scan Appx -> cases -> desinstall + section "Nettoyage complementaire" : suppression OneDrive (kill+setup /uninstall+dossiers+CLSID) et blocage raccourcis Edge (policy EdgeUpdate) |
| **Applications** | AppsService + ResidusService | Liste programmes (registre), recherche, desinstall natif + Expander "Residus" (dossiers+registre, SafetyGuard) |
| **Etat systeme** | MonitorService | Monitoring temps reel (DispatcherTimer 2s) : CPU, RAM, top process, details hardware |
| **Gaming** | GamingService | 6 toggles : Plan HP/Ultimate, Game Mode, HAGS (avert. redemarrage), Xbox Game Bar, Win32PrioritySeparation, acceleration souris |
| **Services fantomes** | GhostServicesService | Scan services avec binaire absent (PowerShell+JSON) ; 5 niveaux SafetyGuard ; cases non cochees par defaut ; suppression via sc.exe delete |
| **MAJ Windows** | UpdatesService | Derniere MAJ, reboot pending, comptage via WUA COM, liens ms-settings |
| **Pilotes** | DriversService | Scan Win32_PnPSignedDriver (PowerShell+JSON), filtre > 3 ans, ouverture gestionnaire/WU optionnel |
| **Planificateur** | PlanificateurService | Taches non-Microsoft, activer/desactiver/supprimer, confirmation event |
| **Paquets (Winget)** | WingetService | Analyse MAJ disponibles (parser colonne-fixe + StripCarriageReturns), update selectif ou global avec stdout live (DispatcherQueue) |
| **Outils systeme** | SystemToolsService | SFC /scannow, DISM /restorehealth, point de restauration ; feedback live, boutons bloques pendant execution |
| **Parametres** | (code-behind) | Theme clair/sombre persistent (AppData\PCDoctor\theme.txt), logs, statut admin |

---

## 7. SECURITE — briques en place (TRES IMPORTANT)

**`Services/Logger.cs`** : ecrit dans `%APPDATA%\PCDoctor\PCDoctor_yyyyMMdd.log`. Methodes `Info/Warn/Error/Action`.

**`Services/SafetyGuard.cs`** :
- `IsProtectedService(name)` : liste noire anti-cheats (BEService, EasyAntiCheat, vgc, vgk, FACEIT, mhyprot, **ucldr_battlegrounds_gl/ucsvc** [Wellbia/PUBG], etc.)
- `IsSafeLocation(path)` : bloque Program Files, Steam, Epic, Riot, GOG, emplacements anti-cheat
- `IsProtectedResidu(path)` : version plus fine pour les residus (uniquement anti-cheat + System32/SysWOW64)

**L'INCIDENT A NE JAMAIS REPRODUIRE :** dans l'ancien PCDoctor PowerShell, une fonction "services fantomes" sans safeguard a supprime le service+dossier de l'anti-cheat **Wellbia/Uncheater de PUBG** (`ucldr_battlegrounds_gl`). PUBG inlancable, reinstall Windows complet. LECON : jamais de suppression de masse sur critere unique faible ; scan lecture seule -> selection individuelle -> confirmation -> log, avec SafetyGuard.

---

## 8. LACUNES PAR RAPPORT AU PS1 ORIGINAL (basses priorites)

Ces fonctions existent dans `F:\Backup_Profil\PCDoctor.ps1` mais n'ont pas ete portees (jugees mineures ou hors perimetre) :

| Fonctionnalite | Categorie | Effort |
|---|---|---|
| Flush DNS, Reset Winsock | Reseau | Faible |
| Desactivation Power Throttling | Gaming/Optim | Faible |
| Desactivation Fast Startup | Optim | Faible |
| Compression memoire toggle | Optim | Faible |
| Algorithme Nagle (TCP) | Reseau/Gaming | Faible |
| Scan Defender, MAJ signatures | Hardening | Faible |
| BitLocker check, ASR Rules, Exploit Protection | Hardening avance | Moyen |
| Suppression Bonjour/mDNS | Hardening | Tres faible |
| Copilot/AI Features | Confidentialite | Faible |
| Shell extensions, extensions navigateurs, grands fichiers | Audits | Moyen |
| Profils rapides (Quick Actions), rapport HTML, journal undo | Accueil/Avance | Eleve |

---

## 9. BUGS RESOLUS (ne pas refaire ces erreurs)

- **Acces HKLM refuse** : resolu en passant l'app en non-packagé (section 1).
- **Thème sombre menu restait blanc** : `<Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">` + `ApplyTheme` sur `this.Content as FrameworkElement`.
- **Proprietes ObservableProperty absentes** : faire un Rebuild (pas juste Build).
- **DataGrid en-tetes** : binding casse -> en-tetes en dur.
- **WMI vide (STA/MTA)** : ManagementObjectSearcher dans Task.Run = exception silencieuse. FIX : PowerShell+JSON.
- **Win32_PnPSignedDriver SELECT colonnes** : WQL refuse SELECT avec colonnes nommees -> `SELECT *`.
- **System.Management retire par le trimmer** : tout WMI/COM est supprime lors du PublishSingleFile. FIX : P/Invoke (RAM), PowerShell+JSON (tout le reste).
- **Winget parser** : le spinner d'animation et le tableau partagent la meme ligne `\n` via `\r`. `StripCarriageReturns` : split sur `\r`, dernier segment non-vide. Colonnes a 1 seul espace -> parser par index depuis l'en-tete, pas par `\s{2+}`.
- **BoolToVisibilityConverter** : pas de converter dans ce projet. Utiliser des proprietes string ou bool dediees.
- **ContentDialog CS4036** : `using System;` manquant dans le code-behind -> `IAsyncOperation.GetAwaiter()` introuvable.

---

## 10. WORKFLOW RECOMMANDE

1. Claude Code edite les fichiers directement.
2. `dotnet build PCDoctor/PCDoctor.csproj -c Debug -r win-x64` pour verifier.
3. L'utilisateur lance l'app (Visual Studio F5 ou exe publish) pour le test visuel.
4. Commit Git apres validation.
5. Publication : `dotnet publish ... -r win-x64 --self-contained true -p:PublishSingleFile=true`
