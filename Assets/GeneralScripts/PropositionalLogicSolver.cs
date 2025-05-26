using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PropositionalLogicSolver : MonoBehaviour
{
    private List<string> formula;
    private List<string> variables;
    private int position;
    private List<string> tokens;
    private Dictionary<string, bool> currentAssignment;

    // Subexpression tracking
    private List<string> subexpressionLabels = new List<string>();
    private List<(string label, bool value)> lastSubexpressionResults = new List<(string, bool)>();

    public PropositionalLogicSolver(List<string> formulaTokens)
    {
        formula = formulaTokens;
        variables = ExtractVariables(formula);
    }

    private List<string> ExtractVariables(List<string> formula)
    {
        var operators = new HashSet<string> { "&&", "||", "!", "xor", "<->", "->", "(", ")" };
        HashSet<string> vars = new HashSet<string>();

        foreach (var token in formula)
        {
            if (!operators.Contains(token) && !string.IsNullOrEmpty(token))
                vars.Add(token);
        }

        return vars.OrderBy(v => v).ToList();
    }

    public List<string> GetVariables() => variables;

    public List<string> GetSubexpressionLabels() => subexpressionLabels;

    public List<(string label, bool value)> GetLastSubexpressionResultsOrdered() => lastSubexpressionResults;

    public List<Dictionary<string, bool>> GenerateTruthAssignments()
    {
        int n = variables.Count;
        int rows = 1 << n;
        var assignments = new List<Dictionary<string, bool>>();

        for (int i = 0; i < rows; i++)
        {
            var assignment = new Dictionary<string, bool>();
            for (int j = 0; j < n; j++)
            {
                bool value = (i & (1 << j)) != 0;
                assignment[variables[j]] = value;
            }
            assignments.Add(assignment);
        }

        return assignments;
    }

    public bool Evaluate(Dictionary<string, bool> assignment)
    {
        tokens = formula;
        position = 0;
        currentAssignment = assignment;

        subexpressionLabels.Clear();
        lastSubexpressionResults.Clear();

        return ParseExpressionWithLabel(out _);
    }

    private bool ParseExpressionWithLabel(out string label)
    {
        return ParseEquivalenceWithLabel(out label);
    }

    private bool ParseEquivalenceWithLabel(out string label)
    {
        bool left = ParseImplicationWithLabel(out string leftLabel);
        label = leftLabel;

        while (Match("<->"))
        {
            bool right = ParseImplicationWithLabel(out string rightLabel);
            bool result = left == right;

            label = $"({leftLabel}↔{rightLabel})";
            AddSubexpression(label, result);

            left = result;
            leftLabel = label;
        }

        label = leftLabel;
        return left;
    }

    private bool ParseImplicationWithLabel(out string label)
    {
        bool left = ParseOrXorWithLabel(out string leftLabel);
        label = leftLabel;

        while (Match("->"))
        {
            bool right = ParseImplicationWithLabel(out string rightLabel);
            bool result = !left || right;

            label = $"({leftLabel}→{rightLabel})";
            AddSubexpression(label, result);

            left = result;
            leftLabel = label;
        }

        label = leftLabel;
        return left;
    }

    private bool ParseOrXorWithLabel(out string label)
    {
        bool left = ParseAndWithLabel(out string leftLabel);
        label = leftLabel;

        while (true)
        {
            if (Match("||"))
            {
                bool right = ParseAndWithLabel(out string rightLabel);
                bool result = left || right;

                label = $"({leftLabel}∨{rightLabel})";
                AddSubexpression(label, result);

                left = result;
                leftLabel = label;
            }
            else if (Match("xor"))
            {
                bool right = ParseAndWithLabel(out string rightLabel);
                bool result = left ^ right;

                label = $"({leftLabel}⊕{rightLabel})";
                AddSubexpression(label, result);

                left = result;
                leftLabel = label;
            }
            else
            {
                break;
            }
        }

        label = leftLabel;
        return left;
    }

    private bool ParseAndWithLabel(out string label)
    {
        bool left = ParseNotWithLabel(out string leftLabel);
        label = leftLabel;

        while (Match("&&"))
        {
            bool right = ParseNotWithLabel(out string rightLabel);
            bool result = left && right;

            label = $"({leftLabel}∧{rightLabel})";
            AddSubexpression(label, result);

            left = result;
            leftLabel = label;
        }

        label = leftLabel;
        return left;
    }

    private bool ParseNotWithLabel(out string label)
    {
        if (Match("!"))
        {
            bool val = ParseNotWithLabel(out string innerLabel);
            label = $"¬{innerLabel}";
            bool result = !val;
            AddSubexpression(label, result);
            return result;
        }
        else
        {
            return ParsePrimaryWithLabel(out label);
        }
    }

    private bool ParsePrimaryWithLabel(out string label)
    {
        if (Match("("))
        {
            bool expr = ParseExpressionWithLabel(out label);
            Expect(")");
            label = $"({label})";
            return expr;
        }
        else
        {
            string varName = ConsumeVariable();
            if (!currentAssignment.ContainsKey(varName))
                throw new Exception($"Unknown variable '{varName}'");
            label = varName;
            return currentAssignment[varName];
        }
    }

    private void AddSubexpression(string label, bool value)
    {
        if (!variables.Contains(label) && !string.IsNullOrEmpty(label))
        {
            if (!subexpressionLabels.Contains(label))
                subexpressionLabels.Add(label);

            int index = lastSubexpressionResults.FindIndex(x => x.label == label);
            if (index >= 0)
                lastSubexpressionResults[index] = (label, value);
            else
                lastSubexpressionResults.Add((label, value));

            Debug.Log($"Adding subexpression: {label} = {value}");
        }
    }

    private bool Match(string expected)
    {
        if (position < tokens.Count && tokens[position] == expected)
        {
            position++;
            return true;
        }
        return false;
    }

    private string ConsumeVariable()
    {
        if (position < tokens.Count)
        {
            string var = tokens[position];
            position++;
            return var;
        }
        throw new Exception("Unexpected end of formula while expecting variable");
    }

    private void Expect(string expected)
    {
        if (!Match(expected))
        {
            throw new Exception($"Expected '{expected}' but found '{(position < tokens.Count ? tokens[position] : "end of input")}'");
        }
    }
}
