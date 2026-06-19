# ETAT_PROJET.md — PCDoctor (édition C# / WinUI 3)

> Fichier de passation pour reprendre le développement dans Claude Code.
> Dernière mise à jour : portage WinUI en cours, 12 pages fonctionnelles.

---

## 1. CONTEXTE DU PROJET

**PCDoctor** est un outil personnel d'administration Windows (audit, nettoyage, durcissement, optimisation) pour le PC perso de Hugues (admin IT). Il remplace une ancienne version PowerShell/WPF monolithique de ~13 000 lignes, abandonnée car difficile à maintenir et impliquée dans un incident (voir section 7).

**Stack technique :**
- **Langage** : C# / .NET 8
- **UI** : WinUI 3 (Windows App SDK)
- **Pattern** : MVVM avec CommunityToolkit.Mvvm
- **IDE** : Visual Studio Community 2022 (l'utilisateur lance/teste ; build CLI possible via `dotnet build`)
- **Emplacement** : `C:\Users\hugue\source\repos\PCDoctor`
- **Git** : dépôt local (branche master/main), commit après chaque page validée

**IMPORTANT — mode non-packagé :** le projet a été passé en non-packagé (`<WindowsPackageType>None</WindowsPackageType>` dans le .csproj) car les apps WinUI packagées (MSIX) tournent dans un conteneur sandboxé qui bloque l'accès en écriture à HKLM, même en admin. Un `app.manifest` déclare `requireAdministrator` (bloc trustInfo) pour l'élévation UAC. Quand lancé depuis Visual Studio (qui tourne en admin), l'app hérite des droits admin sans prompt UAC.

---

## 2. CONVENTIONS DE TRAVAIL

- **Langue** : français, ton semi-formel.
- **JAMAIS de tirets cadratins (—)** dans le code, les commentaires ou les textes. Utiliser des tirets simples (-).
- **Attention à l'encodage** : éviter les caractères Unicode spéciaux (─, etc.) dans les fichiers .cs ; ils ont déjà cassé un copier-coller (chaîne inachevée). Préférer ASCII dans les commentaires.
- L'utilisateur **pilote les décisions** ; l'assistant écrit le code.
- **Toujours valider chaque page** (build + test visuel par l'utilisateur) avant de passer à la suivante.
- **Commit Git** après chaque page validée.
- Surveiller les quotas : ne pas démarrer une grosse fonction sensible si le budget est court.

---

## 3. ARCHITECTURE

```
PCDoctor/
├── Services/        → logique métier pure (réutilisable, sans UI)
├── ViewModels/      → MVVM, [ObservableProperty] + [RelayCommand]
├── Views/           → pages XAML + code-behind
├── MainWindow.xaml  → NavigationView (menu latéral) + Frame de navigation
├── App.xaml.cs      → démarrage ; expose `public static Window? MainWindowRef`
├── app.manifest     → requireAdministrator (trustInfo)
└── PCDoctor.csproj  → WindowsPackageType=None
```

**Navigation** (`MainWindow.xaml.cs`) : `NavView_SelectionChanged` lit le `Tag` de l'item sélectionné et fait `ContentFrame.Navigate(typeof(Views.XxxPage))` via un `switch(pageTag)`. L'engrenage Paramètres natif (`IsSettingsVisible="True"`) donne `pageTag = "AProposPage"` → navigue vers SettingsPage.

**Menu (5 catégories) :**
- Accueil
- Diagnostic : Audits, État système, Applications, Démarrage
- Optimisation : Nettoyage, Optimisations, Gaming, Bloatware
- Sécurité & Vie privée : Hardening, Confidentialité, Réseau
- Maintenance : MAJ Windows, Pilotes, Planificateur
- (engrenage) Paramètres + À propos

---

## 4. PATTERN MVVM ÉTABLI (à suivre pour toute nouvelle page)

1. **Service** (`Services/XxxService.cs`) : logique pure. Méthodes qui lisent/modifient le système. Try/catch systématique, log via `Logger`.
2. **ViewModel** (`ViewModels/XxxViewModel.cs`) : hérite `ObservableObject`. Propriétés `[ObservableProperty] private type xxx;` (génère `Xxx` en PascalCase APRÈS un Rebuild). Actions `[RelayCommand]`.
3. **View** (`Views/XxxPage.xaml`) : `<Page.DataContext><vm:XxxViewModel/></Page.DataContext>`, bindings `{Binding Xxx}`.
4. **Navigation** : ajouter un `case "XxxPage":` dans le switch de `MainWindow.xaml.cs`.

**Pièges connus :**
- Les propriétés générées par `[ObservableProperty]` n'apparaissent qu'après un **Rebuild** (sinon erreurs "le nom Xxx n'existe pas" — faire Générer → Régénérer).
- **Toggles** : pattern avec flag `_loading` (mis à true pendant la synchro initiale pour ne pas déclencher les actions), méthode `Sync()` qui lit l'état réel, et `partial void OnXxxChanged(bool v)` qui applique l'action si `!_loading`. Convention : **ON = fonctionnalité Windows active** (donc OFF = sécurisé/désactivé), comme dans les Paramètres Windows.
- **ContentDialog en MVVM** : le ViewModel ne peut pas afficher de dialog (il ne connaît pas l'UI). Il expose `public event Func<string, Task<bool>> ConfirmRequested;`. La Page s'abonne dans son constructeur et affiche un `ContentDialog` (avec `XamlRoot = this.XamlRoot`). Utilisé pour Nettoyage, Bloatware, Applications.
- **Boutons dans un ItemsControl/DataTemplate** : pour qu'un bouton dans un template appelle une commande du ViewModel (pas de l'item), nommer la Page `x:Name="PageRoot"` et binder `Command="{Binding DataContext.XxxCommand, ElementName=PageRoot}"` + `CommandParameter="{Binding}"`.
- **DataGrid** : package `CommunityToolkit.WinUI.UI.Controls.DataGrid` (ancienne version avec `.UI.`). Namespace XAML : `xmlns:controls="using:CommunityToolkit.WinUI.UI.Controls"`. ATTENTION : le binding des `Header` de colonnes ne marche pas (affiche "Microsoft.UI.Xaml.Data.Binding") → utiliser des **en-têtes fixes en dur** (`Header="Élément"`, etc.).

---

## 5. PACKAGES NUGET INSTALLÉS

- `CommunityToolkit.Mvvm` (8.x) — ObservableObject, ObservableProperty, RelayCommand
- `System.Management` — WMI/CIM (Win32_*, MSFT_MpComputerStatus...)
- `CommunityToolkit.WinUI.UI.Controls.DataGrid` — la DataGrid (version .UI.)
- `System.Diagnostics.PerformanceCounter` — compteurs CPU temps réel

NOTE : la source NuGet a dû être configurée sur nuget.org (`https://api.nuget.org/v3/index.json`) car elle était sur "offline packages" par défaut.

---

## 6. PAGES FAITES (12 fonctionnelles, toutes testées OK)

| Page | Service | Contenu |
|------|---------|---------|
| **Accueil** | SystemInfoService | Dashboard : 6 cartes (OS, RAM, Sécurité Defender, Uptime, Disques avec barres) |
| **Audits** | AuditService | DataGrid : Services tiers, Drivers, Defender, Infos système. En-têtes fixes. |
| **Hardening** | HardeningService + RegistryHelper | 3 toggles : LLMNR, SMBv1 (DISM), PUA/SmartScreen (Defender) |
| **Privacy** | PrivacyService | 6 toggles : Télémétrie, Cortana, Activity History, Pubs, Advertising ID, Office |
| **Nettoyage** | CleanupService | Scan (Temp user/Windows, cache miniatures, corbeille via shell32) → cases → confirmation → clean + log |
| **Network** | NetworkService | 2 toggles : IPv6, DoH Cloudflare (commandes PowerShell) |
| **Optimisations** | OptimService | 1 toggle : Hibernation (powercfg /h) |
| **Démarrage** | StartupService | Entrées registre Run (HKCU+HKLM). Désactiver = déplacer vers clé Run-PCDoctorDisabled (réversible) |
| **Bloatware** | BloatwareService | Scan Appx contre liste blanche curée → cases → confirmation → Remove-AppxPackage + log |
| **Applications** | AppsService | Liste programmes (registre Uninstall HKLM+WOW64+HKCU), recherche live, désinstall via UninstallString natif |
| **État système** | MonitorService | Monitoring temps réel (DispatcherTimer 2s) : CPU% (PerformanceCounter), RAM, infos détaillées, top 10 process. NB : Uptime FAUX (Fast Startup casse TickCount64 et LastBootUpTime) — laissé tel quel, accessoire. |
| **Paramètres + À propos** | (code-behind) | Engrenage natif. Thème clair/sombre, ouvrir dossier logs, statut admin, à propos v3.0 |

---

## 7. SÉCURITÉ — briques en place (TRÈS IMPORTANT)

Deux classes utilitaires prêtes, **à utiliser pour toute opération destructrice** :

**`Services/Logger.cs`** (statique) : écrit dans `%APPDATA%\PCDoctor\PCDoctor_yyyyMMdd.log`. Méthodes `Info/Warn/Error/Action`. `Action` = pour les modifications système sensibles. Ne plante jamais l'app (try/catch interne). Méthode `GetLogPath()`.

**`Services/SafetyGuard.cs`** (statique) : porte la leçon de l'incident (voir ci-dessous).
- `IsProtectedService(name)` : liste blanche anti-cheats JAMAIS supprimables (BEService, BEDaisy, EasyAntiCheat(_EOS), vgc, vgk, FACEIT, FaceitService, ESEADriver2, xigncode, xhunter1, GgcService, GameGuard, npggsvc, PnkBstrA/B, mhyprot/2/3, **ucldr_battlegrounds_gl, ucsvc** [Wellbia/PUBG — l'incident], "Zakynthos Service", zksvc).
- `IsSafeLocation(path)` : détecte les emplacements éditeurs à protéger (Common Files, Program Files(x86), Wellbia, BattlEye, EasyAntiCheat, AntiCheat, Uncheater, Steam, Epic Games, Riot, GOG).

**⚠️ L'INCIDENT À NE JAMAIS REPRODUIRE :** dans l'ancien PCDoctor PowerShell, une fonction "services fantômes" supprimait en masse des services jugés orphelins sur un critère trop faible (binaire absent). Elle a supprimé le service+dossier de l'anti-cheat **Wellbia/Uncheater de PUBG** (`ucldr_battlegrounds_gl`, `C:\Program Files\Common Files\Wellbia.com\`), rendant PUBG incrashable au lancement. Diagnostic long, a fini par un réinstall Windows complet. LEÇON : jamais de suppression de masse sur critère unique faible ; les anti-cheats installent/retirent leurs binaires dynamiquement. Toute suppression = scan lecture seule → sélection individuelle explicite → confirmation → log, avec SafetyGuard en garde-fou.

---

## 8. RESTE À FAIRE

**Pages d'action restantes :**
- **Gaming** : périmètre à définir avec l'utilisateur (tweaks/profils — ex. plan d'alimentation hautes perfs, Game Mode, GPU scheduling, désactivation Xbox Game Bar...).
- **Maintenance** : MAJ Windows (état/recherche), Pilotes (lister/vérifier), Planificateur (tâches planifiées tierces).
- **🛡️ Services fantômes** — LE BOUQUET FINAL, gardé pour la fin. La "revanche" de l'incident : refaire proprement avec SafetyGuard (scan lecture seule multi-critères → sélection individuelle → confirmation → log). Pattern comme Nettoyage. Tout est prêt côté SafetyGuard.

**Améliorations :**
- **Nettoyage de résidus** pour la page Applications (façon Revo Uninstaller) : après désinstallation native, scanner dossiers + clés registre résiduels, présenter une liste à cocher (jamais auto), protéger emplacements partagés via SafetyGuard, confirmer + logger. FONCTION SENSIBLE — à faire posément.
- **Thème noir pur** (optionnel) : l'utilisateur préférerait un noir véritable + texte blanc au lieu du gris WinUI standard. Demande de surcharger les ThemeResources (ResourceDictionary.ThemeDictionaries). Cosmétique, à part.

**Finition / distribution :**
- Vérifier que l'UAC se déclenche bien quand on lance le .exe SEUL (hors Visual Studio) — pas encore testé.
- Publier un .exe distribuable à terme.

---

## 9. BUGS RÉSOLUS (pour mémoire, ne pas refaire les erreurs)

- **Accès HKLM refusé** : résolu en passant l'app en non-packagé (voir section 1).
- **"The project needs to be deployed"** au passage non-packagé : décocher Deploy dans le Gestionnaire de configurations, ou choisir le profil de lancement non-packagé (pas "PCDoctor (Package)").
- **Thème sombre : menu latéral restait blanc** : bug WinUI connu (issue #8249, changement de thème runtime qui diffère du thème OS). FIX appliqué : donner un fond explicite au Grid racine → `<Grid x:Name="RootGrid" Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">`, et `ApplyTheme` applique `RequestedTheme` sur `this.Content as FrameworkElement`. MainWindow expose `public void ApplyTheme(ElementTheme theme)`.
- **Erreurs "le nom Xxx n'existe pas"** sur propriétés ObservableProperty : faire un Rebuild (le générateur de code du toolkit doit tourner).
- **DataGrid en-têtes** : binding cassé → en-têtes en dur.
- **Copier-coller partiel** a déjà cassé des fichiers (accolade manquante, chaîne inachevée) : en Claude Code, éditer directement les fichiers évite ce problème.

---

## 10. WORKFLOW RECOMMANDÉ EN CLAUDE CODE

1. Claude Code édite les fichiers directement (fini le copier-coller manuel).
2. Claude lance `dotnet build` pour vérifier la compilation et lire les erreurs.
3. **L'utilisateur lance l'app dans Visual Studio (F5)** pour le test visuel (l'UI WinUI s'affiche mieux via VS que via CLI).
4. Commit Git après chaque page validée par l'utilisateur.
5. Garder ce fichier ETAT_PROJET.md à jour au fil de l'avancement.
