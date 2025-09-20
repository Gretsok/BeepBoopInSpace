using System.Collections;
using System.Collections.Generic;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.Environments.Core;
using TMPro;
using Unity.EditorCoroutines.Editor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Game.Tools.LevelEditing.Editor
{
    public class LevelEditor : EditorWindow
    {
        private const string CELLS_POOL_DATA_ASSET_PATH_KEY = "CPDAPK";
        
        [SerializeField]
        private VisualTreeAsset m_visualTreeAsset;

        [SerializeField]
        private VisualTreeAsset m_cellPreviewButton;
        
        [MenuItem("Tools/Levels/Level Editor")]
        public static void ShowLevelEditor()
        {
            LevelEditor wnd = GetWindow<LevelEditor>();
            wnd.titleContent = new GUIContent("LevelEditor");
        }


        private Dictionary<Button, Cell> m_buttonsCellsAssociations = new();
        
        private ObjectField m_cellsPoolField;
        private VisualElement m_cellsPrefabContainer;
        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;
            
            // Instantiate UXML
            VisualElement uiDocumentUXML = m_visualTreeAsset.Instantiate();
            root.Add(uiDocumentUXML);

            m_cellsPrefabContainer = root.Q<VisualElement>("CellsPreviewGroupBox");

            m_cellsPoolField = root.Q<ObjectField>("CellsPoolField");
            m_cellsPoolField.RegisterValueChangedCallback(HandleCellsPoolFieldValueChanged);
            m_cellsPoolField.value = AssetDatabase.LoadAssetAtPath<CellsPoolDataAsset>(EditorPrefs.GetString(CELLS_POOL_DATA_ASSET_PATH_KEY, string.Empty));
        } 

        private void HandleCellsPoolFieldValueChanged(ChangeEvent<Object> evt)
        {
            var poolDataAsset = (evt.newValue as CellsPoolDataAsset);
            
            // Cleaning previous buttons
            m_cellsPrefabContainer.Clear();
            m_buttonsCellsAssociations.Clear();

            if (!poolDataAsset)
            {
                EditorPrefs.SetString(CELLS_POOL_DATA_ASSET_PATH_KEY, string.Empty);
                return;
            }
            
            EditorPrefs.SetString(CELLS_POOL_DATA_ASSET_PATH_KEY, AssetDatabase.GetAssetPath(poolDataAsset));
            
            // Creating new buttons
            EditorCoroutineUtility.StartCoroutine(InflatingPreviewsRoutine(poolDataAsset), this);
        }

        private IEnumerator InflatingPreviewsRoutine(CellsPoolDataAsset poolDataAsset)
        {
            for (int i = 0; i < poolDataAsset.Cells.Count; i++)
            {
                var cellPrefab = poolDataAsset.Cells[i];
                var previewWidget = m_cellPreviewButton.Instantiate().Q<Button>();
                m_cellsPrefabContainer.Add(previewWidget);
                //AssetPreview.
                var previewTexture = AssetPreview.GetAssetPreview(cellPrefab.gameObject); // Triggers loading of asset preview but returns null if the cache was null at this moment.
                yield return new WaitUntil(() => !AssetPreview.IsLoadingAssetPreview(cellPrefab.GetInstanceID())); // So we wait
                previewTexture = AssetPreview.GetAssetPreview(cellPrefab.gameObject); // And we try to get it again... :clown:
                Debug.Assert(previewTexture != null, "Preview texture is null."); 
                previewWidget.style.backgroundImage = previewTexture;
                previewWidget.text = cellPrefab.name;
                
                previewWidget.RegisterCallback<ClickEvent>(HandlePreviewWidgetClicked);
                m_buttonsCellsAssociations.Add(previewWidget, cellPrefab);
            }
        }

        private void HandlePreviewWidgetClicked(ClickEvent evt)
        {
            // Assign a random new color
            var previewWidget = evt.target as Button;
            if (previewWidget == null || !m_buttonsCellsAssociations.TryGetValue(previewWidget, out var cellPrefab))
                return;
            
            var selectedCell = Selection.activeGameObject?.GetComponent<Cell>();
            if (!selectedCell || !selectedCell.IsSceneBound())
            {
                Debug.LogWarning($"No valid cell selected.");
                return;
            }
            
            var cell = PrefabUtility.InstantiatePrefab(cellPrefab) as Cell;
            cell.transform.SetParent(selectedCell.transform.parent);
            cell.transform.position = selectedCell.transform.position;
            
            cell.SetBackwardCell(selectedCell.BackwardCell);
            cell.SetForwardCell(selectedCell.ForwardCell);
            cell.SetLeftCell(selectedCell.LeftCell);
            cell.SetRightCell(selectedCell.RightCell);
            
            cell.ForwardCell?.SetBackwardCell(cell);
            cell.BackwardCell?.SetForwardCell(cell);
            cell.RightCell?.SetLeftCell(cell);
            cell.LeftCell?.SetRightCell(cell);
            
            DestroyImmediate(selectedCell.gameObject, false);
            
            EditorUtility.SetDirty(cell.gameObject);
            //EditorUtility.SetDirty(selectedCell.gameObject);
        }
    }
}
