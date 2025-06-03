

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Common.Mod;
using Common.Utility;
using UnityEngine;
using UnityEngine.UI;
using Nautilus.Assets.Gadgets;
using Nautilus.Crafting;
using Ingredient = CraftData.Ingredient;
using Nautilus.Assets;
using Nautilus.Assets.PrefabTemplates;
using TMPro;
using Newtonsoft.Json;
using UWE;
using System.Text;
using System;
using Nautilus.Handlers;

namespace AutosortLockers
{
    public class AutosortTarget : MonoBehaviour
    {
        public const int MaxTypes = 7;
        public const float MaxDistance = 3;

        private bool initialized;
        private Constructable constructable;
        private StorageContainer container;
        private AutosortTypePicker picker;
        private CustomizeScreen customizeScreen;
        private Coroutine plusCoroutine;
        private SaveDataEntry saveData;

        [SerializeField]
        private bool isStandingLocker = false;
        [SerializeField]
        private bool isLocker = false;
        [SerializeField]
        private TextMeshProUGUI textPrefab; // Problem child needs to be created before anything else is called.
        [SerializeField]
        private Image background;
        [SerializeField]
        private Image icon;
        [SerializeField]
        private ConfigureButton configureButton;
        [SerializeField]
        private Image configureButtonImage;
        [SerializeField]
        private ConfigureButton customizeButton;
        [SerializeField]
        private Image customizeButtonImage;
        [SerializeField]
        private TextMeshProUGUI text;
        [SerializeField]
        private TextMeshProUGUI label;
        [SerializeField]
        private TextMeshProUGUI plus;
        [SerializeField]
        private TextMeshProUGUI quantityText;
        [SerializeField]
        private List<AutosorterFilter> currentFilters = new List<AutosorterFilter>();

        public void Awake()
        {
            constructable = GetComponent<Constructable>();
            if (constructable == null)
            {
                Debug.LogWarning("Constructable component not found.");
            }

            container = gameObject.GetComponent<StorageContainer>();
            if (container == null)
            {
                Debug.LogWarning("StorageContainer component not found.");
            }
        }

        public void Start()
        {
            if (!initialized && constructable != null && constructable._constructed && transform.parent != null)
            {
                Initialize();
            }
        }

        public void SetPicker(AutosortTypePicker picker)
        {
            if (picker == null)
            {
                Debug.LogWarning("Picker is null.");
                return;
            }

            this.picker = picker;

            // Define different positions based on the values of isStandingLocker and isLocker
            Vector3 standingLockerPosition = new Vector3(0.0f, 0.61f, 0.25f); // Position for standing locker
            Vector3 lockerPosition = new Vector3(0.0f, -0.4f, 0.4f); // Position for regular locker

            // Set the position based on the conditions
            if (isStandingLocker)
            {
                picker.transform.localPosition = standingLockerPosition;
                // Additional actions specific to the condition of isStandingLocker can be added here
            }

            if (isLocker)
            {
                picker.transform.localPosition = lockerPosition;
                // Additional actions specific to the condition of isLocker can be added here
            }
        }


    

    public List<AutosorterFilter> GetCurrentFilters()
        {
            if (currentFilters == null)
            {
                Debug.LogWarning("Current filters list is null.");
                return new List<AutosorterFilter>(); // Return an empty list or handle this case accordingly
            }

            return new List<AutosorterFilter>(currentFilters); // Return a copy of the currentFilters list
        }

        public void AddFilter(AutosorterFilter filter, PickerButton button)
        {
            if (currentFilters.Count >= AutosortTarget.MaxTypes)
            {
                return;
            }
            if (ContainsFilter(filter))
            {
                return;
            }
            if (AnAutosorterIsSorting())
            {
                return;
            }
            button.gameObject.SetActive(false);
            currentFilters.Add(filter);
            UpdateText();
        }

        public bool ContainsFilter(AutosorterFilter filter)
        {

            foreach (var f in currentFilters)
            {
                if (f.IsSame(filter))
                {
                    return true;
                }
            }

            return false;
        }

        public void RemoveFilter(AutosorterFilter filter)
        {
            if (AnAutosorterIsSorting())
            {
                return;
            }

            // Use a for loop to iterate over the indices of currentFilters
            for (int i = currentFilters.Count - 1; i >= 0; i--)
            {
                if (currentFilters[i].IsSame(filter))
                {
                    currentFilters.RemoveAt(i); // Remove the filter at index i
                    break; // Exit the loop after removing the first occurrence
                }
            }

            UpdateText();
        }

