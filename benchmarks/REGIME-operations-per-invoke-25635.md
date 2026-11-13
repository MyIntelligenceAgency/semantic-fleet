# Régime à N croissant — balayage explicite `OperationsPerInvoke` (CoursIA #25635)

> **Mesuré le 2026-11-13 sur un second siège** (i7-11800H, poste de travail, .NET 9/10/11).
> Lane `myia-po-2024:CoursIA-2`. Ce document est le **complément** de la variante « per-op
> instrument » déjà présente dans `PromptSettingsBenchmarks` : celle-ci mesure le coût/op à
> **un seul N** (N = 10 000 pour `Matches` et `InterpolateKeys`, N = 1 000 pour les deux bancs
> JSON). La classe `OperationsPerInvokeRegimeBenchmarks` ajoute le **balayage explicite**
> N = 1 / 100 / 10 000 sur les deux cellules sous 100 µs, pour trancher — par la mesure, pas
> par le raisonnement — si le coût/op est porté par un **coût fixe par itération** (amorti
> quand N croît) ou par le **coût réel** de l'opération (stable quand N croît).

## Pourquoi un balayage, et pas seulement un point

La baseline publiée (`BASELINE-net9.0.md`) et le commentaire du banc portent déjà la
conclusion « coût/op **stable de N=1 à N=10 000** (ratios 0,92-0,99) ». Mais ce résultat
s'appuyait sur **deux points** (le banc « par appel » ≈ N=1, et la variante per-op ≈
N=10 000) — le point intermédiaire N=100 n'était pas mesuré. Le balayage 3 points ferme
l'entre-deux : si une pente cachée (un coût fixe partiellement amorti) existait entre les
deux extrémités, elle apparaîtrait à N=100. Elle n'apparaît pas.

## Protocole de mesure

| Élément | Valeur |
| --- | --- |
| Commit mesuré | `e675df9` (pointe de `stable-from-v0343` de ce clone) |
| Runtime | .NET 9 / 10 / 11 (un TFM par run, DLL construite une fois, exécutée par le `dotnet` propriétaire du runtime) |
| BenchmarkDotNet | 0.15.8, `InProcessEmitToolchain`, `Job.ShortRun` 5 warmup / 15 itérations, `IterationTime(100 ms)`, `UnrollFactor(1)`, invocation count auto |
| Cellules | `PromptSignature.Matches` (chemin StartsWith) et `PromptTransform.InterpolateKeys` (regex `{key}`) |
| N | 1 / 100 / 10 000 (`OperationsPerInvoke` fixe, la méthode tourne N opérations en interne) |
| Répétition | 5 passes entrelacées net9 → net10 → net11 par pass (15 exécutions, charge hôte CPU + nb processus échantillonnée à chaque appel) |

Le design anti-cache (rotation sur 8 payloads distincts + consommation dans un sink statique)
est identique à celui du per-op instrument de `PromptSettingsBenchmarks` : chaque itération
consomme une instance de chaîne distincte, le compilateur ne peut pas replier la boucle, et
le marqueur de variante est posé **au-delà** des 100 caractères de `PromptStart` pour que
l'appariement de signature conserve le même chemin StartsWith.

## Résultats (coût/op en ns, médiane des moyennes des 5 passes)

**`PromptSignature.Matches`** (chemin StartsWith, 80 B alloués)

| N | net9 | net10 | net11 |
| ---: | ---: | ---: | ---: |
| 1 | 25,89 | 19,46 | 18,92 |
| 100 | 26,53 | 19,65 | 18,70 |
| 10 000 | 25,33 | 17,00 | 18,47 |

**`PromptTransform.InterpolateKeys`** (regex compilée + `string.Format`, ~9,5-9,7 KB alloués)

| N | net9 | net10 | net11 |
| ---: | ---: | ---: | ---: |
| 1 | 1 913 | 1 936 | 1 932 |
| 100 | 1 797 | 1 978 | 1 994 |
| 10 000 | 1 697 | 2 068 | 1 841 |

Ratios N=10 000 / N=1 : `Matches` 0,92-0,98, `InterpolateKeys` 0,89-0,97 selon le runtime —
dans la bande 0,92-0,99 que la baseline retient comme signature du **coût réel**.

## Cohérence avec la baseline publiée

