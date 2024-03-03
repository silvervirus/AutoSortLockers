#if BELOWZERO

using Common.Mod;
using Discord;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace AutosortLockers
{
    internal class AutosortUnloader : MonoBehaviour
    {
        private static readonly Color MainColor = new Color(0.2f, 0.2f, 1.0f);
        private static readonly Color PulseColor = Color.white;
        private float updateIntervalInSeconds = AutosortConfig.updatePresentsIntervals.Value; // Example: Update presence every 1 second
        private bool initialized;
        private Constructable constructable;
        private StorageContainer container;
        private List<AutosortTarget> singleItemTargets = new List<AutosortTarget>();
        private List<AutosortTarget> categoryTargets = new List<AutosortTarget>();
        private List<AutosortTarget> anyTargets = new List<AutosortTarget>();

        private int unsortableItems = 0;
        private int unloadableItems = 0;

        [SerializeField]
        private Image background;
        [SerializeField]
        private Image icon;
        [SerializeField]
        private Image unloadIcon;
        [SerializeField]
        private TextMeshProUGUI text;
        [SerializeField]
        private TextMeshProUGUI sortingText;
        [SerializeField]
        private TextMeshProUGUI unloadingText;
        [SerializeField]
        private TextMeshProUGUI unloadingTitle;

        [SerializeField]
        private bool isSorting;
        [SerializeField]
        private bool isUnloading;
        [SerializeField]
        private bool sortedItem;
        [SerializeField]
        private List<ItemsContainer> containerTargets;


        public bool IsSorting => isSorting;

        private void Awake()
        {
            constructable = GetComponent<Constructable>();
            container = GetComponent<StorageContainer>();
            container.hoverText = "Open autosorter";
            container.storageLabel = "Autosorter";

            containerTargets = new List<ItemsContainer>();
            // Limit range of AutosortUnloader
            LimitRange();
        }
        private bool allowUnload = true;

        private IEnumerator UpdateLimitRangeCoroutine()
        {
            while (true)
            {
                LimitRange(); // Call the original method

                // Wait for a certain interval before updating presence again
                yield return new WaitForSeconds(updateIntervalInSeconds);
            }
        }
        private void LimitRange()
        {
            // Define maximum range for MoonpoolExpansionManager
            float maxRange = AutosortConfig.RangeLimit.Value; // Adjust as needed

            // Get all instances of MoonpoolExpansionManager in the scene
            MoonpoolExpansionManager[] managers = FindObjectsOfType<MoonpoolExpansionManager>();

            bool withinRange = true;

            foreach (MoonpoolExpansionManager manager in managers)
            {
                // Check if the MoonpoolExpansionManager is within range
                if (Vector3.Distance(transform.position, manager.transform.position) > maxRange)
                {
                    // MoonpoolExpansionManager is out of range for at least one instance, disable its functionality
                    allowUnload = false;
                    Debug.Log("MoonpoolExpansionManager is out of range, functionality deactivated.");
                    return; // Exit the loop since functionality is already disabled for at least one instance
                }
            }

            // Update the status based on range
            allowUnload = withinRange;
        }



        private void Update()
        {
            if (!initialized && constructable._constructed && transform.parent != null)
            {
                Initialize();
            }

            if (!initialized || !constructable._constructed)
            {
                return;
            }
            UpdateSortingText();
            UpdateUnloadingText();
            UpdateLimitRangeCoroutine();
        }

        private IEnumerator Start()
        {
            while (true)
            {
                yield return new WaitForSeconds(Mathf.Max(0, AutosortConfig.SortInterval.Value - (unsortableItems / 60.0f)));

                yield return Sort();

                yield return Unload();
            }
        }

        private void UpdateUnloadingText()
        {
            if (isUnloading)
            {
                unloadingText.text = "Unloading...";

            } else if (unloadableItems > 0)
            {
                unloadingText.text = $"Unhandled Items: {unloadableItems}";
            } else {
                unloadingText.text = "Ready to Unload";
            }
        }
        private void UpdateSortingText()
        {
            if (isSorting)
            {
                sortingText.text = "Sorting...";

            } else if (unsortableItems > 0)
            {
                sortingText.text = "Unsorted Items: " + unsortableItems;

            }
            else
            {
                sortingText.text = "Ready to Sort";
            }
        }

        private void Initialize()
        {
            background.gameObject.SetActive(true);
            icon.gameObject.SetActive(true);
            text.gameObject.SetActive(true);
            sortingText.gameObject.SetActive(true);

            background.sprite = Common.Utility.ImageUtils.TextureToSprite(Utilities.GetTexture("LockerScreen"));

            icon.sprite = Common.Utility.ImageUtils.TextureToSprite(Utilities.GetTexture("Seatruck"));



            unloadIcon.sprite = Common.Utility.ImageUtils.TextureToSprite(Utilities.GetTexture("Unloading"));

            initialized = true;
        }

        private void AccumulateTargets()
        {
            singleItemTargets.Clear();
            categoryTargets.Clear();
            anyTargets.Clear();

            SubRoot subRoot = gameObject.GetComponentInParent<SubRoot>();
            if (subRoot == null)
            {
                return;
            }

            var allTargets = subRoot.GetComponentsInChildren<AutosortTarget>().ToList();
            foreach (var target in allTargets)
            {
                if (target.isActiveAndEnabled && target.CanAddItems())
                {
                    if (target.CanTakeAnyItem())
                    {
                        anyTargets.Add(target);
                    }
                    else
                    {
                        if (target.HasItemFilters())
                        {
                            singleItemTargets.Add(target);
                        }
                        if (target.HasCategoryFilters())
                        {
                            categoryTargets.Add(target);
                        }
                    }
                }
            }
        }
        private IEnumerator Sort()
        {
            sortedItem = false;
            unsortableItems = container.container.count;

            if (!initialized || container.IsEmpty())
            {
                isSorting = false;
                yield break;
            }

            AccumulateTargets();
            if (NoTargets())
            {
                isSorting = false;
                yield break;
            }

            yield return SortFilteredTargets(false);
            if (sortedItem)
            {
                isSorting = true;
                yield break;
            }

            yield return SortFilteredTargets(true);
            if (sortedItem)
            {
                isSorting = true;
                yield break;
            }

            yield return SortAnyTargets();
            if (sortedItem)
            {
                isSorting = true;
                yield break;
            }

            isSorting = false;
        }








        private bool notDockedMessageLogged = false;
        private void AccumulateUnloadTargets()
        {
            if (!allowUnload)
            {
                // Unloading is not allowed, return without accumulating unload targets
                return;
            }

            unloadableItems = 0;
            containerTargets.Clear();

            MoonpoolExpansionManager manager = FindObjectOfType<MoonpoolExpansionManager>();
            if (manager == null)
            {
                Debug.LogError("MoonpoolExpansionManager not found in the scene.");
                return;
            }

            // Define maximum range
            float maxRange = AutosortConfig.RangeLimit.Value; // Adjust as needed

            // Check if the SeaTruck is fully docked
            if (manager.isFullyDocked)
            {
                // Find the SeaTruck object
                GameObject seaTruck = GameObject.Find("SeaTruck(Clone)");
                if (seaTruck == null)
                {
                    Debug.LogError("SeaTruck(Clone) not found in the scene.");
                    return;
                }

                // Find the UpgradeModulesRoot object inside SeaTruck
                Transform upgradeModulesRoot = seaTruck.transform.Find("UpgradeModulesRoot");
                if (upgradeModulesRoot == null)
                {
                    Debug.LogError("UpgradeModulesRoot not found inside SeaTruck(Clone).");
                    return;
                }

                // Iterate through all child objects of UpgradeModulesRoot
                foreach (Transform child in upgradeModulesRoot)
                {
                    // Find the SeamothStorageContainer component attached to each child
                    SeamothStorageContainer storageContainer = child.GetComponent<SeamothStorageContainer>();
                    if (storageContainer != null && Vector3.Distance(transform.position, storageContainer.transform.position) <= maxRange)
                    {
                        // Access the container's items and check if it has items
                        ItemsContainer itemContainer = storageContainer.container;
                        if (itemContainer.count > 0)
                        {
                            unloadableItems += itemContainer.count;
                            containerTargets.Add(itemContainer);
                            Debug.LogError("Added items from container to unloadableItems.");
                        }
                        else
                        {
                            Debug.LogWarning("No items found in the container.");
                        }
                    }
                }

                // Find all SeaTruck segments in the scene
                SeaTruckSegment[] seaTruckSegments = FindObjectsOfType<SeaTruckSegment>();

                foreach (var seaTruckSegment in seaTruckSegments)
                {
                    // Get all storage containers attached to the SeaTruck segment
                    StorageContainer[] storageContainers = seaTruckSegment.GetComponentsInChildren<StorageContainer>();

                    foreach (var storageContainer in storageContainers)
                    {
                        if (Vector3.Distance(transform.position, storageContainer.transform.position) <= maxRange)
                        {
                            ItemsContainer itemContainer = storageContainer.container;
                            if (itemContainer.count > 0)
                            {
                                unloadableItems += itemContainer.count;
                                containerTargets.Add(itemContainer);
                            }
                        }
                    }
                }

                // Reset the notDockedMessageLogged flag when the SeaTruck is docked
                notDockedMessageLogged = false;
            }
            else
            {
                // Log the message only if it hasn't been logged before
                if (!notDockedMessageLogged)
                {
                    Debug.Log("Seatruck is Not Docked");
                    notDockedMessageLogged = true; // Set the flag to true after logging the message
                }
            }
        }


        private IEnumerator Unload()
        {
            isUnloading = true;
            AccumulateUnloadTargets();

            if (container.container.IsFull() || containerTargets.Count == 0)
            {
                isUnloading = false;
                yield break;
            }

            foreach (var containerTarget in containerTargets)
            {
                foreach (var item in containerTarget.ToList())
                {
                    if (container.container.HasRoomFor(item.techType))
                    {
                        container.container.AddItem(item.item);
                        StartCoroutine(PulseUnloadIcon()); // Check if this coroutine should be started here or elsewhere
                    }
                    else
                    {
                        isUnloading = false;
                        yield break; // Exit the loop if there's no room in the main container
                    }
                    yield return null;
                }
            }

            // Clear containerTargets after unloading
            containerTargets.Clear();
            isUnloading = false;
        }

        private bool NoTargets()
        {
            return singleItemTargets.Count <= 0 && categoryTargets.Count <= 0 && anyTargets.Count <= 0;
        }

        private IEnumerator SortFilteredTargets(bool byCategory)
        {
            int callsToCanAddItem = 0;
            const int CanAddItemCallThreshold = 10;

            foreach (AutosortTarget target in byCategory ? categoryTargets : singleItemTargets)
            {
                foreach (AutosorterFilter filter in target.GetCurrentFilters())
                {
                    if (filter.IsCategory() == byCategory)
                    {
                        foreach (var techType in filter.Types)
                        {
                            callsToCanAddItem++;
                            if (!TechTypeExtensions.FromString(techType, out TechType tt, true))
                            {
                                continue;
                            }

                            var items = container.container.GetItems(tt);
                            if (items != null && items.Count > 0 && target.CanAddItem(items[0].item))
                            {
                                unsortableItems -= items.Count;
                                SortItem(items[0].item, target);
                                sortedItem = true;
                                yield break;
                            }
                            else if (callsToCanAddItem > CanAddItemCallThreshold)
                            {
                                callsToCanAddItem = 0;
                                yield return null;
                            }
                        }
                    }
                }
            }
        }

        private IEnumerator SortAnyTargets()
        {
            int callsToCanAddItem = 0;
            const int CanAddItemCallThreshold = 10;
            foreach (var item in container.container.ToList())
            {
                foreach (AutosortTarget target in anyTargets)
                {
                    callsToCanAddItem++;
                    if (target.CanAddItem(item.item))
                    {
                        SortItem(item.item, target);
                        unsortableItems--;
                        sortedItem = true;
                        yield break;
                    }
                    else if (callsToCanAddItem > CanAddItemCallThreshold)
                    {
                        callsToCanAddItem = 0;
                        yield return null;
                    }
                }
            }
        }

        private void SortItem(Pickupable pickup, AutosortTarget target)
        {
            container.container.RemoveItem(pickup, true);
            target.AddItem(pickup);

            StartCoroutine(PulseIcon());
        }

        public IEnumerator PulseIcon()
        {
            float t = 0;
            float rate = 0.5f;
            while (t < 1.0)
            {
                t += Time.deltaTime * rate;
                icon.color = Color.Lerp(PulseColor, MainColor, t);
                yield return null;
            }
        }
        public IEnumerator PulseUnloadIcon()
        {
            float t = 0;
            float rate = 0.5f;
            while (t < 1.0)
            {
                t += Time.deltaTime * rate;
                unloadIcon.color = Color.Lerp(PulseColor, MainColor, t);
                yield return null;
            }
        }

        internal class AutosortUnloaderLockerBuildable
        {
            public static PrefabInfo Info { get; private set; }
            public static void Patch()
            {

                Info = Utilities.CreatePrefabInfo(
                    "AutosortUnloader",
                    "Autosort Seatruck Unloader",
                    "Works like an Autosort Receptacle, while also unloading items from docked vehicles!",
                    Utilities.GetSprite("AutosortSEAUnloader")
                    );




                var customPrefab = new CustomPrefab(Info);
                var clonePrefab = new CloneTemplate(Info, TechType.SmallLocker);

                clonePrefab.ModifyPrefab += obj =>
                {



                    try
                    {
                       
                    var container = obj.GetComponent<StorageContainer>();
                    container.width = AutosortConfig.AutosorterWidth.Value;
                    container.height = AutosortConfig.AutosorterHeight.Value;
                    container.container.Resize(AutosortConfig.AutosorterWidth.Value, AutosortConfig.AutosorterHeight.Value);

                    var meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();
                    foreach (var meshRenderer in meshRenderers)
                    {
                        meshRenderer.material.color = new Color(0.2f, 0.4f, 1.0f);
                    }
                    
                     var prefabText = obj.GetComponentInChildren<TextMeshProUGUI>();

                    var label = obj.FindChild("Label");
                    DestroyImmediate(label);

                    var autoSorter = obj.AddComponent<AutosortUnloader>();
                   
                    var canvas = LockerPrefabShared.CreateCanvas(obj.transform);
                        







                    autoSorter.background = LockerPrefabShared.CreateBackground(canvas.transform);
                    autoSorter.icon = LockerPrefabShared.CreateIcon(autoSorter.background.transform, MainColor, 80); // Originally 40
                    autoSorter.text = LockerPrefabShared.CreateText(autoSorter.background.transform, prefabText, MainColor, 44, 14, "Autosorter");

                    autoSorter.sortingText = LockerPrefabShared.CreateText(autoSorter.background.transform, prefabText, MainColor, -70, 8, "Sorting...");
                    autoSorter.sortingText.alignment = TextAlignmentOptions.Top;

                    autoSorter.unloadIcon = LockerPrefabShared.CreateIcon(autoSorter.background.transform, MainColor, -30); // Originally 40
                    autoSorter.unloadingTitle = LockerPrefabShared.CreateText(autoSorter.background.transform, prefabText, MainColor, -64, 14, "Unloader");

                    autoSorter.unloadingText = LockerPrefabShared.CreateText(autoSorter.background.transform, prefabText, MainColor, -180, 8, "Unloading...");
                    autoSorter.unloadingText.alignment = TextAlignmentOptions.Top;

                    autoSorter.background.gameObject.SetActive(false);
                    autoSorter.icon.gameObject.SetActive(false);
                    autoSorter.text.gameObject.SetActive(false);
                    autoSorter.sortingText.gameObject.SetActive(false);
                    }
                    catch (NullReferenceException)
                    { 
                    }

                };

                if (AutosortConfig.EasyBuild.Value == true)
                {
                    customPrefab.SetRecipeFromJson(Common.Utils.JsonUtils.GetJsonRecipe("EZAutosortUnloader"));
                }
                else
                {
                    customPrefab.SetRecipeFromJson(Common.Utils.JsonUtils.GetJsonRecipe("AutosortUnloader"));
                };
                customPrefab.SetUnlock(TechType.Workbench);
                customPrefab.SetGameObject(clonePrefab);
                customPrefab.SetPdaGroupCategory(TechGroup.InteriorModules, TechCategory.InteriorModule);
                customPrefab.Register();
            }
        }

    }
}
#endif
#if SUBNAUTICA
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Common.Mod;
using UnityEngine;
using UnityEngine.UI;
using Nautilus.Assets.Gadgets;
using Nautilus.Crafting;
using Ingredient = CraftData.Ingredient;
using Nautilus.Assets;
using Nautilus.Assets.PrefabTemplates;
using TMPro;
using Nautilus.Utility;
using System.Drawing;
using Color = UnityEngine.Color;
using Image = UnityEngine.UI.Image;
using System.Diagnostics.Eventing.Reader;

