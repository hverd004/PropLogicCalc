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
}
