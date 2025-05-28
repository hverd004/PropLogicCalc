using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FormulaInputManager : MonoBehaviour
{
    [SerializeField] private List<string> formula = new List<string>();
    [SerializeField] private TextMeshProUGUI formulaDisplay;

    [Header("Truth Table UI")]
    [SerializeField] private GameObject cellPrefab;    // Prefab with TextMeshProUGUI component
    [SerializeField] private Transform tableParent;    // Panel with GridLayoutGroup

    public void AddToFormula(string character)
    {
        formula.Add(character);
        formulaDisplay.text = string.Join(" ", formula.Select(GetDisplaySymbol));
    }

    public void DeleteLastInput()
    {
        if (formula.Count > 0)
        {
            formula.RemoveAt(formula.Count - 1);
            formulaDisplay.text = string.Join(" ", formula.Select(GetDisplaySymbol));
        }
    }

    public void DeleteAllInput()
    {
        formula.Clear();
        formulaDisplay.text = "";
        ClearTruthTableUI();
    }

    private string GetDisplaySymbol(string token)
    {
        return token switch
        {
            "&&" => "∧",
            "||" => "∨",
            "!" => "¬",
            "xor" => "⊕",
            "<->" => "↔",
            "->" => "→",
            "(" => "(",
            ")" => ")",
            _ => token
        };
    }

    public void SolveFormula()
    {
        if (formula.Count == 0) return;

        PropositionalLogicSolver solver = new PropositionalLogicSolver(formula);

        var variables = solver.GetVariables();
        // Evaluate one assignment to populate subexpression labels
        var tempAssignments = solver.GenerateTruthAssignments();
        if (tempAssignments.Count > 0)
            solver.Evaluate(tempAssignments[0]);

        var subExprResults = solver.GetSubexpressionLabels();

        // Now it's safe to configure the grid
        GridLayoutGroup grid = tableParent.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            int columnCount = variables.Count + subExprResults.Count;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columnCount;
        }


        var headers = variables.Concat(subExprResults).ToList();

        var assignments = solver.GenerateTruthAssignments();

        List<List<string>> rows = new List<List<string>>();

        foreach (var assignment in assignments)
        {
            solver.Evaluate(assignment);

            var lastSubResults = solver.GetLastSubexpressionResultsOrdered();

            List<string> row = new List<string>();

            // Variable truth values
            foreach (var v in variables)
                row.Add(assignment[v] ? "T" : "F");

            // Subexpression truth values
            foreach (var label in subExprResults)
            {
                var res = lastSubResults.Find(x => x.label == label);
                row.Add(res.value ? "T" : "F");
            }

            rows.Add(row);
        }

        BuildTruthTableUI(headers, rows);
    }

    private void ClearTruthTableUI()
    {
        foreach (Transform child in tableParent)
        {
            Destroy(child.gameObject);
        }
    }

    //Grid Version
    private void BuildTruthTableUI(List<string> headers, List<List<string>> rows)
    {
        ClearTruthTableUI();

        // Create header cells
        foreach (var header in headers)
        {
            var cell = Instantiate(cellPrefab, tableParent);
            var tmp = cell.GetComponent<TextMeshProUGUI>();
            tmp.text = header;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.color = Color.white;
        }

        // Create data cells
        foreach (var row in rows)
        {
            foreach (var cellText in row)
            {
                var cell = Instantiate(cellPrefab, tableParent);
                var tmp = cell.GetComponent<TextMeshProUGUI>();
                tmp.text = cellText;

                if (cellText == "T")
                    tmp.color = Color.green;
                else if (cellText == "F")
                    tmp.color = Color.red;
                else
                    tmp.color = Color.gray;
            }
        }
    }
    //resize version NOT FUNCTIONAL
    /*private void BuildTruthTableUI(List<string> headers, List<List<string>> rows)
    {
        ClearTruthTableUI();

        int columnCount = headers.Count;

        // Step 1: Measure max width per column
        float[] columnWidths = new float[columnCount];

        // Create a temporary text object to measure widths
        var tempCell = Instantiate(cellPrefab);
        var measureTMP = tempCell.GetComponent<TextMeshProUGUI>();

        // Measure header widths
        for (int i = 0; i < headers.Count; i++)
        {
            measureTMP.text = headers[i];
            LayoutRebuilder.ForceRebuildLayoutImmediate(measureTMP.rectTransform);
            columnWidths[i] = Mathf.Max(columnWidths[i], measureTMP.preferredWidth);
        }

        // Measure data cell widths
        for (int r = 0; r < rows.Count; r++)
        {
            for (int c = 0; c < rows[r].Count; c++)
            {
                measureTMP.text = rows[r][c];
                LayoutRebuilder.ForceRebuildLayoutImmediate(measureTMP.rectTransform);
                columnWidths[c] = Mathf.Max(columnWidths[c], measureTMP.preferredWidth);
            }
        }

        Destroy(tempCell); // Clean up temp object

        // Step 2: Create header row
        var headerRow = new GameObject("HeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        headerRow.transform.SetParent(tableParent, false);

        var headerLayout = headerRow.GetComponent<HorizontalLayoutGroup>();
        headerLayout.childControlWidth = false;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;
        headerLayout.spacing = 20;

        for (int i = 0; i < headers.Count; i++)
        {
            var cell = Instantiate(cellPrefab, headerRow.transform);
            var tmp = cell.GetComponent<TextMeshProUGUI>();
            tmp.text = headers[i];
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;

            // Apply measured width
            var layoutElement = cell.GetComponent<LayoutElement>() ?? cell.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = columnWidths[i];
        }

        // Step 3: Create data rows
        foreach (var row in rows)
        {
            var rowGO = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGO.transform.SetParent(tableParent, false);

            var rowLayout = rowGO.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.spacing = 20;

            for (int i = 0; i < row.Count; i++)
            {
                var cell = Instantiate(cellPrefab, rowGO.transform);
                var tmp = cell.GetComponent<TextMeshProUGUI>();
                tmp.text = row[i];

                tmp.color = row[i] switch
                {
                    "T" => Color.green,
                    "F" => Color.red,
                    _ => Color.gray
                };

                // Apply measured width
                var layoutElement = cell.GetComponent<LayoutElement>() ?? cell.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = columnWidths[i];
            }
        }
    }*/

}