namespace AutosortLockers
{
    internal class AutosortUnloader : MonoBehaviour
    {
        private static readonly Color MainColor = new Color(0.2f, 0.2f, 1.0f);
        private static readonly Color PulseColor = Color.white;
        private float updateIntervalInSeconds = AutosortConfig.updatePresentsIntervals.Value; // Example: Update presence every 1 second
        private bool initialized;
        private Constructable constructable;
        private StorageContainer container;
        private List<AutosortTarget> singleItemTargets = new List<AutosortTarget>();
        private List<AutosortTarget> categoryTargets = new List<AutosortTarget>();
        private List<AutosortTarget> anyTargets = new List<AutosortTarget>();

        private int unsortableItems = 0;
        private int unloadableItems = 0;

        [SerializeField]
        private Image background;
        [SerializeField]
        private Image icon;
        [SerializeField]
        private Image unloadIcon;
        [SerializeField]
        private TextMeshProUGUI text;
        [SerializeField]
        private TextMeshProUGUI sortingText;
        [SerializeField]
        private TextMeshProUGUI unloadingText;
        [SerializeField]
        private TextMeshProUGUI unloadingTitle;

        [SerializeField]
        private bool isSorting;
        [SerializeField]
        private bool isUnloading;
        [SerializeField]
        private bool sortedItem;
        [SerializeField]
        private List<ItemsContainer> containerTargets;


