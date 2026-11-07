# Creeper World 4 — Patch français

Patch français communautaire pour **Creeper World 4, version 2.5.1**, avec installateur Windows et restauration de l’anglais.

## Télécharger

➡️ **[Télécharger le patch avec sa notice](https://github.com/marko87dev/creeper-world-4-patch-fr/releases/latest)**

L’installateur fait environ **5,2 Mo**. Il fonctionne hors ligne et ne nécessite pas Python.

## Installer

1. Fermez Creeper World 4.
2. Lancez `CW4_Patch_FR_1.0_2.5.1.exe`.
3. Sélectionnez le dossier contenant `CW4.exe`.
4. Cliquez sur **Installer le français**, attendez la confirmation, puis relancez le jeu.

Prévoyez **3 Go libres** pendant l’opération. Si le dossier du jeu nécessite des droits supplémentaires, relancez l’installateur avec **Exécuter en tant qu’administrateur**.

## Compatibilité

- Version ciblée : **2.5.1**, Windows 64 bits.
- **Aucune boutique n’est imposée.** Les fichiers sont vérifiés par leur empreinte avant application.
- Une installation modifiée ou une édition 2.5.1 dont les fichiers diffèrent de ceux pris en charge sera refusée sans remplacer les fichiers du jeu. Le numéro de version seul ne garantit pas des fichiers identiques.

La traduction couvre les menus, les aides des unités, la campagne, les missions bonus et les répliques scriptées. La relecture a apporté **1 116 améliorations** à la première version. Les cartes communautaires téléchargées ne sont pas traduites automatiquement ; certains textes déjà présents dans une ancienne sauvegarde peuvent rester en anglais.

## Restaurer l’anglais

Relancez le même installateur et choisissez **Restaurer l’anglais**. Les originaux sont conservés dans `CW4_Patch_FR_Backup`, dans le dossier du jeu. Conservez cette sauvegarde. L’installateur ne modifie pas vos sauvegardes de parties.

Le gros fichier du jeu est stocké sans compression après application. Avec la sauvegarde des originaux, l’installation occupe environ **900 Mo supplémentaires**. Les données graphiques et sonores restent identiques ; seul leur conteneur change de compression.

## Ce que contient le dépôt

- `src/` : sources C# de l’installateur et du moteur de patch.
- `payload/` : correctifs binaires différentiels, sans fichiers complets du jeu.
- `build.ps1` : compilation locale de l’exécutable.
- `docs/VALIDATION.md` : contrôles réalisés et empreintes de la version publiée.

Le jeu complet, les sauvegardes personnelles et les fichiers de travail extraits ne sont pas inclus.

## Compiler

Sur Windows 64 bits avec le compilateur C# du .NET Framework 4 installé :

```powershell
.\build.ps1
```

Le résultat est placé dans `build/CW4_Patch_FR_1.0_2.5.1.exe`. La notice accompagne les fichiers de la section **Releases**.

## Signaler un problème

Ouvrez une [issue](https://github.com/marko87dev/creeper-world-4-patch-fr/issues) en indiquant la version du jeu, la boutique et le message d’erreur. Pour une traduction à corriger, joignez le texte ou une capture de l’écran concerné. Ne publiez pas de clé d’achat ni de fichiers complets du jeu.

Projet communautaire non officiel, non affilié à Knuckle Cracker. Creeper World 4 et ses éléments appartiennent à leurs ayants droit respectifs.
