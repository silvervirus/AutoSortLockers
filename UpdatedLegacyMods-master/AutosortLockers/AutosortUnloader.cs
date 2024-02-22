#if BELOWZERO

using Common.Mod;
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
            unloadableItems = 0;
            containerTargets.Clear();

            MoonpoolExpansionManager manager = FindObjectOfType<MoonpoolExpansionManager>();
            if (manager == null)
            {
                Debug.LogError("MoonpoolExpansionManager not found in the scene.");
                return;
            }

            // Check if the SeaTruck is fully docked
            if (manager.isFullyDocked)
            {
                // Search for all SeaTruck segments in the scene
                SeaTruckSegment[] seaTruckSegments = FindObjectsOfType<SeaTruckSegment>();

                foreach (var seaTruckSegment in seaTruckSegments)
                {
                    // Get all storage containers attached to the SeaTruck segment
                    StorageContainer[] storageContainers = seaTruckSegment.GetComponentsInChildren<StorageContainer>();

                    foreach (var storageContainer in storageContainers)
                    {
                        ItemsContainer itemContainer = storageContainer.container;
                        if (itemContainer.count > 0)
                        {
                            unloadableItems += itemContainer.count;
                            containerTargets.Add(itemContainer);
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

            icon.sprite = Common.Utility.ImageUtils.TextureToSprite(Utilities.GetTexture("Sorter"));


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
        private void UpdatePresence()
        {
            exosuitPresent = false;
            seamothPresent = false;

            Exosuit[] exosuits = FindObjectsOfType<Exosuit>();
            SeaMoth[] seaMoths = FindObjectsOfType<SeaMoth>();

            foreach (var exosuit in exosuits)
            {
                if (exosuit.docked)
                {
                    exosuitPresent = true;
                }
            }

            foreach (var seaMoth in seaMoths)
            {
                if (seaMoth.docked)
                {
                    seamothPresent = true;
                }
            }

            // Update the icon sprite based on the presence of Exosuits and SeaMoths
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

        bool exosuitPresent = false;
        bool seamothPresent = false;
        private void AccumulateUnloadTargets()
        {
            unloadableItems = 0;


            Exosuit[] exosuits = FindObjectsOfType<Exosuit>();
            SeaMoth[] seaMoths = FindObjectsOfType<SeaMoth>();

            containerTargets.Clear();

            foreach (var exosuit in exosuits)
            {
                // Check if the Exosuit is docked before unloading items
                if (!exosuit.docked)
                {
                    exosuitPresent = true;
                    continue; // Skip this Exosuit if it's not docked
                }

                // Access the storage container attached to the Exosuit
                StorageContainer storageContainer = exosuit.storageContainer;

                // Ensure the storage container is not null and has items
                if (storageContainer != null && storageContainer.container.count > 0)
                {
                    // Sort the items in the container before adding it to the list
                    storageContainer.container.Sort();

                    // Increment the total unloadable items count
                    unloadableItems += storageContainer.container.count;

                    // Add the container to the list of container targets
                    containerTargets.Add(storageContainer.container);
                }
            }
            foreach (var seaMoth in seaMoths)
            {
               // Debug.Log("SeaMoth Iteration: " + seaMoth.name);

                if (!seaMoth.docked)
                {
                    seamothPresent=true;
                   // Debug.Log("SeaMoth " + seaMoth.name + " is not docked. Skipping unloading items.");
                    continue; // Skip this SeaMoth if it's not docked
                }

                // Access the UpgradeModulesRoot of the SeaMoth
                Transform upgradeModulesRoot = seaMoth.transform.Find("UpgradeModulesRoot");
                if (upgradeModulesRoot != null)
                {
                    // Access the SeamothStorageModule under UpgradeModulesRoot
                    Transform seamothStorageModule = upgradeModulesRoot.Find("SeamothStorageModule(Clone)");
                    if (seamothStorageModule != null)
                    {
                        //Debug.Log("SeamothStorageModule found on SeaMoth " + seaMoth.name);

                        // Access the storage container under SeamothStorageModule
                        SeamothStorageContainer storageContainer = seamothStorageModule.GetComponent<SeamothStorageContainer>();

                        // Ensure the storage container is not null and has items
                        if (storageContainer != null && storageContainer.container.count > 0)
                        {
                            //Debug.Log("Storage container has items: " + storageContainer.container.count);

                            // Add items count to unloadableItems
                            unloadableItems += storageContainer.container.count;

                            // Add the container to the list of container targets
                            containerTargets.Add(storageContainer.container);

                            // Add debug log to indicate unloading process
                            Debug.Log("Unloading items from storage container");

                            // Transfer items from the storage container
                            TransferItems(storageContainer);
                        }
                        else
                        {
                           // Debug.Log("Storage container is null or empty.");
                        }
                    }
                    else
                    {
                       // Debug.Log("SeamothStorageModule not found on SeaMoth " + seaMoth.name);
                    }
                }
                else
                {
                    //Debug.Log("UpgradeModulesRoot not found on SeaMoth " + seaMoth.name);
                }
            }

            // Method to transfer items from the storage container
            void TransferItems(SeamothStorageContainer storageContainer)
            {
                // Access the container's items and perform unload logic here
                // For example:
                // storageContainer.container.Clear(); // Clear the container after unloading
            }
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