        public bool IsSorting => isSorting;

        private void Awake()
        {
            constructable = GetComponent<Constructable>();
            container = GetComponent<StorageContainer>();
            container.hoverText = "Open autosorter";
            container.storageLabel = "Autosorter";

            containerTargets = new List<ItemsContainer>();
            // Limit range of AutosortUnloader
            LimitRange();
        }

        private IEnumerator UpdateLimitRangeCoroutine()
        {
            while (true)
            {
                LimitRange(); // Call the original method

                // Wait for a certain interval before updating presence again
                yield return new WaitForSeconds(updateIntervalInSeconds);
            }
        }
        private bool allowUnload = true;

        private void LimitRange()
        {
            // Define maximum range for AutosortUnloader
            float maxRange = AutosortConfig.RangeLimit.Value; // Adjust as needed

            // Get all VehicleDockingBays in the scene
            VehicleDockingBay[] dockingBays = UnityEngine.Object.FindObjectsOfType<VehicleDockingBay>();

            foreach (VehicleDockingBay dockingBay in dockingBays)
            {
                // Check if the AutosortUnloader is within range of any docking bay
                if (Vector3.Distance(transform.position, dockingBay.transform.position) > maxRange)
                {
                    // AutosortUnloader is out of range, deactivate unloading
                    allowUnload = false;
                    Debug.Log("AutosortUnloader is out of range, unloading deactivated.");
                    return;
                }
            }

            // If within range of all docking bays, allow unloading
            allowUnload = true;
        }
        private void Update()
        {
            if (!initialized && constructable._constructed && transform.parent != null)
            {
                Initialize();
            }

            if (!initialized || !constructable._constructed)
            {
                return;
            }
            UpdateSortingText();
            UpdateUnloadingText();
            UpdateLimitRangeCoroutine();
        }