        public void UpdateText()
        {
            // Check if text is null or empty
            if (text == null)
            {
                Debug.LogWarning("Text component is null.");
                return;
            }

            // Check if currentFilters is null or empty
            if (currentFilters == null || currentFilters.Count == 0)
            {
                text.text = "[Any]";
            }
            else
            {
                // Use StringBuilder to efficiently construct the text
                StringBuilder sb = new StringBuilder();
                foreach (var filter in currentFilters)
                {
                    if (filter == null)
                    {
                        Debug.LogWarning("Found a null filter.");
                        continue;
                    }

                    string filterText = filter.IsCategory() ? "[" + filter.GetString() + "]" : filter.GetString();
                    sb.AppendLine(filterText);
                }
                text.text = sb.ToString().TrimEnd(); // TrimEnd to remove trailing newline
            }

            // Update saveData only if it's not null
            if (saveData != null)
            {
                saveData.FilterData = GetNewVersion(currentFilters);
                SettingModified();
            }
            else
            {
                Debug.LogWarning("SaveData is null.");
            }
        }


        public void AddItem(Pickupable item)
        {
            if (container == null)
            {
                Debug.LogWarning("Container is null. Cannot add item.");
                return;
            }

            container.container.AddItem(item);

            // Stop the previously running coroutine (if any)
            if (plusCoroutine != null)
            {
                StopCoroutine(plusCoroutine);
            }

            // Start a new coroutine to show the visual indicator
            plusCoroutine = StartCoroutine(ShowPlus());
        }


        public bool CanAddItemByItemFilter(Pickupable item)
        {
            bool allowed = IsTypeAllowedByItemFilter(item.GetTechType());
            return allowed && container.container.HasRoomFor(item);
        }

        public bool CanAddItemByCategoryFilter(Pickupable item)
        {
            bool allowed = IsTypeAllowedByCategoryFilter(item.GetTechType());
            return allowed && container.container.HasRoomFor(item);
        }

        public bool CanAddItem(Pickupable item)
        {
            bool allowed = CanTakeAnyItem() || IsTypeAllowed(item.GetTechType());
            return allowed && container.container.HasRoomFor(item);
        }

        public bool CanTakeAnyItem()
        {
            return currentFilters == null || currentFilters.Count == 0;
        }

        public bool CanAddItems()
        {
            return constructable.constructed;
        }

