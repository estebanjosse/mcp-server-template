## Why

Le template est actuellement aligné sur .NET 8, ce qui limite l’adoption des améliorations de .NET 10 (performance, tooling, sécurité et support long terme selon la stratégie produit).
La migration maintenant réduit la dette technique, maintient la compatibilité de la chaîne CI/CD et évite une divergence entre exécution locale, tests et image conteneur.

## What Changes

- Migrer les projets et paramètres de build du template de .NET 8 vers .NET 10 (TFM, SDK, versions outillage, documentation associée).
- Mettre à jour la configuration de packaging et de tests pour exécuter et valider le template sur .NET 10.
- Mettre à jour explicitement le `Dockerfile` pour utiliser les images SDK/runtime .NET 10 et conserver le même comportement d’exécution.
- **BREAKING**: le template nécessitera désormais un SDK/runtime .NET 10, ce qui retire la compatibilité d’exécution sur environnements .NET 8.

## Impact

- Code affecté: solution et projets sous `src/` et `tests/`, fichiers de build racine, scripts de validation, et documentation.
- Runtime/ops: image conteneur et pipeline de build/test devront cibler .NET 10.
- Risque principal: incompatibilités de packages ou outils dépendants de .NET 8, à identifier et corriger pendant l’implémentation.