        private IEnumerator Start()
        {
            StartCoroutine(UpdatePresenceCoroutine());
            while (true)
            {
                yield return new WaitForSeconds(Mathf.Max(0, AutosortConfig.SortInterval.Value - (unsortableItems / 60.0f)));

                yield return Sort();

                yield return Unload();
            }
           
        }

        private void UpdateUnloadingText()
        {
            if (isUnloading)
            {
                unloadingText.text = "Unloading...";

            }
            else if (unloadableItems > 0)
            {
                unloadingText.text = $"Unhandled Items: {unloadableItems}";
            }
            else
            {
                unloadingText.text = "Ready to Unload";
            }
        }
        private void UpdateSortingText()
        {
            if (isSorting)
            {
                sortingText.text = "Sorting...";

            }
            else if (unsortableItems > 0)
            {
                sortingText.text = "Unsorted Items: " + unsortableItems;

            }
            else
            {
                sortingText.text = "Ready to Sort";
            }
        }

        private void Initialize()
        {
            background.gameObject.SetActive(true);
            icon.gameObject.SetActive(true);
            text.gameObject.SetActive(true);
            sortingText.gameObject.SetActive(true);

            background.sprite = Common.Utility.ImageUtils.TextureToSprite(Utilities.GetTexture("LockerScreen"));

            


            unloadIcon.sprite = Common.Utility.ImageUtils.TextureToSprite(Utilities.GetTexture("Unloading"));

            initialized = true;
        }

