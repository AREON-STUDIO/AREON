namespace AREON.Interaction
{
    /// <summary>
    /// The shared contract for anything the player can use (facilities, terminals, consoles...).
    /// Implement this on a MonoBehaviour that sits on, or above, the object's Collider.
    /// PlayerInteractor finds it with GetComponentInParent&lt;IInteractable&gt;().
    ///
    /// Rules for implementers:
    ///  - Do NOT show your own prompt. PlayerInteractor + InteractionPromptUI show exactly one.
    ///  - Do NOT read input. PlayerInteractor calls Interact() when the interact action fires.
    ///  - GetPrompt / CanInteract are called every frame while targeted: keep them cheap.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Short action text without the key, e.g. "Download Science Data".</summary>
        string GetPrompt(PlayerInteractor interactor);

        /// <summary>
        /// True if the object can be used right now.
        /// When false, set unavailableReason to a short player-facing message (may be empty).
        /// </summary>
        bool CanInteract(PlayerInteractor interactor, out string unavailableReason);

        /// <summary>
        /// Perform the interaction. Only called after CanInteract returned true.
        /// </summary>
        void Interact(PlayerInteractor interactor);
    }
}
