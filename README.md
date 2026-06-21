# PCDoctor

Outil d'administration et d'optimisation Windows, conçu pour les utilisateurs avancés qui veulent garder le contrôle de leur système.

![Windows 11](https://img.shields.io/badge/Windows%2011-0078D4?style=flat&logo=windows11&logoColor=white)
![.NET 8](https://img.shields.io/badge/.NET%208-512BD4?style=flat&logo=dotnet&logoColor=white)
![WinUI 3](https://img.shields.io/badge/WinUI%203-0078D4?style=flat&logo=microsoft&logoColor=white)
![Version](https://img.shields.io/badge/version-1.2-blue?style=flat)

## Fonctionnalités

| Catégorie | Ce que ça fait |
|---|---|
| **Sécurité** | Hardening système, confidentialité, configuration réseau |
| **Performances** | Optimisations OS, mode Gaming, profils d'alimentation, démarrage |
| **Nettoyage** | Fichiers temporaires, bloatware, applications, services fantômes |
| **Diagnostic** | Audits système, état matériel (CPU, RAM, GPU, disques) |
| **Maintenance** | Mises à jour Windows, pilotes, planificateur, Winget, outils système |

- Score de santé système en temps réel
- Export HTML/CSV des rapports d'audit
- Backup/restore JSON de tous les réglages
- Historique des actions
- Notifications Windows à la fin des audits longs
- Profils de configuration (Gaming, Travail, Économie d'énergie...)
- Vérification et installation des mises à jour directement depuis l'application

## Prérequis

- Windows 10 (1809+) ou Windows 11
- [.NET 8 Desktop Runtime](https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe) — installé automatiquement par le setup si absent
- Architecture x64

## Installation

Télécharge le setup depuis la page [Releases](https://github.com/popolski/PCDoctor/releases) et lance `PCDoctor_Setup_v1.2.exe`. L'installeur gère les prérequis automatiquement.

Les mises à jour suivantes peuvent être installées directement depuis l'application (page Accueil).

> PCDoctor nécessite les droits administrateur pour fonctionner.

## Stack technique

- **UI** : WinUI 3 + Windows App SDK 2.2, Mica backdrop, barre de titre custom
- **Architecture** : MVVM via CommunityToolkit.Mvvm 8.4.2
- **Runtime** : .NET 8, non-packaged (`WindowsPackageType=None`)
- **Installeur** : Inno Setup 7

## Build

```powershell
# Publier
dotnet publish PCDoctor\PCDoctor.csproj -c Release -p:Platform=x64 /p:PublishProfile=win-x64 /p:PublishSingleFile=false /p:PublishTrimmed=false

# Générer l'installeur (nécessite Inno Setup 7)
& "C:\Program Files\Inno Setup 7\iscc.exe" installer.iss
```

## Licence

MIT — voir [LICENSE](LICENSE)