        private void AccumulateTargets()
        {
            singleItemTargets.Clear();
            categoryTargets.Clear();
            anyTargets.Clear();

            SubRoot subRoot = gameObject.GetComponentInParent<SubRoot>();
            if (subRoot == null)
            {
                return;
            }

            var allTargets = subRoot.GetComponentsInChildren<AutosortTarget>().ToList();
            foreach (var target in allTargets)
            {
                if (target.isActiveAndEnabled && target.CanAddItems())
                {
                    if (target.CanTakeAnyItem())
                    {
                        anyTargets.Add(target);
                    }
                    else
                    {
                        if (target.HasItemFilters())
                        {
                            singleItemTargets.Add(target);
                        }
                        if (target.HasCategoryFilters())
                        {
                            categoryTargets.Add(target);
                        }
                    }
                }
            }
        }
        private IEnumerator Sort()
        {
            sortedItem = false;
            unsortableItems = container.container.count;

            if (!initialized || container.IsEmpty())
            {
                isSorting = false;
                yield break;
            }

            AccumulateTargets();
            if (NoTargets())
            {
                isSorting = false;
                yield break;
            }

            yield return SortFilteredTargets(false);
            if (sortedItem)
            {
                isSorting = true;
                yield break;
            }

            yield return SortFilteredTargets(true);
            if (sortedItem)
            {
                isSorting = true;
                yield break;
            }

            yield return SortAnyTargets();
            if (sortedItem)
            {
                isSorting = true;
                yield break;
            }

            isSorting = false;

        }
        private IEnumerator UpdatePresenceCoroutine()
        {
            while (true)
            {
                UpdatePresence(); // Call the original method

                // Wait for a certain interval before updating presence again
                yield return new WaitForSeconds(updateIntervalInSeconds);
            }
        }
        float maxRange = AutosortConfig.RangeLimit.Value; // Define maximum range

