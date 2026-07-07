using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PackageDecisionFlowTests
{
    private const string CurrentNightKey = "PackageInspection_CurrentNight";
    private const string UnlockedNightKey = "PackageInspection_UnlockedNight";

    private readonly List<GameObject> createdObjects = new List<GameObject>();
    private bool hadCurrentNight;
    private bool hadUnlockedNight;
    private int savedCurrentNight;
    private int savedUnlockedNight;

    [SetUp]
    public void SetUp()
    {
        hadCurrentNight = PlayerPrefs.HasKey(CurrentNightKey);
        hadUnlockedNight = PlayerPrefs.HasKey(UnlockedNightKey);
        savedCurrentNight = PlayerPrefs.GetInt(CurrentNightKey, 1);
        savedUnlockedNight = PlayerPrefs.GetInt(UnlockedNightKey, 1);
        PlayerPrefs.DeleteKey(CurrentNightKey);
        PlayerPrefs.DeleteKey(UnlockedNightKey);

        NightManager.ResetInstanceForTests();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        NightManager.ResetInstanceForTests();
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        RestorePlayerPrefs();

        foreach (GameObject createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                Object.DestroyImmediate(createdObject);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void PackageWithoutValidationErrorsCanBeAccepted()
    {
        PackageData package = CreateLegacyPackage(new List<string>());

        Assert.That(package.ShouldReject, Is.False);
        AssertDecision(package, accepted: true, expectedCorrect: true);
    }

    [Test]
    public void PackageWithValidationErrorsMustBeRejected()
    {
        PackageData package = CreateLegacyPackage(new List<string>
        {
            "Destino inexistente"
        });

        Assert.That(package.ShouldReject, Is.True);
        AssertDecision(package, accepted: false, expectedCorrect: true);
    }

    [Test]
    public void VisualMismatchRefreshesRejectionReasons()
    {
        var package = new PackageData
        {
            reportShape = "Small Box",
            boxShape = "small box ",
            reportBarcode = "PKG-123-F",
            boxBarcode = "PKG-123-F",
            reportLogo = "North Annex",
            boxLogo = "Night Archive",
            reportTapeColor = "Red",
            boxTapeColor = "red",
            reportDestination = "Cold Storage",
            boxDestination = "Cold Storage",
            reportWeight = "4.0kg",
            boxWeight = "4.0KG"
        };

        package.RefreshValidationReasons();

        Assert.That(package.ShouldReject, Is.True);
        Assert.That(package.rejectionReasons, Is.EquivalentTo(new[] { "Logo mismatch" }));
    }

    [Test]
    public void CorrectDecisionsIncreaseQuotaAndCanTriggerVictory()
    {
        GameManager gameManager = CreateGameManager();
        gameManager.SetQuotaRequired(2);

        bool firstDecision = gameManager.RegisterPackageDecision(CreateLegacyPackage(new List<string>()), accepted: true);
        bool secondDecision = gameManager.RegisterPackageDecision(CreateLegacyPackage(new List<string>()), accepted: true);

        Assert.That(firstDecision, Is.True);
        Assert.That(secondDecision, Is.True);
        Assert.That(gameManager.quotaAtual, Is.EqualTo(2));
        Assert.That(gameManager.CurrentState, Is.EqualTo(GameState.Victory));
    }

    [Test]
    public void WrongDecisionDoesNotIncreaseQuota()
    {
        GameManager gameManager = CreateGameManager();

        bool correctDecision = gameManager.RegisterPackageDecision(CreateLegacyPackage(new List<string>
        {
            "Peso acima do permitido"
        }), accepted: true);

        Assert.That(correctDecision, Is.False);
        Assert.That(gameManager.quotaAtual, Is.EqualTo(0));
        Assert.That(gameManager.dangerLevel, Is.EqualTo(0));
        Assert.That(gameManager.CurrentState, Is.EqualTo(GameState.Playing));
    }

    [Test]
    public void TimeoutCountsAsWrongDecision()
    {
        GameManager gameManager = CreateGameManager();
        gameManager.SetQuotaRequired(10);

        var decisionObject = new GameObject("DecisionManager Test Host");
        createdObjects.Add(decisionObject);
        DecisionManager decisionManager = decisionObject.AddComponent<DecisionManager>();

        bool correct = decisionManager.SubmitTimeout(CreateLegacyPackage(new List<string>()));

        Assert.That(correct, Is.False);
        Assert.That(gameManager.WrongDecisionCount, Is.EqualTo(1));
        Assert.That(gameManager.CurrentState, Is.EqualTo(GameState.Playing));
    }

    [Test]
    public void ThreeWrongDecisionsTriggerGameOver()
    {
        GameManager gameManager = CreateGameManager();
        gameManager.SetQuotaRequired(10);
        PackageData cleanPackage = CreateLegacyPackage(new List<string>());

        gameManager.RegisterPackageDecision(cleanPackage, accepted: false);
        gameManager.RegisterPackageDecision(cleanPackage, accepted: false);
        gameManager.RegisterPackageDecision(cleanPackage, accepted: false);

        Assert.That(gameManager.WrongDecisionCount, Is.EqualTo(3));
        Assert.That(gameManager.CurrentState, Is.EqualTo(GameState.GameOver));
    }

    [Test]
    public void ShiftProgressAlwaysStaysOnSingleNight()
    {
        NightManager nightManager = CreateNightManager();

        nightManager.ResetProgressToFirstNight();
        Assert.That(nightManager.CurrentNight, Is.EqualTo(1));
        Assert.That(nightManager.UnlockedNight, Is.EqualTo(1));

        nightManager.UnlockNextNight();

        Assert.That(nightManager.CurrentNight, Is.EqualTo(1));
        Assert.That(nightManager.UnlockedNight, Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt(CurrentNightKey), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt(UnlockedNightKey), Is.EqualTo(1));
    }

    [Test]
    public void CurrentNightCannotLeaveSingleShift()
    {
        NightManager nightManager = CreateNightManager();
        nightManager.UnlockNextNight();

        nightManager.SetCurrentNight(3);

        Assert.That(nightManager.CurrentNight, Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt(CurrentNightKey), Is.EqualTo(1));
    }

    [Test]
    public void NightSettingsUseSingleFinalShiftQuota()
    {
        NightManager nightManager = CreateNightManager();

        nightManager.ApplyNightSettings();

        Assert.That(nightManager.CurrentSettings.quotaRequired, Is.EqualTo(10));
        Assert.That(nightManager.CurrentSettings.shiftDuration, Is.EqualTo(360f));

        nightManager.UnlockNextNight();
        nightManager.ApplyNightSettings();

        Assert.That(nightManager.CurrentSettings.quotaRequired, Is.EqualTo(10));
        Assert.That(nightManager.CurrentSettings.shiftDuration, Is.EqualTo(360f));
    }

    [Test]
    public void AssetPackageManifestMatchesDecisionRules()
    {
        List<ManifestItem> manifestItems = LoadManifestItems();
        List<PackageData> packages = PackageCatalog.CreateAssetPackages();

        Assert.That(packages, Has.Count.EqualTo(manifestItems.Count));

        var manifestById = new Dictionary<string, ManifestItem>();
        foreach (ManifestItem item in manifestItems)
        {
            manifestById[item.id] = item;
        }

        foreach (PackageData package in packages)
        {
            Assert.That(manifestById.ContainsKey(package.id), Is.True, package.id + " is missing from the manifest.");
            ManifestItem manifestItem = manifestById[package.id];
            package.RefreshValidationReasons();

            bool calculatedReject = HasAnyManifestMismatch(manifestItem);
            Assert.That(package.ShouldReject, Is.EqualTo(calculatedReject), package.id + " calculated rejection does not match report/box fields.");
            Assert.That(package.ShouldReject, Is.EqualTo(manifestItem.shouldReject), package.id + " manifest shouldReject flag does not match gameplay decision.");

            bool acceptIsCorrect = !package.ShouldReject;
            bool rejectIsCorrect = package.ShouldReject;
            Assert.That(acceptIsCorrect, Is.EqualTo(!manifestItem.shouldReject), package.id + " accept button expectation is inverted.");
            Assert.That(rejectIsCorrect, Is.EqualTo(manifestItem.shouldReject), package.id + " reject button expectation is inverted.");
        }
    }

    [Test]
    public void AssetPackageManifestFilesExist()
    {
        foreach (ManifestItem item in LoadManifestItems())
        {
            Assert.That(File.Exists(ToAssetResourceFilePath(item.reportImage)), Is.True, item.id + " report image is missing.");
            Assert.That(File.Exists(ToAssetResourceFilePath(item.boxLabel)), Is.True, item.id + " box label image is missing.");
        }
    }

    private void AssertDecision(PackageData package, bool accepted, bool expectedCorrect)
    {
        GameManager gameManager = CreateGameManager();

        bool correctDecision = gameManager.RegisterPackageDecision(package, accepted);

        Assert.That(correctDecision, Is.EqualTo(expectedCorrect));
    }

    private GameManager CreateGameManager()
    {
        var gameObject = new GameObject("GameManager Test Host");
        createdObjects.Add(gameObject);
        return gameObject.AddComponent<GameManager>();
    }

    private NightManager CreateNightManager()
    {
        var gameObject = new GameObject("NightManager Test Host");
        createdObjects.Add(gameObject);
        return gameObject.AddComponent<NightManager>();
    }

    private void RestorePlayerPrefs()
    {
        if (hadCurrentNight)
        {
            PlayerPrefs.SetInt(CurrentNightKey, savedCurrentNight);
        }
        else
        {
            PlayerPrefs.DeleteKey(CurrentNightKey);
        }

        if (hadUnlockedNight)
        {
            PlayerPrefs.SetInt(UnlockedNightKey, savedUnlockedNight);
        }
        else
        {
            PlayerPrefs.DeleteKey(UnlockedNightKey);
        }

        PlayerPrefs.Save();
    }

    private static PackageData CreateLegacyPackage(List<string> rejectionReasons)
    {
        return new PackageData(
            "North Annex",
            "Sorting Room",
            4.2f,
            "documents",
            "PKG-123-G",
            "Scan limpo. Peso declarado confere. Rota confirmada.",
            rejectionReasons
        );
    }

    private static List<ManifestItem> LoadManifestItems()
    {
        TextAsset manifest = Resources.Load<TextAsset>("PackageInspectionAssets/package_manifest_30");
        Assert.That(manifest, Is.Not.Null, "Package manifest could not be loaded from Resources.");

        ManifestWrapper wrapper = JsonUtility.FromJson<ManifestWrapper>("{\"items\":" + manifest.text + "}");
        Assert.That(wrapper, Is.Not.Null);
        Assert.That(wrapper.items, Is.Not.Null);
        return new List<ManifestItem>(wrapper.items);
    }

    private static bool HasAnyManifestMismatch(ManifestItem item)
    {
        return !Matches(item.report.shape, item.box.shape)
            || !Matches(item.report.barcode, item.box.barcode)
            || !Matches(item.report.logo, item.box.logo)
            || !Matches(item.report.tapeColor, item.box.tapeColor)
            || !Matches(item.report.destination, item.box.destination)
            || !Matches(item.report.weight, item.box.weight);
    }

    private static bool Matches(string left, string right)
    {
        return string.Equals(Clean(left), Clean(right), System.StringComparison.OrdinalIgnoreCase);
    }

    private static string Clean(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }

    private static string ToAssetResourceFilePath(string manifestPath)
    {
        return Path.Combine("Assets/Resources/PackageInspectionAssets", manifestPath).Replace("\\", "/");
    }

#pragma warning disable 0649
    [System.Serializable]
    private class ManifestWrapper
    {
        public ManifestItem[] items;
    }

    [System.Serializable]
    private class ManifestItem
    {
        public string id;
        public string difficulty;
        public string reportImage;
        public string boxLabel;
        public ManifestSide report;
        public ManifestSide box;
        public bool shouldReject;
    }

    [System.Serializable]
    private class ManifestSide
    {
        public string shape;
        public string barcode;
        public string logo;
        public string tapeColor;
        public string destination;
        public string weight;
    }
#pragma warning restore 0649
}
