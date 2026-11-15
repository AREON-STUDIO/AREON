// AREON - Stage 7
// Path: Assets/AREON/Scripts/Core/ActionResult.cs
//
// A small, immutable "receipt" returned by every Power action (science download,
// shield reinforcement). Station scripts and UI only need to read this struct:
// they never inspect Power or Shield themselves.

public enum ActionOutcome
{
    Success,
    InsufficientPower,
    AlreadyDownloaded,
    ShieldFull,
    NotConfigured,
    InternalError
}

public readonly struct ActionResult
{
    public readonly ActionOutcome Outcome;
    public readonly string Message;

    /// <summary>Power actually removed from ResourceManager by this action (0 when rejected).</summary>
    public readonly float PowerSpent;

    /// <summary>Gameplay amount applied (e.g. Shield points actually restored). 0 when rejected.</summary>
    public readonly float AmountApplied;

    public bool Succeeded => Outcome == ActionOutcome.Success;

    private ActionResult(ActionOutcome outcome, string message, float powerSpent, float amountApplied)
    {
        Outcome = outcome;
        Message = message;
        PowerSpent = powerSpent;
        AmountApplied = amountApplied;
    }

    public static ActionResult Success(string message, float powerSpent, float amountApplied = 0f)
    {
        return new ActionResult(ActionOutcome.Success, message, powerSpent, amountApplied);
    }

    public static ActionResult Failure(ActionOutcome outcome, string message)
    {
        return new ActionResult(outcome, message, 0f, 0f);
    }
}