        private void UpdatePresence()
        {
            exosuitPresent = false;
            seamothPresent = false;

            Exosuit[] exosuits = FindObjectsOfType<Exosuit>();
            SeaMoth[] seaMoths = FindObjectsOfType<SeaMoth>();

            foreach (var exosuit in exosuits)
            {
                if (exosuit.docked && IsWithinRange(exosuit.transform.position))
                {
                    exosuitPresent = true;
                    break; // No need to continue iterating once a docked Exosuit within range is found
                }
            }

            foreach (var seaMoth in seaMoths)
            {
                if (seaMoth.docked && IsWithinRange(seaMoth.transform.position))
                {
                    seamothPresent = true;
                    break; // No need to continue iterating once a docked SeaMoth within range is found
                }
            }

            // Update the icon sprite based on the presence of Exosuits and SeaMoths within range
            if (exosuitPresent && seamothPresent)
            {
                icon.sprite = Common.Utility.ImageUtils.TextureToSprite(Utilities.GetTexture("Sorter"));
            }
            else if (exosuitPresent)
            {
                icon.sprite = Common.Utility.ImageUtils.TextureToSprite(Utilities.GetTexture("Exosuit"));
            }
            else if (seamothPresent)
            {
                icon.sprite = Common.Utility.ImageUtils.TextureToSprite(Utilities.GetTexture("Seamoth"));
            }
            else
            {
                icon.sprite = Common.Utility.ImageUtils.TextureToSprite(Utilities.GetTexture("Sorter"));
            }
        }

        private bool IsWithinRange(Vector3 position)
        {
            return Vector3.Distance(transform.position, position) <= maxRange;
        }


