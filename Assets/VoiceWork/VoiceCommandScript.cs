using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Meta.WitAi;
using Meta.WitAi.Json;
using Oculus.Voice;
using UnityEngine;

public class VoiceCommandScript : MonoBehaviour
{
    [SerializeField] private AppVoiceExperience wit;
    [SerializeField] private FormulaInputManager fim;

    private bool keepListening = false;

    public void ToggleListening()
    {
        if (keepListening)
        {
            keepListening = false;
            wit.Deactivate();
            Debug.Log("Voice Recognition Disabled");
        }
        else
        {
            keepListening = true;
            wit.Activate();
            Debug.Log("Voice Recognition Enabled");
        }
    }

    private void OnEnable()
    {
        if (wit != null)
        {
            wit.VoiceEvents.OnResponse.RemoveListener(HandleWitResponse);
            wit.VoiceEvents.OnResponse.AddListener(HandleWitResponse);

            // Optional: stop listening event if you want to handle that
            // wit.VoiceEvents.OnStoppedListening.RemoveListener(OnStoppedListening);
            // wit.VoiceEvents.OnStoppedListening.AddListener(OnStoppedListening);
        }
    }

    private void OnDisable()
    {
        if (wit != null)
        {
            wit.VoiceEvents.OnResponse.RemoveListener(HandleWitResponse);
            // wit.VoiceEvents.OnStoppedListening.RemoveListener(OnStoppedListening);
        }
    }

    private void HandleWitResponse(WitResponseNode response)
    {
        Debug.Log("Responding");
        var firstIntent = response.GetFirstIntent();
        if (firstIntent == null)
        {
            Debug.LogWarning("No intent found.");
        }
        else
        {
            string intentName = firstIntent["name"];
            Debug.Log("Detected Intent: " + intentName);

            string methodName = intentName.Replace("_", "");

            MethodInfo method = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (method != null)
            {
                method.Invoke(this, null);
            }
            else
            {
                Debug.LogWarning($"No method found matching intent: {intentName}");
            }
        }

        // Reactivate listening immediately after processing the command, if toggled on
        if (keepListening)
        {
            Debug.Log("Reactivating listening...");
            wit.Activate();
        }
    }

    // Your logic methods here
    private void AddAnd() => fim.AddToFormula("&&");
    private void AddOr() => fim.AddToFormula("||");
    private void AddNot() => fim.AddToFormula("!");
    private void AddImplication() => fim.AddToFormula("->");
    private void AddEquivalence() => fim.AddToFormula("<->");
    private void AddXor() => fim.AddToFormula("xor");
    private void AddOpenParentheses() => fim.AddToFormula("(");
    private void AddClosedParentheses() => fim.AddToFormula(")");
    private void AddP() => fim.AddToFormula("P");
    private void AddQ() => fim.AddToFormula("Q");
    private void AddR() => fim.AddToFormula("R");
    private void AddS() => fim.AddToFormula("S");
    private void SolveFormula() => fim.SolveFormula();
    private void ClearFormula() => fim.DeleteAllInput();
    private void DeleteFormula() => fim.DeleteLastInput();
}