La baseline (`d53f533`, i7-12700H, net9, BDN 0.15.8) publie deux points par cellule. Les
extrémités de ce balayage y recollent, à un siège et un commit d'écart :

| Cellule | Baseline (N=1 / N=10 000) | Ce balayage net9 (N=1 / N=100 / N=10 000) |
| --- | --- | --- |
| `Matches` | 24,27 ns / 22,97 ns | 25,89 / 26,53 / 25,33 ns |
| `InterpolateKeys` | 1,62 µs / 1,93 µs | 1,913 / 1 797 / 1 697 ns |

Le point intermédiaire **N=100 reste sur la même ligne horizontale** entre les deux
extrémités déjà publiées : ni saut, ni creux. C'est la contribution propre de ce balayage.

## Verdict de séparabilité

Critère : pour deux N disjoints, on compare les intervalles `[min, max]` des moyennes des
5 passes. Si les intervalles **ne se chevauchent pas**, le régime est **séparé** (une pente
réelle) ; s'ils **se chevauchent**, le régime est **non établi** — ce qui est en soi un
résultat publiable.

**Toutes les paires de N disjoints se chevauchent, sur les deux cellules et les trois
runtimes.** Le coût/op n'est donc **pas séparable** en un terme amorti et un terme réel
par ce critère : il est **plat** dans la bande de bruit. Concrètement, sur ces deux
cellules, **aucun coût fixe par itération n'est résolu au-dessus de la bande de bruit**
sous l'instrument `OperationsPerInvoke` — le coût/op mesuré **est** le coût réel. Les
drapeaux isolés « FIXED? » rendus par l'analyseur sont des oscillations de la médiane à
l'intérieur d'intervalles qui se chevauchent, pas une pente.

## Contraste de mécanisme avec l'instrument `InvocationCount`

La section « régime à N croissant » de `docs/dotnet/net11-runtime-deltas.md` (mesurée le
2026-11-10 sur un i9-14900KF, BDN 0.14.0) a utilisé `InvocationCount = 1 / 100 / 10 000` —
BDN **appelle la méthode N fois**. Ce couplage lie N à la **promotion de tiering du JIT**
(méthode appelée davantage → montée de tier0/tier1 vers tier2) : le coût par appel
**diminuait** avec N (ratios 0,017-0,133).

Cette classe fixe `OperationsPerInvoke = N` : la méthode tourne N opérations **en interne**,
BDN divise par N, et **le nombre d'appels est identique pour tous les N** → le tiering est
**constant**, seul le terme de harnais `c/N` est isolé. D'où le contraste net :

| Instrument | Ce que N pilote | Observation |
| --- | --- | --- |
| `InvocationCount = N` | le nombre d'appels → **le tiering** | coût/appel **décroît** (c/N + tiering) |
| `OperationsPerInvoke = N` | le travail interne, appels constants | coût/op **plat** (c/N isolé, tiering constant) |

Les deux instruments mesurent **deux décompositions différentes** du même coût sous-jacent
`w + c/N` (`w` = travail réel, `c` = surcoût de harnais par appel) : l'un couple N au
tiering, l'autre l'isole. **Les deux mécanismes sont réels et additifs** — ce contraste
est le point de ce banc, au même titre que la stabilité elle-même.

## Provenance et reproduction

- Logs bruts (15 fichiers `net{9,10,11}_pass{1..5}.log` + `sweep.log`) : **hors dépôt**,
  conservés dans le scratch de la lane (`D:/CoursIA-2/.tmp/sf-25635/results/`, éphémère).
  Analyse : médiane des 5 moyennes par (cellule, N, runtime), intervalles `[min, max]`.
- Le balayage multi-runtime se reproduit en construisant les 3 TFM une fois, puis en
  exécutant chaque DLL par le `dotnet` propriétaire de son runtime (évite le
  `NETSDK1045` du restore) :
  ```bash
  dotnet build -c Release benchmarks/SemanticFleet.Benchmarks
  dotnet benchmarks/SemanticFleet.Benchmarks/bin/Release/net9.0/SemanticFleet.Benchmarks.dll --filter '*OperationsPerInvokeRegime*'
  # idem net10 (dotnet système) et net11 (SDK .NET 11 local)
  ```

Le balayage multi-TFM n'est **pas** imposé au projet : la classe compile et tourne sur le
TFM unique `net9.0` du projet, comme le reste de la suite ; le multi-TFM n'a servi qu'à la
mesure.