        bool exosuitPresent = false;
        bool seamothPresent = false;
        private void AccumulateUnloadTargets()
        {
            if (!allowUnload) return; // Check if unloading is allowed

            this.unloadableItems = 0;
            Exosuit[] exosuits = UnityEngine.Object.FindObjectsOfType<Exosuit>();
            SeaMoth[] seaMoths = UnityEngine.Object.FindObjectsOfType<SeaMoth>();
            this.containerTargets.Clear();

            List<TechType> exclusionTechTypes = new List<TechType> { TechType.SeamothTorpedoModule, TechType.ExosuitTorpedoArmModule };
            float maxRange = AutosortConfig.RangeLimit.Value; // Define maximum range

            foreach (Exosuit exosuit in exosuits)
            {
                if (Vector3.Distance(transform.position, exosuit.transform.position) <= maxRange)
                {
                    bool docked = !exosuit.docked;
                    if (!docked)
                    {
                        StorageContainer storageContainer = exosuit.storageContainer;
                        if (storageContainer != null && storageContainer.container.count > 0)
                        {
                            storageContainer.container.Sort();
                            this.unloadableItems += storageContainer.container.count;
                            this.containerTargets.Add(storageContainer.container);
                        }

                        foreach (Transform child in exosuit.transform)
                        {
                            string[] storageModuleNames = new string[] { "StorageModuleMk5(Clone)", "StorageModuleMk4(Clone)", "StorageModuleMk3(Clone)", "StorageModuleMk2(Clone)", "StorageModuleMk1(Clone)" };
                            foreach (string moduleName in storageModuleNames)
                            {
                                Transform storageModule = child.Find(moduleName);
                                if (storageModule != null && Vector3.Distance(transform.position, storageModule.position) <= maxRange)
                                {
                                    Transform storageRoot = storageModule.Find("StorageRoot");
                                    if (storageRoot != null)
                                    {
                                        SeamothStorageContainer storageContainerExo = storageRoot.GetComponent<SeamothStorageContainer>();
                                        if (storageContainerExo != null && storageContainerExo.container.count > 0 && !exclusionTechTypes.Contains(storageContainerExo.GetComponent<TechTag>().type))
                                        {
                                            this.unloadableItems += storageContainerExo.container.count;
                                            this.containerTargets.Add(storageContainerExo.container);
                                            Debug.Log("Unloading items from Exosuit storage module");
                                            this.TransferItems(storageContainerExo);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (SeaMoth seaMoth in seaMoths)
            {
                if (Vector3.Distance(transform.position, seaMoth.transform.position) <= maxRange)
                {
                    bool docked = !seaMoth.docked;
                    if (!docked)
                    {
                        Transform upgradeModulesRoot = seaMoth.transform.Find("UpgradeModulesRoot");
                        if (upgradeModulesRoot != null)
                        {
                            foreach (Transform child2 in upgradeModulesRoot)
                            {
                                SeamothStorageContainer seamothStorageContainer = child2.GetComponent<SeamothStorageContainer>();
                                if (seamothStorageContainer != null && Vector3.Distance(transform.position, seamothStorageContainer.transform.position) <= maxRange)
                                {
                                    if (seamothStorageContainer.container.count > 0 && !exclusionTechTypes.Contains(seamothStorageContainer.GetComponent<TechTag>().type))
                                    {
                                        this.unloadableItems += seamothStorageContainer.container.count;
                                        this.containerTargets.Add(seamothStorageContainer.container);
                                        Debug.Log("Unloading items from Seamoth storage container");
                                        this.TransferItems(seamothStorageContainer);
                                    }
                                }

                                SeamothStorageContainer vehicleStorageContainer = child2.GetComponent<SeamothStorageContainer>();
                                if (vehicleStorageContainer != null && Vector3.Distance(transform.position, vehicleStorageContainer.transform.position) <= maxRange)
                                {
                                    if (vehicleStorageContainer.container.count > 0 && !exclusionTechTypes.Contains(vehicleStorageContainer.GetComponent<TechTag>().type))
                                    {
                                        this.unloadableItems += vehicleStorageContainer.container.count;
                                        this.containerTargets.Add(vehicleStorageContainer.container);
                                        Debug.Log("Unloading items from Vehicle storage container");
                                        this.TransferItems(vehicleStorageContainer);
                                    }
                                }

                                string[] storageModuleNames2 = new string[] { "StorageModule(Clone)", "StorageModuleMk5(Clone)", "StorageModuleMk4(Clone)", "StorageModuleMk3(Clone)", "StorageModuleMk2(Clone)", "StorageModuleMk1(Clone)" };
                                foreach (string moduleName2 in storageModuleNames2)
                                {
                                    Transform storageModuleRoot = child2.Find(moduleName2);
                                    if (storageModuleRoot != null && Vector3.Distance(transform.position, storageModuleRoot.position) <= maxRange)
                                    {
                                        SeamothStorageContainer storageContainer2 = storageModuleRoot.GetComponent<SeamothStorageContainer>();
                                        if (storageContainer2 != null && storageContainer2.container.count > 0 && !exclusionTechTypes.Contains(storageContainer2.GetComponent<TechTag>().type))
                                        {
                                            this.unloadableItems += storageContainer2.container.count;
                                            this.containerTargets.Add(storageContainer2.container);
                                            Debug.Log("Unloading items from " + moduleName2 + " storage container");
                                            this.TransferItems(storageContainer2);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Method to transfer items from the storage container
        void TransferItems(SeamothStorageContainer storageContainer)
        {
            if (!allowUnload) return; // Check if unloading is allowed

            // Access the container's items and perform unload logic here
            // For example:
            // storageContainer.container.Clear(); // Clear the container after unloading
        }






        private IEnumerator Unload()
        {
            isUnloading = false;

            AccumulateUnloadTargets();

            if (container.container.IsFull())
                yield break;

            if (containerTargets.Count() <= 0)
                yield break;

            foreach (var containerTarget in containerTargets)
            {
                foreach (var item in containerTarget.ToList())
                {
                    isUnloading = true;
                    if (container.container.HasRoomFor(item.techType))
                    {
                        container.container.AddItem(item.item);
                        StartCoroutine(PulseUnloadIcon());

                        yield break;
                    }
                    else
                    {
                        isUnloading = false;
                        yield return null;
                    }
                }
            }
            isUnloading = false;
        }
        private bool NoTargets()
        {
            return singleItemTargets.Count <= 0 && categoryTargets.Count <= 0 && anyTargets.Count <= 0;
        }

        private IEnumerator SortFilteredTargets(bool byCategory)
        {
            int callsToCanAddItem = 0;
            const int CanAddItemCallThreshold = 10;

            foreach (AutosortTarget target in byCategory ? categoryTargets : singleItemTargets)
            {
                foreach (AutosorterFilter filter in target.GetCurrentFilters())
                {
                    if (filter.IsCategory() == byCategory)
                    {
                        foreach (var techType in filter.Types)
                        {
                            callsToCanAddItem++;
                            if (!TechTypeExtensions.FromString(techType, out TechType tt, true))
                            {
                                continue;
                            }

                            var items = container.container.GetItems(tt);
                            if (items != null && items.Count > 0 && target.CanAddItem(items[0].item))
                            {
                                unsortableItems -= items.Count;
                                SortItem(items[0].item, target);
                                sortedItem = true;
                                yield break;
                            }
                            else if (callsToCanAddItem > CanAddItemCallThreshold)
                            {
                                callsToCanAddItem = 0;
                                yield return null;
                            }
                        }
                    }
                }
            }
        }

        private IEnumerator SortAnyTargets()
        {
            int callsToCanAddItem = 0;
            const int CanAddItemCallThreshold = 10;
            foreach (var item in container.container.ToList())
            {
                foreach (AutosortTarget target in anyTargets)
                {
                    callsToCanAddItem++;
                    if (target.CanAddItem(item.item))
                    {
                        SortItem(item.item, target);
                        unsortableItems--;
                        sortedItem = true;
                        yield break;
                    }
                    else if (callsToCanAddItem > CanAddItemCallThreshold)
                    {
                        callsToCanAddItem = 0;
                        yield return null;
                    }
                }
            }
        }

        private void SortItem(Pickupable pickup, AutosortTarget target)
        {
            container.container.RemoveItem(pickup, true);
            target.AddItem(pickup);

            StartCoroutine(PulseIcon());
        }

        public IEnumerator PulseIcon()
        {
            float t = 0;
            float rate = 0.5f;
            while (t < 1.0)
            {
                t += Time.deltaTime * rate;
                icon.color = Color.Lerp(PulseColor, MainColor, t);
                yield return null;
            }
        }
        public IEnumerator PulseUnloadIcon()
        {
            float t = 0;
            float rate = 0.5f;
            while (t < 1.0)
            {
                t += Time.deltaTime * rate;
                unloadIcon.color = Color.Lerp(PulseColor, MainColor, t);
                yield return null;
            }
        }

        internal class AutosortUnloaderLockerBuildable
        {
            public static PrefabInfo Info { get; private set; }
            public static void Patch()
            {
                Info = Utilities.CreatePrefabInfo(
                    "AutosortUnloader",
                    "Autosort Vehicle Unloader",
                    "Works like an Autosort Receptacle, while also unloading items from docked vehicles!",
                    Utilities.GetSprite("AutosortUnloader")
                    );

                var customPrefab = new CustomPrefab(Info);
                var clonePrefab = new CloneTemplate(Info, TechType.SmallLocker);

                clonePrefab.ModifyPrefab += obj =>
                {
                    var triggerCull = obj.GetComponentInChildren<TriggerCull>();
                    var container = obj.GetComponent<StorageContainer>();
                    container.width = AutosortConfig.AutosorterWidth.Value;
                    container.height = AutosortConfig.AutosorterHeight.Value;
                    container.container.Resize(AutosortConfig.AutosorterWidth.Value, AutosortConfig.AutosorterHeight.Value);

                    var meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();
                    foreach (var meshRenderer in meshRenderers)
                    {
                        meshRenderer.material.color = new Color(0.2f, 0.4f, 1.0f);
                    }

                    var prefabText = obj.GetComponentInChildren<TextMeshProUGUI>();

                    var label = obj.FindChild("Label");
                    DestroyImmediate(label);

                    var autoSorter = obj.AddComponent<AutosortUnloader>();

                    var canvas = LockerPrefabShared.CreateCanvas(obj.transform);
                    triggerCull.objectToCull = canvas.gameObject;

                    autoSorter.background = LockerPrefabShared.CreateBackground(canvas.transform);
                    autoSorter.icon = LockerPrefabShared.CreateIcon(autoSorter.background.transform, MainColor, 80); // Originally 40
                    autoSorter.text = LockerPrefabShared.CreateText(autoSorter.background.transform, prefabText, MainColor, 44, 14, "Autosorter");

                    autoSorter.sortingText = LockerPrefabShared.CreateText(autoSorter.background.transform, prefabText, MainColor, -70, 8, "Sorting...");
                    autoSorter.sortingText.alignment = TextAlignmentOptions.Top;

                    autoSorter.unloadIcon = LockerPrefabShared.CreateIcon(autoSorter.background.transform, MainColor, -30); // Originally 40
                    autoSorter.unloadingTitle = LockerPrefabShared.CreateText(autoSorter.background.transform, prefabText, MainColor, -64, 14, "Unloader");

                    autoSorter.unloadingText = LockerPrefabShared.CreateText(autoSorter.background.transform, prefabText, MainColor, -180, 8, "Unloading...");
                    autoSorter.unloadingText.alignment = TextAlignmentOptions.Top;

                    autoSorter.background.gameObject.SetActive(false);
                    autoSorter.icon.gameObject.SetActive(false);
                    autoSorter.text.gameObject.SetActive(false);
                    autoSorter.sortingText.gameObject.SetActive(false);
                };

                if (AutosortConfig.EasyBuild.Value == true)
                {
                    customPrefab.SetRecipeFromJson(Common.Utils.JsonUtils.GetJsonRecipe("EZAutosortUnloader"));
                }
                else
                {
                    customPrefab.SetRecipeFromJson(Common.Utils.JsonUtils.GetJsonRecipe("AutosortUnloader"));
                };
                customPrefab.SetUnlock(TechType.Workbench);
                customPrefab.SetGameObject(clonePrefab);
                customPrefab.SetPdaGroupCategory(TechGroup.InteriorModules, TechCategory.InteriorModule);
                customPrefab.Register();
            }
        }

    }
}
#endif