        public bool HasCategoryFilters()
        {
            foreach (var filter in currentFilters)
            {
                if (filter.IsCategory())
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasItemFilters()
        {
            foreach (var filter in currentFilters)
            {
                if (!filter.IsCategory())
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsTypeAllowedByCategoryFilter(TechType techType)
        {
            foreach (var filter in currentFilters)
            {
                if (filter.IsCategory() && filter.IsTechTypeAllowed(techType))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsTypeAllowedByItemFilter(TechType techType)
        {
            foreach (var filter in currentFilters)
            {
                if (!filter.IsCategory() && filter.IsTechTypeAllowed(techType))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsTypeAllowed(TechType techType)
        {
            foreach (var filter in currentFilters)
            {
                if (filter.IsTechTypeAllowed(techType))
                {
                    return true;
                }
            }

            return false;
        }

        public void Update()
        {
            if (!initialized || !constructable._constructed)
            {
                return;
            }

            if (Player.main != null)
            {
                float distSq = (Player.main.transform.position - transform.position).sqrMagnitude;
                bool playerInRange = distSq <= (MaxDistance * MaxDistance);
                configureButton.enabled = playerInRange;
                customizeButton.enabled = playerInRange;

                if (picker != null && picker.isActiveAndEnabled && !playerInRange)
                {
                    picker.gameObject.SetActive(false);
                }
                if (customizeScreen != null && customizeScreen.isActiveAndEnabled && !playerInRange)
                {
                    customizeScreen.gameObject.SetActive(false);
                }
            }

            container.enabled = ShouldEnableContainer();

            UpdateQuantityText();
        }

        public bool AnAutosorterIsSorting()
        {
            var root = GetComponentInParent<SubRoot>();
            if (root != null && root.isBase)
            {
                var autosorters = root.GetComponentsInChildren<AutosortLocker>();
                foreach (var autosorter in autosorters)
                {
                    if (autosorter.IsSorting)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool ShouldEnableContainer()
        {
            return (picker == null || !picker.isActiveAndEnabled)
                && (customizeScreen == null || !customizeScreen.isActiveAndEnabled)
                && (!configureButton.pointerOver || !configureButton.enabled)
                && (!customizeButton.pointerOver || !customizeButton.enabled);
        }

        public void ShowConfigureMenu()
        {
            foreach (var otherPicker in GameObject.FindObjectsOfType<AutosortTarget>())
            {
                otherPicker.HideAllMenus();
            }
            picker.gameObject.SetActive(true);
        }

       public void ShowCustomizeMenu()
        {
            foreach (var otherPicker in GameObject.FindObjectsOfType<AutosortTarget>())
            {
                otherPicker.HideAllMenus();
            }
            customizeScreen.gameObject.SetActive(true);
        }

        public void HideConfigureMenu()
        {
            if (picker != null)
            {
                picker.gameObject.SetActive(false);
            }
        }

        public void HideCustomizeMenu()
        {
            if (customizeScreen != null)
            {
                customizeScreen.gameObject.SetActive(false);
            }
        }

        public void HideAllMenus()
        {
            if (initialized)
            {
                HideConfigureMenu();
                HideCustomizeMenu();
            }
        }

        public void Initialize()
        {
            background.gameObject.SetActive(true);
            icon.gameObject.SetActive(true);
            text.gameObject.SetActive(true);

            background.sprite = ImageUtils.TextureToSprite(Utilities.GetTexture("LockerScreen"));
            icon.sprite = ImageUtils.TextureToSprite(Utilities.GetTexture("Receptacle"));

            configureButtonImage.sprite = ImageUtils.TextureToSprite(Utilities.GetTexture("Configure"));
            customizeButtonImage.sprite = ImageUtils.TextureToSprite(Utilities.GetTexture("Edit"));

            configureButton.onClick = ShowConfigureMenu;
            customizeButton.onClick = ShowCustomizeMenu;

            saveData = GetSaveData();

            InitializeFromSaveData();

            InitializeFilters();

            UpdateText();

            UWE.CoroutineHost.StartCoroutine(CreateCustomizeScreen(background, saveData));

            CreatePicker();

            initialized = true;

        }





        public void InitializeFromSaveData()
        {
            if (saveData != null)
            {
                label.text = saveData.Label;
                label.color = saveData.LabelColor.ToColor();
                icon.color = saveData.IconColor.ToColor();
                configureButtonImage.color = saveData.ButtonsColor.ToColor();
                customizeButtonImage.color = saveData.ButtonsColor.ToColor();
                text.color = saveData.OtherTextColor.ToColor();
                quantityText.color = saveData.ButtonsColor.ToColor();
                SetLockerColor(saveData.LockerColor.ToColor());
            }
        }

        public void SetLockerColor(Color color)
        {
            var meshRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var meshRenderer in meshRenderers)
            {
                meshRenderer.material.color = color;
            }
        }

        public SaveDataEntry GetSaveData()
        {
            var prefabIdentifier = GetComponent<PrefabIdentifier>();
            var id = prefabIdentifier.id;

            return Mod.GetSaveData(id);
        }

        public void InitializeFilters()
        {
            if (saveData == null)
            {
                currentFilters = new List<AutosorterFilter>();
                return;
            }

            currentFilters = GetNewVersion(saveData.FilterData);
        }

        public List<AutosorterFilter> GetNewVersion(List<AutosorterFilter> filterData)
        {
            Dictionary<string, AutosorterFilter> validItems = new Dictionary<string, AutosorterFilter>();
            Dictionary<string, AutosorterFilter> validCategories = new Dictionary<string, AutosorterFilter>();
            var filterList = AutosorterList.GetFilters();
            foreach (var filter in filterList)
            {
                if (filter.IsCategory())
                {
                    validCategories[filter.Category] = filter;
                }
                else
                {
                    validItems[filter.Types[0]] = filter;
                }
            }

            var newData = new List<AutosorterFilter>();
            foreach (var filter in filterData)
            {
                if (validCategories.ContainsKey(filter.Category) || filter.Category == "")
                {
                    newData.Add(filter);
                    continue;
                }

                if (filter.Category == "0")
                {
                    filter.Category = "";
                    newData.Add(filter);
                    continue;
                }

                var newTypes = AutosorterList.GetOldFilter(filter.Category, out bool success, out string newCategory);
                if (success)
                {
                    newData.Add(new AutosorterFilter() { Category = newCategory, Types = newTypes });
                    continue;
                }

                newData.Add(filter);
            }
            return newData;
        }

        public void CreatePicker()
        {
            SetPicker(AutosortTypePicker.Create(transform, textPrefab));

            picker.Initialize(this);
            picker.gameObject.SetActive(false);
        }

        public void SettingModified()
        {

            var id = this.GetComponent<PrefabIdentifier>().id;

            Mod.saveData.Entries[id] = saveData;

            InitializeFromSaveData();
        }

        public IEnumerator CreateCustomizeScreen(Image background, SaveDataEntry saveData)
        {
            TaskResult<CustomizeScreen> result = new TaskResult<CustomizeScreen>();
            yield return CustomizeScreen.Create(background.transform, saveData, result);
            customizeScreen = result.Get();

            customizeScreen.onModified += SettingModified;
            customizeScreen.Initialize(saveData);
            customizeScreen.gameObject.SetActive(false);
        }

        public IEnumerator ShowPlus()
        {
            plus.color = new Color(plus.color.r, plus.color.g, plus.color.b, 1);
            float t = 0;
            float rate = 0.5f;
            while (t < 1.0)
            {
                t += Time.deltaTime * rate;
                plus.color = new Color(plus.color.r, plus.color.g, plus.color.b, Mathf.Lerp(1, 0, t));
                yield return null;
            }
        }

        public void UpdateQuantityText()
        {
            var count = container.container.count;
            quantityText.text = count == 0 ? "empty" : count.ToString();
        }

        public class AutosortTargetBuildable
        {
            public static PrefabInfo Info { get; private set; }
            public static TextMeshProUGUI textPrefabs;
            public static void Patch()
            {
                Info = Utilities.CreateBuildablePrefabInfo(
                    "AutosortTarget",
                    "Autosort Receptacle",
                    "Wall locker linked to an Autosorter that receives sorted items.",
                    Utilities.GetSprite("AutosortTarget"));
                UWE.CoroutineHost.StartCoroutine(AutosortStandingTargetBuildable.SetTextMeshProPrefab());

                var customPrefab = new CustomPrefab(Info);
                var clonePrefab = new CloneTemplate(Info, TechType.SmallLocker);
                
                clonePrefab.ModifyPrefab += obj =>
                {
                    var triggerCull = obj.GetComponentInChildren<TriggerCull>();
                    DestroyImmediate(triggerCull);

                    var label = obj.FindChild("Label");
                    DestroyImmediate(label);

                    StorageContainer container = obj.GetComponent<StorageContainer>();
                    container.width = AutosortConfig.ReceptacleWidth.Value;
                    container.height = AutosortConfig.ReceptacleHeight.Value;

                    //container.container.Resize(AutosortConfig.ReceptacleWidth.Value, AutosortConfig.ReceptacleHeight.Value);
                   
                    var meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();
                    foreach (var meshRenderer in meshRenderers)
                    {
                        meshRenderer.material.color = new Color(0.3f, 0.3f, 0.3f);
                    }

                    var autosortTarget = obj.AddComponent<AutosortTarget>();
                    autosortTarget.isLocker = true;

                    autosortTarget.textPrefab = Instantiate(textPrefabs, obj.transform);

                    var canvas = LockerPrefabShared.CreateCanvas(obj.transform);
                    autosortTarget.transform.localPosition = new Vector3(0f, -0.42f, 0.345f);
                    autosortTarget.transform.localEulerAngles = new Vector3(0, 180f, 0);
                   // autosortTarget.transform.localScale = new Vector3(0.65f, 0.80f, 0.80f);
                    canvas.sortingOrder = 1;

                    autosortTarget.background = LockerPrefabShared.CreateBackground(canvas.transform);
                    autosortTarget.icon = LockerPrefabShared.CreateIcon(autosortTarget.background.transform, autosortTarget.textPrefab.color, 70);
                    autosortTarget.text = LockerPrefabShared.CreateText(autosortTarget.background.transform, autosortTarget.textPrefab, autosortTarget.textPrefab.color, -20, 10, "Any");

                    autosortTarget.label = LockerPrefabShared.CreateText(autosortTarget.background.transform, autosortTarget.textPrefab, autosortTarget.textPrefab.color, 100, 12, "Locker");

                    autosortTarget.background.gameObject.SetActive(false);
                    autosortTarget.icon.gameObject.SetActive(false);
                    autosortTarget.text.gameObject.SetActive(false);

                    autosortTarget.plus = LockerPrefabShared.CreateText(autosortTarget.background.transform, autosortTarget.textPrefab, autosortTarget.textPrefab.color, 0, 30, "+");
                    autosortTarget.plus.color = new Color(autosortTarget.textPrefab.color.r, autosortTarget.textPrefab.color.g, autosortTarget.textPrefab.color.g, 0);
                    autosortTarget.plus.rectTransform.anchoredPosition += new Vector2(30, 70);

                    autosortTarget.quantityText = LockerPrefabShared.CreateText(autosortTarget.background.transform, autosortTarget.textPrefab, autosortTarget.textPrefab.color, 0, 10, "XX");
                    autosortTarget.quantityText.rectTransform.anchoredPosition += new Vector2(-32, -104);

                    autosortTarget.configureButton = ConfigureButton.Create(autosortTarget.background.transform, autosortTarget.textPrefab.color, 40);
                    autosortTarget.configureButtonImage = autosortTarget.configureButton.GetComponent<Image>();
                    autosortTarget.customizeButton = ConfigureButton.Create(autosortTarget.background.transform, autosortTarget.textPrefab.color, 20);
                    autosortTarget.customizeButtonImage = autosortTarget.customizeButton.GetComponent<Image>();
                    var autosortTypePicker = obj.GetComponent<AutosortTypePicker>();
                    if (autosortTypePicker != null)
                    {
                        autosortTarget.SetPicker(autosortTypePicker);
                        // Modify the local position of the picker here if needed
                        autosortTypePicker.transform.localPosition = new  Vector3(0.0f, -0.4f, 0.4f); // Modify x, y, z as needed
                    }
                };


                if (AutosortConfig.EasyBuild.Value == true)
                {
                    customPrefab.SetRecipeFromJson(Common.Utils.JsonUtils.GetJsonRecipe("EZAutosortTarget"));
                }
                else
                {
                    customPrefab.SetRecipeFromJson(Common.Utils.JsonUtils.GetJsonRecipe("AutosortTarget"));
                };

                
                customPrefab.SetGameObject(clonePrefab);
                KnownTechHandler.UnlockOnStart(Info.TechType);
                customPrefab.SetPdaGroupCategory(TechGroup.InteriorModules, TechCategory.InteriorModule);

                customPrefab.Register();
            }
        }

        public class AutosortStandingTargetBuildable
        {
            public static PrefabInfo Info;
            private static TextMeshProUGUI textPrefab;

            public static void Patch()
            {
                Info = Utilities.CreateBuildablePrefabInfo(
                    "AutosortTargetStanding",
                    "Standing Autosort Receptacle",
                    "Large locker linked to an Autosorter that receives sorted items.",
                    Utilities.GetSprite("AutosortTargetStanding"));
                UWE.CoroutineHost.StartCoroutine(AutosortStandingTargetBuildable.SetTextMeshProPrefab());


                var customPrefab = new CustomPrefab(Info);
                var clonePrefab = new CloneTemplate(Info, TechType.Locker);

                clonePrefab.ModifyPrefab += obj =>
                {
                    StorageContainer container = obj.GetComponent<StorageContainer>();
                    container.width = AutosortConfig.StandingReceptacleWidth.Value;

                    container.height = AutosortConfig.StandingReceptacleHeight.Value;

                    //container.container.Resize(AutosortConfig.StandingReceptacleWidth.Value, AutosortConfig.StandingReceptacleHeight.Value);

                    var meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();
                    foreach (var meshRenderer in meshRenderers)
                    {
                        meshRenderer.material.color = new Color(0.3f, 0.3f, 0.3f);
                    }

                    var autosortTarget = obj.AddComponent<AutosortTarget>();
                    autosortTarget.isStandingLocker = true;

                    autosortTarget.textPrefab = Instantiate(textPrefab, obj.transform);

                    var canvas = LockerPrefabShared.CreateCanvas(obj.transform);
                    canvas.transform.localPosition = new Vector3(0, 1.1f, 0.25f);
                    canvas.sortingOrder = 1;

                    autosortTarget.background = LockerPrefabShared.CreateBackground(canvas.transform);
                    autosortTarget.icon = LockerPrefabShared.CreateIcon(autosortTarget.background.transform, autosortTarget.textPrefab.color, 70);
                    autosortTarget.text = LockerPrefabShared.CreateText(autosortTarget.background.transform, autosortTarget.textPrefab, autosortTarget.textPrefab.color, -20, 10, "Any");

                    autosortTarget.label = LockerPrefabShared.CreateText(autosortTarget.background.transform, autosortTarget.textPrefab, autosortTarget.textPrefab.color, 100, 12, "Locker");

                    autosortTarget.background.gameObject.SetActive(false);
                    autosortTarget.icon.gameObject.SetActive(false);
                    autosortTarget.text.gameObject.SetActive(false);

                    autosortTarget.plus = LockerPrefabShared.CreateText(autosortTarget.background.transform, autosortTarget.textPrefab, autosortTarget.textPrefab.color, 0, 30, "+");
                    autosortTarget.plus.color = new Color(autosortTarget.textPrefab.color.r, autosortTarget.textPrefab.color.g, autosortTarget.textPrefab.color.g, 0);
                    autosortTarget.plus.rectTransform.anchoredPosition += new Vector2(30, 70);

                    autosortTarget.quantityText = LockerPrefabShared.CreateText(autosortTarget.background.transform, autosortTarget.textPrefab, autosortTarget.textPrefab.color, 0, 10, "XX");
                    autosortTarget.quantityText.rectTransform.anchoredPosition += new Vector2(-32, -104);

                    autosortTarget.configureButton = ConfigureButton.Create(autosortTarget.background.transform, autosortTarget.textPrefab.color, 40);
                    autosortTarget.configureButtonImage = autosortTarget.configureButton.GetComponent<Image>();
                    autosortTarget.customizeButton = ConfigureButton.Create(autosortTarget.background.transform, autosortTarget.textPrefab.color, 20);
                    autosortTarget.customizeButtonImage = autosortTarget.customizeButton.GetComponent<Image>();
                    var autosortTypePicker = obj.GetComponent<AutosortTypePicker>();
                    if (autosortTypePicker != null)
                    {
                        autosortTarget.SetPicker(autosortTypePicker);
                        // Modify the position of the picker here if needed
                        autosortTypePicker.transform.position = new Vector3(24.67f, -4.93f, 62.17f);// Modify x, y, z as needed
                        // Modify the local position of the picker here if needed
                        autosortTypePicker.transform.localPosition = new Vector3(0.2f, -0.61f, 0.25f);
                        
                    }
                };

                if (AutosortConfig.EasyBuild.Value == true)
                {
                    customPrefab.SetRecipeFromJson(Common.Utils.JsonUtils.GetJsonRecipe("EZAutosortTargetStanding"));
                }
                else
                {
                    customPrefab.SetRecipeFromJson(Common.Utils.JsonUtils.GetJsonRecipe("AutosortTargetStanding"));
                };
                customPrefab.SetGameObject(clonePrefab);
                KnownTechHandler.UnlockOnStart(Info.TechType);
                customPrefab.SetPdaGroupCategory(TechGroup.InteriorModules, TechCategory.InteriorModule);
                customPrefab.Register();

            }

            public static IEnumerator SetTextMeshProPrefab()
            {
                CoroutineTask<GameObject> task = CraftData.GetBuildPrefabAsync(TechType.SmallLocker);

                while (task.MoveNext())
                {
                    yield return task.Current;
                }

                GameObject prefab = task.GetResult();
                textPrefab = prefab.GetComponentInChildren<TextMeshProUGUI>();
                AutosortTargetBuildable.textPrefabs = textPrefab;
            }
        

            public static TextMeshProUGUI AddTextMeshProComponent(GameObject prefab)
            {
                // Create a new TextMeshProUGUI component
                TextMeshProUGUI textPrefab = prefab.AddComponent<TextMeshProUGUI>();

                // Set properties of the TextMeshProUGUI component as needed
                // For example:
                // textPrefab.text = "Your Text Here";
                // textPrefab.fontSize = 12;
                // textPrefab.color = Color.white;

                return textPrefab;
            }





        }


        public static CoroutineTask<GameObject> GetBuildPrefabAsync(TechType recipe)
        {
            return CraftData.GetPrefabForTechTypeAsync(recipe, true);
        }
    }
}