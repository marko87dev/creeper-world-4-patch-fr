# Validation de la version 1.0

Les essais ont été réalisés sur une copie vierge des fichiers d’origine. L’exécutable publié, et pas seulement son moteur isolé, a été exécuté pour les opérations d’installation et de restauration.

| Contrôle | Résultat |
|---|---|
| Restauration lorsque l’anglais est déjà présent | Aucune modification inutile |
| Fichier d’une version incompatible | Refus avant remplacement |
| Installation sur les fichiers anglais d’origine | Empreintes françaises attendues |
| Deuxième installation identique | Aucun remplacement inutile |
| Retour à l’anglais | Empreintes d’origine exactes |
| Échec simulé du remplacement du second fichier | Premier fichier rétabli à l’identique |
| Nouvelle installation après cet échec | Réussie |
| Sauvegarde anglaise modifiée | Restauration refusée sans altérer le jeu |
| Restauration finale | Empreintes d’origine exactes |
| Fichier annexe témoin | Préservé |

Les dossiers temporaires sont nettoyés après réussite ou retour automatique réussi. Si la récupération elle-même échoue, les fichiers utiles à la récupération sont conservés et leur emplacement est indiqué.

## Contrôles de contenu

- 90 018 objets du conteneur reconstruit comparés à ceux du patch relu : données identiques.
- Balises de mise en forme et variables de traduction contrôlées.
- Shaders, matériaux, textures, lumières, caméras et sons préservés.
- Identifiants techniques partagés avec le moteur conservés, notamment `Depth` et `Rematerialize`.
- Fenêtre de l’installateur inspectée visuellement.
- Archive de distribution vérifiée : uniquement l’exécutable, la notice et son empreinte SHA-256.

## Empreinte de l’exécutable publié

Fichier : `CW4_Patch_FR_1.0_2.5.1.exe`

```text
0b93bef19848ce426b7f871995eab095fc14fb706fb57e33e54d427284ab71ec
```

Une recompilation locale peut produire une empreinte différente en raison des métadonnées du compilateur. L’empreinte ci-dessus désigne l’exécutable fourni dans la release v1.0.0.

## Limite de portée

Ces contrôles ne signifient pas que chaque écran de chaque mission a été rejoué intégralement. Les éditions dont les fichiers ne correspondent pas aux entrées prises en charge sont refusées, même si leur numéro de version est 2.5.1.
