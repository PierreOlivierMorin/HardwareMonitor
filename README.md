# ⚡ Hardware Monitor — Mini GUI

Application de monitoring matériel ultra-compacte, personnalisable et fluide développée en **C# / WPF (.NET 8)** avec le moteur de télémétrie **LibreHardwareMonitor**.

---

## ✨ Fonctionnalités Clés

1. **Double Mode d'Affichage :**
   - **Mode Mini Pill (Discret) :** Barre flottante compacte (coins arrondis, fond sombre transparent) affichant les températures, fréquences (GHz) et pourcentages d'utilisation du CPU et du GPU en direct.
   - **Mode Dashboard Étendu :** Un simple clic sur le chevron `▾` déploie l'interface complète avec graphiques historiques, cartes de métriques et statistiques avancées.

2. **Télémétrie en Temps Réel :**
   - **CPU (AMD Ryzen 9 7900X3D) :** Température Tctl/Package (°C), Fréquence effective (GHz), Utilisation globale (%).
   - **GPU (NVIDIA GeForce RTX 4080 SUPER) :** Température Core (°C), Hotspot (°C), Fréquence Core (GHz), Charge globale (%).
   - Code couleur d'alerte thermique dynamique : Vert (<60°C), Jaune (60-75°C), Orange (75-85°C), Rouge (>85°C).

3. **Historique des Performances Personnalisable :**
   - Sélecteur de période temporelle : **5 min**, **10 min**, **15 min**, **20 min**, **60 min**.
   - Tampon circulaire (Ring Buffer) en mémoire vive (jusqu'à 3 600 points d'échantillonnage à 1 Hz, 0 écriture disque inutile).
   - Rendu graphique vectoriel accéléré avec zones dégradées (Cyan néon pour CPU, Vert émeraude pour GPU).
   - Curseur d'inspection interactif au survol de la souris avec bulle d'informations (valeurs exactes et horodatage).

4. **Statistiques en Direct :**
   - Calcul automatique et instantané du **Minimum**, **Maximum** et de la **Moyenne** sur la plage temporelle sélectionnée.
   - Sélecteur de métrique à visualiser sur la courbe : **Température (°C)**, **Fréquence (GHz)** ou **Charge (%)**.

5. **Personnalisation & Ergonomie :**
   - **Always-on-Top (📌) :** Gardez le widget visible par-dessus vos jeux ou applications.
   - **Curseur d'Opacité :** Réglage fluide de 40% à 100% pour une transparence discrète.
   - **Déplaçable partout :** Cliquez et glissez le widget n'importe où sur l'écran.

---

## 🚀 Comment Lancer l'Application

Vous avez deux lanceurs rapides à la racine du projet :

* **[Launch.bat](file:///c:/projects/test%20project/Launch.bat)** : Lancement standard (accès immédiat à la télémétrie complète NVIDIA RTX 4080 SUPER et aux fréquences CPU en GHz).
* **[Launch-Admin.bat](file:///c:/projects/test%20project/Launch-Admin.bat)** : Lancement en mode Administrateur (permet à LibreHardwareMonitor de charger le pilote noyau Ring-0 pour lire également la température interne Tctl/Tdie de l'AMD Ryzen 9).

L'exécutable autonome se trouve également dans :
`c:\projects\test project\publish\HardwareMonitor.exe`

---

## 🛠️ Structure du Projet

* `Controls/HardwareChart.cs` : Contrôle graphique vectoriel haute performance WPF (`DrawingContext`).
* `Services/HardwareService.cs` : Collecteur de sondes matérielles (LibreHardwareMonitor + Performance Counters fallback).
* `Services/TelemetryStore.cs` : Tampon circulaire 3 600 points et moteur de calcul statistique.
* `ViewModels/MainViewModel.cs` : Logique de présentation, commandes et boucle d'échantillonnage 1s.
* `MainWindow.xaml` : Interface utilisateur moderne au look dark/glass avec transitions.
